using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Referencias")]
    public PlayerController player;
    public Transform ball;
    public Transform netPosition;
    public Transform rightBoundary;

    [Header("Movimento")]
    public float speed = 5.8f;
    public float jumpForce = 11.6f;
    public float groundCheckYOffset = -0.85f;
    public float recuoDefesa = 3f;
    public float zonaLevantamento = 1.95f;
    public float zonaAtaque = 1.2f;
    public float limiteDireitaCampo = 15.5f;
    public float rightBoundaryPadding = 0.8f;
    public float netBarrierPadding = 0.08f;
    public float sprintMultiplier = 1.35f;
    public float antecipacaoBola = 0.45f;
    public float alturaLeituraRececao = 0.95f;
    public float velocidadeQuedaUrgente = -2.2f;
    public float distanciaTravagem = 0.12f;
    public float targetSmoothing = 0.28f;
    public float targetDeadZone = 0.1f;

    [Header("Servico")]
    public float serveForce = 18.5f;
    public float serveDelay = 0.35f;
    public float serveBackOffset = 0.08f;
    public float serveBoundaryPadding = 0.02f;
    public float serveRecoveryTime = 0.45f;

    [Header("Sistema de Toques")]
    public int botTouchCount = 0;
    public float touchCooldown = 0.06f;
    public float hitRange = 2.35f;
    public float receiveForce = 8.75f;
    public float setForce = 11.75f;
    public float sendForce = 12.75f;
    public float spikeForce = 18.5f;
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
    public float blockThreatPlayerNetDistance = 1.4f;
    public float blockThreatMinHorizontalSpeed = 3.8f;
    public float blockThreatMaxRiseVelocity = 0.95f;
    public float blockThreatExtraReactionTime = 0.16f;
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
    public LayerMask groundLayer;
    public Transform groundCheck;

    private Rigidbody2D rb;
    private Collider2D enemyCollider;
    private BallController ballScript;
    private Animator anim;
    private bool isGrounded;
    private bool isServing;
    private bool isBlocking;
    private bool isChargingBlock;
    private bool blockInteractionResolved;
    private float nextAllowedBallTouchTime = -999f;
    private float nextAllowedBlockTime = -999f;
    private float blockEndTime = -999f;
    private float blockChargeStartTime = -999f;
    private float serveStartTime = -999f;
    private float smoothedMoveTargetX;
    private bool hasSmoothedMoveTarget;
    private Transform serveHoldPoint;
    private Vector3 baseScale;

    public bool IsServing => isServing;

    void Start()
    {
        NormalizeVisualFacing();
        rb = GetComponent<Rigidbody2D>();
        enemyCollider = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        if (anim == null)
            anim = GetComponentInChildren<Animator>();
        baseScale = transform.localScale;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        AutoDetectReferences();
        IgnoreBoundaryCollision();
        EnsureGroundCheck();
        EnsureServeHoldPoint();
    }

    void Update()
    {
        AutoDetectReferences();
        IgnoreBoundaryCollision();
        EnsureGroundCheck();
        EnsureServeHoldPoint();

        if (ball == null || netPosition == null)
            return;

        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.3f, groundLayer);

        UpdateBlockingState();

        if (isServing)
        {
            HandleServe();
            UpdateAnimatorState();
            return;
        }

        if (ballScript != null)
            botTouchCount = ballScript.GetTouchCountFor(BallController.TeamSide.Bot);

        if (player != null && player.isServing && ballScript != null && ballScript.IsBeingHeld)
        {
            MoverPara(GetSmoothedMoveTargetX(netPosition.position.x + recuoDefesa));
            UpdateAnimatorState();
            return;
        }

        if (HandleBlockBehaviour())
        {
            UpdateAnimatorState();
            return;
        }

        if (ball.position.x < netPosition.position.x - 0.05f)
        {
            botTouchCount = 0;
            MoverPara(GetSmoothedMoveTargetX(netPosition.position.x + recuoDefesa));
            UpdateAnimatorState();
            return;
        }

        MoverPara(GetSmoothedMoveTargetX(CalcularPosicaoAlvo()));

        if (botTouchCount == 2 && isGrounded && DeveSaltarParaAtacar())
        {
            isGrounded = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

            if (anim != null)
                anim.SetTrigger("jump");
        }

        TentarTocarBola();
        UpdateAnimatorState();
    }

    void HandleServe()
    {
        float serveDirectionX = GetServeDirectionX();
        OrientarPara(serveDirectionX);
        ResetSmoothedMoveTarget(transform.position.x);
        ClampServePosition();
        ClampToBotSideOfNet();

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

        PerformServe(serveDirectionX);
    }

    void MoverPara(float targetX)
    {
        if (isBlocking || isChargingBlock)
        {
            OrientarPara(-1f);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            ClampToBotSideOfNet();
            return;
        }

        float distancia = targetX - transform.position.x;
        float distanciaAbs = Mathf.Abs(distancia);
        float moveSpeed = speed;
        float urgenciaRececao = CalcularUrgenciaRececao();

        if (distanciaAbs > 1.5f)
            moveSpeed *= sprintMultiplier;

        if (urgenciaRececao > 0f)
            moveSpeed *= Mathf.Lerp(1f, 1.22f, urgenciaRececao);

        float stopDistance = Mathf.Lerp(distanciaTravagem, 0.05f, urgenciaRececao);
        if (distanciaAbs > stopDistance)
        {
            OrientarPara(Mathf.Sign(distancia));

            float moveFactor = Mathf.Clamp01(distanciaAbs / 0.8f);
            float appliedSpeed = Mathf.Lerp(speed * 0.3f, moveSpeed, moveFactor);
            rb.linearVelocity = new Vector2(Mathf.Sign(distancia) * appliedSpeed, rb.linearVelocity.y);
        }
        else
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        ClampToBotSideOfNet();
    }

    float CalcularPosicaoAlvo()
    {
        float minX = netPosition.position.x + 0.8f;
        float maxX = Mathf.Max(minX, GetPlayableRightLimitX());

        if (ballScript == null)
            return Mathf.Clamp(netPosition.position.x + recuoDefesa, minX, maxX);

        int currentTouches = ballScript.GetTouchCountFor(BallController.TeamSide.Bot);
        float predictedBallX = PredictBallTargetX(transform.position.y + alturaLeituraRececao);
        float urgenciaRececao = CalcularUrgenciaRececao();

        if (currentTouches == 0)
        {
            float defensiveBase = netPosition.position.x + recuoDefesa;
            if (!BolaNoLadoDoBot())
                return Mathf.Clamp(defensiveBase, minX, maxX);

            float lateralBias = Mathf.Clamp(ballScript.Velocity.x * 0.14f, -0.22f, 0.12f);
            float receiveTarget = predictedBallX + lateralBias;
            float ballHeightWeight = Mathf.InverseLerp(transform.position.y + 3.3f, transform.position.y + 1.1f, ball.position.y);
            float trackingWeight = Mathf.Lerp(0.38f, 0.95f, Mathf.Max(urgenciaRececao, ballHeightWeight));
            if (ballScript.Velocity.y > 1.4f)
                trackingWeight *= 0.7f;

            float blendedTarget = Mathf.Lerp(defensiveBase, receiveTarget, trackingWeight);
            return Mathf.Clamp(blendedTarget, minX, maxX);
        }

        if (currentTouches == 1)
        {
            float levantamentoBase = netPosition.position.x + zonaLevantamento;
            float levantamentoTarget = Mathf.Lerp(predictedBallX, levantamentoBase, 0.76f);

            if (ballScript.Velocity.y < velocidadeQuedaUrgente * 0.5f)
                levantamentoTarget = Mathf.Lerp(levantamentoTarget, predictedBallX, 0.22f);

            return Mathf.Clamp(levantamentoTarget, minX, maxX);
        }

        float ataqueBase = netPosition.position.x + zonaAtaque;
        float ataqueLead = predictedBallX + Mathf.Clamp(ballScript.Velocity.x * 0.16f, -0.18f, 0.08f);
        float ataqueTarget = Mathf.Lerp(ataqueLead, ataqueBase, 0.84f);
        return Mathf.Clamp(ataqueTarget, minX, maxX);
    }

    bool DeveSaltarParaAtacar()
    {
        if (ballScript == null)
            return false;

        float distancia = Mathf.Abs(ball.position.x - transform.position.x);
        bool bolaAlta = ball.position.y > transform.position.y + 0.8f;
        bool bolaADescer = ballScript.Velocity.y <= 1.1f;
        return distancia < 1.45f && bolaAlta && bolaADescer;
    }

    void TentarTocarBola()
    {
        if (isBlocking || isChargingBlock || ballScript == null || Time.time < nextAllowedBallTouchTime)
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
            ballScript.ApplyForce(new Vector2(-0.03f, 1.9f), receiveForce);

            if (anim != null)
                anim.SetTrigger("manchete");
        }
        else if (touchNumber == 2)
        {
            // Levantamento mais alto para dar tempo ao jogador.
            ballScript.ApplyForce(new Vector2(-0.05f, 2.55f), setForce);

            if (anim != null)
                anim.SetTrigger("manchete");
        }
        else if (spikeInput)
        {
            ballScript.ApplyForce(new Vector2(-1.3f, -0.55f), spikeForce);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Min(rb.linearVelocity.y, -1.5f));

            if (anim != null)
                anim.SetTrigger("smash");
        }
        else
        {
            ballScript.ApplyForce(new Vector2(-0.95f, 0.95f), sendForce);

            if (anim != null)
                anim.SetTrigger("manchete");
        }
    }

    Vector2 GetHitCenter()
    {
        float yOffset = isGrounded ? 0.8f : 1.1f;
        return (Vector2)transform.position + new Vector2(-0.35f, yOffset);
    }

    Vector2 GetBlockCenter()
    {
        return (Vector2)transform.position + new Vector2(-blockForwardOffset, blockHeightOffset);
    }

    public void PrepareServe(BallController targetBall)
    {
        if (targetBall != null)
            ballScript = targetBall;

        if (ballScript != null)
            ball = ballScript.transform;

        AutoDetectReferences();
        EnsureServeHoldPoint();
        CancelBlock();

        float serveX = GetServePositionX();
        ResetSmoothedMoveTarget(serveX);
        transform.position = new Vector3(serveX, transform.position.y, transform.position.z);
        rb.linearVelocity = Vector2.zero;
        OrientarPara(GetServeDirectionX());

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
        CancelBlock();
        hasSmoothedMoveTarget = false;
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
        return GetServeMovementLimitX() + serveBackOffset;
    }

    float GetServeDirectionX()
    {
        if (netPosition == null)
            return -1f;

        return netPosition.position.x <= transform.position.x ? -1f : 1f;
    }

    float GetPlayableRightLimitX()
    {
        if (CourtReferences.TryGetBoundaryInnerX(rightBoundary, false, out float rightLimit))
            return rightLimit - rightBoundaryPadding;

        return limiteDireitaCampo;
    }

    float GetServeBoundaryX()
    {
        if (CourtReferences.TryGetBoundaryInnerX(rightBoundary, false, out float rightLimit))
            return rightLimit + serveBoundaryPadding;

        return limiteDireitaCampo;
    }

    float GetServeMovementLimitX()
    {
        return GetServeBoundaryX() + GetColliderHalfWidth();
    }

    void ClampServePosition()
    {
        if (!isServing)
            return;

        float serveMovementLimitX = GetServeMovementLimitX();
        if (transform.position.x < serveMovementLimitX)
        {
            transform.position = new Vector3(serveMovementLimitX, transform.position.y, transform.position.z);
            rb.linearVelocity = new Vector2(Mathf.Max(0f, rb.linearVelocity.x), rb.linearVelocity.y);
        }
    }

    void ClampToBotSideOfNet()
    {
        if (netPosition == null || rb == null)
            return;

        float minX = netPosition.position.x + GetColliderHalfWidth() + netBarrierPadding;
        if (transform.position.x >= minX)
            return;

        transform.position = new Vector3(minX, transform.position.y, transform.position.z);
        rb.linearVelocity = new Vector2(Mathf.Max(0f, rb.linearVelocity.x), rb.linearVelocity.y);
    }

    float GetColliderHalfWidth()
    {
        if (enemyCollider == null)
            enemyCollider = GetComponent<Collider2D>();

        return enemyCollider != null ? enemyCollider.bounds.extents.x : 0f;
    }

    void OrientarPara(float direction)
    {
        if (direction > 0f)
            transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
        else if (direction < 0f)
            transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
    }

    void NormalizeVisualFacing()
    {
        float localYRotation = transform.localEulerAngles.y;
        if (Mathf.Abs(Mathf.DeltaAngle(localYRotation, 180f)) > 0.01f)
            return;

        Vector3 normalizedScale = transform.localScale;
        transform.localRotation = Quaternion.identity;
        normalizedScale.x = -Mathf.Abs(normalizedScale.x);
        transform.localScale = normalizedScale;
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

    void IgnoreBoundaryCollision()
    {
        if (enemyCollider == null)
            enemyCollider = GetComponent<Collider2D>();

        if (enemyCollider == null)
            return;

        if (rightBoundary == null)
            return;

        Collider2D boundaryCollider = rightBoundary.GetComponent<Collider2D>();
        if (boundaryCollider != null)
            Physics2D.IgnoreCollision(enemyCollider, boundaryCollider, true);
    }

    bool BolaNoLadoDoBot()
    {
        return netPosition == null || ball == null || ball.position.x >= netPosition.position.x - 0.05f;
    }

    void UpdateBlockingState()
    {
        if (isBlocking && Time.time >= blockEndTime)
            CancelBlock();
    }

    bool HandleBlockBehaviour()
    {
        if (ballScript == null || ball == null || isServing)
            return false;

        if (isChargingBlock)
        {
            OrientarPara(-1f);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            ClampToBotSideOfNet();

            if (ShouldReleaseBlock())
                ReleaseChargedBlock();
            else if (!ShouldKeepChargingBlock())
                CancelBlock();

            return true;
        }

        if (isBlocking)
        {
            OrientarPara(-1f);
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            ClampToBotSideOfNet();
            TryBlockBall();
            return true;
        }

        if (!TryGetBlockThreat(out float blockTargetX, out _))
            return false;

        MoverPara(blockTargetX);

        if (isGrounded && Time.time >= nextAllowedBlockTime && IsCloseEnoughToNetForBlock())
            StartBlockCharge();

        return true;
    }

    void StartBlockCharge()
    {
        if (isChargingBlock || isBlocking)
            return;

        isChargingBlock = true;
        blockInteractionResolved = false;
        blockChargeStartTime = Time.time;
        OrientarPara(-1f);
        rb.linearVelocity = new Vector2(0f, Mathf.Min(rb.linearVelocity.y, 0f));
    }

    bool ShouldKeepChargingBlock()
    {
        if (!isChargingBlock)
            return false;

        if (!isGrounded || !IsCloseEnoughToNetForBlock())
            return false;

        return TryGetBlockThreat(out _, out _);
    }

    bool ShouldReleaseBlock()
    {
        if (!isChargingBlock)
            return false;

        float chargedTime = Time.time - blockChargeStartTime;
        if (chargedTime >= blockChargeDuration)
            return true;

        if (!TryGetBlockThreat(out _, out float threatTime))
            return chargedTime >= 0.06f;

        return threatTime <= 0.11f;
    }

    void ReleaseChargedBlock()
    {
        if (!isChargingBlock)
            return;

        float chargeTime = Mathf.Clamp(Time.time - blockChargeStartTime, 0f, blockChargeDuration);
        isChargingBlock = false;
        blockChargeStartTime = -999f;

        if (!isGrounded || !IsCloseEnoughToNetForBlock())
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

        if (!ballScript.TryApplyBlock(BallController.TeamSide.Bot, blockDirection, appliedBlockForce))
            return;

        blockInteractionResolved = true;
        botTouchCount = ballScript.GetTouchCountFor(BallController.TeamSide.Bot);
        nextAllowedBallTouchTime = Time.time;
    }

    bool CanAttemptBlock(BallController controlledBall)
    {
        if (controlledBall == null)
            return false;

        if (controlledBall.LastTeamToTouch != BallController.TeamSide.Player)
            return false;

        if (controlledBall.Velocity.x <= 0.05f)
            return false;

        if (!IsCloseEnoughToNetForBlock())
            return false;

        Vector2 blockCenter = GetBlockCenter();
        Vector2 ballPosition = controlledBall.transform.position;

        if (ballPosition.x > transform.position.x + 0.1f)
            return false;

        return Vector2.Distance(blockCenter, ballPosition) <= blockRange;
    }

    bool IsCloseEnoughToNetForBlock()
    {
        if (netPosition == null)
            return false;

        float netLimitX = netPosition.position.x + GetColliderHalfWidth() + netBarrierPadding;
        return transform.position.x <= netLimitX + blockNearNetTolerance;
    }

    float GetBlockJumpVelocity(float normalizedCharge)
    {
        float minVelocity = Mathf.Max(blockMinJumpForce, 3f);
        float maxVelocity = Mathf.Max(minVelocity, jumpForce - blockJumpReductionFromNormal);
        return Mathf.Lerp(minVelocity, maxVelocity, Mathf.Clamp01(normalizedCharge));
    }

    bool GetBlockResponse(BallController controlledBall, out Vector2 direction, out float force)
    {
        if (IsPerfectBlockTiming(controlledBall))
        {
            direction = new Vector2(-perfectBlockHorizontalPush, -perfectBlockDownwardPush);
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

        float horizontalDirection = -1f;
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

    bool IsPerfectBlockTiming(BallController controlledBall)
    {
        if (controlledBall == null || rb == null)
            return false;

        if (Mathf.Abs(rb.linearVelocity.y) > perfectBlockVelocityWindow)
            return false;

        float ballHeightOffset = Mathf.Abs(controlledBall.transform.position.y - GetBlockCenter().y);
        return ballHeightOffset <= perfectBlockHeightTolerance;
    }

    bool TryGetBlockThreat(out float blockTargetX, out float timeToThreat)
    {
        blockTargetX = netPosition != null ? netPosition.position.x + GetColliderHalfWidth() + netBarrierPadding : transform.position.x;
        timeToThreat = -1f;

        if (ballScript == null || ball == null || ballScript.IsBeingHeld || netPosition == null)
            return false;

        if (ballScript.GetTouchCountFor(BallController.TeamSide.Player) < 3)
            return false;

        if (ballScript.LastTeamToTouch != BallController.TeamSide.Player)
            return false;

        Vector2 velocity = ballScript.Velocity;
        if (velocity.x <= blockThreatMinHorizontalSpeed)
            return false;

        if (velocity.y > blockThreatMaxRiseVelocity)
            return false;

        float netX = netPosition.position.x;
        if (player != null && player.transform.position.x < netX - blockThreatPlayerNetDistance)
            return false;

        float horizontalDistanceToNet = netX - ball.position.x;
        if (horizontalDistanceToNet < -0.08f || horizontalDistanceToNet > 2.35f)
            return false;

        timeToThreat = horizontalDistanceToNet / Mathf.Max(velocity.x, 0.01f);
        if (timeToThreat < -0.02f || timeToThreat > blockChargeDuration + blockThreatExtraReactionTime)
            return false;

        float predictedNetY = PredictBallHeightAtTime(timeToThreat);
        float blockCenterY = GetBlockCenter().y;
        if (predictedNetY < blockCenterY - blockRange * 0.85f)
            return false;

        if (predictedNetY > blockCenterY + blockRange * 0.55f)
            return false;

        return true;
    }

    float CalcularUrgenciaRececao()
    {
        if (ballScript == null || ball == null || !BolaNoLadoDoBot())
            return 0f;

        float alturaUrgencia = Mathf.InverseLerp(transform.position.y + 2.7f, transform.position.y + 0.6f, ball.position.y);
        float quedaUrgencia = Mathf.InverseLerp(-0.1f, velocidadeQuedaUrgente, ballScript.Velocity.y);
        return Mathf.Clamp01(Mathf.Max(alturaUrgencia, quedaUrgencia));
    }

    float PredictBallTargetX(float targetY)
    {
        if (ballScript == null || ball == null)
            return transform.position.x;

        Vector2 velocity = ballScript.Velocity;
        Rigidbody2D ballBody = ball.GetComponent<Rigidbody2D>();
        float gravityScale = ballBody != null ? ballBody.gravityScale : 1f;
        float gravity = Physics2D.gravity.y * gravityScale;
        float predictedTime = SolvePositiveTravelTime(ball.position.y, velocity.y, gravity, targetY);

        if (predictedTime < 0f)
            predictedTime = antecipacaoBola;

        predictedTime = Mathf.Clamp(predictedTime, 0.08f, 1.15f);
        return ball.position.x + velocity.x * predictedTime;
    }

    float PredictBallHeightAtTime(float time)
    {
        if (ballScript == null || ball == null)
            return transform.position.y;

        Vector2 velocity = ballScript.Velocity;
        Rigidbody2D ballBody = ball.GetComponent<Rigidbody2D>();
        float gravityScale = ballBody != null ? ballBody.gravityScale : 1f;
        float gravity = Physics2D.gravity.y * gravityScale;
        return ball.position.y + velocity.y * time + 0.5f * gravity * time * time;
    }

    float SolvePositiveTravelTime(float startY, float velocityY, float gravity, float targetY)
    {
        if (Mathf.Abs(gravity) < 0.001f)
        {
            if (Mathf.Abs(velocityY) < 0.001f)
                return -1f;

            float linearTime = (targetY - startY) / velocityY;
            return linearTime > 0f ? linearTime : -1f;
        }

        float a = 0.5f * gravity;
        float b = velocityY;
        float c = startY - targetY;
        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f)
            return -1f;

        float root = Mathf.Sqrt(discriminant);
        float t1 = (-b - root) / (2f * a);
        float t2 = (-b + root) / (2f * a);
        float bestTime = float.MaxValue;

        if (t1 > 0f)
            bestTime = Mathf.Min(bestTime, t1);

        if (t2 > 0f)
            bestTime = Mathf.Min(bestTime, t2);

        return bestTime == float.MaxValue ? -1f : bestTime;
    }

    void PerformServe(float serveDirectionX)
    {
        CancelBlock();
        isServing = false;
        serveStartTime = -999f;
        botTouchCount = 0;
        nextAllowedBallTouchTime = Time.time + serveRecoveryTime;

        if (ballScript != null)
            ballScript.ReleaseServe(BallController.TeamSide.Bot, GetServeDirection(serveDirectionX), serveForce * Random.Range(0.96f, 1.04f));

        if (anim != null)
            anim.SetTrigger("serveAction");
    }

    Vector2 GetServeDirection(float serveDirectionX)
    {
        float horizontal = Random.Range(0.82f, 0.92f);
        float vertical = Random.Range(0.98f, 1.12f);
        return new Vector2(serveDirectionX * horizontal, vertical);
    }

    void UpdateAnimatorState()
    {
        if (anim == null || rb == null)
            return;

        anim.SetBool("isGrounded", isGrounded);

        float horizontalSpeed = speed > 0.01f ? Mathf.Abs(rb.linearVelocity.x) / speed : 0f;
        anim.SetBool("isRunning", horizontalSpeed > 0.01f);
        anim.SetFloat("Speed", horizontalSpeed);
    }

    float GetSmoothedMoveTargetX(float desiredTargetX)
    {
        if (!hasSmoothedMoveTarget)
        {
            ResetSmoothedMoveTarget(desiredTargetX);
            return desiredTargetX;
        }

        if (Mathf.Abs(desiredTargetX - smoothedMoveTargetX) <= targetDeadZone)
            return smoothedMoveTargetX;

        smoothedMoveTargetX = Mathf.Lerp(smoothedMoveTargetX, desiredTargetX, targetSmoothing);
        return smoothedMoveTargetX;
    }

    void ResetSmoothedMoveTarget(float targetX)
    {
        smoothedMoveTargetX = targetX;
        hasSmoothedMoveTarget = true;
    }

    void CancelBlock()
    {
        isBlocking = false;
        isChargingBlock = false;
        blockInteractionResolved = false;
        blockEndTime = -999f;
        blockChargeStartTime = -999f;
    }
}
