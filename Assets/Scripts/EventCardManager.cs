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
/// <b>every joined player</b> has voted (B / A / Y) and <b>all votes match</b>; then one shared cost applies to the group
/// (<see cref="ResourceBank"/>):
///   <b>B</b> (Fire 2) → −Money · <b>A</b> (Fire 1) → −Energy · <b>Y</b> (Fire 4) → −Network
/// Uses the Input System (not <c>onPlayerButton_Y</c>, which is never invoked by playerController for absorb).
/// Uses <see cref="InputAction.performed"/> so inputs register while <c>Time.timeScale == 0</c>.
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

    private enum EventVoteChoice
    {
        Unset = 0,
        Money,
        Energy,
        Network
    }

    private readonly List<playerController> _votingPlayers = new List<playerController>();
    private readonly Dictionary<playerController, EventVoteChoice> _playerVotes = new Dictionary<playerController, EventVoteChoice>();
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
        _votingPlayers.Clear();
        _playerVotes.Clear();
        if (playersInfo != null && playersInfo.allControllers != null)
        {
            foreach (var pc in playersInfo.allControllers)
            {
                if (pc != null)
                    _votingPlayers.Add(pc);
            }
        }

        var card = eventCards[_currentIndex];
        if (card.overlay != null)
        {
            card.overlay.SetActive(true);
            card.overlay.transform.SetAsLastSibling();
        }

        if (card.choicePromptText != null)
        {
            card.choicePromptText.gameObject.SetActive(true);
            UpdateChoicePromptText(card);
        }

        Time.timeScale = 0f;
        ClearVoteBoardSlots();
        SubscribeToPlayers();
        RefreshVoteBoardUI();

        Debug.Log(
            $"[EventCardManager] Card {_currentIndex + 1}/{eventCards.Length}: {StripRichText(BuildGroupChoicePromptRichText(card))}");
    }

    private void CloseCardAndAdvanceRound()
    {
        if (eventCards == null || eventCards.Length == 0) return;

        _cardActive   = false;
        _votingPlayers.Clear();
        _playerVotes.Clear();

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

    private void RecordVote(playerController pc, EventVoteChoice choice)
    {
        if (!_cardActive || pc == null || choice == EventVoteChoice.Unset) return;
        if (!_votingPlayers.Contains(pc)) return;

        _playerVotes[pc] = choice;

        if (eventCards != null && eventCards.Length > 0 && _currentIndex >= 0 && _currentIndex < eventCards.Length)
            UpdateChoicePromptText(eventCards[_currentIndex]);

        RefreshVoteBoardUI();
        TryResolveUnanimous();
    }

    private void TryResolveUnanimous()
    {
        if (!_cardActive || eventCards == null || eventCards.Length == 0) return;

        int n = _votingPlayers.Count;
        if (n == 0)
        {
            UnsubscribeFromPlayers();
            CloseCardAndAdvanceRound();
            return;
        }

        foreach (var pc in _votingPlayers)
        {
            if (!_playerVotes.TryGetValue(pc, out var v) || v == EventVoteChoice.Unset)
                return;
        }

        EventVoteChoice first = _playerVotes[_votingPlayers[0]];
        for (int i = 1; i < n; i++)
        {
            if (_playerVotes[_votingPlayers[i]] != first)
                return;
        }

        UnsubscribeFromPlayers();

        switch (first)
        {
            case EventVoteChoice.Money:
                if (bank != null && moneyResource != null)
                    bank.Add(moneyResource, -eventCards[_currentIndex].moneyCost);
                break;
            case EventVoteChoice.Energy:
                if (bank != null && energyResource != null)
                    bank.Add(energyResource, -eventCards[_currentIndex].energyCost);
                break;
            case EventVoteChoice.Network:
                if (bank != null && networkResource != null)
                    bank.Add(networkResource, -eventCards[_currentIndex].networkCost);
                break;
        }

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
            hook.Fire2 = ctx =>
            {
                if (ctx.performed)
                    RecordVote(pc, EventVoteChoice.Money);
            };
            hook.Fire1 = ctx =>
            {
                if (ctx.performed)
                    RecordVote(pc, EventVoteChoice.Energy);
            };
            hook.Fire4 = ctx =>
            {
                if (ctx.performed)
                    RecordVote(pc, EventVoteChoice.Network);
            };

            f2.performed += hook.Fire2;
            f1.performed += hook.Fire1;
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
    void UpdateChoicePromptText(EventCard card)
    {
        if (card.choicePromptText == null) return;
        card.choicePromptText.text = BuildGroupChoicePromptRichText(card);
    }

    string BuildGroupChoicePromptRichText(EventCard card)
    {
        string title = string.IsNullOrWhiteSpace(card.cardTitle) ? "Group choice" : card.cardTitle;
        int total = _votingPlayers.Count;
        int voted = 0;
        foreach (var pc in _votingPlayers)
        {
            if (_playerVotes.TryGetValue(pc, out var v) && v != EventVoteChoice.Unset)
                voted++;
        }

        string voteLine = total > 0
            ? $"<size=85%>Each player votes with <b>[B]</b> / <b>[A]</b> / <b>[Y]</b>. All must pick the same option. (Locked in: {voted}/{total})</size>\n"
            : "<size=85%>Each player votes with <b>[B]</b> / <b>[A]</b> / <b>[Y]</b>. All must pick the same option.</size>\n";

        return $"<b>{title}</b>\n" + voteLine +
               $"<b>[B]</b>  -{card.moneyCost} Money\n" +
               $"<b>[A]</b>  -{card.energyCost} Energy\n" +
               $"<b>[Y]</b>  -{card.networkCost} Network";
    }

    static string StripRichText(string rich)
    {
        if (string.IsNullOrEmpty(rich)) return rich;
        return System.Text.RegularExpressions.Regex.Replace(rich, "<.*?>", string.Empty);
    }

    void ClearVoteBoardSlots()
    {
        var ptf = PlayerTransactionFeedback.Instance;
        if (ptf == null) return;
        for (int i = 0; i < 4; i++)
            ptf.ClearBoardTextLine(i);
    }

    void RefreshVoteBoardUI()
    {
        var ptf = PlayerTransactionFeedback.Instance;
        if (ptf == null || !_cardActive || eventCards == null || eventCards.Length == 0) return;
        if (_currentIndex < 0 || _currentIndex >= eventCards.Length) return;

        EventCard card = eventCards[_currentIndex];
        string title = string.IsNullOrWhiteSpace(card.cardTitle) ? "Group choice" : card.cardTitle;

        foreach (var pc in _votingPlayers)
        {
            if (pc == null) continue;
            int bi = PlayerTransactionFeedback.BoardIndexForPlayer(pc);
            _playerVotes.TryGetValue(pc, out var choice);
            string body = BuildPerPlayerVoteBoardRichText(pc, card, choice);
            ptf.SetPlayerBoardMessage(bi, $"<b>{title}</b>\n{body}");
        }
    }

    string BuildPerPlayerVoteBoardRichText(playerController pc, EventCard card, EventVoteChoice choice)
    {
        if (choice == EventVoteChoice.Unset)
            return BuildWaitingVoteRichText();

        var pi = ResolvePlayerInput(pc);
        string schemeGroup = PlayerResourceBindingPrompts.ResolvePromptGroup(pi);

        string buttonLabel = "<b>[?]</b>";
        string costLine;
        switch (choice)
        {
            case EventVoteChoice.Money:
                costLine = $"{card.moneyCost} Money";
                if (pi != null && pi.actions != null)
                    buttonLabel = PlayerResourceBindingPrompts.BoldBracketLabel(pi, ActionFire2, schemeGroup);
                break;
            case EventVoteChoice.Energy:
                costLine = $"{card.energyCost} Energy";
                if (pi != null && pi.actions != null)
                    buttonLabel = PlayerResourceBindingPrompts.BoldBracketLabel(pi, ActionFire1, schemeGroup);
                break;
            case EventVoteChoice.Network:
                costLine = $"{card.networkCost} Network";
                if (pi != null && pi.actions != null)
                    buttonLabel = PlayerResourceBindingPrompts.BoldBracketLabel(pi, ActionFire4, schemeGroup);
                break;
            default:
                costLine = "?";
                break;
        }

        return $"<size=92%><b>Locked in:</b> −{costLine} {buttonLabel}</size>";
    }

    static string BuildWaitingVoteRichText()
    {
        return "<size=92%>Waiting on player choice</size>";
    }
}
