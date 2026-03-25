using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movimentacao")]
    public float speed = 6f;
    public float jumpForce = 14f;
    public bool isServing = true;

    [Header("Servico")]
    public float serveForce = 18f;

    [Header("Deteccao e Fisica")]
    public Transform groundCheck;
    public float checkRadius = 0.4f;
    public float groundCheckYOffset = -0.85f;
    public LayerMask groundLayer;
    public LayerMask ballLayer;
    public Transform netPosition;

    [Header("Sistema de Voleibol")]
    public int playerTouchCount = 0;
    public float hitRange = 2.1f;
    public float inputTouchCooldown = 0.1f;
    public float receiveForce = 10.5f;
    public float setForce = 12.5f;
    public float sendForce = 15f;
    public float spikeForce = 22f;

    [Header("Referencias")]
    public BallController ballScript;
    public Transform ballHoldPoint;

    private Rigidbody2D rb;
    private Animator anim;
    private bool isGrounded;
    private float lastHitInputTime = -999f;
    private Vector3 baseScale;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        baseScale = transform.localScale;
        rb.gravityScale = 4f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        AutoDetectReferences();
        EnsureGroundCheck();
        EnsureBallLayer();

        if (isServing && ballScript != null && ballHoldPoint != null)
            ballScript.SetToServing(ballHoldPoint);
    }

    void Update()
    {
        AutoDetectReferences();
        EnsureGroundCheck();
        EnsureBallLayer();

        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        if (anim != null)
            anim.SetBool("isGrounded", isGrounded);

        if (ballScript != null)
            playerTouchCount = ballScript.GetTouchCountFor(BallController.TeamSide.Player);

        HandleMovement();

        if (isServing)
        {
            if (Input.GetKeyDown(KeyCode.E))
                PerformServe();

            return;
        }

        HandleActions();
    }

    void HandleMovement()
    {
        float moveInput = 0f;

        if (Input.GetKey(KeyCode.A))
            moveInput -= 1f;

        if (Input.GetKey(KeyCode.D))
            moveInput += 1f;

        rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);

        if (moveInput > 0f)
            transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        else if (moveInput < 0f)
            transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);

        if (anim != null)
            anim.SetBool("isRunning", Mathf.Abs(moveInput) > 0.01f);
    }

    void HandleActions()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded)
                Jump();
            else
                TryHitBall(true);
        }

        if (Input.GetKeyDown(KeyCode.E))
            TryHitBall(false);
    }

    void Jump()
    {
        isGrounded = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        if (anim != null)
            anim.SetTrigger("jump");
    }

    void TryHitBall(bool spikeInput)
    {
        if (ballScript == null || Time.time < lastHitInputTime + inputTouchCooldown)
            return;

        BallController ball = GetTargetBallInRange();
        if (ball == null || ball.IsBeingHeld)
            return;

        if (!BolaNoLadoDoPlayer(ball.transform.position.x))
            return;

        int currentTouches = ball.GetTouchCountFor(BallController.TeamSide.Player);

        if (spikeInput)
        {
            if (isGrounded || currentTouches != 2)
                return;

            if (!ball.TryRegisterTouch(BallController.TeamSide.Player, out int touchNumber) || touchNumber != 3)
                return;

            ExecutarToque(ball, touchNumber, true);
        }
        else
        {
            if (!ball.TryRegisterTouch(BallController.TeamSide.Player, out int touchNumber))
                return;

            ExecutarToque(ball, touchNumber, false);
        }

        playerTouchCount = ball.GetTouchCountFor(BallController.TeamSide.Player);
        lastHitInputTime = Time.time;
    }

    void ExecutarToque(BallController ball, int touchNumber, bool spikeInput)
    {
        if (touchNumber == 1)
        {
            ball.ApplyForce(new Vector2(0.08f, 1.75f), receiveForce);

            if (anim != null)
                anim.SetTrigger("manchete");
        }
        else if (touchNumber == 2)
        {
            ball.ApplyForce(new Vector2(0.2f, 1.7f), setForce);

            if (anim != null)
                anim.SetTrigger("manchete");
        }
        else if (spikeInput)
        {
            ball.ApplyForce(new Vector2(1.55f, -0.72f), spikeForce);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Min(rb.linearVelocity.y, -1.5f));

            if (anim != null)
                anim.SetTrigger("smash");
        }
        else
        {
            ball.ApplyForce(new Vector2(1.15f, 0.85f), sendForce);

            if (anim != null)
                anim.SetTrigger("manchete");
        }
    }

    void PerformServe()
    {
        isServing = false;
        playerTouchCount = 0;

        if (ballScript != null)
            ballScript.ReleaseServe(new Vector2(0.95f, 1.1f), serveForce);

        if (anim != null)
            anim.SetTrigger("serveAction");
    }

    public void PrepareServe(BallController targetBall)
    {
        if (targetBall != null)
            ballScript = targetBall;

        AutoDetectReferences();
        isServing = true;
        playerTouchCount = 0;
        lastHitInputTime = -999f;

        if (ballScript != null && ballHoldPoint != null)
            ballScript.SetToServing(ballHoldPoint);
    }

    public void StopServing()
    {
        isServing = false;
        playerTouchCount = 0;
        lastHitInputTime = -999f;
    }

    bool BolaNoLadoDoPlayer(float ballX)
    {
        return netPosition == null || ballX <= netPosition.position.x + 0.45f;
    }

    Vector2 GetHitCenter()
    {
        float yOffset = isGrounded ? 0.75f : 1.1f;
        return (Vector2)transform.position + new Vector2(0.35f, yOffset);
    }

    void AutoDetectReferences()
    {
        if (ballScript == null)
            ballScript = FindObjectOfType<BallController>();

        if (netPosition != null)
            return;

        GameObject netObject = GameObject.Find("netcheck");
        if (netObject == null)
            netObject = GameObject.Find("net");

        if (netObject != null)
            netPosition = netObject.transform;
    }

    BallController GetTargetBallInRange()
    {
        Vector2 hitCenter = GetHitCenter();

        if (ballLayer.value != 0)
        {
            Collider2D detectedCollider = Physics2D.OverlapCircle(hitCenter, hitRange, ballLayer);
            if (detectedCollider != null)
            {
                BallController detectedBall = detectedCollider.GetComponent<BallController>();
                if (detectedBall != null)
                    return detectedBall;
            }
        }

        if (ballScript == null)
            return null;

        return Vector2.Distance(hitCenter, ballScript.transform.position) <= hitRange
            ? ballScript
            : null;
    }

    void EnsureBallLayer()
    {
        if (ballLayer.value == 0 && ballScript != null)
            ballLayer = 1 << ballScript.gameObject.layer;
    }

    void EnsureGroundCheck()
    {
        if (groundCheck != null && groundCheck.parent == transform)
            return;

        Transform existingCheck = transform.Find("PlayerGroundCheck");
        if (existingCheck == null)
        {
            GameObject checkObject = new GameObject("PlayerGroundCheck");
            existingCheck = checkObject.transform;
            existingCheck.SetParent(transform);
            existingCheck.localRotation = Quaternion.identity;
            existingCheck.localScale = Vector3.one;
        }

        existingCheck.localPosition = new Vector3(0f, groundCheckYOffset, 0f);
        groundCheck = existingCheck;
    }
}
