using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Seguimento")]
    public Transform ball;
    public float smoothSpeed = 0.125f;
    [Range(0f, 1f)] public float horizontalFollowStrength = 0.35f;
    public float lookAheadByVelocity = 0.08f;
    public float maxLookAhead = 0.9f;
    public Vector3 offset = new Vector3(0, 2, -10);

    [Header("Limites da Camera")]
    public Transform leftBoundary;
    public Transform rightBoundary;
    public Transform visualLeftBoundary;
    public Transform visualRightBoundary;
    public SpriteRenderer visualBoundsSprite;
    public float leftVisualPadding = 2.5f;
    public float rightVisualPadding = 2.5f;
    public float minX = -5f;
    public float maxX = 5f;
    public float minY = 0f;
    public float maxY = 10f;

    private Camera cam;
    private Rigidbody2D ballRb;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        AutoDetectReferences();
    }

    void LateUpdate()
    {
        AutoDetectReferences();

        if (ball == null)
            return;

        float desiredX = CalculateDesiredX();
        float desiredY = Mathf.Clamp(ball.position.y + offset.y, minY, maxY);
        Vector3 desiredPosition = new Vector3(desiredX, desiredY, offset.z);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        transform.position = new Vector3(
            ClampCameraX(smoothedPosition.x),
            Mathf.Clamp(smoothedPosition.y, minY, maxY),
            offset.z);
    }

    float CalculateDesiredX()
    {
        float neutralX = GetNeutralX() + offset.x;
        float lookAhead = 0f;

        if (ballRb != null)
            lookAhead = Mathf.Clamp(ballRb.linearVelocity.x * lookAheadByVelocity, -maxLookAhead, maxLookAhead);

        float trackedBallX = ball.position.x + offset.x + lookAhead;
        return Mathf.Lerp(neutralX, trackedBallX, horizontalFollowStrength);
    }

    float ClampCameraX(float targetX)
    {
        if (TryGetEffectiveBounds(out float leftLimit, out float rightLimit))
        {
            float halfWidth = GetHalfCameraWidth();
            float minCameraX = leftLimit + halfWidth;
            float maxCameraX = rightLimit - halfWidth;

            if (minCameraX > maxCameraX)
                return (leftLimit + rightLimit) * 0.5f;

            return Mathf.Clamp(targetX, minCameraX, maxCameraX);
        }

        return Mathf.Clamp(targetX, minX, maxX);
    }

    float GetNeutralX()
    {
        if (TryGetEffectiveBounds(out float leftLimit, out float rightLimit))
            return (leftLimit + rightLimit) * 0.5f;

        return (minX + maxX) * 0.5f;
    }

    bool TryGetEffectiveBounds(out float leftLimit, out float rightLimit)
    {
        if (TryGetVisualBounds(out leftLimit, out rightLimit))
            return true;

        if (TryGetPhysicalBounds(out leftLimit, out rightLimit))
        {
            leftLimit -= leftVisualPadding;
            rightLimit += rightVisualPadding;
            return true;
        }

        leftLimit = 0f;
        rightLimit = 0f;
        return false;
    }

    bool TryGetVisualBounds(out float leftLimit, out float rightLimit)
    {
        leftLimit = 0f;
        rightLimit = 0f;

        if (visualLeftBoundary != null && visualRightBoundary != null)
        {
            leftLimit = Mathf.Min(visualLeftBoundary.position.x, visualRightBoundary.position.x);
            rightLimit = Mathf.Max(visualLeftBoundary.position.x, visualRightBoundary.position.x);
            return rightLimit > leftLimit;
        }

        if (visualBoundsSprite != null && TryGetSpriteBounds(visualBoundsSprite, out leftLimit, out rightLimit))
            return rightLimit > leftLimit;

        return false;
    }

    bool TryGetPhysicalBounds(out float leftLimit, out float rightLimit)
    {
        leftLimit = 0f;
        rightLimit = 0f;

        if (leftBoundary == null)
            leftBoundary = CourtReferences.FindBoundary("limit L");

        if (rightBoundary == null)
            rightBoundary = CourtReferences.FindBoundary("limit R");

        return CourtReferences.TryGetBoundaryInnerX(leftBoundary, true, out leftLimit)
            && CourtReferences.TryGetBoundaryInnerX(rightBoundary, false, out rightLimit)
            && rightLimit > leftLimit;
    }

    bool TryGetSpriteBounds(SpriteRenderer spriteRenderer, out float leftLimit, out float rightLimit)
    {
        leftLimit = 0f;
        rightLimit = 0f;

        if (spriteRenderer == null || spriteRenderer.sprite == null)
            return false;

        Vector3 spriteSize = spriteRenderer.sprite.bounds.size;
        Vector3 worldScale = spriteRenderer.transform.lossyScale;
        float worldWidth = Mathf.Abs(spriteSize.x * worldScale.x);

        if (worldWidth <= 0f)
            return false;

        float centerX = spriteRenderer.transform.position.x;
        leftLimit = centerX - (worldWidth * 0.5f);
        rightLimit = centerX + (worldWidth * 0.5f);
        return true;
    }

    float GetHalfCameraWidth()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam == null || !cam.orthographic)
            return 0f;

        return cam.orthographicSize * cam.aspect;
    }

    void AutoDetectReferences()
    {
        if (ball == null)
        {
            BallController detectedBall = FindObjectOfType<BallController>();
            if (detectedBall != null)
                ball = detectedBall.transform;
        }

        if (ball != null && (ballRb == null || ballRb.transform != ball))
            ballRb = ball.GetComponent<Rigidbody2D>();

        if (leftBoundary == null)
            leftBoundary = CourtReferences.FindBoundary("limit L");

        if (rightBoundary == null)
            rightBoundary = CourtReferences.FindBoundary("limit R");

        if (visualLeftBoundary == null)
            visualLeftBoundary = FindFirstNamedTransform("CameraLimitL", "Camera Limit L", "Camara Limit L");

        if (visualRightBoundary == null)
            visualRightBoundary = FindFirstNamedTransform("CameraLimitR", "Camera Limit R", "Camara Limit R");

        if (visualBoundsSprite == null)
            visualBoundsSprite = FindVisualBoundsSprite();
    }

    Transform FindFirstNamedTransform(params string[] objectNames)
    {
        foreach (string objectName in objectNames)
        {
            GameObject targetObject = GameObject.Find(objectName);
            if (targetObject != null)
                return targetObject.transform;
        }

        return null;
    }

    SpriteRenderer FindVisualBoundsSprite()
    {
        SpriteRenderer namedBackground = FindSpriteRenderer("CourtBackground");
        if (namedBackground != null)
            return namedBackground;

        namedBackground = FindSpriteRenderer("Background");
        if (namedBackground != null)
            return namedBackground;

        namedBackground = FindSpriteRenderer("background");
        if (namedBackground != null)
            return namedBackground;

        namedBackground = FindSpriteRenderer("Campo");
        if (namedBackground != null)
            return namedBackground;

        return null;
    }

    SpriteRenderer FindSpriteRenderer(string objectName)
    {
        GameObject targetObject = GameObject.Find(objectName);
        return targetObject != null ? targetObject.GetComponent<SpriteRenderer>() : null;
    }
}
