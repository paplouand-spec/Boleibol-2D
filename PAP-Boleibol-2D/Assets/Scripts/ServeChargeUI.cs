using UnityEngine;
using UnityEngine.UI;

public class ServeChargeUI : MonoBehaviour
{
    const string BarResourcePath = "UI/ServeMeterBar";
    const string MeterObjectName = "ServeChargeMeter";
    const string BarObjectName = "Bar";
    const string MarkerObjectName = "Marker";
    const float MeterWidth = 285f;
    const float MeterHeight = 42f;
    const float MarkerWidth = 5f;
    const float MarkerHeight = 28f;
    const float MarkerHorizontalPadding = 16f;

    static Texture2D barTexture;
    static Texture2D whiteTexture;

    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform meterRoot;
    private RectTransform markerRect;
    private Transform followTarget;
    private Collider2D followCollider;
    private float followYOffset = 0.45f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateServeChargeUI()
    {
        if (FindObjectOfType<ServeChargeUI>() != null)
            return;

        if (FindObjectOfType<PlayerController>() == null)
            return;

        GameObject meterObject = new GameObject(nameof(ServeChargeUI));
        meterObject.AddComponent<ServeChargeUI>();
    }

    public static ServeChargeUI EnsureInstance()
    {
        ServeChargeUI existingUi = FindObjectOfType<ServeChargeUI>();
        if (existingUi != null)
            return existingUi;

        if (FindObjectOfType<PlayerController>() == null)
            return null;

        GameObject meterObject = new GameObject(nameof(ServeChargeUI));
        return meterObject.AddComponent<ServeChargeUI>();
    }

    void Start()
    {
        if (FindObjectOfType<PlayerController>() == null)
        {
            Destroy(gameObject);
            return;
        }

        EnsureMeter();
        SetVisible(false);
    }

    void LateUpdate()
    {
        if (meterRoot == null || !meterRoot.gameObject.activeSelf)
            return;

        UpdateFollowPosition();
    }

    public void SetFollowTarget(Transform target, Collider2D targetCollider, float yOffset)
    {
        followTarget = target;
        followCollider = targetCollider;
        followYOffset = yOffset;
        EnsureMeter();
        UpdateFollowPosition();
    }

    public void SetVisible(bool isVisible)
    {
        EnsureMeter();

        if (meterRoot != null)
            meterRoot.gameObject.SetActive(isVisible);
    }

    public void SetMarkerNormalized(float normalizedValue)
    {
        EnsureMeter();

        if (markerRect == null)
            return;

        float clampedValue = Mathf.Clamp01(normalizedValue);
        float leftEdge = (-MeterWidth * 0.5f) + MarkerHorizontalPadding;
        float rightEdge = (MeterWidth * 0.5f) - MarkerHorizontalPadding;
        markerRect.anchoredPosition = new Vector2(Mathf.Lerp(leftEdge, rightEdge, clampedValue), 0f);
    }

    void EnsureMeter()
    {
        if (meterRoot != null && markerRect != null)
            return;

        canvas = FindOrCreateCanvas();
        if (canvas == null)
            return;

        canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null)
            return;

        Transform existingMeter = canvas.transform.Find(MeterObjectName);
        if (existingMeter != null)
            Destroy(existingMeter.gameObject);

        GameObject meterObject = new GameObject(MeterObjectName, typeof(RectTransform));
        meterRoot = meterObject.GetComponent<RectTransform>();
        meterRoot.SetParent(canvas.transform, false);
        meterRoot.anchorMin = new Vector2(0.5f, 0.5f);
        meterRoot.anchorMax = new Vector2(0.5f, 0.5f);
        meterRoot.pivot = new Vector2(0.5f, 0.5f);
        meterRoot.sizeDelta = new Vector2(MeterWidth, MeterHeight);
        meterRoot.SetAsLastSibling();

