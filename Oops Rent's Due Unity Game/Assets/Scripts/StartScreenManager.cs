using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Pauses the game until any key / face button. Keeps the start / instruction overlay the topmost UI
/// sibling so it covers <c>playerBoards</c>, transaction popups, and other canvas content.
/// </summary>
[DefaultExecutionOrder(500)]
public class StartScreenManager : MonoBehaviour
{
    public static bool IsGameplayStarted { get; private set; }
    public GameObject startScreenOverlay;
    private Canvas startScreenOverlaySortingCanvas;
    [SerializeField] private Sprite startScreenSprite;
    [SerializeField] private Sprite instructionScreenSprite;
    [SerializeField] private Sprite apartmentUpgradesScreenSprite;
    [Header("Apartment Upgrade Voting UI (optional)")]
    [Tooltip("Bottom-of-screen text slots for joined players (index 0-3).")]
    [SerializeField] private TMP_Text[] apartmentVoteTexts = new TMP_Text[4];
    [SerializeField] private TMP_Text apartmentVoteTimerText;
    [SerializeField] private RectTransform apartmentVoteUiRoot;
    private Canvas apartmentVoteOverlayCanvas;
    [SerializeField] private float apartmentVoteTimeLimitSeconds = 60f;
    [Header("Keyboard Quick Test (no controllers needed)")]
    [Tooltip("When enabled, simulates multiple voters on one keyboard for apartment-vote testing.")]
    [SerializeField] private bool enableKeyboardQuickVoteTest = true;
    [Tooltip("Number of virtual keyboard voters for quick test (1-2).")]
    [SerializeField, Range(1, 2)] private int keyboardQuickTestPlayers = 2;

    private bool gameStarted = false;
    private bool waitingForAdvanceRelease = false;
    private Image overlayImage;
    private bool _startingAfterConsensus = false;
    private bool _apartmentVoteTimerRunning;
    private float _apartmentVoteSecondsRemaining;
    private enum IntroStage { StartScreen, InstructionScreen, ApartmentUpgradesScreen }
    private IntroStage currentStage = IntroStage.StartScreen;
    private readonly Dictionary<int, ApartmentUpgradeChoice> _apartmentVotes = new Dictionary<int, ApartmentUpgradeChoice>();

    private IEnumerator Start()
    {
        // This component must be on an active GameObject. If startScreenOverlay is inactive in the
        // hierarchy, Unity will not run Start() here.
        Time.timeScale = 0f;
        IsGameplayStarted = false;
        PlayerGameStats.ResetAll();
        ApartmentUpgradeSelection.ResetForNewRun();
        EnsureApartmentUpgradeSpriteAssigned();
        EnsureApartmentVoteTextSlots();

        if (startScreenOverlay != null)
        {
            EnsureStartScreenOverlaySorting();

            overlayImage = startScreenOverlay.GetComponent<Image>();
            if (overlayImage != null && startScreenSprite != null)
                overlayImage.sprite = startScreenSprite;

            startScreenOverlay.SetActive(true);
            // Sibling order = draw order (later = in front). Scene order may place playerBoards after
            // the start screen; wait one frame then move overlay to the top.
            yield return null;
            if (startScreenOverlay != null)
                startScreenOverlay.transform.SetAsLastSibling();
        }

        if (apartmentUpgradesScreenSprite == null)
            Debug.LogWarning("StartScreenManager: Apartment Upgrades Screen Sprite is not assigned. Apartment stage will still run, but art will look unchanged.");

        PlayerTransactionFeedback.Instance?.HideAllStationPromptsAndMenus();
    }

    private void LateUpdate()
    {
        // Other systems (e.g. player feedback) may add last-sibling UI on the same canvas; keep the
        // intro overlay in front for both start and instruction stages until gameplay begins.
        if (gameStarted || startScreenOverlay == null || !startScreenOverlay.activeInHierarchy) return;
        EnsureStartScreenOverlaySorting();
        startScreenOverlay.transform.SetAsLastSibling();
    }

