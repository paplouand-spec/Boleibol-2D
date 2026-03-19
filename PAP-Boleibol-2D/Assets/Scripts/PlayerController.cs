using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movimentação")]
    public float speed = 8f;
    public float jumpForce = 12f;
    public bool isServing = true; // Necessário para o EnemyAI não dar erro

    [Header("Configurações de Salto")]
    public Transform groundCheck;
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;
    
    [Header("Referências da Bola")]
    public BallController ballScript; 
    public Transform ballHoldPoint; 
    public float hitRange = 2f;
    public LayerMask ballLayer;

    private Rigidbody2D rb;
    private bool isGrounded;
    private bool canDoubleJump;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Se começar sacando, prende a bola na mão
        if (isServing && ballScript != null)
        {
            ballScript.SetToServing(ballHoldPoint);
        }
    }

    void Update()
    {
        // 1. Verificação de Chão
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

        if (isGrounded) canDoubleJump = true;

        // 2. Lógica de Serviço (Sacar)
        if (isServing)
        {
            // O boneco fica parado enquanto saca
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            if (Input.GetKeyDown(KeyCode.E))
            {
                PerformServe();
            }
            return; // Interrompe o Update aqui para não andar enquanto saca
        }

        // 3. Movimentação Normal (A e D)
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);

        // 4. Pulo e Ataque (Espaço)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded)
            {
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }
            else if (canDoubleJump)
            {
                Smash();
            }
        }

        // 5. Manchete (Botão E fora do saque)
        if (Input.GetKeyDown(KeyCode.E))
        {
            Manchete();
        }
    }

    void PerformServe()
    {
        isServing = false; // Agora o AI pode se mover!
        ballScript.ReleaseServe(new Vector2(0.5f, 1f).normalized, 10f);
    }

    void Smash()
    {
        Collider2D hitBall = Physics2D.OverlapCircle(transform.position, hitRange, ballLayer);
        if (hitBall != null)
        {
            hitBall.GetComponent<BallController>().ApplyForce(new Vector2(1f, -0.7f).normalized, 20f);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -3f);
            canDoubleJump = false;
        }
    }

    void Manchete()
    {
        Collider2D hitBall = Physics2D.OverlapCircle(transform.position, hitRange, ballLayer);
        if (hitBall != null)
        {
            // Joga a bola para cima e um pouco para frente
            hitBall.GetComponent<BallController>().ApplyForce(new Vector2(0.3f, 1.2f).normalized, 13f);
        }
    }

    // Ajuda a visualizar o alcance do pulo e da batida no editor
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, hitRange);
        }
    }
}