using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerController player;
    public Transform ball;
    public Transform netPosition;
    public Transform rightBoundary;

    [Header("Movimento")]
    public float speed = 5f;
    public float jumpForce = 11f;
    public float groundCheckYOffset = -0.85f;
    public float recuoDefesa = 3f;
    public float zonaLevantamento = 1.95f;
    public float zonaAtaque = 1.2f;
    public float limiteDireitaCampo = 15.5f;
    public float rightBoundaryPadding = 0.8f;

    [Header("Servico")]
    public float serveForce = 18.5f;
    public float serveDelay = 0.85f;
    public float serveBackOffset = 0.55f;
    public float serveRecoveryTime = 0.45f;

    [Header("Sistema de Toques")]
    public int botTouchCount = 0;
    public float touchCooldown = 0.1f;
    public float hitRange = 2.15f;
    public float receiveForce = 8.75f;
    public float setForce = 11.75f;
    public float sendForce = 12.75f;
    public float spikeForce = 18.5f;
    public LayerMask groundLayer;
    public Transform groundCheck;

    private Rigidbody2D rb;
    private BallController ballScript;
    private bool isGrounded;
    private bool isServing;
    private float nextAllowedBallTouchTime = -999f;
    private float serveStartTime = -999f;
    private Transform serveHoldPoint;
    private Vector3 baseScale;

    public bool IsServing => isServing;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        AutoDetectReferences();
        EnsureGroundCheck();
        EnsureServeHoldPoint();
    }

    void Update()
    {
        AutoDetectReferences();
        EnsureGroundCheck();
        EnsureServeHoldPoint();

        if (ball == null || netPosition == null)
            return;

        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.3f, groundLayer);

        if (ballScript != null)
            botTouchCount = ballScript.GetTouchCountFor(BallController.TeamSide.Bot);

        if (isServing)
        {
            HandleServe();
            return;
        }

        if (player != null && player.isServing && ballScript != null && ballScript.IsBeingHeld)
        {
            MoverPara(netPosition.position.x + recuoDefesa);
            return;
        }

        if (ball.position.x < netPosition.position.x - 0.05f)
        {
            botTouchCount = 0;
            MoverPara(netPosition.position.x + recuoDefesa);
            return;
        }

        MoverPara(CalcularPosicaoAlvo());

        if (botTouchCount == 2 && isGrounded && DeveSaltarParaAtacar())
        {
            isGrounded = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        TentarTocarBola();
    }

    void HandleServe()
    {
        OrientarPara(-1f);

        if (ballScript == null || !ballScript.IsBeingHeld)
        {
            isServing = false;
            serveStartTime = -999f;
            return;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (!isGrounded)
            return;

        if (serveStartTime < 0f)
            serveStartTime = Time.time;

        if (Time.time < serveStartTime + serveDelay)
            return;

        isServing = false;
        serveStartTime = -999f;
        botTouchCount = 0;
        nextAllowedBallTouchTime = Time.time + serveRecoveryTime;
        ballScript.ReleaseServe(new Vector2(-0.95f, 1.1f), serveForce);
    }

    void MoverPara(float targetX)
    {
        float distancia = targetX - transform.position.x;

        if (Mathf.Abs(distancia) > 0.15f)
        {
            OrientarPara(Mathf.Sign(distancia));
            rb.linearVelocity = new Vector2(Mathf.Sign(distancia) * speed, rb.linearVelocity.y);
        }
        else
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    float CalcularPosicaoAlvo()
    {
        float minX = netPosition.position.x + 0.8f;
        float maxX = Mathf.Max(minX, GetPlayableRightLimitX());

        if (ballScript == null)
            return Mathf.Clamp(netPosition.position.x + recuoDefesa, minX, maxX);

        int currentTouches = ballScript.GetTouchCountFor(BallController.TeamSide.Bot);

        if (currentTouches == 0)
            return Mathf.Clamp(ball.position.x + 0.15f, minX, maxX);

        if (currentTouches == 1)
            return Mathf.Clamp(netPosition.position.x + zonaLevantamento, minX, maxX);

        return Mathf.Clamp(netPosition.position.x + zonaAtaque, minX, maxX);
    }

    bool DeveSaltarParaAtacar()
    {
        if (ballScript == null)
            return false;

        float distancia = Mathf.Abs(ball.position.x - transform.position.x);
        bool bolaAlta = ball.position.y > transform.position.y + 1f;
        bool bolaADescer = ballScript.Velocity.y <= 0.5f;
        return distancia < 1.25f && bolaAlta && bolaADescer;
    }

    void TentarTocarBola()
    {
        if (ballScript == null || Time.time < nextAllowedBallTouchTime)
            return;

        if (Vector2.Distance(GetHitCenter(), (Vector2)ball.position) > hitRange)
            return;

        int currentTouches = ballScript.GetTouchCountFor(BallController.TeamSide.Bot);
        bool spikeInput = !isGrounded && currentTouches == 2;

        if (spikeInput)
        {
            if (!ballScript.TryRegisterTouch(BallController.TeamSide.Bot, out int touchNumber) || touchNumber != 3)
                return;

            ExecutarToque(touchNumber, true);
        }
        else
        {
            if (!ballScript.TryRegisterTouch(BallController.TeamSide.Bot, out int touchNumber))
                return;

            ExecutarToque(touchNumber, false);
        }

        botTouchCount = ballScript.GetTouchCountFor(BallController.TeamSide.Bot);
        nextAllowedBallTouchTime = Time.time + touchCooldown;
    }

    void ExecutarToque(int touchNumber, bool spikeInput)
    {
        if (touchNumber == 1)
        {
            ballScript.ApplyForce(new Vector2(-0.18f, 1.45f), receiveForce);
        }
        else if (touchNumber == 2)
        {
            // Levantamento mais alto para dar tempo ao jogador.
            ballScript.ApplyForce(new Vector2(-0.05f, 2.55f), setForce);
        }
        else if (spikeInput)
        {
            ballScript.ApplyForce(new Vector2(-1.3f, -0.55f), spikeForce);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Min(rb.linearVelocity.y, -1.5f));
        }
        else
        {
            ballScript.ApplyForce(new Vector2(-0.95f, 0.95f), sendForce);
        }
    }

    Vector2 GetHitCenter()
    {
        float yOffset = isGrounded ? 0.8f : 1.1f;
        return (Vector2)transform.position + new Vector2(-0.35f, yOffset);
    }

    public void PrepareServe(BallController targetBall)
    {
        if (targetBall != null)
            ballScript = targetBall;

        if (ballScript != null)
            ball = ballScript.transform;

        AutoDetectReferences();
        EnsureServeHoldPoint();

        float serveX = GetServePositionX();
        transform.position = new Vector3(serveX, transform.position.y, transform.position.z);
        rb.linearVelocity = Vector2.zero;
        OrientarPara(-1f);

        isServing = true;
        botTouchCount = 0;
        nextAllowedBallTouchTime = -999f;
        serveStartTime = -999f;

        if (ballScript != null && serveHoldPoint != null)
            ballScript.SetToServing(serveHoldPoint);
    }

    public void StopServing()
    {
        isServing = false;
        botTouchCount = 0;
        nextAllowedBallTouchTime = -999f;
        serveStartTime = -999f;
    }

    void EnsureServeHoldPoint()
    {
        if (serveHoldPoint == null)
        {
            Transform existingPoint = transform.Find("BotServePoint");
            if (existingPoint != null)
            {
                serveHoldPoint = existingPoint;
            }
            else
            {
                GameObject holdObject = new GameObject("BotServePoint");
                holdObject.transform.SetParent(transform);
                serveHoldPoint = holdObject.transform;
            }
        }

        serveHoldPoint.localPosition = new Vector3(0.45f, 0.95f, 0f);
        serveHoldPoint.localRotation = Quaternion.identity;
        serveHoldPoint.localScale = Vector3.one;
    }

    float GetServePositionX()
    {
        float minX = netPosition != null ? netPosition.position.x + 0.8f : transform.position.x;
        float maxX = Mathf.Max(minX, GetPlayableRightLimitX());
        return Mathf.Clamp(maxX - serveBackOffset, minX, maxX);
    }

    float GetPlayableRightLimitX()
    {
        if (CourtReferences.TryGetBoundaryInnerX(rightBoundary, false, out float rightLimit))
            return rightLimit - rightBoundaryPadding;

        return limiteDireitaCampo;
    }

    void OrientarPara(float direction)
    {
        if (direction > 0f)
            transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        else if (direction < 0f)
            transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
    }

    void EnsureGroundCheck()
    {
        if (groundCheck != null && groundCheck.parent == transform)
            return;

        Transform existingCheck = transform.Find("BotGroundCheck");
        if (existingCheck == null)
        {
            GameObject checkObject = new GameObject("BotGroundCheck");
            existingCheck = checkObject.transform;
            existingCheck.SetParent(transform);
            existingCheck.localRotation = Quaternion.identity;
            existingCheck.localScale = Vector3.one;
        }

        existingCheck.localPosition = new Vector3(0f, groundCheckYOffset, 0f);
        groundCheck = existingCheck;
    }

    void AutoDetectReferences()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        if (ballScript == null && ball != null)
            ballScript = ball.GetComponent<BallController>();

        if (ball == null && ballScript != null)
            ball = ballScript.transform;

        if (!CourtReferences.IsPlayableNetPosition(netPosition))
            netPosition = CourtReferences.FindNetPosition();

        if (rightBoundary == null)
            rightBoundary = CourtReferences.FindBoundary("limit R");
    }
}