    void Update()
    {
        if (gameStarted) return;

        PlayerTransactionFeedback.Instance?.HideAllStationPromptsAndMenus();

        if (currentStage == IntroStage.ApartmentUpgradesScreen)
        {
            keyboardQuickTestPlayers = Mathf.Clamp(keyboardQuickTestPlayers, 1, 2);
            EnsureApartmentVoteTextSlots();
            SetApartmentVoteUiVisible(true);
            UpdateApartmentUpgradeVoting();
            return;
        }
        _startingAfterConsensus = false;
        SetApartmentVoteUiVisible(false);

        bool xPressed = IsAdvancePressed();
        if (!xPressed)
            waitingForAdvanceRelease = false;

        if (xPressed && !waitingForAdvanceRelease)
        {
            waitingForAdvanceRelease = true;

            if (currentStage == IntroStage.StartScreen)
            {
                currentStage = IntroStage.InstructionScreen;
                if (overlayImage != null && instructionScreenSprite != null)
                    overlayImage.sprite = instructionScreenSprite;
                return;
            }

            if (currentStage == IntroStage.InstructionScreen)
            {
                currentStage = IntroStage.ApartmentUpgradesScreen;
                if (overlayImage != null && apartmentUpgradesScreenSprite != null)
                    overlayImage.sprite = apartmentUpgradesScreenSprite;
                EnsureApartmentVoteTextSlots();
                _apartmentVotes.Clear();
                StartApartmentVoteTimer();
                RefreshApartmentVoteUi();
                return;
            }

            StartGameplay();
        }
    }

    private static bool IsAdvancePressed()
    {
        bool keyboard = Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame;
        bool gamepad = Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame;
        return keyboard || gamepad;
    }

    private void UpdateApartmentUpgradeVoting()
    {
        UpdateApartmentVoteTimerUiAndTimeout();

        bool anyJoined = false;
        var livePlayers = new HashSet<int>();

        foreach (var pi in PlayerInput.all)
        {
            if (pi == null) continue;
            anyJoined = true;
            int idx = Mathf.Clamp(pi.playerIndex, 0, 3);
            livePlayers.Add(idx);

            var actions = pi.actions;
            if (actions == null) continue;
            actions.FindActionMap("Player", throwIfNotFound: false)?.Enable();

            var vote = ReadApartmentVoteFromPlayerInput(pi);
            if (vote != ApartmentUpgradeChoice.None)
                _apartmentVotes[idx] = vote;
        }

        if (enableKeyboardQuickVoteTest && PlayerInput.all.Count == 0)
        {
            AddKeyboardQuickTestVotes(livePlayers);
            if (livePlayers.Count > 0)
                anyJoined = true;
        }

        if (_apartmentVotes.Count > 0)
        {
            var deadKeys = new List<int>();
            foreach (var key in _apartmentVotes.Keys)
            {
                if (!livePlayers.Contains(key))
                    deadKeys.Add(key);
            }

            foreach (var key in deadKeys)
                _apartmentVotes.Remove(key);
        }

        RefreshApartmentVoteUi();

        if (!anyJoined || livePlayers.Count == 0)
        {
            // Playtest fallback: allow keyboard/gamepad face buttons to pick directly
            // even when no PlayerInput has joined yet.
            if (TryGetApartmentUpgradeChoiceFallback(out var fallbackChoice))
            {
                ApartmentUpgradeSelection.Select(fallbackChoice);
                HomeStationInteraction.Instance?.ApplyApartmentUpgradeRentModifier();
                StartGameplay();
            }
            return;
        }
        if (_apartmentVotes.Count < livePlayers.Count) return;

        ApartmentUpgradeChoice first = ApartmentUpgradeChoice.None;
        foreach (var idx in livePlayers)
        {
            if (!_apartmentVotes.TryGetValue(idx, out var vote))
                return;
            if (vote == ApartmentUpgradeChoice.None)
                return;
            if (first == ApartmentUpgradeChoice.None)
                first = vote;
            else if (first != vote)
                return;
        }

        ApartmentUpgradeSelection.Select(first);
        HomeStationInteraction.Instance?.ApplyApartmentUpgradeRentModifier();
        if (!_startingAfterConsensus)
            StartCoroutine(StartGameplayAfterConsensusPreview());
    }

