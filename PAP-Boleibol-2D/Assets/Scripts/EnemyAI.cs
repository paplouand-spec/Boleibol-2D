using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 11f;
    public Transform ball;
    public LayerMask groundLayer;
    public Transform groundCheck;

    // NOVO: Referência para o script do jogador
    public PlayerController player; 

    private Rigidbody2D rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 1. VERIFICAÇÃO: Se o jogador ainda estiver sacando, o AI não faz nada
        if (player != null && player.isServing)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Fica parado no X
            return; // Sai do Update aqui e ignora o resto
        }

        // --- Resto do código que você já tem ---
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.2f, groundLayer);
        float direction = ball.position.x - transform.position.x;
        
        if (Mathf.Abs(direction) > 0.5f)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(direction) * speed, rb.linearVelocity.y);
        }

        if (isGrounded && ball.position.y > transform.position.y + 2f && Mathf.Abs(direction) < 2f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            BallController ballScript = collision.gameObject.GetComponent<BallController>();
            // Rebate para a esquerda
            ballScript.ApplyForce(new Vector2(-1f, 1f).normalized, 10f);
        }
    }
}