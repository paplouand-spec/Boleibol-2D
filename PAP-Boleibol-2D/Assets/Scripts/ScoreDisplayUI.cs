using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreDisplayUI : MonoBehaviour
{
    const string HudRootName = "ScoreHUD";
    const string LegacyScoreObjectName = "ScoreText";
    const string PlayerPanelName = "PlayerPanel";
    const string BotPanelName = "BotPanel";
    const string PlayerScoreName = "PlayerScoreValue";
    const string BotScoreName = "BotScoreValue";
    const string PointBannerName = "PointBanner";

    static Sprite solidSprite;

    readonly Color backgroundBlue = new Color(0.06f, 0.19f, 0.43f, 0.95f);
    readonly Color accentBlue = new Color(0.12f, 0.36f, 0.76f, 1f);
    readonly Color accentYellow = new Color(0.96f, 0.82f, 0.24f, 1f);
    readonly Color panelWhite = new Color(0.985f, 0.995f, 1f, 0.98f);
    readonly Color softWhite = new Color(1f, 1f, 1f, 0.9f);
    readonly Color darkText = new Color(0.07f, 0.17f, 0.34f, 1f);
    readonly Color bannerBackground = new Color(0.03f, 0.08f, 0.18f, 0.6f);
    readonly Color bannerHighlight = new Color(1f, 1f, 1f, 0.18f);

    private BallController ballController;
    private RectTransform hudRoot;
    private RectTransform playerPanel;
    private RectTransform botPanel;
    private TextMeshProUGUI playerScoreText;
    private TextMeshProUGUI botScoreText;
    private RectTransform pointBanner;
    private CanvasGroup pointBannerCanvasGroup;
    private TextMeshProUGUI pointBannerText;
    private int displayedPlayerScore = -1;
    private int displayedBotScore = -1;
    private Coroutine playerPulseRoutine;
    private Coroutine botPulseRoutine;
    private Coroutine pointBannerRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateScoreDisplay()
    {
        EnsureInstance();
    }

    public static ScoreDisplayUI EnsureInstance()
    {
        ScoreDisplayUI existingDisplay = FindObjectOfType<ScoreDisplayUI>();
        if (existingDisplay != null)
            return existingDisplay;

        if (FindObjectOfType<BallController>() == null)
            return null;

        GameObject displayObject = new GameObject(nameof(ScoreDisplayUI));
        return displayObject.AddComponent<ScoreDisplayUI>();
    }

    void Start()
    {
        ballController = FindObjectOfType<BallController>();
        if (ballController == null)
        {
            Destroy(gameObject);
            return;
        }

        hudRoot = FindOrCreateScoreHud();
        if (hudRoot == null || playerScoreText == null || botScoreText == null)
        {
            Destroy(gameObject);
            return;
        }

        ballController.ScoreChanged += HandleScoreChanged;
        ballController.PointBannerRequested += HandlePointScored;
        UpdateScoreText(ballController.PlayerScore, ballController.BotScore);
    }

    void OnDestroy()
    {
        if (ballController != null)
        {
            ballController.ScoreChanged -= HandleScoreChanged;
            ballController.PointBannerRequested -= HandlePointScored;
        }
    }

    void HandleScoreChanged(int playerScore, int botScore)
    {
        UpdateScoreText(playerScore, botScore);
    }

    void HandlePointScored(BallController.TeamSide winningTeam)
    {
        if (winningTeam == BallController.TeamSide.Player)
            ShowPointBanner("Ponto do Jogador");
        else if (winningTeam == BallController.TeamSide.Bot)
            ShowPointBanner("Ponto do Adversário");
    }

    void UpdateScoreText(int playerScore, int botScore)
    {
        if (playerScoreText == null || botScoreText == null)
            return;

        bool playerChanged = displayedPlayerScore >= 0 && displayedPlayerScore != playerScore;
        bool botChanged = displayedBotScore >= 0 && displayedBotScore != botScore;

        displayedPlayerScore = playerScore;
        displayedBotScore = botScore;

        playerScoreText.text = playerScore.ToString();
        botScoreText.text = botScore.ToString();

        if (playerChanged && playerPanel != null)
            playerPulseRoutine = RestartPulse(playerPulseRoutine, playerPanel);

        if (botChanged && botPanel != null)
            botPulseRoutine = RestartPulse(botPulseRoutine, botPanel);
    }

    RectTransform FindOrCreateScoreHud()
    {
        Canvas canvas = null;
        GameObject uiRoot = GameObject.Find("UI");
        if (uiRoot != null)
            canvas = uiRoot.GetComponent<Canvas>();

        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();

        if (canvas == null)
            canvas = CreateCanvas();

        if (canvas == null)
            return null;

        Transform legacyScore = canvas.transform.Find(LegacyScoreObjectName);
        if (legacyScore != null)
            Destroy(legacyScore.gameObject);

        Transform existingHud = canvas.transform.Find(HudRootName);
        if (existingHud != null)
            Destroy(existingHud.gameObject);

        return CreateHud(canvas.transform);
    }

    RectTransform CreateHud(Transform parent)
    {
        RectTransform root = CreatePanel(parent, HudRootName, new Vector2(460f, 124f), backgroundBlue);
        root.anchorMin = new Vector2(0.5f, 1f);
        root.anchorMax = new Vector2(0.5f, 1f);
        root.pivot = new Vector2(0.5f, 1f);
        root.anchoredPosition = new Vector2(0f, -22f);

        Image rootImage = root.GetComponent<Image>();
        rootImage.raycastTarget = false;

        Outline rootOutline = root.gameObject.AddComponent<Outline>();
        rootOutline.effectColor = new Color(1f, 1f, 1f, 0.22f);
        rootOutline.effectDistance = new Vector2(2f, -2f);

        Shadow rootShadow = root.gameObject.AddComponent<Shadow>();
        rootShadow.effectColor = new Color(0f, 0.05f, 0.15f, 0.38f);
        rootShadow.effectDistance = new Vector2(0f, -8f);

        RectTransform topStripe = CreatePanel(root, "TopStripe", new Vector2(0f, 12f), accentYellow);
        ConfigureHorizontalBand(topStripe, 12f, true);

        RectTransform bottomGlow = CreatePanel(root, "BottomGlow", new Vector2(0f, 6f), new Color(1f, 1f, 1f, 0.13f));
        ConfigureHorizontalBand(bottomGlow, 6f, false);

        CreateText(
            root,
            "Title",
            "PLACAR",
            18f,
            softWhite,
            TextAlignmentOptions.Center,
            FontStyles.Bold,
            new Vector2(0f, -10f),
            new Vector2(140f, 24f));

        playerPanel = CreatePanel(root, PlayerPanelName, new Vector2(144f, 72f), accentYellow);
        playerPanel.anchorMin = new Vector2(0.5f, 0.5f);
        playerPanel.anchorMax = new Vector2(0.5f, 0.5f);
        playerPanel.pivot = new Vector2(0.5f, 0.5f);
        playerPanel.anchoredPosition = new Vector2(-126f, -14f);
        AddPanelShadow(playerPanel, new Color(0.34f, 0.28f, 0.05f, 0.28f));

        CreateText(
            playerPanel,
            "PlayerLabel",
            "Jogador",
            16f,
            darkText,
            TextAlignmentOptions.Center,
            FontStyles.Bold,
            new Vector2(0f, 18f),
            new Vector2(120f, 20f));
        playerScoreText = CreateText(
            playerPanel,
            PlayerScoreName,
            "0",
            44f,
            darkText,
            TextAlignmentOptions.Center,
            FontStyles.Bold,
            new Vector2(0f, -8f),
            new Vector2(120f, 46f));

        RectTransform centerBadge = CreatePanel(root, "CenterBadge", new Vector2(88f, 44f), accentBlue);
        centerBadge.anchorMin = new Vector2(0.5f, 0.5f);
        centerBadge.anchorMax = new Vector2(0.5f, 0.5f);
        centerBadge.pivot = new Vector2(0.5f, 0.5f);
        centerBadge.anchoredPosition = new Vector2(0f, -8f);

        Outline centerOutline = centerBadge.gameObject.AddComponent<Outline>();
        centerOutline.effectColor = new Color(1f, 1f, 1f, 0.22f);
        centerOutline.effectDistance = new Vector2(1f, -1f);

        CreateText(
            centerBadge,
            "CenterText",
            "VS",
            24f,
            Color.white,
            TextAlignmentOptions.Center,
            FontStyles.Bold,
            Vector2.zero,
            new Vector2(72f, 36f));

        botPanel = CreatePanel(root, BotPanelName, new Vector2(144f, 72f), panelWhite);
        botPanel.anchorMin = new Vector2(0.5f, 0.5f);
        botPanel.anchorMax = new Vector2(0.5f, 0.5f);
        botPanel.pivot = new Vector2(0.5f, 0.5f);
        botPanel.anchoredPosition = new Vector2(126f, -14f);
        AddPanelShadow(botPanel, new Color(0.03f, 0.12f, 0.28f, 0.22f));

        RectTransform botAccent = CreatePanel(botPanel, "BotAccent", new Vector2(0f, 8f), accentBlue);
        ConfigureHorizontalBand(botAccent, 8f, true);

        CreateText(
            botPanel,
            "BotLabel",
            "Adversário",
            16f,
            accentBlue,
            TextAlignmentOptions.Center,
            FontStyles.Bold,
            new Vector2(0f, 18f),
            new Vector2(120f, 20f));
        botScoreText = CreateText(
            botPanel,
            BotScoreName,
            "0",
            44f,
            accentBlue,
            TextAlignmentOptions.Center,
            FontStyles.Bold,
            new Vector2(0f, -8f),
            new Vector2(120f, 46f));

        pointBanner = CreatePanel(root, PointBannerName, new Vector2(312f, 52f), bannerBackground);
        pointBanner.anchorMin = new Vector2(0.5f, 1f);
        pointBanner.anchorMax = new Vector2(0.5f, 1f);
        pointBanner.pivot = new Vector2(0.5f, 1f);
        pointBanner.anchoredPosition = new Vector2(0f, -138f);

        Outline bannerOutline = pointBanner.gameObject.AddComponent<Outline>();
        bannerOutline.effectColor = bannerHighlight;
        bannerOutline.effectDistance = new Vector2(1f, -1f);

        Shadow bannerShadow = pointBanner.gameObject.AddComponent<Shadow>();
        bannerShadow.effectColor = new Color(0f, 0f, 0f, 0.28f);
        bannerShadow.effectDistance = new Vector2(0f, -6f);

        pointBannerCanvasGroup = pointBanner.gameObject.AddComponent<CanvasGroup>();
        pointBannerCanvasGroup.alpha = 0f;
        pointBannerCanvasGroup.blocksRaycasts = false;
        pointBannerCanvasGroup.interactable = false;

        pointBannerText = CreateText(
            pointBanner,
            "PointBannerText",
            string.Empty,
            22f,
            Color.white,
            TextAlignmentOptions.Center,
            FontStyles.Bold,
            Vector2.zero,
            new Vector2(280f, 34f));

        return root;
    }

    RectTransform CreatePanel(Transform parent, string objectName, Vector2 size, Color color)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.sizeDelta = size;

        Image image = panelObject.GetComponent<Image>();
        image.sprite = GetSolidSprite();
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return rectTransform;
    }

    TextMeshProUGUI CreateText(
        Transform parent,
        string objectName,
        string content,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        FontStyles fontStyle,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;

        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.fontStyle = fontStyle;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        text.margin = new Vector4(8f, 2f, 8f, 2f);
        return text;
    }

    void AddPanelShadow(RectTransform panel, Color shadowColor)
    {
        Shadow shadow = panel.gameObject.AddComponent<Shadow>();
        shadow.effectColor = shadowColor;
        shadow.effectDistance = new Vector2(0f, -5f);
    }

    void ConfigureHorizontalBand(RectTransform rectTransform, float height, bool topAligned)
    {
        float anchorY = topAligned ? 1f : 0f;
        float pivotY = topAligned ? 1f : 0f;

        rectTransform.anchorMin = new Vector2(0f, anchorY);
        rectTransform.anchorMax = new Vector2(1f, anchorY);
        rectTransform.pivot = new Vector2(0.5f, pivotY);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.offsetMin = new Vector2(0f, topAligned ? -height : 0f);
        rectTransform.offsetMax = new Vector2(0f, topAligned ? 0f : height);
    }

    Coroutine RestartPulse(Coroutine routine, RectTransform target)
    {
        if (routine != null)
            StopCoroutine(routine);

        return StartCoroutine(PulseRect(target));
    }

    IEnumerator PulseRect(RectTransform target)
    {
        if (target == null)
            yield break;

        Vector3 baseScale = Vector3.one;
        Vector3 peakScale = new Vector3(1.1f, 1.1f, 1f);
        float duration = 0.22f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - normalized, 3f);
            float pulse = Mathf.Sin(eased * Mathf.PI);
            target.localScale = Vector3.LerpUnclamped(baseScale, peakScale, pulse);
            yield return null;
        }

        target.localScale = baseScale;
    }

    void ShowPointBanner(string message)
    {
        if (pointBannerText == null || pointBannerCanvasGroup == null)
            return;

        pointBannerText.text = message;

        if (pointBannerRoutine != null)
            StopCoroutine(pointBannerRoutine);

        pointBannerRoutine = StartCoroutine(AnimatePointBanner());
    }

    IEnumerator AnimatePointBanner()
    {
        if (pointBanner == null || pointBannerCanvasGroup == null)
            yield break;

        const float fadeInDuration = 0.16f;
        const float holdDuration = 1.15f;
        const float fadeOutDuration = 0.28f;

        Vector3 hiddenScale = new Vector3(0.96f, 0.96f, 1f);
        Vector3 shownScale = Vector3.one;

        pointBanner.localScale = hiddenScale;
        pointBannerCanvasGroup.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            pointBannerCanvasGroup.alpha = Mathf.LerpUnclamped(0f, 1f, eased);
            pointBanner.localScale = Vector3.LerpUnclamped(hiddenScale, shownScale, eased);
            yield return null;
        }

        pointBannerCanvasGroup.alpha = 1f;
        pointBanner.localScale = shownScale;
        yield return new WaitForSecondsRealtime(holdDuration);

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            float eased = t * t;
            pointBannerCanvasGroup.alpha = Mathf.Lerp(1f, 0f, eased);
            pointBanner.localScale = Vector3.LerpUnclamped(shownScale, hiddenScale, eased);
            yield return null;
        }

        pointBannerCanvasGroup.alpha = 0f;
        pointBanner.localScale = hiddenScale;
        pointBannerRoutine = null;
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

    static Sprite GetSolidSprite()
    {
        if (solidSprite != null)
            return solidSprite;

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;

        solidSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
        solidSprite.hideFlags = HideFlags.HideAndDontSave;
        return solidSprite;
    }
}
