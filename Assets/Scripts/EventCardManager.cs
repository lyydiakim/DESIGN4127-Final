using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shows event cards every <see cref="eventIntervalSeconds"/> of game time.
/// Call StartTimer() (e.g. from StartScreenManager) to begin the first round.
/// Cards play in order (1 → 2 → 3). After the last card, one more timer interval runs
/// (final round, no card) before lose. Each card pauses the game until
/// <b>any one player</b> picks an option for the <b>whole group</b> (shared <see cref="ResourceBank"/>):
///   <b>B</b> (Fire 2) → −Money · <b>A</b> (Fire 1) → −Energy · <b>Y</b> (Fire 4) → −Network
/// Uses the Input System (not <c>onPlayerButton_Y</c>, which is never invoked by playerController for absorb).
/// Subscribes to both <see cref="InputAction.started"/> and <see cref="InputAction.performed"/> so gamepads
/// (Joystick / face buttons) and keyboards register while <c>Time.timeScale == 0</c>.
/// </summary>
public class EventCardManager : MonoBehaviour
{
    public static EventCardManager Instance { get; private set; }

    /// <summary>True while an event card is on screen (game paused for the choice).</summary>
    public bool IsEventCardShowing => _cardActive;

    const string ActionFire1 = "Player/Fire 1";
    const string ActionFire2 = "Player/Fire 2";
    const string ActionFire4 = "Player/Fire 4";

    [Serializable]
    public class EventCard
    {
        public GameObject overlay;
        public int moneyCost   = 35;
        public int energyCost  = 30;
        public int networkCost = 1;
        [Tooltip("e.g. Rat infestation — used in optional prompt text.")]
        public string cardTitle = "";
        [Tooltip("Optional TMP under this card to show B/A/Y costs for the group choice.")]
        public TMP_Text choicePromptText;
    }

    [Header("Event Cards (each once per cycle, then final timer round)")]
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
    [Tooltip("If true (default): after every event card has been resolved once, one more interval runs without a card, then lose. If false: lose when RoundsCompleted reaches Round Limit (legacy).")]
    [SerializeField] private bool finalRoundAfterAllCards = true;
    [Tooltip("Used only when Final Round After All Cards is off. Game ends (lose) after this many card dismissals.")]
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
    [SerializeField] private bool   _inFinalTimerRound = false;

    private bool _choiceLocked;
    private readonly Dictionary<playerController, EventCardInputHooks> _inputHooks = new();

    private sealed class EventCardInputHooks
    {
        public Action<InputAction.CallbackContext> Fire1;
        public Action<InputAction.CallbackContext> Fire2;
        public Action<InputAction.CallbackContext> Fire4;
    }

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
        {
            if (card.overlay != null)
                card.overlay.SetActive(false);
            if (card.choicePromptText != null)
                card.choicePromptText.gameObject.SetActive(false);
        }
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
            if (_inFinalTimerRound)
            {
                _inFinalTimerRound = false;
                _timerRunning = false;
                Debug.Log("[EventCardManager] Final round (no event card) complete — triggering lose.");
                WinLoseManager.Instance?.TriggerLose();
                return;
            }

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
        _cardActive   = true;
        _choiceLocked = false;

        var card = eventCards[_currentIndex];
        if (card.overlay != null)
        {
            card.overlay.SetActive(true);
            card.overlay.transform.SetAsLastSibling();
        }

        if (card.choicePromptText != null)
        {
            card.choicePromptText.gameObject.SetActive(true);
            card.choicePromptText.text = BuildGroupChoicePromptRichText(card);
        }

        Time.timeScale = 0f;
        SubscribeToPlayers();

