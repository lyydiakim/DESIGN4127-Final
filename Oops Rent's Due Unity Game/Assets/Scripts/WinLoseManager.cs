using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles win/lose screens.
/// Win  : call OnRentPaid() — wire this to the Home station's "On Successful Trade" event.
/// Lose : called automatically by EventCardManager when the round limit is reached.
/// </summary>
public class WinLoseManager : MonoBehaviour
{
    public static WinLoseManager Instance { get; private set; }
    public bool IsEndScreenShowing => _gameOver && _endCanvas != null && _endCanvas.gameObject.activeInHierarchy;

    [Header("Game End Sprites")]
    [SerializeField] private Sprite winSprite;
    [SerializeField] private Sprite loseSprite;
    [SerializeField] private Sprite endScreenSprite;

    [Header("Presentation")]
    [Tooltip("Higher than ResourceManager_Canvas (0) so the result card draws on top of all UI.")]
    [SerializeField] private int endScreenCanvasSortOrder = 10000;
    private const int MinEndScreenSortOrder = 40000;
    [Tooltip("Optional fallback TMP font for runtime-generated end-screen text.")]
    [SerializeField] private TMP_FontAsset fallbackFont;

    private bool _gameOver = false;
    private Canvas _endCanvas;
    private Image _endImage;
    private TextMeshProUGUI _resultText;
    private readonly TextMeshProUGUI[] _playerColumnTexts = new TextMeshProUGUI[4];
    private bool _teamPaidRentOnTime;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Screen Space Overlay so the card is composited above other canvases (not behind world/UI).
        var canvasGo = new GameObject("EndScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        _endCanvas = canvasGo.GetComponent<Canvas>();
        _endCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _endCanvas.overrideSorting = true;
        _endCanvas.sortingOrder = Mathf.Max(endScreenCanvasSortOrder, MinEndScreenSortOrder);

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var imgGo = new GameObject("EndScreenImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imgGo.transform.SetParent(canvasGo.transform, false);
        var rt = imgGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _endImage = imgGo.GetComponent<Image>();
        _endImage.raycastTarget = true;
        _endImage.preserveAspect = true;

        _resultText = CreateResultText(canvasGo.transform);
        for (int i = 0; i < 4; i++)
            _playerColumnTexts[i] = CreatePlayerColumnText(canvasGo.transform, i);
        EnsureEndTextFonts();

        canvasGo.SetActive(false);
    }

    /// <summary>
    /// Wire this to the Home station's "On Successful Trade" UnityEvent.
    /// </summary>
    public void OnRentPaid()
    {
        if (_gameOver) return;
        _teamPaidRentOnTime = true;
        ShowEndScreen(endScreenSprite != null ? endScreenSprite : winSprite);
        Debug.Log("[WinLoseManager] Rent paid — players WIN!");
    }

    /// <summary>
    /// Called automatically by EventCardManager when the round limit is reached.
    /// </summary>
    public void TriggerLose()
    {
        if (_gameOver) return;
        _teamPaidRentOnTime = false;
        ShowEndScreen(endScreenSprite != null ? endScreenSprite : loseSprite);
        Debug.Log("[WinLoseManager] Round limit reached without rent paid — players LOSE.");
    }

    private void ShowEndScreen(Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogWarning("[WinLoseManager] No sprite assigned for this outcome.", this);
            return;
        }

        _gameOver = true;
        Time.timeScale = 0f;

        _endImage.sprite = sprite;
        RenderEndSummary();
        _endCanvas.overrideSorting = true;
        _endCanvas.sortingOrder = Mathf.Max(endScreenCanvasSortOrder, MinEndScreenSortOrder);
        _endCanvas.gameObject.SetActive(true);
        _endCanvas.transform.SetAsLastSibling();
    }

    private void RenderEndSummary()
    {
        if (_resultText != null)
        {
            _resultText.text = _teamPaidRentOnTime
                ? "<size=120%><b>Great Job! Your lease was extended for another month!</b></size>"
                : "<size=120%><b>Aw shucks...you got evicted!</b></size>";
        }

        for (int i = 0; i < _playerColumnTexts.Length; i++)
        {
            var t = _playerColumnTexts[i];
            if (t == null) continue;
            t.text = BuildPlayerColumn(i);
        }
    }

