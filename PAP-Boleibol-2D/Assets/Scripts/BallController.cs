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
    public float failSafeResetY = -8f;
    public Transform netPosition;

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
    private bool pointEnding;
    private float pointResolveTime = -999f;
    private float pendingLandingX;
    private int playerScore;
    private int botScore;

    public bool IsBeingHeld => isBeingHeld;
    public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;
    public int PlayerScore => playerScore;
    public int BotScore => botScore;
    public event Action<int, int> ScoreChanged;

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
        pointEnding = false;
        pointResolveTime = -999f;
        isBeingHeld = true;
        holdPoint = targetPoint;
        rb.simulated = true;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;

        if (ballCollider != null)
            ballCollider.isTrigger = true;
    }

    public void ReleaseServe(Vector2 direction, float force)
    {
        ResetTouches();
        ReleaseFromHold();
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

    void ReleaseFromHold()
    {
        isBeingHeld = false;
        holdPoint = null;
        rb.gravityScale = rallyGravityScale;

        if (ballCollider != null)
            ballCollider.isTrigger = false;
    }

    void IniciarFimDoPonto(float landingX)
    {
        if (pointEnding)
            return;

        pointEnding = true;
        pendingLandingX = landingX;
        pointResolveTime = Time.time + pointEndDelay;
        ResetTouches();
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;
    }

    void ResolverPonto()
    {
        float landingX = pendingLandingX;

        if (enemy != null)
            enemy.StopServing();

        bool pontoDoPlayer = netPosition == null || landingX > netPosition.position.x;

        if (pontoDoPlayer)
            playerScore++;
        else
            botScore++;

        NotifyScoreChanged();

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

    void AutoDetectReferences()
    {
        if (player == null)
            player = FindObjectOfType<PlayerController>();

        if (enemy == null)
            enemy = FindObjectOfType<EnemyAI>();

        if (netPosition != null)
            return;

        GameObject netObject = GameObject.Find("netcheck");
        if (netObject == null)
            netObject = GameObject.Find("net");

        if (netObject != null)
            netPosition = netObject.transform;
    }

    void NotifyScoreChanged()
    {
        ScoreChanged?.Invoke(playerScore, botScore);
    }
}