        Shadow shadow = meterObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.22f);
        shadow.effectDistance = new Vector2(0f, -4f);

        RawImage barImage = CreateRawImage(BarObjectName, meterRoot, new Vector2(MeterWidth, MeterHeight), GetBarTexture(out Rect barUvRect), Color.white);
        barImage.rectTransform.anchoredPosition = Vector2.zero;
        barImage.uvRect = barUvRect;

        RawImage markerImage = CreateRawImage(MarkerObjectName, meterRoot, new Vector2(MarkerWidth, MarkerHeight), GetWhiteTexture(), new Color(0.07f, 0.07f, 0.07f, 1f));
        markerRect = markerImage.rectTransform;
        markerRect.anchoredPosition = Vector2.zero;

        Outline markerOutline = markerImage.gameObject.AddComponent<Outline>();
        markerOutline.effectColor = new Color(1f, 1f, 1f, 0.3f);
        markerOutline.effectDistance = new Vector2(1f, 0f);
    }

    void UpdateFollowPosition()
    {
        if (followTarget == null || meterRoot == null || canvasRect == null)
            return;

        Camera targetCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? Camera.main : canvas.worldCamera;
        if (targetCamera == null)
            targetCamera = Camera.main;

        Vector3 worldPosition = GetFollowWorldPosition();
        Vector3 screenPosition = targetCamera != null ? targetCamera.WorldToScreenPoint(worldPosition) : worldPosition;
        if (screenPosition.z < 0f)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCamera,
            out Vector2 localPoint);

        meterRoot.anchoredPosition = localPoint;
    }

    Vector3 GetFollowWorldPosition()
    {
        if (followCollider != null)
        {
            Bounds bounds = followCollider.bounds;
            return new Vector3(bounds.center.x, bounds.max.y + followYOffset, 0f);
        }

        return followTarget.position + new Vector3(0f, followYOffset, 0f);
    }

    RawImage CreateRawImage(string objectName, RectTransform parent, Vector2 size, Texture texture, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(RawImage));
        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = size;

        RawImage rawImage = imageObject.GetComponent<RawImage>();
        rawImage.texture = texture;
        rawImage.color = color;
        rawImage.raycastTarget = false;
        return rawImage;
    }

    Canvas FindOrCreateCanvas()
    {
        Canvas foundCanvas = null;
        GameObject uiRoot = GameObject.Find("UI");
        if (uiRoot != null)
            foundCanvas = uiRoot.GetComponent<Canvas>();

        if (foundCanvas == null)
            foundCanvas = FindObjectOfType<Canvas>();

        if (foundCanvas == null)
            foundCanvas = CreateCanvas();

        return foundCanvas;
    }

    Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "UI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas createdCanvas = canvasObject.GetComponent<Canvas>();
        createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return createdCanvas;
    }

    static Texture2D GetBarTexture(out Rect uvRect)
    {
        Texture2D resourceTexture = Resources.Load<Texture2D>(BarResourcePath);
        if (resourceTexture != null)
        {
            uvRect = new Rect(0.0605f, 0.468f, 0.884f, 0.1375f);
            return resourceTexture;
        }

        if (barTexture != null)
        {
            uvRect = new Rect(0f, 0f, 1f, 1f);
            return barTexture;
        }

        const int textureWidth = 512;
        const int textureHeight = 72;
        const int borderInset = 5;
        const int innerInset = 11;

        barTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        barTexture.wrapMode = TextureWrapMode.Clamp;
        barTexture.filterMode = FilterMode.Bilinear;

        Color shellWhite = new Color(0.97f, 0.98f, 1f, 1f);
        Color shellEdge = new Color(0.82f, 0.84f, 0.88f, 1f);

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                Color pixelColor = Color.clear;
                if (IsInsideRoundedRect(x, y, textureWidth, textureHeight, borderInset))
                {
                    if (IsInsideRoundedRect(x, y, textureWidth, textureHeight, innerInset))
                    {
                        float normalizedX = Mathf.Clamp01((x - innerInset) / (float)(textureWidth - (innerInset * 2) - 1));
                        float verticalT = y / (float)(textureHeight - 1);
                        Color gradientColor = EvaluateServeGradient(normalizedX);
                        float highlight = Mathf.Lerp(0.14f, -0.08f, verticalT);
                        pixelColor = Color.Lerp(gradientColor, Color.white, Mathf.Max(0f, highlight));
                    }
                    else
                    {
                        float distanceToEdge = DistanceToRoundedRectEdge(x, y, textureWidth, textureHeight, borderInset);
                        float edgeT = Mathf.Clamp01(distanceToEdge / 6f);
                        pixelColor = Color.Lerp(shellEdge, shellWhite, edgeT);
                    }
                }

                barTexture.SetPixel(x, y, pixelColor);
            }
        }

        barTexture.Apply();
        uvRect = new Rect(0f, 0f, 1f, 1f);
        return barTexture;
    }

    static Color EvaluateServeGradient(float t)
    {
        Color[] colors =
        {
            new Color32(236, 35, 37, 255),
            new Color32(255, 118, 25, 255),
            new Color32(255, 229, 20, 255),
            new Color32(61, 194, 38, 255),
            new Color32(255, 229, 20, 255),
            new Color32(255, 118, 25, 255),
            new Color32(236, 35, 37, 255)
        };

        float[] stops = { 0f, 0.18f, 0.36f, 0.5f, 0.64f, 0.82f, 1f };

        for (int i = 0; i < stops.Length - 1; i++)
        {
            if (t <= stops[i + 1])
            {
                float localT = Mathf.InverseLerp(stops[i], stops[i + 1], t);
                return Color.Lerp(colors[i], colors[i + 1], localT);
            }
        }

        return colors[colors.Length - 1];
    }

    static bool IsInsideRoundedRect(int x, int y, int width, int height, int inset)
    {
        float rectWidth = width - inset * 2f;
        float rectHeight = height - inset * 2f;
        float radius = rectHeight * 0.5f;
        Vector2 point = new Vector2(x - width * 0.5f + 0.5f, y - height * 0.5f + 0.5f);
        Vector2 halfSize = new Vector2(rectWidth * 0.5f, rectHeight * 0.5f);
        Vector2 q = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - (halfSize - new Vector2(radius, radius));
        Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
        return outside.sqrMagnitude <= radius * radius;
    }

    static float DistanceToRoundedRectEdge(int x, int y, int width, int height, int inset)
    {
        float rectWidth = width - inset * 2f;
        float rectHeight = height - inset * 2f;
        float radius = rectHeight * 0.5f;
        Vector2 point = new Vector2(x - width * 0.5f + 0.5f, y - height * 0.5f + 0.5f);
        Vector2 halfSize = new Vector2(rectWidth * 0.5f, rectHeight * 0.5f);
        Vector2 q = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - (halfSize - new Vector2(radius, radius));
        Vector2 outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f));
        float outsideDistance = outside.magnitude;
        float insideDistance = Mathf.Min(Mathf.Max(q.x, q.y), 0f);
        return radius - (outsideDistance + insideDistance);
    }

    static Texture2D GetWhiteTexture()
    {
        if (whiteTexture != null)
            return whiteTexture;

        whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
        whiteTexture.wrapMode = TextureWrapMode.Clamp;
        whiteTexture.filterMode = FilterMode.Bilinear;
        return whiteTexture;
    }
}
