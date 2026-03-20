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
    public int playerTouchCount = 0; // 0: Recepção, 1: Levantamento, 2: Spike
    public float hitRange = 2.5f;

    [Header("Referências")]
    public BallController ballScript;
    public Transform ballHoldPoint;
    
    private Rigidbody2D rb;
    private Animator anim;
    private bool isGrounded;
    private bool canAction = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        // Configuração inicial de física
        rb.gravityScale = 4.0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Inicia prendendo a bola se estiver sacando
        if (isServing && ballScript != null)
        {
            ballScript.SetToServing(ballHoldPoint);
        }
    }

    void Update()
    {
        // 1. Verificação de Chão
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        
        if (anim) anim.SetBool("isGrounded", isGrounded);

        // 2. Trava de Saque
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

        // Inverter o lado do boneco (Flip)
        if (moveInput > 0) transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        else if (moveInput < 0) transform.localScale = new Vector3(-0.4f, 0.4f, 0.4f);

        if (anim) anim.SetBool("isRunning", moveInput != 0);
    }

    void HandleActions()
    {
        // PULO (Espaço no chão)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            if (anim) anim.SetTrigger("jump");
        }

        // ATAQUE/TOQUE (E ou Espaço no ar)
        if (Input.GetKeyDown(KeyCode.E) || (Input.GetKeyDown(KeyCode.Space) && !isGrounded))
        {
            TryHitBall();
        }
    }

    void TryHitBall()
    {
        // Procura a bola no raio de alcance
        Collider2D ballCollider = Physics2D.OverlapCircle(transform.position, hitRange, ballLayer);
        if (ballCollider == null) return;

        BallController ball = ballCollider.GetComponent<BallController>();
        if (ball == null) return;

        // Lógica de 3 Toques Progressivos
        if (playerTouchCount == 0) 
        {
            // 1º Toque: Amortecer (Recepção)
            ball.ApplyForce(new Vector2(0.2f, 1.4f).normalized, 11f);
            if (anim) anim.SetTrigger("manchete");
            playerTouchCount = 1;
        }
        else if (playerTouchCount == 1) 
        {
            // 2º Toque: Levantamento (Bola sobe muito para preparar o Smash)
            ball.ApplyForce(new Vector2(0.1f, 1.8f).normalized, 13f);
            if (anim) anim.SetTrigger("manchete"); 
            playerTouchCount = 2;
        }
        else 
        {
            // 3º Toque: SPIKE / SMASH (Ataque Forte)
            ball.ApplyForce(new Vector2(1.5f, -0.8f).normalized, 25f);
            if (anim) anim.SetTrigger("smash");
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -5f); // Pequeno tranco para baixo
            playerTouchCount = 0; // Reseta o ciclo
        }
    }

    void PerformServe()
    {
        isServing = false;
        playerTouchCount = 0;
        if (ballScript != null)
        {
            ballScript.ReleaseServe(new Vector2(0.4f, 1f).normalized, 45f);
        }
        if (anim) anim.SetTrigger("serveAction");
    }

    // Função para ser chamada quando a bola toca o chão ou o Bot faz ponto
    public void ResetToServe()
    {
        isServing = true;
        playerTouchCount = 0;
        if (ballScript != null) ballScript.SetToServing(ballHoldPoint);
    }

    private void OnDrawGizmos()
    {
        // Visualizar o GroundCheck (Verde se no chão, Vermelho se no ar)
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }

        // Visualizar o alcance do toque (Azul)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hitRange);
    }
}