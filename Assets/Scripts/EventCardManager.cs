using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows event cards every <see cref="eventIntervalSeconds"/> of game time.
/// Call StartTimer() (e.g. from StartScreenManager) to begin the first round.
/// Cards play in order (1 → 2 → 3) then loop. Each card pauses the game
/// until any player picks an option:
///   B  →  -Money cost
///   A  →  -Energy cost
///   Y  →  -Network cost
/// After the player chooses, the card dismisses and the next 2-minute round begins.
/// </summary>
public class EventCardManager : MonoBehaviour
{
    public static EventCardManager Instance { get; private set; }

    [Serializable]
    public class EventCard
    {
        public GameObject overlay;
        public int moneyCost   = 35;
        public int energyCost  = 30;
        public int networkCost = 1;
    }

    [Header("Event Cards (played in order, then loops)")]
    [SerializeField] private EventCard[] eventCards;

    [Header("Resources")]
    [SerializeField] private ResourceBank bank;
    [SerializeField] private Resource moneyResource;
    [SerializeField] private Resource energyResource;
    [SerializeField] private Resource networkResource;

    [Header("Players")]
    [SerializeField] private playersInfo playersInfo;

    [Header("Timing")]
    [Tooltip("Seconds between event cards. Default 120 = 2 minutes.")]
    public float eventIntervalSeconds = 120f;
    [Tooltip("Game ends (lose) after this many rounds if rent hasn't been paid.")]
    [SerializeField] private int roundLimit = 6;

    [Header("Timer Display (optional)")]
    [Tooltip("Assign a TMP Text element to show the countdown on screen.")]
    [SerializeField] private TMP_Text timerText;

    /// <summary>Fired each time a round ends (i.e. a card is dismissed).</summary>
    public static event Action OnRoundEnd;

    /// <summary>Total number of rounds completed so far.</summary>
    public int RoundsCompleted { get; private set; }

    // ── Inspector-visible debug state (read-only at runtime) ──────────────────
    [Header("Debug (read-only)")]
    [SerializeField] private float  _elapsed       = 0f;
    [SerializeField] private bool   _timerRunning  = false;
    [SerializeField] private bool   _cardActive    = false;
    [SerializeField] private int    _currentIndex  = 0;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (eventCards == null) return;
        foreach (var card in eventCards)
            if (card.overlay != null)
                card.overlay.SetActive(false);
    }

    private void Update()
    {
        if (!_timerRunning || _cardActive) return;

        _elapsed += Time.deltaTime;

        float remaining = Mathf.Max(0f, eventIntervalSeconds - _elapsed);
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);
        if (timerText != null)
            timerText.text = $"{minutes}:{seconds:00}";

        if (_elapsed >= eventIntervalSeconds)
        {
            _elapsed = 0f;
            ShowCurrentCard();
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this when the start screen closes to begin the first round.
    /// </summary>
    public void StartTimer()
    {
        _elapsed      = 0f;
        _timerRunning = true;
        Debug.Log($"[EventCardManager] Timer started. First card in {eventIntervalSeconds}s.");
    }

    // ── Show / Dismiss ────────────────────────────────────────────────────────
    private void ShowCurrentCard()
    {
        if (_cardActive || eventCards == null || eventCards.Length == 0) return;
        _cardActive = true;

        var card = eventCards[_currentIndex];
        if (card.overlay != null)
            card.overlay.SetActive(true);

        Time.timeScale = 0f;
        SubscribeToPlayers();
        Debug.Log($"[EventCardManager] Showing card {_currentIndex + 1} of {eventCards.Length}.");
    }

    private void DismissCard()
    {
        if (!_cardActive) return;
        _cardActive = false;

        var card = eventCards[_currentIndex];
        if (card.overlay != null)
            card.overlay.SetActive(false);

        UnsubscribeFromPlayers();

        _currentIndex = (_currentIndex + 1) % eventCards.Length;
        _elapsed      = 0f;

        RoundsCompleted++;
        OnRoundEnd?.Invoke();

        Time.timeScale = 1f;
        Debug.Log($"[EventCardManager] Card dismissed. Round {RoundsCompleted}/{roundLimit} complete. Next card in {eventIntervalSeconds}s.");

        if (RoundsCompleted >= roundLimit)
            WinLoseManager.Instance?.TriggerLose();
    }

    // ── Choice callbacks ──────────────────────────────────────────────────────
    private void OnChoiceMoney()
    {
        if (!_cardActive) return;
        bank.Add(moneyResource, -eventCards[_currentIndex].moneyCost);
        DismissCard();
    }

    private void OnChoiceEnergy()
    {
        if (!_cardActive) return;
        bank.Add(energyResource, -eventCards[_currentIndex].energyCost);
        DismissCard();
    }

    private void OnChoiceNetwork()
    {
        if (!_cardActive) return;
        bank.Add(networkResource, -eventCards[_currentIndex].networkCost);
        DismissCard();
    }

    // ── Player event wiring ───────────────────────────────────────────────────
    private void SubscribeToPlayers()
    {
        if (playersInfo == null) return;
        foreach (var pc in playersInfo.allControllers)
        {
            if (pc == null) continue;
            pc.onPlayerButton_B.AddListener(OnChoiceMoney);
            pc.onPlayerButton_A.AddListener(OnChoiceEnergy);
            pc.onPlayerButton_Y.AddListener(OnChoiceNetwork);
        }
    }

    private void UnsubscribeFromPlayers()
    {
        if (playersInfo == null) return;
        foreach (var pc in playersInfo.allControllers)
        {
            if (pc == null) continue;
            pc.onPlayerButton_B.RemoveListener(OnChoiceMoney);
            pc.onPlayerButton_A.RemoveListener(OnChoiceEnergy);
            pc.onPlayerButton_Y.RemoveListener(OnChoiceNetwork);
        }
    }
}
