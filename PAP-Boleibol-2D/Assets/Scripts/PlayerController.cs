using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movimentacao")]
    public float speed = 6f;
    public float jumpForce = 14f;
    public bool isServing = true;

    [Header("Servico")]
    public float serveForce = 18f;
    public float safeServeForce = 15.5f;
    public float faultServeForce = 19.5f;
    public float serveMeterSpeed = 1.7f;
    public float serveMeterYOffset = 0.25f;
    [Range(0.05f, 0.35f)] public float serveFaultZoneWidth = 0.18f;
    [Range(0.05f, 0.4f)] public float serveGreenZoneWidth = 0.22f;

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
    public float blockMinJumpForce = 4.5f;
    public float blockJumpReductionFromNormal = 5f;
    public float blockChargeDuration = 0.18f;
    public float blockNearNetTolerance = 0.18f;
    public float blockPassThroughChance = 0.22f;
    public float blockPopUpChance = 0.43f;
    public float blockPopUpHorizontalPush = 0.42f;
    public float blockPopUpVerticalPush = 1.7f;
    public float blockFlatHorizontalPush = 1.35f;
    public float blockFlatVerticalPush = 0.24f;
    public float perfectBlockVelocityWindow = 0.65f;
    public float perfectBlockHeightTolerance = 0.28f;
    public float perfectBlockForceMultiplier = 1.08f;
    public float perfectBlockHorizontalPush = 0.22f;
    public float perfectBlockDownwardPush = 1.75f;

    [Header("Limites e Servico")]
    public float serveBackOffset = 0.08f;
    public float serveBoundaryPadding = 0.02f;
    public float netBarrierPadding = 0.08f;
    public float limiteEsquerdoCampo = -6.5f;

    [Header("Referencias")]
    public BallController ballScript;
    public Transform ballHoldPoint;

    private Rigidbody2D rb;
    private Collider2D playerCollider;
    private Animator anim;
    private bool isGrounded;
    private bool isBlocking;
    private bool isChargingBlock;
    private bool blockInteractionResolved;
    private float lastHitInputTime = -999f;
    private float blockEndTime = -999f;
    private float blockChargeStartTime = -999f;
    private float nextAllowedBlockTime = -999f;
    private Vector3 baseScale;
    private ServeChargeUI serveChargeUI;
    private bool isChargingServe;
    private float serveChargeNormalized = 0.5f;
    private float serveChargeDirection = 1f;

    enum ServeChargeResult
    {
        Fault,
        Safe,
        Power
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        baseScale = transform.localScale;
        rb.gravityScale = 4f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        serveChargeUI = ServeChargeUI.EnsureInstance();

        AutoDetectReferences();
        IgnoreBoundaryCollision();
        EnsureGroundCheck();
        EnsureBallLayer();
        ResetServeCharge();
        if (isServing)
        {
            MoveToServePosition();

            if (ballScript != null && ballHoldPoint != null)
                ballScript.SetToServing(ballHoldPoint);
        }

        UpdateServeChargeUI();
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
        UpdateServeChargeUI();

        if (isBlocking)
            TryBlockBall();

        if (isServing)
        {
            HandleServeInput();
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

        if (isBlocking || isChargingBlock || isChargingServe)
            moveInput = 0f;

        rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);
        ClampServePosition();
        ClampToPlayerSideOfNet();

        if (isBlocking || isChargingBlock)
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

        UpdateBlockInput();
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

    void HandleServeInput()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isChargingServe)
            StartServeCharge();

        if (!isChargingServe)
            return;

        UpdateServeCharge();

        if (Input.GetKeyUp(KeyCode.E))
            ReleaseChargedServe();
    }

    void StartServeCharge()
    {
        if (!isServing)
            return;

        ResetServeCharge();
        isChargingServe = true;
        UpdateServeChargeUI();
    }

    void UpdateServeCharge()
    {
        serveChargeNormalized += serveChargeDirection * serveMeterSpeed * Time.deltaTime;

        if (serveChargeNormalized >= 1f)
        {
            serveChargeNormalized = 1f;
            serveChargeDirection = -1f;
        }
        else if (serveChargeNormalized <= 0f)
        {
            serveChargeNormalized = 0f;
            serveChargeDirection = 1f;
        }
    }

    void ReleaseChargedServe()
    {
        if (!isChargingServe)
            return;

        isChargingServe = false;
        PerformServe(EvaluateServeCharge(serveChargeNormalized));
    }

    void PerformServe(ServeChargeResult serveResult)
    {
        isServing = false;
        isChargingServe = false;
        playerTouchCount = 0;

        if (ballScript != null)
            ballScript.ReleaseServe(BallController.TeamSide.Player, GetServeDirection(serveResult), GetServeForce(serveResult));

        if (anim != null)
            anim.SetTrigger("serveAction");

        ResetServeCharge();
        UpdateServeChargeUI(true);
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
        ResetServeCharge();
        MoveToServePosition();

        if (ballScript != null && ballHoldPoint != null)
            ballScript.SetToServing(ballHoldPoint);

        UpdateServeChargeUI();
    }

    public void StopServing()
    {
        CancelBlock();
        isServing = false;
        playerTouchCount = 0;
        lastHitInputTime = -999f;
        ResetServeCharge();
        UpdateServeChargeUI(true);
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
            CancelBlock();
    }

    void UpdateBlockInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            TryStartBlock();

        if (isChargingBlock && Input.GetKeyUp(KeyCode.Q))
            ReleaseChargedBlock();
    }

    void TryStartBlock()
    {
        if (isServing || isBlocking || isChargingBlock || Time.time < nextAllowedBlockTime)
            return;

        if (!isGrounded || !IsCloseEnoughToNetForBlock())
            return;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        isChargingBlock = true;
        blockInteractionResolved = false;
        blockChargeStartTime = Time.time;
        OrientarPara(1f);
        rb.linearVelocity = new Vector2(0f, Mathf.Min(rb.linearVelocity.y, 0f));
    }

    void ReleaseChargedBlock()
    {
        if (!isChargingBlock)
            return;

        float chargeTime = Mathf.Clamp(Time.time - blockChargeStartTime, 0f, blockChargeDuration);
        isChargingBlock = false;
        blockChargeStartTime = -999f;

        if (isServing || !isGrounded || !IsCloseEnoughToNetForBlock())
            return;

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            return;

        float normalizedCharge = blockChargeDuration <= 0.001f ? 1f : chargeTime / blockChargeDuration;

        isBlocking = true;
        blockEndTime = Time.time + blockDuration;
        nextAllowedBlockTime = Time.time + blockCooldown;
        isGrounded = false;
        rb.linearVelocity = new Vector2(0f, GetBlockJumpVelocity(normalizedCharge));

        if (anim != null)
            anim.SetTrigger("block");

        TryBlockBall();
    }

    void TryBlockBall()
    {
        if (!isBlocking || blockInteractionResolved || ballScript == null || ballScript.IsBeingHeld)
            return;

        if (!CanAttemptBlock(ballScript))
            return;

        if (!GetBlockResponse(ballScript, out Vector2 blockDirection, out float appliedBlockForce))
        {
            blockInteractionResolved = true;
            return;
        }

        if (!ballScript.TryApplyBlock(BallController.TeamSide.Player, blockDirection, appliedBlockForce))
            return;

        blockInteractionResolved = true;
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

        if (!IsCloseEnoughToNetForBlock())
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
        isChargingBlock = false;
        blockInteractionResolved = false;
        blockChargeStartTime = -999f;
    }

    float GetBlockJumpVelocity(float normalizedCharge)
    {
        float minVelocity = Mathf.Max(blockMinJumpForce, 3f);
        float maxVelocity = Mathf.Max(minVelocity, jumpForce - blockJumpReductionFromNormal);
        return Mathf.Lerp(minVelocity, maxVelocity, Mathf.Clamp01(normalizedCharge));
    }

    bool GetBlockResponse(BallController ball, out Vector2 direction, out float force)
    {
        if (IsPerfectBlockTiming(ball))
        {
            direction = new Vector2(perfectBlockHorizontalPush, -perfectBlockDownwardPush);
            force = blockForce * perfectBlockForceMultiplier;
            return true;
        }

        float roll = Random.value;
        if (roll < blockPassThroughChance)
        {
            direction = Vector2.zero;
            force = 0f;
            return false;
        }

        float horizontalDirection = 1f;
        if (roll < blockPassThroughChance + blockPopUpChance)
        {
            direction = new Vector2(horizontalDirection * blockPopUpHorizontalPush, blockPopUpVerticalPush);
            force = blockForce;
            return true;
        }

        direction = new Vector2(horizontalDirection * blockFlatHorizontalPush, blockFlatVerticalPush);
        force = blockForce * 1.05f;
        return true;
    }

    bool IsPerfectBlockTiming(BallController ball)
    {
        if (ball == null || rb == null)
            return false;

        if (Mathf.Abs(rb.linearVelocity.y) > perfectBlockVelocityWindow)
            return false;

        float ballHeightOffset = Mathf.Abs(ball.transform.position.y - GetBlockCenter().y);
        return ballHeightOffset <= perfectBlockHeightTolerance;
    }

    bool IsCloseEnoughToNetForBlock()
    {
        if (netPosition == null)
            return false;

        float netLimitX = netPosition.position.x - GetColliderHalfWidth() - netBarrierPadding;
        return transform.position.x >= netLimitX - blockNearNetTolerance;
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

    void ClampToPlayerSideOfNet()
    {
        if (netPosition == null || rb == null)
            return;

        float maxX = netPosition.position.x - GetColliderHalfWidth() - netBarrierPadding;
        if (transform.position.x <= maxX)
            return;

        transform.position = new Vector3(maxX, transform.position.y, transform.position.z);
        rb.linearVelocity = new Vector2(Mathf.Min(0f, rb.linearVelocity.x), rb.linearVelocity.y);
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

    void ResetServeCharge()
    {
        isChargingServe = false;
        serveChargeNormalized = 0.5f;
        serveChargeDirection = 1f;
    }

    ServeChargeResult EvaluateServeCharge(float normalizedValue)
    {
        float clampedValue = Mathf.Clamp01(normalizedValue);
        float halfGreenZone = serveGreenZoneWidth * 0.5f;

        if (clampedValue <= serveFaultZoneWidth || clampedValue >= 1f - serveFaultZoneWidth)
            return ServeChargeResult.Fault;

        if (Mathf.Abs(clampedValue - 0.5f) <= halfGreenZone)
            return ServeChargeResult.Power;

        return ServeChargeResult.Safe;
    }

    void UpdateServeChargeUI(bool forceHide = false)
    {
        if (serveChargeUI == null)
            serveChargeUI = ServeChargeUI.EnsureInstance();

        if (serveChargeUI == null)
            return;

        bool shouldShow = !forceHide && isServing && isChargingServe;
        serveChargeUI.SetVisible(shouldShow);

        if (!shouldShow)
            return;

        serveChargeUI.SetFollowTarget(transform, playerCollider, serveMeterYOffset);
        serveChargeUI.SetMarkerNormalized(serveChargeNormalized);
    }

    float GetServeForce(ServeChargeResult serveResult)
    {
        switch (serveResult)
        {
            case ServeChargeResult.Fault:
                return faultServeForce;
            case ServeChargeResult.Safe:
                return safeServeForce;
            default:
                return serveForce;
        }
    }

    Vector2 GetServeDirection(ServeChargeResult serveResult)
    {
        switch (serveResult)
        {
            case ServeChargeResult.Fault:
                return new Vector2(1.22f, 0.72f);
            case ServeChargeResult.Safe:
                return new Vector2(0.68f, 1.34f);
            default:
                return new Vector2(0.86f, 1.08f);
        }
    }
}
