using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
    public TMP_Text TimerText => timerText;

    /// <summary>True while an event card is on screen (game paused for the choice).</summary>
    public bool IsEventCardShowing => _cardActive;

    const string ActionFire1 = "Player/Fire 1";
    const string ActionFire2 = "Player/Fire 2";
    const string ActionFire4 = "Player/Fire 4";
    const float RentBarRuntimeNudgeRightPx = 0f;
    const float RentBarRightTrimPx = 30f;
    const float RentBarExtraWidthReductionPx = 120f;
    const float RentLabelGapPx = 8f;

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
    [Tooltip("Seconds players have to reach event-card consensus.")]
    [SerializeField] private float eventCardDecisionSeconds = 60f;
    [Tooltip("If true (default): after every event card has been resolved once, one more interval runs without a card, then lose. If false: lose when RoundsCompleted reaches Round Limit (legacy).")]
    [SerializeField] private bool finalRoundAfterAllCards = true;
    [Tooltip("Used only when Final Round After All Cards is off. Game ends (lose) after this many card dismissals.")]
    [SerializeField] private int roundLimit = 6;

    [Header("Timer Display (optional)")]
    [Tooltip("Assign a TMP Text element to show the countdown on screen.")]
    [SerializeField] private TMP_Text timerText;
    [Tooltip("Optional fallback TMP font for timer text if its font reference is missing.")]
    [SerializeField] private TMP_FontAsset fallbackTimerFont;

    [Header("Rent Progress HUD (optional)")]
    [Tooltip("If enabled, creates a rent progress bar below the timer when fields are unassigned.")]
    [SerializeField] private bool autoCreateRentProgressHud = true;
    [SerializeField] private TMP_Text rentProgressText;
    [SerializeField] private Image rentProgressFillImage;
    [SerializeField] private RectTransform rentProgressRoot;
    [SerializeField] private RectTransform rentProgressBackgroundRect;
    private Canvas rentProgressOverlayCanvas;
    [SerializeField] private Color rentProgressFillColor = new Color(0.31f, 0.76f, 0.34f, 1f);
    [SerializeField] private Color rentProgressBackgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    [Tooltip("Fine-tune HUD position if your canvas anchors offset it from the timer.")]
    [SerializeField] private Vector2 rentProgressOffset = new Vector2(0f, -66f);

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
    private GUIStyle _rentHudGuiStyle;
    private RectTransform _eventCardTimerRoot;
    private TMP_Text _eventCardTimerText;
    private float _eventCardDecisionRemaining;
    private bool _eventCardDecisionRunning;

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
        EnsureTimerFontAssigned();
        ForceRebuildRentProgressHud();
        EnsureRentProgressHud();
        PositionRentProgressHud();
        RefreshTimerText(eventIntervalSeconds);
        RefreshRentProgressHud();

        if (eventCards == null) return;
        foreach (var card in eventCards)
        {
            if (card.overlay != null)
                card.overlay.SetActive(false);
            if (card.choicePromptText != null)
                card.choicePromptText.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        HomeStationInteraction.OnRentProgressChanged += OnRentProgressChanged;
    }

    private void OnDisable()
    {
        HomeStationInteraction.OnRentProgressChanged -= OnRentProgressChanged;
    }

    private void EnsureTimerFontAssigned()
    {
        if (timerText == null || timerText.font != null) return;

        TMP_FontAsset resolved = fallbackTimerFont;
        if (resolved == null)
            resolved = TMP_Settings.defaultFontAsset;
        if (resolved == null)
            resolved = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (resolved == null) return;

        timerText.font = resolved;
        if (resolved.material != null)
            timerText.fontSharedMaterial = resolved.material;
    }

    private void Update()
    {
        if (rentProgressRoot == null || rentProgressFillImage == null || rentProgressText == null)
            EnsureRentProgressHud();

        PositionRentProgressHud();
        RefreshRentProgressHud();

        if (_cardActive)
        {
            UpdateEventCardDecisionTimer();
            return;
        }

        if (!_timerRunning || _cardActive) return;

        _elapsed += Time.deltaTime;

        float remaining = Mathf.Max(0f, eventIntervalSeconds - _elapsed);
        RefreshTimerText(remaining);

        if (_elapsed >= eventIntervalSeconds)
        {
            _elapsed = 0f;
            RefreshTimerText(0f);
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

    private void OnGUI()
    {
        if (!StartScreenManager.IsGameplayStarted)
            return;
        if (WinLoseManager.Instance != null && WinLoseManager.Instance.IsEndScreenShowing)
            return;
        if (timerText == null)
            return;

        bool hasTimerRect = TryGetTimerScreenRect(out Rect timerRect);
        if (!hasTimerRect)
            return;

        HomeStationInteraction.TryGetRentProgress(out int paid, out int total);
        int clampedTotal = Mathf.Max(0, total);
        int clampedPaid = Mathf.Clamp(Mathf.Max(0, paid), 0, clampedTotal > 0 ? clampedTotal : int.MaxValue);
        int remaining = Mathf.Max(0, clampedTotal - clampedPaid);
        float paidPct = clampedTotal > 0 ? (float)clampedPaid / clampedTotal : 0f;

        float barWidth = Mathf.Clamp(timerRect.width * 0.8f, 300f, 460f);
        float barHeight = 44f;
        float barX = timerRect.xMin + 140f;
        float barY = timerRect.yMax + 10f;
        barX = Mathf.Max(20f, barX);
        if (barX + barWidth > Screen.width - 20f)
            barX = Screen.width - 20f - barWidth;
        var bgRect = new Rect(barX, barY, barWidth, barHeight);
        var fillRect = new Rect(barX, barY, barWidth * paidPct, barHeight);
        var labelRect = new Rect(barX, barY + barHeight + RentLabelGapPx, 280f, 36f);

        Color prev = GUI.color;
        GUI.color = rentProgressBackgroundColor;
        GUI.DrawTexture(bgRect, Texture2D.whiteTexture);
        GUI.color = rentProgressFillColor;
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
        GUI.color = Color.black;
        DrawGuiRectBorder(bgRect, 2f);

        if (_rentHudGuiStyle == null)
        {
            _rentHudGuiStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                normal = { textColor = Color.black }
            };
        }
        if (timerText != null && timerText.font != null && timerText.font.sourceFontFile != null)
            _rentHudGuiStyle.font = timerText.font.sourceFontFile;

        GUI.Label(labelRect, $"${remaining} left", _rentHudGuiStyle);
        GUI.color = prev;
    }

    private bool TryGetTimerScreenRect(out Rect rect)
    {
        rect = default;
        if (timerText == null) return false;

        var rt = timerText.rectTransform;
        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Camera cam = null;
        var c = timerText.canvas;
        if (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = c.worldCamera;

        Vector2 bl = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 tr = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
        float x = bl.x;
        float y = Screen.height - tr.y;
        float w = Mathf.Abs(tr.x - bl.x);
        float h = Mathf.Abs(tr.y - bl.y);
        rect = new Rect(x, y, w, h);
        return w > 1f && h > 1f;
    }

    private static void DrawGuiRectBorder(Rect rect, float thickness)
    {
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), Texture2D.whiteTexture);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this when the start screen closes to begin the first round.
    /// </summary>
    public void StartTimer()
    {
        _elapsed      = 0f;
        _timerRunning = true;
        ApartmentUpgradeSelection.ApplyStartOfGameBonus(bank, moneyResource, energyResource);
        ApartmentUpgradeSelection.ApplyRoundBonus(bank, energyResource, networkResource);
        RefreshTimerText(eventIntervalSeconds);
        RefreshRentProgressHud();
        Debug.Log($"[EventCardManager] Timer started. First card in {eventIntervalSeconds}s.");
    }

    // ── Show / Dismiss ────────────────────────────────────────────────────────
    private void ShowCurrentCard()
    {
        if (_cardActive || eventCards == null || eventCards.Length == 0) return;
        _cardActive   = true;
        PlayerTransactionFeedback.Instance?.HideAllStationPromptsAndMenus();
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
            EnsureEventOverlayTopmost(card.overlay);
            EnsureEventCardDecisionTimerUi(card.overlay.transform);
            if (_eventCardTimerText != null)
                _eventCardTimerText.color = _currentIndex == 2 ? Color.black : Color.white;
            StartEventCardDecisionTimer();
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

    private static void EnsureEventOverlayTopmost(GameObject overlayGo)
    {
        if (overlayGo == null) return;

        var overlayCanvas = overlayGo.GetComponent<Canvas>();
        if (overlayCanvas == null)
            overlayCanvas = overlayGo.AddComponent<Canvas>();
        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = 32750;

        if (overlayGo.GetComponent<GraphicRaycaster>() == null)
            overlayGo.AddComponent<GraphicRaycaster>();
    }

    private void EnsureEventCardDecisionTimerUi(Transform overlayRoot)
    {
        if (overlayRoot == null) return;

        if (_eventCardTimerRoot != null && _eventCardTimerRoot.parent != overlayRoot)
        {
            Destroy(_eventCardTimerRoot.gameObject);
            _eventCardTimerRoot = null;
            _eventCardTimerText = null;
        }

        if (_eventCardTimerRoot != null && _eventCardTimerText != null)
            return;

        var timerRootGo = new GameObject("EventCardDecisionTimerBox", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        timerRootGo.transform.SetParent(overlayRoot, false);
        _eventCardTimerRoot = timerRootGo.GetComponent<RectTransform>();
        _eventCardTimerRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _eventCardTimerRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _eventCardTimerRoot.pivot = new Vector2(0.5f, 0.5f);
        _eventCardTimerRoot.anchoredPosition = Vector2.zero;
        _eventCardTimerRoot.sizeDelta = new Vector2(100f, 70f);

        var bg = timerRootGo.GetComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0f);
        bg.raycastTarget = false;

        var textGo = new GameObject("EventCardDecisionTimerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(timerRootGo.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(5f, 5f);
        textRt.offsetMax = new Vector2(-5f, -5f);

        _eventCardTimerText = textGo.GetComponent<TextMeshProUGUI>();
        _eventCardTimerText.alignment = TextAlignmentOptions.Center;
        _eventCardTimerText.fontSize = 12f;
        _eventCardTimerText.textWrappingMode = TextWrappingModes.NoWrap;
        _eventCardTimerText.color = Color.white;
        if (timerText != null && timerText.font != null)
            _eventCardTimerText.font = timerText.font;
        else if (fallbackTimerFont != null)
            _eventCardTimerText.font = fallbackTimerFont;
        else if (TMP_Settings.defaultFontAsset != null)
            _eventCardTimerText.font = TMP_Settings.defaultFontAsset;
        ResizeEventCardDecisionTimerBox();
    }

    private void StartEventCardDecisionTimer()
    {
        _eventCardDecisionRemaining = Mathf.Max(1f, eventCardDecisionSeconds);
        _eventCardDecisionRunning = true;
        UpdateEventCardDecisionTimerLabel();
        if (_eventCardTimerRoot != null)
            _eventCardTimerRoot.gameObject.SetActive(true);
    }

    private void StopEventCardDecisionTimer()
    {
        _eventCardDecisionRunning = false;
        if (_eventCardTimerRoot != null)
            _eventCardTimerRoot.gameObject.SetActive(false);
    }

    private void UpdateEventCardDecisionTimer()
    {
        if (!_eventCardDecisionRunning) return;
        _eventCardDecisionRemaining = Mathf.Max(0f, _eventCardDecisionRemaining - Time.unscaledDeltaTime);
        UpdateEventCardDecisionTimerLabel();
        if (_eventCardDecisionRemaining > 0f) return;
        HandleEventCardDecisionTimeout();
    }

    private void UpdateEventCardDecisionTimerLabel()
    {
        if (_eventCardTimerText == null) return;
        int total = Mathf.CeilToInt(Mathf.Max(0f, _eventCardDecisionRemaining));
        int minutes = total / 60;
        int seconds = total % 60;
        _eventCardTimerText.text = $"{minutes}:{seconds:00}";
        ResizeEventCardDecisionTimerBox();
    }

    private void ResizeEventCardDecisionTimerBox()
    {
        if (_eventCardTimerRoot == null || _eventCardTimerText == null) return;
        Vector2 pref = _eventCardTimerText.GetPreferredValues(_eventCardTimerText.text);
        // Keep 5px padding around text on all sides.
        float width = Mathf.Max(100f, pref.x + 10f);
        float height = Mathf.Max(70f, pref.y + 10f);
        _eventCardTimerRoot.sizeDelta = new Vector2(width, height);
    }

    private void HandleEventCardDecisionTimeout()
    {
        _eventCardDecisionRunning = false;

        if (bank != null && moneyResource != null)
            bank.Add(moneyResource, -200);

        var ptf = PlayerTransactionFeedback.Instance;
        if (ptf != null)
        {
            for (int i = 0; i < 4; i++)
                ptf.SetPlayerBoardMessageIntro(i, "$200 lost!\nNo decision made");
        }

        CloseCardAndAdvanceRound();
    }

    private void CloseCardAndAdvanceRound()
    {
        if (eventCards == null || eventCards.Length == 0) return;

        StopEventCardDecisionTimer();
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
            ApartmentUpgradeSelection.ApplyRoundBonus(bank, energyResource, networkResource);
            RefreshTimerText(eventIntervalSeconds);
            Debug.Log(
                $"[EventCardManager] All {eventCards.Length} event card(s) resolved. Final round: {eventIntervalSeconds}s (no card), then lose if rent unpaid.");
            return;
        }

        _currentIndex = (_currentIndex + 1) % eventCards.Length;
        _elapsed = 0f;
        ApartmentUpgradeSelection.ApplyRoundBonus(bank, energyResource, networkResource);
        RefreshTimerText(eventIntervalSeconds);

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

    private void RefreshTimerText(float remainingSeconds)
    {
        if (timerText == null) return;

        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remainingSeconds));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = $"{minutes}:{seconds:00}";
    }

    private void EnsureRentProgressHud()
    {
        if (rentProgressText != null && rentProgressFillImage != null)
        {
            if (rentProgressBackgroundRect == null && rentProgressFillImage != null)
                rentProgressBackgroundRect = rentProgressFillImage.rectTransform.parent as RectTransform;
            if (rentProgressRoot == null && rentProgressBackgroundRect != null)
                rentProgressRoot = rentProgressBackgroundRect.parent as RectTransform;
            if (rentProgressRoot == null && rentProgressFillImage != null)
                rentProgressRoot = rentProgressFillImage.transform.parent?.parent as RectTransform;
            if (rentProgressRoot == null)
            {
                // Incomplete inspector references; rebuild HUD so it can be positioned reliably.
                rentProgressText = null;
                rentProgressFillImage = null;
                rentProgressBackgroundRect = null;
            }
        }

        if (rentProgressText != null && rentProgressFillImage != null && rentProgressRoot != null)
        {
            LayoutRentProgressTextOutsideBar();
            return;
        }
        if (!autoCreateRentProgressHud)
            Debug.LogWarning("EventCardManager: rent progress HUD refs missing; forcing runtime rebuild.");

        RectTransform parentRt = null;
        Vector2 rootAnchoredPosition = new Vector2(0f, -120f);
        Vector2 rootSize = new Vector2(420f, 72f);
        Vector2 rootAnchorMin = new Vector2(0.5f, 0.5f);
        Vector2 rootAnchorMax = new Vector2(0.5f, 0.5f);
        Vector2 rootPivot = new Vector2(0.5f, 0.5f);

        EnsureRentProgressOverlayCanvas();
        if (rentProgressOverlayCanvas != null)
            parentRt = rentProgressOverlayCanvas.GetComponent<RectTransform>();

        if (parentRt == null)
        {
            Canvas anyCanvas = FindFirstObjectByType<Canvas>();
            if (anyCanvas == null)
            {
                var canvasGo = new GameObject("RentProgressCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                anyCanvas = canvasGo.GetComponent<Canvas>();
                anyCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }

            parentRt = anyCanvas.GetComponent<RectTransform>();
            rootAnchoredPosition = new Vector2(0f, -120f);
        }

        var rootGo = new GameObject("RentProgressHUD", typeof(RectTransform));
        rootGo.transform.SetParent(parentRt, false);
        var rootRt = rootGo.GetComponent<RectTransform>();
        rentProgressRoot = rootRt;
        rootRt.anchorMin = rootAnchorMin;
        rootRt.anchorMax = rootAnchorMax;
        rootRt.pivot = rootPivot;
        rootRt.sizeDelta = rootSize;
        rootRt.anchoredPosition = rootAnchoredPosition;

        var backgroundGo = new GameObject("BarBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backgroundGo.transform.SetParent(rootGo.transform, false);
        var backgroundRt = backgroundGo.GetComponent<RectTransform>();
        rentProgressBackgroundRect = backgroundRt;
        backgroundRt.anchorMin = new Vector2(0f, 0.5f);
        backgroundRt.anchorMax = new Vector2(0f, 0.5f);
        backgroundRt.pivot = new Vector2(0f, 0.5f);
        backgroundRt.anchoredPosition = Vector2.zero;
        backgroundRt.sizeDelta = new Vector2(
            Mathf.Max(200f, rootRt.sizeDelta.x - 80f - RentBarRightTrimPx - RentBarExtraWidthReductionPx),
            60f);
        var backgroundImage = backgroundGo.GetComponent<Image>();
        backgroundImage.color = rentProgressBackgroundColor;
        backgroundImage.maskable = false;
        var bgOutline = backgroundGo.AddComponent<Outline>();
        bgOutline.effectColor = Color.black;
        bgOutline.effectDistance = new Vector2(2f, -2f);
        bgOutline.useGraphicAlpha = true;

        var fillGo = new GameObject("BarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fillGo.transform.SetParent(backgroundGo.transform, false);
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0f, 0f);
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.pivot = new Vector2(0f, 0.5f);
        // With vertical stretch anchors, y sizeDelta must stay 0 to exactly match parent height.
        fillRt.sizeDelta = Vector2.zero;
        fillRt.anchoredPosition = Vector2.zero;
        rentProgressFillImage = fillGo.GetComponent<Image>();
        rentProgressFillImage.color = rentProgressFillColor;
        rentProgressFillImage.maskable = false;
        var fillOutline = fillGo.AddComponent<Outline>();
        fillOutline.effectColor = Color.black;
        fillOutline.effectDistance = new Vector2(2f, -2f);
        fillOutline.useGraphicAlpha = true;

        var textGo = new GameObject("RentLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(rootGo.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0f, 0.5f);
        textRt.anchorMax = new Vector2(0f, 0.5f);
        textRt.pivot = new Vector2(0f, 0.5f);
        textRt.sizeDelta = new Vector2(190f, 60f);
        rentProgressText = textGo.GetComponent<TextMeshProUGUI>();
        rentProgressText.alignment = TextAlignmentOptions.MidlineLeft;
        rentProgressText.fontSize = 28f;
        rentProgressText.color = new Color(0.05f, 0.05f, 0.05f, 1f);
        rentProgressText.textWrappingMode = TextWrappingModes.NoWrap;
        rentProgressText.maskable = false;
        if (timerText != null && timerText.font != null)
        {
            rentProgressText.font = timerText.font;
        }
        else if (fallbackTimerFont != null)
            rentProgressText.font = fallbackTimerFont;
        else if (TMP_Settings.defaultFontAsset != null)
            rentProgressText.font = TMP_Settings.defaultFontAsset;
        else
            rentProgressText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        LayoutRentProgressTextOutsideBar();
    }

    private void ForceRebuildRentProgressHud()
    {
        if (rentProgressRoot != null)
            Destroy(rentProgressRoot.gameObject);
        rentProgressRoot = null;
        rentProgressBackgroundRect = null;
        rentProgressFillImage = null;
        rentProgressText = null;
    }

    private void PositionRentProgressHud()
    {
        if (rentProgressRoot == null) return;
        rentProgressRoot.gameObject.SetActive(true);
        if (rentProgressBackgroundRect != null)
            rentProgressBackgroundRect.gameObject.SetActive(true);
        if (rentProgressFillImage != null)
            rentProgressFillImage.gameObject.SetActive(true);
        if (rentProgressText != null)
            rentProgressText.gameObject.SetActive(true);

        if (timerText != null)
        {
            RectTransform timerRt = timerText.rectTransform;
            EnsureRentProgressOverlayCanvas();
            RectTransform parentRt = rentProgressOverlayCanvas != null
                ? rentProgressOverlayCanvas.GetComponent<RectTransform>()
                : null;
            if (parentRt == null)
                return;

            if (rentProgressRoot.parent != parentRt)
                rentProgressRoot.SetParent(parentRt, worldPositionStays: false);

            Camera timerCam = null;
            var timerCanvas = timerText.canvas;
            if (timerCanvas != null && timerCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                timerCam = timerCanvas.worldCamera;

            var corners = new Vector3[4];
            timerRt.GetWorldCorners(corners); // 0=BL,1=TL,2=TR,3=BR
            Vector3 timerBottomCenterWorld = (corners[0] + corners[3]) * 0.5f;
            Vector2 timerBottomScreen = RectTransformUtility.WorldToScreenPoint(timerCam, timerBottomCenterWorld);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRt, timerBottomScreen, null, out Vector2 timerBottomLocal);

            rentProgressRoot.anchorMin = new Vector2(0.5f, 0.5f);
            rentProgressRoot.anchorMax = new Vector2(0.5f, 0.5f);
            rentProgressRoot.pivot = new Vector2(0.5f, 0.5f);
            rentProgressRoot.sizeDelta = new Vector2(Mathf.Max(420f, timerRt.rect.width * 0.95f), 72f);
            rentProgressRoot.anchoredPosition = new Vector2(timerBottomLocal.x, timerBottomLocal.y + rentProgressOffset.y);
            rentProgressRoot.localScale = Vector3.one;
            rentProgressRoot.SetAsLastSibling();
            ResizeRentProgressBarWidth();
            return;
        }

        rentProgressRoot.anchorMin = new Vector2(0.5f, 1f);
        rentProgressRoot.anchorMax = new Vector2(0.5f, 1f);
        rentProgressRoot.pivot = new Vector2(0.5f, 0.5f);
        rentProgressRoot.anchoredPosition = new Vector2(rentProgressOffset.x, -120f);
        ResizeRentProgressBarWidth();
    }

    private void EnsureRentProgressOverlayCanvas()
    {
        if (rentProgressOverlayCanvas != null) return;

        var existing = GameObject.Find("RentProgressOverlayCanvas");
        if (existing != null)
            rentProgressOverlayCanvas = existing.GetComponent<Canvas>();

        if (rentProgressOverlayCanvas == null)
        {
            var go = new GameObject("RentProgressOverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            rentProgressOverlayCanvas = go.GetComponent<Canvas>();
            rentProgressOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rentProgressOverlayCanvas.sortingOrder = 30000;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }
    }

    private void ResizeRentProgressBarWidth()
    {
        if (rentProgressRoot == null || rentProgressBackgroundRect == null) return;
        float effectiveBarOffset = 0f;
        rentProgressBackgroundRect.anchoredPosition = new Vector2(0f, rentProgressBackgroundRect.anchoredPosition.y);
        rentProgressBackgroundRect.sizeDelta = new Vector2(
            Mathf.Max(200f, rentProgressRoot.sizeDelta.x - effectiveBarOffset - 80f - RentBarRightTrimPx - RentBarExtraWidthReductionPx),
            rentProgressBackgroundRect.sizeDelta.y);
        LayoutRentProgressTextOutsideBar();
    }

    private void LayoutRentProgressTextOutsideBar()
    {
        if (rentProgressText == null || rentProgressRoot == null || rentProgressBackgroundRect == null) return;
        RectTransform textRt = rentProgressText.rectTransform;
        if (textRt.parent != rentProgressBackgroundRect)
            textRt.SetParent(rentProgressBackgroundRect, false);

        // Keep the label locked to the bar's left edge and directly below it.
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(0f, 0f);
        textRt.pivot = new Vector2(0f, 1f);
        textRt.sizeDelta = new Vector2(220f, 42f);
        textRt.anchoredPosition = new Vector2(0f, -RentLabelGapPx);
    }

    private void OnRentProgressChanged(int paid, int total)
    {
        RefreshRentProgressHud(paid, total);
    }

    private void RefreshRentProgressHud()
    {
        if (!HomeStationInteraction.TryGetRentProgress(out int paid, out int total))
        {
            RefreshRentProgressHud(0, 0);
            return;
        }

        RefreshRentProgressHud(paid, total);
    }

    private void RefreshRentProgressHud(int paid, int total)
    {
        if (rentProgressText == null && rentProgressFillImage == null)
            return;

        int clampedTotal = Mathf.Max(0, total);
        int clampedPaid = Mathf.Max(0, paid);
        if (clampedTotal > 0)
            clampedPaid = Mathf.Clamp(clampedPaid, 0, clampedTotal);

        int remaining = Mathf.Max(0, clampedTotal - clampedPaid);
        float paidPct = clampedTotal > 0 ? (float)clampedPaid / clampedTotal : 0f;

        if (rentProgressText != null)
            rentProgressText.text = $"${remaining} left";

        if (rentProgressFillImage != null)
        {
            var fillRt = rentProgressFillImage.rectTransform;
            RectTransform parentRt = fillRt.parent as RectTransform;
            if (parentRt != null)
                fillRt.sizeDelta = new Vector2(parentRt.rect.width * paidPct, 0f);
        }
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
