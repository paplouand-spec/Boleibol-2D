using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Referências Externas")]
    public PlayerController player; 
    public Transform ball;
    public Transform netPosition; 

    [Header("Configurações de Movimento")]
    public float speed = 8f; // Aumentei um pouco a velocidade
    public float jumpForce = 14f;

    [Header("Sistema de Voleibol")]
    public int botTouchCount = 0; 
    public LayerMask groundLayer;
    public Transform groundCheck;
    
    // --- AUMENTO DO RAIO DE ALCANCE ---
    public float hitRange = 3.5f; // Raio maior para ele alcançar a bola de longe

    private Rigidbody2D rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 1. VERIFICAÇÃO DE CHÃO
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.3f, groundLayer);

        // 2. LOGICA DE ESPERA (SAQUE)
        if (player != null && player.isServing)
        {
            // Em vez de fugir para trás, ele fica na POSIÇÃO DE DEFESA (perto da rede)
            float defesaX = netPosition.position.x + 2.5f; // Fica a apenas 2.5m da rede
            float paraDefesa = defesaX - transform.position.x;
            
            if(Mathf.Abs(paraDefesa) > 0.2f)
                rb.linearVelocity = new Vector2(Mathf.Sign(paraDefesa) * speed, rb.linearVelocity.y);
            else
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            return; 
        }

        // 3. LOGICA DE JOGO (BOLA EM MOVIMENTO)
        float distToBall = ball.position.x - transform.position.x;

        // Se a bola estiver no campo do Player
        if (ball.position.x < netPosition.position.x)
        {
            botTouchCount = 0;
            // Posiciona-se no meio do seu campo para esperar o ataque do player
            float meioCampo = netPosition.position.x + 4.0f; 
            float dir = meioCampo - transform.position.x;
            rb.linearVelocity = new Vector2(Mathf.Sign(dir) * speed, rb.linearVelocity.y);
        }
        else // BOLA NO CAMPO DO BOT
        {
            // Segue a bola agressivamente
            rb.linearVelocity = new Vector2(Mathf.Sign(distToBall) * speed, rb.linearVelocity.y);

            // Pulo para o Spike (3º toque)
            if (botTouchCount >= 2 && isGrounded && ball.position.y > transform.position.y + 1.2f)
            {
                if (Mathf.Abs(distToBall) < 1.5f) 
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                }
            }
        }
    }

    // Aumentamos o alcance usando Trigger ou Verificação de proximidade
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            ProcessarToque();
        }
    }

    void ProcessarToque()
    {
        BallController ballScript = ball.GetComponent<BallController>();
        
        if (botTouchCount == 0) 
        {
            ballScript.ApplyForce(new Vector2(-0.2f, 1.6f).normalized, 12f);
            botTouchCount = 1;
        }
        else if (botTouchCount == 1) 
        {
            ballScript.ApplyForce(new Vector2(0f, 1.9f).normalized, 14f);
            botTouchCount = 2;
        }
        else 
        {
            ballScript.ApplyForce(new Vector2(-1.8f, -1.0f).normalized, 24f);
            botTouchCount = 0; 
        }
    }
}