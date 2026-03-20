using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Referências")]
    public PlayerController player;
    public Transform ball;
    public Transform netPosition;

    [Header("Movimento")]
    public float speed = 9f;
    public float jumpForce = 13f;
    public float recuoDefesa = 2.5f;

    [Header("Sistema de Toques")]
    public int botTouchCount = 0;
    private float lastTouchTime; 
    public float touchCooldown = 0.4f; 
    public LayerMask groundLayer;
    public Transform groundCheck;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 1. ESPERA DO SAQUE
        if (player != null && player.isServing)
        {
            MoverPara(netPosition.position.x + recuoDefesa);
            return;
        }

        // 2. LÓGICA DE JOGO
        float distToBall = ball.position.x - transform.position.x;
        Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();

        // Se a bola cruzar a rede para o player, reseta
        if (ball.position.x < netPosition.position.x)
        {
            botTouchCount = 0;
            MoverPara(netPosition.position.x + 4f);
        }
        else 
        {
            // Se a bola estiver muito rápida, o bot corre mais
            float speedAtual = (ballRb.linearVelocity.magnitude > 20f) ? speed * 1.4f : speed;
            rb.linearVelocity = new Vector2(Mathf.Sign(distToBall) * speedAtual, rb.linearVelocity.y);

            bool isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.3f, groundLayer);
            if (botTouchCount >= 2 && isGrounded && ball.position.y > transform.position.y + 1.5f)
            {
                if (Mathf.Abs(distToBall) < 1.5f)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
        }
    }

    void MoverPara(float targetX)
    {
        float distancia = targetX - transform.position.x;
        if (Mathf.Abs(distancia) > 0.2f)
            rb.linearVelocity = new Vector2(Mathf.Sign(distancia) * speed, rb.linearVelocity.y);
        else
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            if (Time.time > lastTouchTime + touchCooldown)
            {
                // Zera a velocidade da bola para o saque de 45f não atravessar o bot
                collision.gameObject.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
                
                ExecutarToque();
                lastTouchTime = Time.time;
            }
        }
    }

    void ExecutarToque()
    {
        BallController ballScript = ball.GetComponent<BallController>();
        
        if (botTouchCount == 0) 
        {
            ballScript.ApplyForce(new Vector2(-0.2f, 1.7f).normalized, 14f);
            botTouchCount = 1;
        }
        else if (botTouchCount == 1) 
        {
            ballScript.ApplyForce(new Vector2(0f, 1.9f).normalized, 15f);
            botTouchCount = 2;
        }
        else 
        {
            ballScript.ApplyForce(new Vector2(-1.6f, -0.9f).normalized, 24f);
            botTouchCount = 0;
        }
    }
}