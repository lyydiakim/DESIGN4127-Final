using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles win/lose screens.
/// Win  : call OnRentPaid() — wire this to the Home station's "On Successful Trade" event.
/// Lose : called automatically by EventCardManager when the round limit is reached.
/// </summary>
public class WinLoseManager : MonoBehaviour
{
    public static WinLoseManager Instance { get; private set; }

    [Header("Game End Sprites")]
    [SerializeField] private Sprite winSprite;
    [SerializeField] private Sprite loseSprite;

    [Header("Presentation")]
    [Tooltip("Higher than ResourceManager_Canvas (0) so the result card draws on top of all UI.")]
    [SerializeField] private int endScreenCanvasSortOrder = 10000;

    private bool _gameOver = false;
    private Canvas _endCanvas;
    private Image _endImage;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Screen Space Overlay so the card is composited above other canvases (not behind world/UI).
        var canvasGo = new GameObject("EndScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        _endCanvas = canvasGo.GetComponent<Canvas>();
        _endCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _endCanvas.sortingOrder = endScreenCanvasSortOrder;

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

        canvasGo.SetActive(false);
    }

    /// <summary>
    /// Wire this to the Home station's "On Successful Trade" UnityEvent.
    /// </summary>
    public void OnRentPaid()
    {
        if (_gameOver) return;
        ShowEndScreen(winSprite);
        Debug.Log("[WinLoseManager] Rent paid — players WIN!");
    }

    /// <summary>
    /// Called automatically by EventCardManager when the round limit is reached.
    /// </summary>
    public void TriggerLose()
    {
        if (_gameOver) return;
        ShowEndScreen(loseSprite);
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
        _endCanvas.gameObject.SetActive(true);
        _endCanvas.transform.SetAsLastSibling();
    }
}
