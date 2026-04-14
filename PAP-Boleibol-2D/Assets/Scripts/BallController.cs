using System;
using UnityEngine;

public class BallController : MonoBehaviour
{
    public enum TeamSide
    {
        None,
        Player,
        Bot
    }

    [Header("Gameplay")]
    public float rallyGravityScale = 2.75f;
    public float maxBallSpeed = 16.5f;
    public float sharedTouchCooldown = 0.08f;
    public float pointEndDelay = 2f;
    public float pointBannerDelay = 0.5f;
    public float failSafeResetY = -8f;
    public float outOfBoundsResolvePadding = 0.12f;
    public float serveLaunchProtectionDuration = 0.12f;
    public Transform netPosition;
    public Transform leftBoundary;
    public Transform rightBoundary;

    [Header("Referencias de Jogo")]
    public PlayerController player;
    public EnemyAI enemy;

    private Rigidbody2D rb;
    private Collider2D ballCollider;
    private bool isBeingHeld;
    private Transform holdPoint;
    private TeamSide currentTouchSide = TeamSide.None;
    private int currentTouchCount;
    private float lastTouchTime = -999f;
    private TeamSide lastTeamToTouch = TeamSide.None;
    private bool pointEnding;
    private float pointResolveTime = -999f;
    private float pointBannerTime = -999f;
    private float pendingLandingX;
    private TeamSide pendingWinningTeam = TeamSide.None;
    private bool pointBannerShown;
    private float serveLaunchProtectionUntil = -999f;
    private int playerScore;
    private int botScore;

