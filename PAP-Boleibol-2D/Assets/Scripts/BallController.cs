using UnityEngine;

public class BallController : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool isBeingHeld = false;
    private Transform holdPoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Se estiver no modo de serviço, a bola gruda na mão
        if (isBeingHeld && holdPoint != null)
        {
            transform.position = holdPoint.position;
            rb.linearVelocity = Vector2.zero; // Garante que ela não acumule velocidade
        }
    }

    // Chamado pelo Player no Start ou quando faz ponto
    public void SetToServing(Transform targetPoint)
{
    isBeingHeld = true;
    holdPoint = targetPoint;
    rb.simulated = true;
    rb.linearVelocity = Vector2.zero;
    rb.gravityScale = 0;
    
    // NOVO: Desativa o colisor para não dar conflito físico
    GetComponent<Collider2D>().isTrigger = true; 
}

public void ReleaseServe(Vector2 direction, float force)
{
    isBeingHeld = false;
    holdPoint = null;
    rb.gravityScale = 3.5f;
    
    // NOVO: Reativa o colisor para o jogo seguir
    GetComponent<Collider2D>().isTrigger = false;
    
    rb.AddForce(direction * force, ForceMode2D.Impulse);
}

    // Chamado pelo Player ao apertar E

    public void ApplyForce(Vector2 direction, float force)
    {
        rb.linearVelocity = Vector2.zero; // Limpa a força anterior para o impacto ser seco
        rb.AddForce(direction * force, ForceMode2D.Impulse);
    }
}