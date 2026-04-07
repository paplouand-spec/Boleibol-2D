using UnityEngine;
using UnityEngine.UI;

public class ServeChargeUI : MonoBehaviour
{
    const string MeterObjectName = "ServeChargeMeter";
    const string MarkerObjectName = "Marker";
    const float MeterWidth = 360f;
    const float TrackHeight = 24f;
    const float MarkerWidth = 5f;
    const float MarkerHeight = 34f;

    private static Texture2D sharedWhiteTexture;

    private RectTransform meterRoot;
    private RectTransform trackRect;
    private RawImage markerImage;

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

    public void SetVisible(bool isVisible)
    {
        EnsureMeter();

        if (meterRoot != null)
            meterRoot.gameObject.SetActive(isVisible);
    }

    public void SetMarkerNormalized(float normalizedValue)
    {
        EnsureMeter();

        if (trackRect == null || markerImage == null)
            return;

        float clampedValue = Mathf.Clamp01(normalizedValue);
        float leftEdge = (-trackRect.sizeDelta.x * 0.5f) + (MarkerWidth * 0.5f);
        float rightEdge = (trackRect.sizeDelta.x * 0.5f) - (MarkerWidth * 0.5f);

        RectTransform markerRect = markerImage.rectTransform;
        markerRect.anchoredPosition = new Vector2(Mathf.Lerp(leftEdge, rightEdge, clampedValue), 0f);
    }

    void EnsureMeter()
    {
        if (meterRoot != null && markerImage != null && trackRect != null)
            return;

        Canvas canvas = FindOrCreateCanvas();
        if (canvas == null)
            return;

        Transform existingMeter = canvas.transform.Find(MeterObjectName);
        if (existingMeter != null)
        {
            meterRoot = existingMeter as RectTransform;
            trackRect = meterRoot != null ? meterRoot.Find("Track") as RectTransform : null;

            Transform existingMarker = meterRoot != null ? meterRoot.Find(MarkerObjectName) : null;
            markerImage = existingMarker != null ? existingMarker.GetComponent<RawImage>() : null;
            return;
        }

        GameObject meterObject = new GameObject(MeterObjectName, typeof(RectTransform));
        meterRoot = meterObject.GetComponent<RectTransform>();
        meterRoot.SetParent(canvas.transform, false);
        meterRoot.anchorMin = new Vector2(0.5f, 0f);
        meterRoot.anchorMax = new Vector2(0.5f, 0f);
        meterRoot.pivot = new Vector2(0.5f, 0f);
        meterRoot.anchoredPosition = new Vector2(0f, 34f);
        meterRoot.sizeDelta = new Vector2(MeterWidth + 48f, 64f);
        meterRoot.SetAsLastSibling();

        RawImage shell = CreateBlock("Shell", meterRoot, new Vector2(MeterWidth + 48f, 48f), new Color(1f, 1f, 1f, 0.95f));
        shell.rectTransform.anchoredPosition = new Vector2(0f, 20f);

        RawImage shadow = CreateBlock("Shadow", meterRoot, new Vector2(MeterWidth + 26f, 30f), new Color(0f, 0f, 0f, 0.14f));
        shadow.rectTransform.anchoredPosition = new Vector2(0f, 18f);
        shadow.rectTransform.SetAsFirstSibling();

        RawImage track = CreateBlock("Track", meterRoot, new Vector2(MeterWidth, TrackHeight), Color.white);
        trackRect = track.rectTransform;
        trackRect.anchoredPosition = new Vector2(0f, 20f);

        CreateSegments(trackRect);

        markerImage = CreateBlock(MarkerObjectName, meterRoot, new Vector2(MarkerWidth, MarkerHeight), new Color(0.08f, 0.08f, 0.08f, 1f));
        markerImage.rectTransform.anchoredPosition = new Vector2(0f, 20f);
    }

    void CreateSegments(RectTransform parent)
    {
        Color[] segmentColors =
        {
            new Color32(244, 67, 54, 255),
            new Color32(255, 152, 0, 255),
            new Color32(255, 235, 59, 255),
            new Color32(156, 204, 101, 255),
            new Color32(76, 175, 80, 255)
        };

        float spacing = 4f;
        float segmentWidth = (MeterWidth - (spacing * (segmentColors.Length - 1))) / segmentColors.Length;
        float startX = (-MeterWidth * 0.5f) + (segmentWidth * 0.5f);

        for (int i = 0; i < segmentColors.Length; i++)
        {
            RawImage segment = CreateBlock($"Segment{i}", parent, new Vector2(segmentWidth, TrackHeight), segmentColors[i]);
            segment.rectTransform.anchoredPosition = new Vector2(startX + (i * (segmentWidth + spacing)), 0f);
        }
    }

    RawImage CreateBlock(string objectName, RectTransform parent, Vector2 size, Color color)
    {
        GameObject blockObject = new GameObject(objectName, typeof(RectTransform), typeof(RawImage));
        RectTransform blockRect = blockObject.GetComponent<RectTransform>();
        blockRect.SetParent(parent, false);
        blockRect.anchorMin = new Vector2(0.5f, 0.5f);
        blockRect.anchorMax = new Vector2(0.5f, 0.5f);
        blockRect.pivot = new Vector2(0.5f, 0.5f);
        blockRect.sizeDelta = size;

        RawImage rawImage = blockObject.GetComponent<RawImage>();
        rawImage.texture = GetWhiteTexture();
        rawImage.color = color;
        return rawImage;
    }

    Canvas FindOrCreateCanvas()
    {
        Canvas canvas = null;
        GameObject uiRoot = GameObject.Find("UI");
        if (uiRoot != null)
            canvas = uiRoot.GetComponent<Canvas>();

        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();

        if (canvas == null)
            canvas = CreateCanvas();

        return canvas;
    }

    Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "UI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    static Texture2D GetWhiteTexture()
    {
        if (sharedWhiteTexture != null)
            return sharedWhiteTexture;

        sharedWhiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        sharedWhiteTexture.SetPixel(0, 0, Color.white);
        sharedWhiteTexture.Apply();
        return sharedWhiteTexture;
    }
}
