using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movimentação")]
    public float speed = 10f;
    public float jumpForce = 15f;
    public bool isServing = true; 

    [Header("Detecção e Física")]
    public Transform groundCheck;
    public float checkRadius = 0.4f;
    public LayerMask groundLayer;
    public LayerMask ballLayer;

    [Header("Sistema de Voleibol (3 Toques)")]
    public int playerTouchCount = 0; 
    public float hitRange = 2.5f;

    [Header("Referências")]
    public BallController ballScript;
    public Transform ballHoldPoint;
    
    private Rigidbody2D rb;
    private Animator anim;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rb.gravityScale = 4.0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (isServing && ballScript != null)
            ballScript.SetToServing(ballHoldPoint);
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        if (anim) anim.SetBool("isGrounded", isGrounded);

        if (isServing)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (Input.GetKeyDown(KeyCode.E)) PerformServe();
            return; 
        }

        HandleMovement();
        HandleActions();
    }

    void HandleMovement()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);

        if (moveInput > 0) transform.localScale = new Vector3(1f, 1f, 1f);
        else if (moveInput < 0) transform.localScale = new Vector3(-1f, 1f, 1f);

        if (anim) anim.SetBool("isRunning", moveInput != 0);
    }

    void HandleActions()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            if (anim) anim.SetTrigger("jump");
        }

        if (Input.GetKeyDown(KeyCode.E) || (Input.GetKeyDown(KeyCode.Space) && !isGrounded))
        {
            TryHitBall();
        }
    }

    void TryHitBall()
    {
        Collider2D ballCollider = Physics2D.OverlapCircle(transform.position, hitRange, ballLayer);
        if (ballCollider == null) return;

        BallController ball = ballCollider.GetComponent<BallController>();
        
        if (playerTouchCount == 0) 
        {
            ball.ApplyForce(new Vector2(0.2f, 1.4f).normalized, 11f);
            if (anim) anim.SetTrigger("manchete");
            playerTouchCount = 1;
        }
        else if (playerTouchCount == 1) 
        {
            ball.ApplyForce(new Vector2(0.1f, 1.8f).normalized, 13f);
            if (anim) anim.SetTrigger("manchete"); 
            playerTouchCount = 2;
        }
        else 
        {
            ball.ApplyForce(new Vector2(1.5f, -0.8f).normalized, 25f);
            if (anim) anim.SetTrigger("smash");
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -5f);
            playerTouchCount = 0; 
        }
    }

    void PerformServe()
    {
        isServing = false;
        playerTouchCount = 0;
        if (ballScript != null)
            ballScript.ReleaseServe(new Vector2(0.4f, 1f).normalized, 45f);
        
        if (anim) anim.SetTrigger("serveAction");
    }
}