    private static ApartmentUpgradeChoice ReadApartmentVoteFromPlayerInput(PlayerInput pi)
    {
        if (pi == null || pi.actions == null) return ApartmentUpgradeChoice.None;

        // For controller players, read actual Xbox face buttons directly so this
        // does not depend on Fire1/2/4 binding order in the input asset.
        var pad = pi.GetDevice<Gamepad>();
        if (pad != null)
        {
            if (pad.buttonEast.wasPressedThisFrame)  return ApartmentUpgradeChoice.UsedTv;            // B
            if (pad.buttonSouth.wasPressedThisFrame) return ApartmentUpgradeChoice.HighSpeedInternet; // A
            if (pad.buttonNorth.wasPressedThisFrame) return ApartmentUpgradeChoice.Decluttering;      // Y
        }

        var fire2 = pi.actions.FindAction(PlayerResourceBindingPrompts.ActionFire2, throwIfNotFound: false); // B
        if (fire2 != null && fire2.WasPressedThisFrame())
            return ApartmentUpgradeChoice.UsedTv;

        var fire1 = pi.actions.FindAction(PlayerResourceBindingPrompts.ActionFire1, throwIfNotFound: false); // A
        if (fire1 != null && fire1.WasPressedThisFrame())
            return ApartmentUpgradeChoice.HighSpeedInternet;

        var fire4 = pi.actions.FindAction(PlayerResourceBindingPrompts.ActionFire4, throwIfNotFound: false); // Y
        if (fire4 != null && fire4.WasPressedThisFrame())
            return ApartmentUpgradeChoice.Decluttering;

        return ApartmentUpgradeChoice.None;
    }

