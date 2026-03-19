using UnityEngine;

public class BallController : MonoBehaviour
{
    private Rigidbody2D rb;
    private bool beingHeld = false;
    private Transform holdPoint;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void Update()
    {
        if (beingHeld && holdPoint != null)
        {
            transform.position = holdPoint.position;
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void SetToServing(Transform point)
    {
        beingHeld = true;
        holdPoint = point;
        rb.simulated = false; // Desativa a física enquanto está na mão
    }

    public void ReleaseServe(Vector2 direction, float force)
    {
        beingHeld = false;
        rb.simulated = true; // Reativa a física
        rb.AddForce(direction * force, ForceMode2D.Impulse);
    }

    public void ApplyForce(Vector2 direction, float force)
    {
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(direction * force, ForceMode2D.Impulse);
    }
}