        Debug.Log(
            $"[EventCardManager] Card {_currentIndex + 1}/{eventCards.Length}: {StripRichText(BuildGroupChoicePromptRichText(card))}");
    }

    private void CloseCardAndAdvanceRound()
    {
        if (eventCards == null || eventCards.Length == 0) return;

        _cardActive   = false;
        _choiceLocked = false;

        var card = eventCards[_currentIndex];
        if (card.overlay != null)
            card.overlay.SetActive(false);
        if (card.choicePromptText != null)
            card.choicePromptText.gameObject.SetActive(false);

        UnsubscribeFromPlayers();

        RoundsCompleted++;
        OnRoundEnd?.Invoke();

        Time.timeScale = 1f;

        if (finalRoundAfterAllCards && RoundsCompleted >= eventCards.Length)
        {
            // All event cards resolved once: next interval is round 4 (timer only, no card), then lose.
            _inFinalTimerRound = true;
            _elapsed = 0f;
            Debug.Log(
                $"[EventCardManager] All {eventCards.Length} event card(s) resolved. Final round: {eventIntervalSeconds}s (no card), then lose if rent unpaid.");
            return;
        }

        _currentIndex = (_currentIndex + 1) % eventCards.Length;
        _elapsed = 0f;

        if (!finalRoundAfterAllCards && RoundsCompleted >= roundLimit)
            WinLoseManager.Instance?.TriggerLose();
        else
            Debug.Log(
                $"[EventCardManager] Group choice resolved. Round {RoundsCompleted} complete. Next card in {eventIntervalSeconds}s.");
    }

    // ── Choice callbacks (first press wins for the whole group) ──────────────
    private void OnChoiceMoney()
    {
        if (!_cardActive || _choiceLocked) return;
        _choiceLocked = true;
        UnsubscribeFromPlayers();
        if (bank != null && moneyResource != null)
            bank.Add(moneyResource, -eventCards[_currentIndex].moneyCost);
        CloseCardAndAdvanceRound();
    }

    private void OnChoiceEnergy()
    {
        if (!_cardActive || _choiceLocked) return;
        _choiceLocked = true;
        UnsubscribeFromPlayers();
        if (bank != null && energyResource != null)
            bank.Add(energyResource, -eventCards[_currentIndex].energyCost);
        CloseCardAndAdvanceRound();
    }

    private void OnChoiceNetwork()
    {
        if (!_cardActive || _choiceLocked) return;
        _choiceLocked = true;
        UnsubscribeFromPlayers();
        if (bank != null && networkResource != null)
            bank.Add(networkResource, -eventCards[_currentIndex].networkCost);
        CloseCardAndAdvanceRound();
    }

    // ── Input (Player/Fire 1 = A, Fire 2 = B, Fire 4 = Y) ───────────────────
    private void SubscribeToPlayers()
    {
        UnsubscribeFromPlayers();
        if (playersInfo == null) return;

        foreach (var pc in playersInfo.allControllers)
        {
            if (pc == null) continue;
            var pi = ResolvePlayerInput(pc);
            if (pi == null || pi.actions == null) continue;

            // Ensure the Player map is enabled (gamepad / Joystick scheme uses the same actions).
            var playerMap = pi.actions.FindActionMap("Player", throwIfNotFound: false);
            playerMap?.Enable();

            var f1 = pi.actions.FindAction(ActionFire1, throwIfNotFound: false);
            var f2 = pi.actions.FindAction(ActionFire2, throwIfNotFound: false);
            var f4 = pi.actions.FindAction(ActionFire4, throwIfNotFound: false);
            if (f1 == null || f2 == null || f4 == null) continue;

            var hook = new EventCardInputHooks();
            hook.Fire2 = _ => OnChoiceMoney();
            hook.Fire1 = _ => OnChoiceEnergy();
            hook.Fire4 = _ => OnChoiceNetwork();

            // started + performed: controllers reliably fire started; some setups duplicate with performed — _choiceLocked dedupes.
            f2.started += hook.Fire2;
            f2.performed += hook.Fire2;
            f1.started += hook.Fire1;
            f1.performed += hook.Fire1;
            f4.started += hook.Fire4;
            f4.performed += hook.Fire4;

            _inputHooks[pc] = hook;
        }
    }

    private void UnsubscribeFromPlayers()
    {
        foreach (var kv in _inputHooks)
        {
            var pc = kv.Key;
            var hook = kv.Value;
            var pi = pc != null ? ResolvePlayerInput(pc) : null;
            if (pi == null || pi.actions == null) continue;

            UnsubAction(pi.actions, ActionFire1, hook.Fire1);
            UnsubAction(pi.actions, ActionFire2, hook.Fire2);
            UnsubAction(pi.actions, ActionFire4, hook.Fire4);
        }

        _inputHooks.Clear();
    }

    static PlayerInput ResolvePlayerInput(playerController pc)
    {
        if (pc == null) return null;
        if (pc.playerInputComponent != null) return pc.playerInputComponent;
        var pi = pc.GetComponent<PlayerInput>();
        if (pi != null) return pi;
        return pc.GetComponentInChildren<PlayerInput>(true);
    }

    private static void UnsubAction(InputActionAsset asset, string actionPath, Action<InputAction.CallbackContext> cb)
    {
        if (cb == null) return;
        var a = asset.FindAction(actionPath, throwIfNotFound: false);
        if (a == null) return;
        a.started -= cb;
        a.performed -= cb;
    }

    // ── Prompt copy ───────────────────────────────────────────────────────────
    static string BuildGroupChoicePromptRichText(EventCard card)
    {
        string title = string.IsNullOrWhiteSpace(card.cardTitle) ? "Group choice" : card.cardTitle;
        return $"<b>{title}</b>\n<size=90%>Any player chooses for the group:</size>\n" +
               $"<b>[B]</b>  -{card.moneyCost} Money\n" +
               $"<b>[A]</b>  -{card.energyCost} Energy\n" +
               $"<b>[Y]</b>  -{card.networkCost} Network";
    }

    static string StripRichText(string rich)
    {
        if (string.IsNullOrEmpty(rich)) return rich;
        return System.Text.RegularExpressions.Regex.Replace(rich, "<.*?>", string.Empty);
    }
}
