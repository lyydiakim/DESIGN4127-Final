using UnityEngine;

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

    private bool _gameOver = false;
    private SpriteRenderer _endRenderer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Create a hidden SpriteRenderer that covers the screen at runtime.
        var go = new GameObject("EndScreen");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;
        _endRenderer = go.AddComponent<SpriteRenderer>();
        _endRenderer.sortingLayerName = "Top";
        _endRenderer.sortingOrder = 99;
        go.SetActive(false);
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
        _gameOver = true;
        Time.timeScale = 0f;
        _endRenderer.sprite = sprite;
        _endRenderer.gameObject.SetActive(true);

        _endRenderer.transform.localScale = new Vector3(0.05f, 0.05f, 1f);

        Camera cam = Camera.main;
        if (cam != null)
            _endRenderer.transform.position = cam.transform.position + Vector3.forward;
    }
}