    private static bool TryGetApartmentUpgradeChoiceFallback(out ApartmentUpgradeChoice choice)
    {
        choice = ApartmentUpgradeChoice.None;

        bool bPressed = (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame) ||
                        (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        if (bPressed)
        {
            choice = ApartmentUpgradeChoice.UsedTv;
            return true;
        }

        bool aPressed = (Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame) ||
                        (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        if (aPressed)
        {
            choice = ApartmentUpgradeChoice.HighSpeedInternet;
            return true;
        }

        bool yPressed = (Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame) ||
                        (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);
        if (yPressed)
        {
            choice = ApartmentUpgradeChoice.Decluttering;
            return true;
        }

        return false;
    }

    private void AddKeyboardQuickTestVotes(HashSet<int> livePlayers)
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Virtual P1 quick-test keys (same thematic mapping as the card prompt).
        livePlayers.Add(0);
        if (kb.bKey.wasPressedThisFrame) _apartmentVotes[0] = ApartmentUpgradeChoice.UsedTv;
        else if (kb.aKey.wasPressedThisFrame) _apartmentVotes[0] = ApartmentUpgradeChoice.HighSpeedInternet;
        else if (kb.yKey.wasPressedThisFrame) _apartmentVotes[0] = ApartmentUpgradeChoice.Decluttering;

        if (keyboardQuickTestPlayers < 2) return;

        // Virtual P2 quick-test keys to allow consensus testing on one keyboard.
        // N = Used TV, J = High-speed Internet, U = Decluttering.
        livePlayers.Add(1);
        if (kb.nKey.wasPressedThisFrame) _apartmentVotes[1] = ApartmentUpgradeChoice.UsedTv;
        else if (kb.jKey.wasPressedThisFrame) _apartmentVotes[1] = ApartmentUpgradeChoice.HighSpeedInternet;
        else if (kb.uKey.wasPressedThisFrame) _apartmentVotes[1] = ApartmentUpgradeChoice.Decluttering;
    }

    private void RefreshApartmentVoteUi()
    {
        if (apartmentVoteTexts == null || apartmentVoteTexts.Length == 0) return;
        var ptf = PlayerTransactionFeedback.Instance;
        var activeSlots = new HashSet<int>();
        bool usingXboxFlow = Gamepad.all.Count > 0;
        for (int i = 0; i < apartmentVoteTexts.Length; i++)
        {
            if (apartmentVoteTexts[i] == null) continue;
            if (!apartmentVoteTexts[i].gameObject.activeSelf)
                apartmentVoteTexts[i].gameObject.SetActive(true);
            var c = apartmentVoteTexts[i].color;
            apartmentVoteTexts[i].color = new Color(c.r, c.g, c.b, 1f);
        }

        foreach (var pi in PlayerInput.all)
        {
            if (pi == null) continue;
            int idx = Mathf.Clamp(pi.playerIndex, 0, Mathf.Min(3, apartmentVoteTexts.Length - 1));
            if (idx < 0 || idx >= apartmentVoteTexts.Length) continue;
            TMP_Text slot = apartmentVoteTexts[idx];
            if (slot == null) continue;
            activeSlots.Add(idx);

            slot.text = BuildVoteStateText(idx, pi, usingXboxFlow);
            ptf?.SetPlayerBoardMessageIntro(idx, slot.text);
        }

        if (!enableKeyboardQuickVoteTest || PlayerInput.all.Count > 0) return;
        for (int i = 0; i < Mathf.Min(keyboardQuickTestPlayers, apartmentVoteTexts.Length); i++)
        {
            var slot = apartmentVoteTexts[i];
            if (slot == null) continue;
            activeSlots.Add(i);

            slot.text = BuildVoteStateText(i, null, usingXboxFlow: false);
            ptf?.SetPlayerBoardMessageIntro(i, slot.text);
        }

        if (usingXboxFlow && PlayerInput.all.Count == 0 && apartmentVoteTexts.Length > 0 && apartmentVoteTexts[0] != null)
        {
            apartmentVoteTexts[0].text = "Pair Controller [Press x]";
            ptf?.SetPlayerBoardMessageIntro(0, apartmentVoteTexts[0].text);
            activeSlots.Add(0);
        }

        for (int i = 0; i < apartmentVoteTexts.Length; i++)
        {
            if (apartmentVoteTexts[i] == null) continue;
            if (activeSlots.Contains(i)) continue;
            apartmentVoteTexts[i].text = string.Empty;
            ptf?.SetPlayerBoardMessageIntro(i, string.Empty);
        }
    }

    private static string UpgradeDisplayName(ApartmentUpgradeChoice choice)
    {
        switch (choice)
        {
            case ApartmentUpgradeChoice.UsedTv: return "TV";
            case ApartmentUpgradeChoice.HighSpeedInternet: return "Internet";
            case ApartmentUpgradeChoice.Decluttering: return "Decluttering";
            default: return "None";
        }
    }

    private string BuildVoteStateText(int playerIndex, PlayerInput pi, bool usingXboxFlow)
    {
        if (usingXboxFlow)
        {
            var pad = pi != null ? pi.GetDevice<Gamepad>() : null;
            if (pad == null || !pad.added)
                return $"<b>P{playerIndex + 1}</b> Pair Controller [Press x]";
        }

        if (_apartmentVotes.TryGetValue(playerIndex, out var vote) && vote != ApartmentUpgradeChoice.None)
            return $"<b>P{playerIndex + 1}</b> {UpgradeDisplayName(vote)} picked";
        return $"<b>P{playerIndex + 1}</b> Waiting on vote...";
    }

    private void StartGameplay()
    {
        gameStarted = true;
        IsGameplayStarted = true;
        _apartmentVoteTimerRunning = false;
        _startingAfterConsensus = false;
        SetApartmentVoteUiVisible(false);
        ClearApartmentVoteUi();
        PlayerTransactionFeedback.Instance?.ClearAllPlayerBoardMessagesIntro();
        if (startScreenOverlay != null)
            startScreenOverlay.SetActive(false);

        Time.timeScale = 1f;
        GameManager.Instance?.StartGame();
        EventCardManager.Instance?.StartTimer();
        PlayerTransactionFeedback.Instance?.OnGameStarted();
        PlayerInstructionDisplay.Instance?.OnGameStarted();
    }

    private IEnumerator StartGameplayAfterConsensusPreview()
    {
        _startingAfterConsensus = true;
        // Let players briefly see each slot switch from waiting to "<Option> picked".
        yield return new WaitForSecondsRealtime(2f);
        StartGameplay();
    }

    private void ClearApartmentVoteUi()
    {
        if (apartmentVoteTexts == null) return;
        for (int i = 0; i < apartmentVoteTexts.Length; i++)
        {
            if (apartmentVoteTexts[i] == null) continue;
            apartmentVoteTexts[i].text = string.Empty;
        }
        if (apartmentVoteTimerText != null)
            apartmentVoteTimerText.text = string.Empty;
    }

    private void EnsureApartmentUpgradeSpriteAssigned()
    {
        if (apartmentUpgradesScreenSprite != null) return;

#if UNITY_EDITOR
        apartmentUpgradesScreenSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Custom Sprites/apt upgrades with timer.png");
#endif
    }

    private void EnsureApartmentVoteTextSlots()
    {
        if (startScreenOverlay == null) return;

        if (apartmentVoteTexts == null || apartmentVoteTexts.Length < 4)
            apartmentVoteTexts = new TMP_Text[4];
        if (apartmentVoteOverlayCanvas == null)
        {
            var canvasGo = new GameObject("ApartmentVoteOverlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            apartmentVoteOverlayCanvas = canvasGo.GetComponent<Canvas>();
            apartmentVoteOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            apartmentVoteOverlayCanvas.sortingOrder = 32761;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var rootGo = new GameObject("ApartmentVoteTexts", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rootGo.transform.SetParent(canvasGo.transform, false);
            apartmentVoteUiRoot = rootGo.GetComponent<RectTransform>();
            apartmentVoteUiRoot.anchorMin = new Vector2(0f, 0f);
            apartmentVoteUiRoot.anchorMax = new Vector2(1f, 0f);
            apartmentVoteUiRoot.pivot = new Vector2(0.5f, 0f);
            apartmentVoteUiRoot.anchoredPosition = new Vector2(0f, 62f);
            apartmentVoteUiRoot.sizeDelta = new Vector2(0f, 190f);
            var bg = rootGo.GetComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0f);
        }

        if (apartmentVoteUiRoot == null) return;

        bool anyMissing = false;
        for (int i = 0; i < apartmentVoteTexts.Length; i++)
        {
            if (apartmentVoteTexts[i] == null ||
                !apartmentVoteTexts[i].transform.IsChildOf(apartmentVoteUiRoot))
            {
                anyMissing = true;
                break;
            }
        }
        if (!anyMissing && apartmentVoteUiRoot != null) return;

        // Remove prior generated vote text objects to prevent layered duplicates.
        var toDelete = new List<GameObject>();
        for (int i = 0; i < apartmentVoteUiRoot.childCount; i++)
        {
            var child = apartmentVoteUiRoot.GetChild(i);
            if (child != null && child.name.StartsWith("ApartmentVoteText_"))
                toDelete.Add(child.gameObject);
        }
        foreach (var go in toDelete)
            Destroy(go);

        for (int i = 0; i < apartmentVoteTexts.Length; i++)
            apartmentVoteTexts[i] = null;

        Transform root = apartmentVoteUiRoot;
        if (root == null) return;

        TMP_FontAsset sharedFont = null;
        if (overlayImage != null && overlayImage.canvas != null)
        {
            var existing = overlayImage.canvas.GetComponentInChildren<TextMeshProUGUI>(true);
            if (existing != null) sharedFont = existing.font;
        }
        if (sharedFont == null) sharedFont = TMP_Settings.defaultFontAsset;
        if (sharedFont == null) sharedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        var gameplayTimerFont = EventCardManager.Instance != null && EventCardManager.Instance.TimerText != null
            ? EventCardManager.Instance.TimerText.font
            : null;
        if (gameplayTimerFont != null)
            sharedFont = gameplayTimerFont;

        for (int i = 0; i < 4; i++)
        {
            if (apartmentVoteTexts[i] != null) continue;

            var go = new GameObject($"ApartmentVoteText_{i + 1}", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(root, false);
            var rt = go.GetComponent<RectTransform>();
            float quarter = 1f / 4f;
            rt.anchorMin = new Vector2(i * quarter, 0.5f);
            rt.anchorMax = new Vector2((i + 1) * quarter, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(-20f, 48f);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.font = sharedFont;
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.Midline;
            tmp.color = Color.black;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = new Color(0f, 0f, 0f, 1f);
            tmp.text = string.Empty;
            apartmentVoteTexts[i] = tmp;
        }

        if (apartmentVoteTimerText == null)
        {
            var timerGo = new GameObject("ApartmentVoteTimerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            timerGo.transform.SetParent(apartmentVoteOverlayCanvas.transform, false);
            var timerRt = timerGo.GetComponent<RectTransform>();
            timerRt.anchorMin = new Vector2(1f, 1f);
            timerRt.anchorMax = new Vector2(1f, 1f);
            timerRt.pivot = new Vector2(1f, 1f);
            timerRt.anchoredPosition = new Vector2(-180f, -120f);
            timerRt.sizeDelta = new Vector2(320f, 80f);

            var timerTmp = timerGo.GetComponent<TextMeshProUGUI>();
            timerTmp.font = sharedFont;
            timerTmp.fontSize = 59f;
            timerTmp.alignment = TextAlignmentOptions.TopRight;
            timerTmp.color = Color.black;
            timerTmp.textWrappingMode = TextWrappingModes.NoWrap;
            timerTmp.outlineWidth = 0.2f;
            timerTmp.outlineColor = new Color(1f, 1f, 1f, 1f);
            timerTmp.text = string.Empty;
            apartmentVoteTimerText = timerTmp;
        }
    }

    private void EnsureStartScreenOverlaySorting()
    {
        if (startScreenOverlay == null) return;

        if (startScreenOverlaySortingCanvas == null)
            startScreenOverlaySortingCanvas = startScreenOverlay.GetComponent<Canvas>();
        if (startScreenOverlaySortingCanvas == null)
            startScreenOverlaySortingCanvas = startScreenOverlay.AddComponent<Canvas>();

        startScreenOverlaySortingCanvas.overrideSorting = true;
        startScreenOverlaySortingCanvas.sortingOrder = 32760;

        var raycaster = startScreenOverlay.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
            startScreenOverlay.AddComponent<GraphicRaycaster>();
    }

    private void SetApartmentVoteUiVisible(bool visible)
    {
        if (apartmentVoteOverlayCanvas == null && apartmentVoteUiRoot == null) return;
        if (apartmentVoteOverlayCanvas != null && apartmentVoteOverlayCanvas.gameObject.activeSelf != visible)
            apartmentVoteOverlayCanvas.gameObject.SetActive(visible);
        if (apartmentVoteUiRoot != null && apartmentVoteUiRoot.gameObject.activeSelf != visible)
            apartmentVoteUiRoot.gameObject.SetActive(visible);
        if (visible && apartmentVoteUiRoot != null)
            apartmentVoteUiRoot.SetAsLastSibling();
    }

    private void StartApartmentVoteTimer()
    {
        _apartmentVoteSecondsRemaining = Mathf.Max(1f, apartmentVoteTimeLimitSeconds);
        _apartmentVoteTimerRunning = true;
        UpdateApartmentVoteTimerLabel();
    }

    private void UpdateApartmentVoteTimerUiAndTimeout()
    {
        if (!_apartmentVoteTimerRunning) return;

        _apartmentVoteSecondsRemaining = Mathf.Max(0f, _apartmentVoteSecondsRemaining - Time.unscaledDeltaTime);
        UpdateApartmentVoteTimerLabel();

        if (_apartmentVoteSecondsRemaining > 0f)
            return;

        _apartmentVoteTimerRunning = false;
        ApartmentUpgradeSelection.Select(ApartmentUpgradeChoice.None);
        HomeStationInteraction.Instance?.ApplyApartmentUpgradeRentModifier();
        StartGameplay();
    }

    private void UpdateApartmentVoteTimerLabel()
    {
        if (apartmentVoteTimerText == null) return;
        int secondsLeft = Mathf.CeilToInt(Mathf.Max(0f, _apartmentVoteSecondsRemaining));
        int minutes = secondsLeft / 60;
        int seconds = secondsLeft % 60;
        apartmentVoteTimerText.text = $"{minutes}:{seconds:00}";
    }
}
