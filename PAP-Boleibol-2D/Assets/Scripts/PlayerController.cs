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
    public Transform leftBoundary;

    [Header("Sistema de Voleibol")]
    public int playerTouchCount = 0;
    public float hitRange = 2.1f;
    public float inputTouchCooldown = 0.1f;
    public float receiveForce = 10.5f;
    public float setForce = 12.5f;
    public float sendForce = 15f;
    public float spikeForce = 22f;
    public float blockForce = 15.5f;
    public float blockDuration = 0.22f;
    public float blockCooldown = 0.45f;
    public float blockRange = 1.15f;
    public float blockHeightOffset = 1.25f;
    public float blockForwardOffset = 0.5f;
    public float blockNetReach = 1.85f;

    [Header("Limites e Servico")]
    public float serveBackOffset = 0.08f;
    public float serveBoundaryPadding = 0.02f;
    public float limiteEsquerdoCampo = -6.5f;

    [Header("Referencias")]
    public BallController ballScript;
    public Transform ballHoldPoint;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private Animator anim;
    private bool isGrounded;
    private bool isBlocking;
    private float lastHitInputTime = -999f;
    private float blockEndTime = -999f;
    private float nextAllowedBlockTime = -999f;
    private Vector3 baseScale;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        baseScale = transform.localScale;
        rb.gravityScale = 4f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        AutoDetectReferences();
        IgnoreBoundaryCollision();
        EnsureGroundCheck();
        EnsureBallLayer();

        if (isServing)
        {
            MoveToServePosition();

            if (ballScript != null && ballHoldPoint != null)
                ballScript.SetToServing(ballHoldPoint);
        }
    }

    void Update()
    {
        AutoDetectReferences();
        IgnoreBoundaryCollision();
        EnsureGroundCheck();
        EnsureBallLayer();

        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        UpdateBlockingState();

        if (anim != null)
            anim.SetBool("isGrounded", isGrounded);

        if (ballScript != null)
            playerTouchCount = ballScript.GetTouchCountFor(BallController.TeamSide.Player);

        HandleMovement();

        if (isBlocking)
            TryBlockBall();

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

        if (isServing)
            moveInput = ApplyServeMovementRestriction(moveInput);

        if (isBlocking)
            moveInput = Mathf.Max(0f, moveInput);

        rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);
        ClampServePosition();

        if (isBlocking)
            OrientarPara(1f);
        else if (moveInput > 0f)
            transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        else if (moveInput < 0f)
            transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);

        if (anim != null)
        {
            float horizontalSpeed = Mathf.Abs(moveInput);
            anim.SetBool("isRunning", horizontalSpeed > 0.01f);
            anim.SetFloat("Speed", horizontalSpeed);
        }
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

        if (Input.GetKeyDown(KeyCode.Q))
            TryStartBlock();
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
            ballScript.ReleaseServe(BallController.TeamSide.Player, new Vector2(0.95f, 1.1f), serveForce);

        if (anim != null)
            anim.SetTrigger("serveAction");
    }

    public void PrepareServe(BallController targetBall)
    {
        if (targetBall != null)
            ballScript = targetBall;

        AutoDetectReferences();
        CancelBlock();
        isServing = true;
        playerTouchCount = 0;
        lastHitInputTime = -999f;
        MoveToServePosition();

        if (ballScript != null && ballHoldPoint != null)
            ballScript.SetToServing(ballHoldPoint);
    }

    public void StopServing()
    {
        CancelBlock();
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

    Vector2 GetBlockCenter()
    {
        return (Vector2)transform.position + new Vector2(blockForwardOffset, blockHeightOffset);
    }

    void AutoDetectReferences()
    {
        if (ballScript == null)
            ballScript = FindObjectOfType<BallController>();

        if (!CourtReferences.IsPlayableNetPosition(netPosition))
            netPosition = CourtReferences.FindNetPosition();

        if (leftBoundary == null)
            leftBoundary = CourtReferences.FindBoundary("limit L");
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

    void UpdateBlockingState()
    {
        if (isBlocking && Time.time >= blockEndTime)
            isBlocking = false;
    }

    void TryStartBlock()
    {
        if (isServing || Time.time < nextAllowedBlockTime)
            return;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        isBlocking = true;
        blockEndTime = Time.time + blockDuration;
        nextAllowedBlockTime = Time.time + blockCooldown;
        OrientarPara(1f);

        float appliedBlockJump = Mathf.Max(jumpForce * 0.75f, 11f);
        if (isGrounded)
        {
            isGrounded = false;
            rb.linearVelocity = new Vector2(Mathf.Max(0f, rb.linearVelocity.x), appliedBlockJump);
        }
        else
        {
            rb.linearVelocity = new Vector2(Mathf.Max(0f, rb.linearVelocity.x), Mathf.Max(rb.linearVelocity.y, appliedBlockJump * 0.55f));
        }

        if (anim != null)
            anim.SetTrigger("block");

        TryBlockBall();
    }

    void TryBlockBall()
    {
        if (!isBlocking || ballScript == null || ballScript.IsBeingHeld)
            return;

        if (!CanAttemptBlock(ballScript))
            return;

        if (!ballScript.TryApplyBlock(BallController.TeamSide.Player, new Vector2(1.05f, 1.15f), blockForce))
            return;

        playerTouchCount = ballScript.GetTouchCountFor(BallController.TeamSide.Player);
        lastHitInputTime = Time.time;
    }

    bool CanAttemptBlock(BallController ball)
    {
        if (ball == null)
            return false;

        if (ball.LastTeamToTouch != BallController.TeamSide.Bot)
            return false;

        if (ball.Velocity.x >= -0.05f)
            return false;

        if (netPosition != null && transform.position.x < netPosition.position.x - blockNetReach)
            return false;

        Vector2 blockCenter = GetBlockCenter();
        Vector2 ballPosition = ball.transform.position;

        if (ballPosition.x < transform.position.x - 0.1f)
            return false;

        return Vector2.Distance(blockCenter, ballPosition) <= blockRange;
    }

    void CancelBlock()
    {
        isBlocking = false;
        blockEndTime = -999f;
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

    void MoveToServePosition()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        float serveX = GetServePositionX();
        transform.position = new Vector3(serveX, transform.position.y, transform.position.z);
        rb.linearVelocity = Vector2.zero;
        OrientarPara(1f);
    }

    float GetServePositionX()
    {
        return GetServeMovementLimitX() - serveBackOffset;
    }

    float GetServeBoundaryX()
    {
        if (CourtReferences.TryGetBoundaryInnerX(leftBoundary, true, out float leftLimit))
            return leftLimit - serveBoundaryPadding;

        return limiteEsquerdoCampo;
    }

    float ApplyServeMovementRestriction(float moveInput)
    {
        float serveMovementLimitX = GetServeMovementLimitX();

        if (moveInput > 0f && transform.position.x >= serveMovementLimitX)
            return 0f;

        return moveInput;
    }

    void ClampServePosition()
    {
        if (!isServing)
            return;

        float serveMovementLimitX = GetServeMovementLimitX();
        if (transform.position.x > serveMovementLimitX)
        {
            transform.position = new Vector3(serveMovementLimitX, transform.position.y, transform.position.z);
            rb.linearVelocity = new Vector2(Mathf.Min(0f, rb.linearVelocity.x), rb.linearVelocity.y);
        }
    }

    float GetServeMovementLimitX()
    {
        return GetServeBoundaryX() - GetColliderHalfWidth();
    }

    float GetColliderHalfWidth()
    {
        if (playerCollider == null)
            playerCollider = GetComponent<Collider2D>();

        return playerCollider != null ? playerCollider.bounds.extents.x : 0f;
    }

    void IgnoreBoundaryCollision()
    {
        if (playerCollider == null)
            playerCollider = GetComponent<Collider2D>();

        if (playerCollider == null)
            return;

        IgnoreCollisionWithBoundary(leftBoundary);
    }

    void IgnoreCollisionWithBoundary(Transform boundary)
    {
        if (boundary == null)
            return;

        Collider2D boundaryCollider = boundary.GetComponent<Collider2D>();
        if (boundaryCollider != null)
            Physics2D.IgnoreCollision(playerCollider, boundaryCollider, true);
    }

    void OrientarPara(float direction)
    {
        if (direction > 0f)
            transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        else if (direction < 0f)
            transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
    }
}