    private static string BuildPlayerColumn(int playerIndex)
    {
        var r = PlayerGameStats.GetReport(playerIndex);
        return $"<b>Player {playerIndex + 1}</b>\n\n" +
               $"Rent contribution: {r.RentContribution}\n\n" +
               $"Money spent: {r.MoneySpent}\n\n" +
               $"Resources earned:\n" +
               $"  Money +{r.EarnedMoney}\n" +
               $"  Energy +{r.EarnedEnergy}\n" +
               $"  Network +{r.EarnedNetwork}\n\n" +
               $"Factory worked: {r.WorkedFactoryCount}x\n" +
               $"Art Store worked: {r.WorkedArtStoreCount}x\n" +
               $"Deli worked: {r.WorkedDeliCount}x\n" +
               $"Grocery worked: {r.WorkedGroceryCount}x";
    }

    private TextMeshProUGUI CreateResultText(Transform parent)
    {
        var go = new GameObject("EndResultText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        const float refW = 1920f;
        const float refH = 1080f;
        const float leftPx = 115f;
        const float rightPx = 1805f;
        const float topPx = 900f;
        const float bottomPx = 760f;
        rt.anchorMin = new Vector2(leftPx / refW, bottomPx / refH);
        rt.anchorMax = new Vector2(rightPx / refW, topPx / refH);
        rt.offsetMin = new Vector2(30f, 0f); // Move game result block right by 30px.
        rt.offsetMax = new Vector2(30f, 0f);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        ConfigureText(tmp, 36f, TextAlignmentOptions.Top);
        return tmp;
    }

    private TextMeshProUGUI CreatePlayerColumnText(Transform parent, int playerIndex)
    {
        var go = new GameObject($"EndPlayer{playerIndex + 1}Column", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        const float sectionWidthPx = 1470f;
        const float gapPx = 8f;
        const float playerHeightPx = 550f;
        const float centerY = -155f; // Places columns below game result region.

        float colWidthPx = (sectionWidthPx - (3f * gapPx)) / 4f;
        float step = colWidthPx + gapPx;
        float firstCenterX = -(sectionWidthPx * 0.5f) + (colWidthPx * 0.5f);
        float centerX = firstCenterX + (playerIndex * step);

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(colWidthPx, playerHeightPx);
        rt.anchoredPosition = new Vector2(centerX, centerY);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        ConfigureText(tmp, 24f, TextAlignmentOptions.TopLeft);
        return tmp;
    }

    private void ConfigureText(TextMeshProUGUI tmp, float fontSize, TextAlignmentOptions align)
    {
        ApplyFallbackFontIfMissing(tmp);
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.fontSize = fontSize;
        tmp.raycastTarget = false;
        tmp.color = new Color(0.08f, 0.08f, 0.08f, 1f);
    }

    private void EnsureEndTextFonts()
    {
        ApplyFallbackFontIfMissing(_resultText);
        for (int i = 0; i < _playerColumnTexts.Length; i++)
            ApplyFallbackFontIfMissing(_playerColumnTexts[i]);
    }

    private void ApplyFallbackFontIfMissing(TMP_Text text)
    {
        if (text == null || text.font != null) return;

        TMP_FontAsset resolved = fallbackFont;
        if (resolved == null)
            resolved = TMP_Settings.defaultFontAsset;
        if (resolved == null)
            resolved = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (resolved == null)
            resolved = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
        if (resolved == null) return;

        text.font = resolved;
        if (resolved.material != null)
            text.fontSharedMaterial = resolved.material;
    }

    private void Update()
    {
        if (!_gameOver) return;
        if (!IsRestartPressed()) return;
        RestartGame();
    }

    private static bool IsRestartPressed()
    {
        bool keyboard = Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame;
        bool gamepad = Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame;
        return keyboard || gamepad;
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        PlayerGameStats.ResetAll();
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
}
