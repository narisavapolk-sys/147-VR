using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime-generated scoreboard (no prefab needed). Shows on screen:
///   - Player 1 vs Player 2 scores
///   - What ball is "on" and how many reds remain
///   - The last ball potted (ball, pocket, points, which player)
///
/// Uses the legacy uGUI Text with the built-in font so it works in every project and
/// in batch/editor tooling without a TextMeshPro font asset. Subscribes to
/// SnookerScoreManager events and rebuilds the text on change.
/// </summary>
public sealed class SnookerScoreUI : MonoBehaviour
{
    [Header("Layout")]
    [Tooltip("Canvas sort order (higher = on top).")]
    public int canvasSortOrder = 10;
    [Tooltip("Font size for the scoreboard text.")]
    public int fontSize = 28;

    [Header("Dependencies")]
    public SnookerScoreManager scoreManager;
    public SnookerTurnManager turnManager;

    private Text _scoreText;
    private Text _lastPotText;
    private Canvas _canvas;
    private bool _built;

    private string _lastPotMessage = "—";

    private void Start()
    {
        EnsureBuilt();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    /// <summary>Public wrapper for tests/editor tooling (Start may not run in batch).</summary>
    public void EnsureBuiltPublic() => EnsureBuilt();

    private void EnsureBuilt()
    {
        if (_built)
            return;

        if (scoreManager == null)
            scoreManager = GetComponent<SnookerScoreManager>();
        if (turnManager == null)
            turnManager = GetComponent<SnookerTurnManager>();

        BuildCanvas();
        Subscribe();
        _built = true;
        Refresh();
    }

    private void BuildCanvas()
    {
        // Canvas
        GameObject canvasGO = new GameObject("Snooker Score UI (runtime)");
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = canvasSortOrder;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Background panel (top-left)
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(16f, -16f);
        panelRect.sizeDelta = new Vector2(560f, 150f);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Score line
        GameObject scoreGO = new GameObject("Score Text");
        scoreGO.transform.SetParent(panel.transform, false);
        RectTransform scoreRect = scoreGO.AddComponent<RectTransform>();
        scoreRect.anchorMin = Vector2.zero;
        scoreRect.anchorMax = Vector2.one;
        scoreRect.offsetMin = new Vector2(16f, 55f);
        scoreRect.offsetMax = new Vector2(-16f, -10f);
        _scoreText = scoreGO.AddComponent<Text>();
        _scoreText.font = font;
        _scoreText.fontSize = fontSize;
        _scoreText.color = Color.white;
        _scoreText.alignment = TextAnchor.MiddleLeft;
        _scoreText.horizontalOverflow = HorizontalWrapMode.Overflow;

        // Last pot line
        GameObject lastGO = new GameObject("Last Pot Text");
        lastGO.transform.SetParent(panel.transform, false);
        RectTransform lastRect = lastGO.AddComponent<RectTransform>();
        lastRect.anchorMin = Vector2.zero;
        lastRect.anchorMax = Vector2.one;
        lastRect.offsetMin = new Vector2(16f, 10f);
        lastRect.offsetMax = new Vector2(-16f, 55f);
        _lastPotText = lastGO.AddComponent<Text>();
        _lastPotText.font = font;
        _lastPotText.fontSize = fontSize - 4;
        _lastPotText.color = new Color(0.9f, 0.85f, 0.6f);
        _lastPotText.alignment = TextAnchor.MiddleLeft;
        _lastPotText.horizontalOverflow = HorizontalWrapMode.Overflow;
    }

    private void Subscribe()
    {
        if (scoreManager != null)
        {
            scoreManager.ScoresChanged += OnScoresChanged;
            scoreManager.PotRecorded += OnPotRecorded;
        }
        else
        {
            Debug.LogWarning("[ScoreUI] No SnookerScoreManager — UI will stay blank.");
        }
    }

    private void Unsubscribe()
    {
        if (scoreManager != null)
        {
            scoreManager.ScoresChanged -= OnScoresChanged;
            scoreManager.PotRecorded -= OnPotRecorded;
        }
    }

    private void OnScoresChanged()
    {
        Refresh();
    }

    private void OnPotRecorded(string ballName, string pocketName, int points, int player)
    {
        string who = player == 1 ? "Player 1" : "Player 2";
        _lastPotMessage = $"{who} scored {points} points — {ballName} into {pocketName}";
        Refresh();
    }

    private void Refresh()
    {
        if (!_built)
            return;

        int p1 = scoreManager != null ? scoreManager.Player1Score : 0;
        int p2 = scoreManager != null ? scoreManager.Player2Score : 0;
        string ballOn = scoreManager != null ? scoreManager.BallOnName() : "?";
        int reds = scoreManager != null ? scoreManager.redsRemaining : 0;
        int turn = turnManager != null ? turnManager.currentPlayer : 0;

        string turnTag = turn == 0 ? "" : $"  |  Turn: Player {turn}";
        _scoreText.text = $"Player 1: {p1}   vs   Player 2: {p2}   |   Ball On: {ballOn}   Reds: {reds}{turnTag}";
        _lastPotText.text = "Last Pot: " + _lastPotMessage;
    }
}
