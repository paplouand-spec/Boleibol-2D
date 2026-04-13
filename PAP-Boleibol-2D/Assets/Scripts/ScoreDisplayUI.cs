using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreDisplayUI : MonoBehaviour
{
    const string ScoreObjectName = "ScoreText";

    private BallController ballController;
    private TextMeshProUGUI scoreText;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateScoreDisplay()
    {
        if (FindObjectOfType<ScoreDisplayUI>() != null)
            return;

        if (FindObjectOfType<BallController>() == null)
            return;

        GameObject displayObject = new GameObject(nameof(ScoreDisplayUI));
        displayObject.AddComponent<ScoreDisplayUI>();
    }

    void Start()
    {
        ballController = FindObjectOfType<BallController>();
        if (ballController == null)
        {
            Destroy(gameObject);
            return;
        }

        scoreText = FindOrCreateScoreText();
        if (scoreText == null)
        {
            Destroy(gameObject);
            return;
        }

        ballController.ScoreChanged += HandleScoreChanged;
        UpdateScoreText(ballController.PlayerScore, ballController.BotScore);
    }

    void OnDestroy()
    {
        if (ballController != null)
            ballController.ScoreChanged -= HandleScoreChanged;
    }

    void HandleScoreChanged(int playerScore, int botScore)
    {
        UpdateScoreText(playerScore, botScore);
    }

    void UpdateScoreText(int playerScore, int botScore)
    {
        if (scoreText == null)
            return;

        scoreText.text = $"{playerScore}  -  {botScore}";
    }

    TextMeshProUGUI FindOrCreateScoreText()
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

        Transform existingText = canvas.transform.Find(ScoreObjectName);
        if (existingText != null)
        {
            TextMeshProUGUI existingScoreText = existingText.GetComponent<TextMeshProUGUI>();
            if (existingScoreText != null)
            {
                ConfigureScoreText(existingScoreText);
                existingScoreText.rectTransform.SetAsLastSibling();
                return existingScoreText;
            }
        }

        GameObject textObject = new GameObject(ScoreObjectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.SetParent(canvas.transform, false);
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.anchoredPosition = new Vector2(0f, -18f);
        rectTransform.sizeDelta = new Vector2(260f, 60f);
        rectTransform.SetAsLastSibling();

        TextMeshProUGUI createdScoreText = textObject.GetComponent<TextMeshProUGUI>();
        ConfigureScoreText(createdScoreText);
        textObject.AddComponent<Shadow>().effectDistance = new Vector2(2f, -2f);
        return createdScoreText;
    }

    void ConfigureScoreText(TextMeshProUGUI targetText)
    {
        if (TMP_Settings.defaultFontAsset != null)
            targetText.font = TMP_Settings.defaultFontAsset;

        targetText.fontSize = 36f;
        targetText.alignment = TextAlignmentOptions.Center;
        targetText.color = Color.white;
        targetText.enableWordWrapping = false;
        targetText.raycastTarget = false;
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
}
