using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform ball; // Arraste a bola para cá
    public float smoothSpeed = 0.125f; // Quão suave é o movimento
    public Vector3 offset = new Vector3(0, 2, -10); // Offset para ver a quadra melhor

    [Header("Limites da Câmera")]
    public float minX = -5f;
    public float maxX = 5f;
    public float minY = 0f;
    public float maxY = 10f;

    void LateUpdate() // LateUpdate é melhor para câmeras
    {
        if (ball == null) return;

        // Posição desejada baseada na bola + offset
        Vector3 desiredPosition = ball.position + offset;

        // Suaviza o movimento (Lerp)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Limita a câmera para ela não sair das bordas da quadra (Clamp)
        float clampedX = Mathf.Clamp(smoothedPosition.x, minX, maxX);
        float clampedY = Mathf.Clamp(smoothedPosition.y, minY, maxY);

        transform.position = new Vector3(clampedX, clampedY, offset.z);
    }
}