    public bool IsBeingHeld => isBeingHeld;
    public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;
    public int PlayerScore => playerScore;
    public int BotScore => botScore;
    public TeamSide LastTeamToTouch => lastTeamToTouch;
    public event Action<int, int> ScoreChanged;
    public event Action<TeamSide> PointScored;
    public event Action<TeamSide> PointBannerRequested;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ballCollider = GetComponent<Collider2D>();
        rb.gravityScale = rallyGravityScale;
    }

    void Start()
    {
        AutoDetectReferences();
        NotifyScoreChanged();
    }

    void Update()
    {
        AutoDetectReferences();

        if (isBeingHeld && holdPoint != null)
        {
            transform.position = holdPoint.position;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            return;
        }

        if (!pointEnding && transform.position.y < failSafeResetY)
            IniciarFimDoPonto(transform.position.x);

        if (!pointEnding && !IsServeLaunchProtected() && TryGetBallOutOfBoundsResolveX(out float resolveX))
            IniciarFimDoPonto(resolveX);

        if (pointEnding && !pointBannerShown && Time.time >= pointBannerTime)
            ShowPendingPointBanner();

        if (pointEnding && Time.time >= pointResolveTime)
            ResolverPonto();
    }

    void FixedUpdate()
    {
        if (isBeingHeld)
            return;

        if (rb.linearVelocity.magnitude > maxBallSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxBallSpeed;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isBeingHeld || pointEnding)
            return;

        AutoDetectReferences();

        if (TryGetTouchingTeam(collision.collider, out TeamSide touchingTeam))
            RegisterPhysicalTouch(touchingTeam);

        if (IsServeLaunchProtected())
            return;

        if (TryGetBoundaryResolveX(collision.collider, out float resolveX))
        {
            IniciarFimDoPonto(resolveX);
            return;
        }

        if (collision.gameObject.CompareTag("Ground"))
            IniciarFimDoPonto(transform.position.x);
    }

    public int GetTouchCountFor(TeamSide team)
    {
        return currentTouchSide == team ? currentTouchCount : 0;
    }

    public bool TryRegisterTouch(TeamSide team, out int touchNumber)
    {
        touchNumber = 0;

        if (isBeingHeld || pointEnding || Time.time < lastTouchTime + sharedTouchCooldown)
            return false;

        if (currentTouchSide != team)
        {
            currentTouchSide = team;
            currentTouchCount = 0;
        }

        if (currentTouchCount >= 3)
            return false;

        currentTouchCount++;
        touchNumber = currentTouchCount;
        lastTouchTime = Time.time;
        lastTeamToTouch = team;
        return true;
    }

    public void ResetTouches()
    {
        currentTouchSide = TeamSide.None;
        currentTouchCount = 0;
        lastTouchTime = -999f;
    }

    public void SetToServing(Transform targetPoint)
    {
        ResetTouches();
        lastTeamToTouch = TeamSide.None;
        pointEnding = false;
        pointResolveTime = -999f;
        pointBannerTime = -999f;
        pendingWinningTeam = TeamSide.None;
        pointBannerShown = false;
        isBeingHeld = true;
        holdPoint = targetPoint;
        rb.simulated = true;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (ballCollider != null)
            ballCollider.isTrigger = true;
    }

    public void ReleaseServe(TeamSide servingTeam, Vector2 direction, float force)
    {
        ResetTouches();
        lastTeamToTouch = servingTeam;
        ReleaseFromHold();
        SnapServeInsideCourt(servingTeam);
        serveLaunchProtectionUntil = Time.time + serveLaunchProtectionDuration;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
    }

    public void ApplyForce(Vector2 direction, float force)
    {
        ReleaseFromHold();
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
    }

    public bool TryApplyBlock(TeamSide blockingTeam, Vector2 direction, float force)
    {
        if (isBeingHeld || pointEnding || Time.time < lastTouchTime + sharedTouchCooldown)
            return false;

        currentTouchSide = TeamSide.None;
        currentTouchCount = 0;
        lastTouchTime = Time.time;
        lastTeamToTouch = blockingTeam;

        ReleaseFromHold();
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
        return true;
    }

    void RegisterPhysicalTouch(TeamSide team)
    {
        if (team == TeamSide.None)
            return;

        lastTeamToTouch = team;
    }

    void ReleaseFromHold()
    {
        isBeingHeld = false;
        holdPoint = null;
        rb.gravityScale = rallyGravityScale;

        if (ballCollider != null)
            ballCollider.isTrigger = false;
    }

    bool IsServeLaunchProtected()
    {
        return Time.time < serveLaunchProtectionUntil;
    }

    void SnapServeInsideCourt(TeamSide servingTeam)
    {
        if (!TryGetCourtBounds(out float leftLimit, out float rightLimit))
            return;

        float horizontalMargin = GetBallHorizontalExtent() + 0.02f;
        Vector3 ballPosition = transform.position;

        if (servingTeam == TeamSide.Player)
            ballPosition.x = Mathf.Max(ballPosition.x, leftLimit + horizontalMargin);
        else if (servingTeam == TeamSide.Bot)
            ballPosition.x = Mathf.Min(ballPosition.x, rightLimit - horizontalMargin);

        transform.position = ballPosition;
        if (rb != null)
            rb.position = ballPosition;
    }

    float GetBallHorizontalExtent()
    {
        if (ballCollider == null)
            ballCollider = GetComponent<Collider2D>();

        return ballCollider != null ? ballCollider.bounds.extents.x : 0f;
    }

    void IniciarFimDoPonto(float landingX)
    {
        if (pointEnding)
            return;

        pointEnding = true;
        pendingLandingX = landingX;
        pendingWinningTeam = ResolveWinningTeam(landingX);
        pointResolveTime = Time.time + pointEndDelay;
        pointBannerTime = Time.time + pointBannerDelay;
        pointBannerShown = false;
        ResetTouches();
    }

    void ResolverPonto()
    {
        if (enemy != null)
            enemy.StopServing();

        TeamSide winningTeam = pendingWinningTeam != TeamSide.None
            ? pendingWinningTeam
            : ResolveWinningTeam(pendingLandingX);
        bool pontoDoPlayer = winningTeam == TeamSide.Player;

        if (pontoDoPlayer)
            playerScore++;
        else
            botScore++;

        NotifyScoreChanged();
        PointScored?.Invoke(winningTeam);

        if (pontoDoPlayer)
        {
            if (player != null)
                player.PrepareServe(this);
        }
        else
        {
            if (player != null)
                player.StopServing();

            if (enemy != null)
                enemy.PrepareServe(this);
        }
    }

    void ShowPendingPointBanner()
    {
        if (pointBannerShown)
            return;

        TeamSide winningTeam = pendingWinningTeam != TeamSide.None
            ? pendingWinningTeam
            : ResolveWinningTeam(pendingLandingX);

        pointBannerShown = true;
        PointBannerRequested?.Invoke(winningTeam);
    }

    void AutoDetectReferences()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        if (enemy == null)
            enemy = FindObjectOfType<EnemyAI>();

        if (!CourtReferences.IsPlayableNetPosition(netPosition))
            netPosition = CourtReferences.FindNetPosition();

        if (leftBoundary == null)
            leftBoundary = CourtReferences.FindBoundary("limit L");

        if (rightBoundary == null)
            rightBoundary = CourtReferences.FindBoundary("limit R");
    }

    TeamSide ResolveWinningTeam(float landingX)
    {
        if (TryGetCourtBounds(out float leftLimit, out float rightLimit) && netPosition != null)
        {
            float netX = netPosition.position.x;
            const float lineTolerance = 0.02f;

            bool landedInsidePlayerCourt = landingX >= leftLimit - lineTolerance && landingX <= netX + lineTolerance;
            bool landedInsideBotCourt = landingX <= rightLimit + lineTolerance && landingX >= netX - lineTolerance;

            if (landedInsideBotCourt && landingX > netX)
                return TeamSide.Player;

            if (landedInsidePlayerCourt && landingX < netX)
                return TeamSide.Bot;
        }

        if (lastTeamToTouch == TeamSide.Player)
            return TeamSide.Bot;

        if (lastTeamToTouch == TeamSide.Bot)
            return TeamSide.Player;

        return netPosition == null || landingX > netPosition.position.x
            ? TeamSide.Player
            : TeamSide.Bot;
    }

    bool TryGetCourtBounds(out float leftLimit, out float rightLimit)
    {
        leftLimit = 0f;
        rightLimit = 0f;

        return CourtReferences.TryGetBoundaryInnerX(leftBoundary, true, out leftLimit)
            && CourtReferences.TryGetBoundaryInnerX(rightBoundary, false, out rightLimit)
            && rightLimit > leftLimit;
    }

    bool TryGetBallOutOfBoundsResolveX(out float resolveX)
    {
        resolveX = 0f;

        if (!TryGetCourtBounds(out float leftLimit, out float rightLimit))
            return false;

        if (transform.position.x < leftLimit - outOfBoundsResolvePadding)
        {
            resolveX = leftLimit - outOfBoundsResolvePadding;
            return true;
        }

        if (transform.position.x > rightLimit + outOfBoundsResolvePadding)
        {
            resolveX = rightLimit + outOfBoundsResolvePadding;
            return true;
        }

        return false;
    }

    bool TryGetBoundaryResolveX(Collider2D collider, out float resolveX)
    {
        resolveX = 0f;
        if (collider == null)
            return false;

        if (leftBoundary != null && (collider.transform == leftBoundary || collider.transform.IsChildOf(leftBoundary)))
        {
            if (TryGetCourtBounds(out float leftLimit, out _))
                resolveX = leftLimit - outOfBoundsResolvePadding;
            else
                resolveX = leftBoundary.position.x - outOfBoundsResolvePadding;

            return true;
        }

        if (rightBoundary != null && (collider.transform == rightBoundary || collider.transform.IsChildOf(rightBoundary)))
        {
            if (TryGetCourtBounds(out _, out float rightLimit))
                resolveX = rightLimit + outOfBoundsResolvePadding;
            else
                resolveX = rightBoundary.position.x + outOfBoundsResolvePadding;

            return true;
        }

        return false;
    }

    bool TryGetTouchingTeam(Collider2D collider, out TeamSide touchingTeam)
    {
        touchingTeam = TeamSide.None;
        if (collider == null)
            return false;

        if (collider.GetComponentInParent<PlayerController>() != null)
        {
            touchingTeam = TeamSide.Player;
            return true;
        }

        if (collider.GetComponentInParent<EnemyAI>() != null)
        {
            touchingTeam = TeamSide.Bot;
            return true;
        }

        return false;
    }

    void NotifyScoreChanged()
    {
        ScoreChanged?.Invoke(playerScore, botScore);
    }
}
