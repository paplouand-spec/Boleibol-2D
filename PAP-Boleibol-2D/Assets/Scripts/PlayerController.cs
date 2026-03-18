using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;
    public float boomJumpForce = 16f; // Salto mais alto durante o run-up
    public float slideForce = 10f;
    public float slideDuration = 0.5f;

    [Header("Detection Settings")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    // Estados
    private bool isGrounded;
    private bool isSliding;
    private bool isReceiving;
    private bool isSpiking;
    private bool isBlocking;
    private float slideTimer;
    private float horizontalInput;

    [Header("Attack Settings")]
    public float spikeForce = 20f;
    public float spikeRadius = 1.5f;
    public LayerMask ballLayer;

    // Componentes
    private Rigidbody2D rb;
    private Animator anim; // Opcional, para animações

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (isSliding) return;

        // Inputs
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Salto (Espaço)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        // Receber (S ou Seta Baixo)
        isReceiving = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        // Slide/Mergulho (Shift ou J)
        if ((Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.J)) && isGrounded && !isSliding)
        {
            StartSlide();
        }

        // Ataque/Spike (Z ou K) - No ar
        if ((Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.K)) && !isGrounded)
        {
            PerformSpike();
        }

        // Bloqueio (Espaço no chão ou tecla dedicada)
        isBlocking = Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.L);

        UpdateAnimations();
    }

    void FixedUpdate()
    {
        CheckGround();

        if (isSliding)
        {
            HandleSlide();
        }
        else if (!isReceiving)
        {
            // Movimento normal
            rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            // Se estiver recebendo, fica parado ou move-se muito devagar
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    private void Jump()
    {
        // Mecânica de "Boom Jump": Se estiver correndo (input horizontal significativo), pula mais alto
        float currentJumpForce = Mathf.Abs(horizontalInput) > 0.1f ? boomJumpForce : jumpForce;
        
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, currentJumpForce);
        
        // Feedback visual/partículas podem ser adicionados aqui
        Debug.Log(currentJumpForce == boomJumpForce ? "BOOM JUMP!" : "Normal Jump");
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        
        // Direção do slide baseada no input ou na direção que o jogador está olhando
        float slideDir = horizontalInput != 0 ? Mathf.Sign(horizontalInput) : transform.localScale.x;
        rb.linearVelocity = new Vector2(slideDir * slideForce, rb.linearVelocity.y);
    }

    private void HandleSlide()
    {
        slideTimer -= Time.fixedDeltaTime;
        if (slideTimer <= 0)
        {
            isSliding = false;
        }
    }

    private void PerformSpike()
    {
        isSpiking = true;
        
        // Detectar a bola no raio de ataque
        Collider2D ball = Physics2D.OverlapCircle(transform.position, spikeRadius, ballLayer);
        if (ball != null)
        {
            Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();
            if (ballRb != null)
            {
                // Aplica força na bola (direção baseada no input ou fixa para baixo/frente)
                Vector2 spikeDir = new Vector2(transform.localScale.x, -0.5f).normalized;
                ballRb.linearVelocity = spikeDir * spikeForce;
                Debug.Log("SPIKE SUCCESSFUL!");
            }
        }
        
        // Resetar estado de spike após um tempo ou animação
        Invoke("ResetSpike", 0.3f);
    }

    private void ResetSpike()
    {
        isSpiking = false;
    }

    private void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void UpdateAnimations()
    {
        if (anim == null) return;

        anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetBool("IsReceiving", isReceiving);
        anim.SetBool("IsSliding", isSliding);
        anim.SetFloat("VerticalVelocity", rb.linearVelocity.y);
    }

    // Gizmos para ajudar a ver o GroundCheck no Editor da Unity
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
