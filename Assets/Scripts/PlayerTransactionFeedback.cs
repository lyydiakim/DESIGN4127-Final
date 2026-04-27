using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class PlayerTransactionFeedback : MonoBehaviour
{
    private static PlayerTransactionFeedback _instance;

    /// <summary>
    /// Resolves even if this component lives on a GameObject that starts inactive (Awake would not have run yet).
    /// </summary>
    public static PlayerTransactionFeedback Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = UnityEngine.Object.FindFirstObjectByType<PlayerTransactionFeedback>(FindObjectsInactive.Include);
                if (_instance != null && !_instance.gameObject.activeInHierarchy)
                    _instance.gameObject.SetActive(true);
            }
            return _instance;
        }
    }

    private static playersInfo _cachedPlayersInfo;

    /// <summary>
    /// Resolves which of the four board slots matches this player. Prefer <see cref="playersInfo.allControllers"/>
    /// order (same order as <see cref="playerController.playerID"/> assignment), then
    /// <see cref="PlayerInput.playerIndex"/> when valid, then <see cref="playerController.playerID"/>.
    /// </summary>
    public static int BoardIndexForPlayer(playerController pc)
    {
        if (pc == null) return 0;

        if (_cachedPlayersInfo == null)
            _cachedPlayersInfo = UnityEngine.Object.FindFirstObjectByType<playersInfo>();
        if (_cachedPlayersInfo != null && _cachedPlayersInfo.allControllers != null)
        {
            int idx = _cachedPlayersInfo.allControllers.IndexOf(pc);
            if (idx >= 0) return Mathf.Clamp(idx, 0, 3);
        }

        PlayerInput pi = PlayerResourceBindingPrompts.ResolvePlayerInput(pc);
        if (pi != null && pi.playerIndex >= 0)
            return Mathf.Clamp(pi.playerIndex, 0, 3);

        return Mathf.Clamp(pc.playerID, 0, 3);
    }

    [Header("Player Panels (index = PlayerInput playerIndex)")]
    [SerializeField] private RectTransform[]    playerPanels    = new RectTransform[4];
    [SerializeField] private TextMeshProUGUI[]  playerBoardTexts = new TextMeshProUGUI[4];

    [Header("Animation")]
    public float moveDuration = 0.7f;
    public float holdDuration = 0.4f;
    public float fadeDuration = 0.4f;
    [Tooltip("Vertical offset from screen centre where the popup spawns (positive = up).")]
    public float popupStartY  = 150f;
    [Tooltip("Horizontal offset from screen centre where the popup spawns.")]
    public float popupStartX  = 0f;

    [Header("Big Grocery menu art")]
    [Tooltip("Pixels between the bottom of the anchor rect and the top of the menu image.")]
    [SerializeField] float groceryMenuGapPx = 8f;
    [Tooltip("If greater than 0, sets the menu image width in canvas pixels (height follows aspect). When set, Max Width and Relative Scale are ignored — adjust this number to resize the menu.")]
    [SerializeField] float groceryMenuWidthPx = 300f;
    [Tooltip("Used only when Grocery Menu Width Px is 0. If 0, no cap — width follows the sprite’s native size (then relative scale). If > 0, images wider than this are shrunk to this width first (two different wide sprites can look the same size).")]
    [SerializeField] float groceryMenuMaxWidthPx = 0f;
    [Tooltip("Used only when Grocery Menu Width Px is 0: uniform scale after max-width clamp (localScale).")]
    [SerializeField] float groceryMenuRelativeScale = 0.24f;
    [Tooltip("If set, anchor uses the board TMP rect only; if off, uses the full player panel (Resource Manager slot) — usually better for centering the menu under the visible frame.")]
    [SerializeField] bool groceryMenuAnchorToBoardText = false;
    [Tooltip("If true, nudges the menu horizontally so it stays inside the canvas (can break centering under a narrow HUD). Off = keep exact center under anchor.")]
    [SerializeField] bool groceryMenuClampHorizontalToCanvas = false;

    [Header("Colors")]
    [SerializeField] private Color costColor   = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color rewardColor = new Color(0.35f, 1f, 0.35f, 1f);

    [Header("Debug")]
    [Tooltip("Shows all four player panel roots in Play Mode for layout. Does not enable station popups; those wait until StartScreenManager calls OnGameStarted.")]
    [SerializeField] private bool showAllPanels = false;
    [Tooltip("If enabled, treats gameplay as started immediately (station HUD works before intro ends). For local testing only; leave off in shipped scenes.")]
    [SerializeField] private bool debugBypassIntroGate = false;

    [Header("TextMeshPro")]
    [Tooltip("Used when board TMPs have no font and for flying transaction popups. If empty, LiberationSans is loaded from Resources.")]
    [SerializeField] private TMP_FontAsset fallbackFontForDynamicText;

    private Canvas       _rootCanvas;
    private Coroutine[]  _popupRoutines = new Coroutine[4];
    /// <summary>Flying TxPopup instance per player — must be destroyed when the coroutine is stopped (spam trades).</summary>
    private readonly GameObject[] _activePopupObjects = new GameObject[4];
    private Coroutine[]  _boardRoutines = new Coroutine[4];
    /// <summary>False until <see cref="OnGameStarted"/>; suppresses station/board/popup UI during start + instruction screens.</summary>
    private bool         _gameStarted   = false;
    private readonly GameObject[] _groceryMenuOverlays = new GameObject[4];

    private static bool IntroUiShouldBlockStationOverlays()
    {
        if (!StartScreenManager.IsGameplayStarted)
            return true;

        var ecm = EventCardManager.Instance;
        return ecm != null && ecm.IsEventCardShowing;
    }

    // -------------------------------------------------------------------------
    private void Awake()
    {
        _instance = this;
        _rootCanvas = GetComponentInParent<Canvas>();
        if (_rootCanvas == null) _rootCanvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();

        // Keep all panels hidden until the start screen is dismissed.
        for (int i = 0; i < playerPanels.Length; i++)
            if (playerPanels[i] != null)
                playerPanels[i].gameObject.SetActive(false);

        // Also force board texts invisible so they don't flash on startup.
        for (int i = 0; i < playerBoardTexts.Length; i++)
            if (playerBoardTexts[i] != null)
            {
                EnsureFontOnTmp(playerBoardTexts[i], i);
                playerBoardTexts[i].alpha = 0f;
                playerBoardTexts[i].text  = string.Empty;
            }
    }

    private void Start()
    {
        if (debugBypassIntroGate)
            _gameStarted = true;

        if (showAllPanels)
        {
            for (int i = 0; i < playerPanels.Length; i++)
                if (playerPanels[i] != null)
                    playerPanels[i].gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Call this when the start screen is dismissed to reveal the player boards.
    /// </summary>
    public void OnGameStarted()
    {
        _gameStarted = true;

        foreach (var pi in PlayerInput.all)
        {
            int i = pi.playerIndex;
            if (i >= 0 && i < playerPanels.Length && playerPanels[i] != null)
                playerPanels[i].gameObject.SetActive(true);
        }
    }

    private void OnEnable()
    {
        if (PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.onPlayerJoined += OnPlayerJoined;
            PlayerInputManager.instance.onPlayerLeft   += OnPlayerLeft;
        }
    }

    private void OnDisable()
    {
        if (PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.onPlayerJoined -= OnPlayerJoined;
            PlayerInputManager.instance.onPlayerLeft   -= OnPlayerLeft;
        }
    }

    private void OnPlayerJoined(PlayerInput player)
    {
        // Only reveal the board once the start screen has been dismissed.
        if (!_gameStarted) return;
        int i = player.playerIndex;
        if (i >= 0 && i < playerPanels.Length && playerPanels[i] != null)
            playerPanels[i].gameObject.SetActive(true);
    }

    private void OnPlayerLeft(PlayerInput player)
    {
        int i = player.playerIndex;
        if (i >= 0 && i < playerPanels.Length && playerPanels[i] != null)
            playerPanels[i].gameObject.SetActive(false);
    }

    private void EnsurePlayerBoardVisible(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerPanels.Length) return;
        var p = playerPanels[playerIndex];
        if (p == null) return;

        // Activate every ancestor so a disabled Canvas (or parent) does not hide the board.
        for (Transform tr = p.transform; tr != null; tr = tr.parent)
        {
            if (!tr.gameObject.activeSelf)
                tr.gameObject.SetActive(true);
        }
    }

    // -------------------------------------------------------------------------
    public void ShowTransaction(int playerIndex,
                                List<ResourceCost> costs,
                                List<ResourceCost> rewards)
    {
        if (!_gameStarted || IntroUiShouldBlockStationOverlays()) return;
        if (playerIndex < 0 || playerIndex >= playerPanels.Length) return;
        EnsurePlayerBoardVisible(playerIndex);
        if (playerPanels[playerIndex] == null) return;
        if (_rootCanvas == null) return;

        string lines = BuildTransactionString(costs, rewards);

        // --- flying popup at screen centre ---
        CancelFlyingPopupForPlayer(playerIndex);
        _popupRoutines[playerIndex] = StartCoroutine(
            RunPopup(playerIndex, lines));

        // --- static text on the player board ---
        if (playerIndex < playerBoardTexts.Length && playerBoardTexts[playerIndex] != null)
        {
            if (_boardRoutines[playerIndex] != null)
                StopCoroutine(_boardRoutines[playerIndex]);
            _boardRoutines[playerIndex] = StartCoroutine(
                ShowBoardText(playerIndex, lines));
        }
    }

    // -------------------------------------------------------------------------
    /// <summary>
    /// Call this when the player can't afford the trade.  Shows which resources
    /// are missing in red, both as a flying popup and as board text.
    /// </summary>
    public void ShowInsufficientFeedback(int playerIndex,
                                         List<ResourceCost> costs,
                                         ResourceBank bank)
    {
        if (!_gameStarted || IntroUiShouldBlockStationOverlays()) return;
        if (playerIndex < 0 || playerIndex >= playerPanels.Length) return;
        EnsurePlayerBoardVisible(playerIndex);
        if (playerIndex >= playerBoardTexts.Length || playerBoardTexts[playerIndex] == null) return;

        string lines = BuildInsufficientString(costs, bank);

        if (_boardRoutines[playerIndex] != null)
            StopCoroutine(_boardRoutines[playerIndex]);
        _boardRoutines[playerIndex] = StartCoroutine(ShowBoardText(playerIndex, lines));
    }

    // -------------------------------------------------------------------------
    private string BuildInsufficientString(List<ResourceCost> costs, ResourceBank bank)
    {
        string red = $"#{ColorUtility.ToHtmlStringRGB(costColor)}";
        var missing = new System.Collections.Generic.List<string>();
        foreach (var c in costs)
        {
            if (c?.resource == null) continue;
            int have = bank.Get(c.resource);
            if (have < c.amount)
                missing.Add($"{c.resource.name} {have}/{c.amount}");
        }
        string detail = missing.Count > 0 ? string.Join(", ", missing) : "";
        return $"<color={red}><b>Not enough!</b></color>\n<color={red}>{detail}</color>".TrimEnd();
    }

    // -------------------------------------------------------------------------
    private string BuildTransactionString(List<ResourceCost> costs, List<ResourceCost> rewards)
    {
        string costHex   = $"#{ColorUtility.ToHtmlStringRGB(costColor)}";
        string rewardHex = $"#{ColorUtility.ToHtmlStringRGB(rewardColor)}";

        var costParts   = new System.Collections.Generic.List<string>();
        var rewardParts = new System.Collections.Generic.List<string>();

        foreach (var c in costs)
            if (c?.resource != null)
                costParts.Add($"-{c.amount} {c.resource.name}");
        foreach (var r in rewards)
            if (r?.resource != null)
                rewardParts.Add($"+{r.amount} {r.resource.name}");

        var lines = new System.Collections.Generic.List<string>();
        if (costParts.Count > 0)
            lines.Add($"<color={costHex}>{string.Join("  ", costParts)}</color>");
        if (rewardParts.Count > 0)
            lines.Add($"<color={rewardHex}>{string.Join("  ", rewardParts)}</color>");

        return string.Join("\n", lines);
    }

    // -------------------------------------------------------------------------
    void CancelFlyingPopupForPlayer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= _popupRoutines.Length) return;
        if (_popupRoutines[playerIndex] != null)
        {
            StopCoroutine(_popupRoutines[playerIndex]);
            _popupRoutines[playerIndex] = null;
        }

        if (_activePopupObjects[playerIndex] != null)
        {
            Destroy(_activePopupObjects[playerIndex]);
            _activePopupObjects[playerIndex] = null;
        }
    }

    // -------------------------------------------------------------------------
    // Spawns a text label at screen centre, flies it to the player board, fades out.
    private IEnumerator RunPopup(int playerIndex, string lines)
    {
        // Build a simple text object at the canvas centre.
        var go  = new GameObject("TxPopup", typeof(RectTransform));
        _activePopupObjects[playerIndex] = go;
        go.transform.SetParent(_rootCanvas.transform, false);

        var rt       = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(popupStartX, popupStartY);
        rt.localScale       = Vector3.one * 2f;

        var cg  = go.AddComponent<CanvasGroup>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        ApplyFontToDynamicTmp(tmp, playerIndex);
        tmp.text      = lines;
        tmp.fontSize  = 16f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        var csf = go.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // Target = the player's board panel position in canvas local space.
        Vector2 boardPos = GetCanvasLocalPos(playerPanels[playerIndex]);

        // Phase 1: fly + shrink toward the board.
        float t = 0f;
        while (t < moveDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / moveDuration);
            rt.anchoredPosition = Vector2.Lerp(new Vector2(popupStartX, popupStartY), boardPos, p);
            rt.localScale       = Vector3.Lerp(Vector3.one * 2f, Vector3.one * 0.8f, p);
            yield return null;
        }

        // Phase 2: brief hold.
        yield return new WaitForSeconds(holdDuration);

        // Phase 3: fade out.
        for (float ft = 0f; ft < fadeDuration; ft += Time.deltaTime)
        {
            if (cg != null) cg.alpha = Mathf.Lerp(1f, 0f, ft / fadeDuration);
            yield return null;
        }

        Destroy(go);
        if (_activePopupObjects[playerIndex] == go)
            _activePopupObjects[playerIndex] = null;
        _popupRoutines[playerIndex] = null;
    }

    // -------------------------------------------------------------------------
    // Updates the text field already placed on the player board, then fades it out.
    private IEnumerator ShowBoardText(int playerIndex, string lines)
    {
        var txt   = playerBoardTexts[playerIndex];
        EnsureFontOnTmp(txt, playerIndex);
        txt.text  = lines;
        txt.alpha = 1f;

        float holdTime = moveDuration + holdDuration;
        yield return new WaitForSeconds(holdTime);

        for (float ft = 0f; ft < fadeDuration; ft += Time.deltaTime)
        {
            txt.alpha = Mathf.Lerp(1f, 0f, ft / fadeDuration);
            yield return null;
        }

        txt.alpha = 0f;
        txt.text  = string.Empty;
        _boardRoutines[playerIndex] = null;
    }

    // -------------------------------------------------------------------------
    /// <summary>
    /// Call when a player enters a station trigger zone.
    /// Shows the station name and its cost/reward info persistently on the
    /// player's board until HideStationPrompt is called.
    /// </summary>
    /// <param name="playerInput">If set, the trade prompt uses <see cref="PlayerResourceBindingPrompts"/> (gamepad vs keyboard).</param>
    public void ShowStationPrompt(int playerIndex, string stationName,
                                  List<ResourceCost> costs,
                                  List<ResourceCost> rewards,
                                  PlayerInput playerInput = null)
    {
        if (!_gameStarted || IntroUiShouldBlockStationOverlays()) return;
        if (playerIndex < 0 || playerIndex >= playerBoardTexts.Length) return;
        EnsurePlayerBoardVisible(playerIndex);
        if (playerBoardTexts[playerIndex] == null) return;

        // Cancel any running board coroutine (e.g. a fading transaction text).
        if (_boardRoutines[playerIndex] != null)
        {
            StopCoroutine(_boardRoutines[playerIndex]);
            _boardRoutines[playerIndex] = null;
        }

        string costHex   = $"#{ColorUtility.ToHtmlStringRGB(costColor)}";
        string rewardHex = $"#{ColorUtility.ToHtmlStringRGB(rewardColor)}";

        var costParts   = new System.Collections.Generic.List<string>();
        var rewardParts = new System.Collections.Generic.List<string>();
        foreach (var c in costs)
            if (c?.resource != null)
                costParts.Add($"<color={costHex}>-{c.amount} {c.resource.name}</color>");
        foreach (var r in rewards)
            if (r?.resource != null)
                rewardParts.Add($"<color={rewardHex}>+{r.amount} {r.resource.name}</color>");

        var allParts = new System.Collections.Generic.List<string>();
        allParts.AddRange(costParts);
        allParts.AddRange(rewardParts);

        string tradeLine = string.Join("  ", allParts);

        string tradeVerb = "[A] trade";
        if (playerInput != null && playerInput.actions != null)
        {
            string g = PlayerResourceBindingPrompts.ResolvePromptGroup(playerInput);
            string label = PlayerResourceBindingPrompts.BindingLabel(playerInput, PlayerResourceBindingPrompts.ActionFire1, g);
            if (!string.IsNullOrEmpty(label) && label != "?")
                tradeVerb = $"<b>[{label}]</b> trade";
        }

        var txt   = playerBoardTexts[playerIndex];
        EnsureFontOnTmp(txt, playerIndex);
        txt.text  = $"<b>{stationName}</b>  {tradeVerb}\n{tradeLine}";
        txt.alpha = 1f;
    }

    /// <summary>
    /// Sets the player board TMP text (e.g. station prompts). Cancels fading board coroutines.
    /// </summary>
    public void SetPlayerBoardMessage(int playerIndex, string richText)
    {
        if (!_gameStarted || IntroUiShouldBlockStationOverlays()) return;
        if (playerIndex < 0 || playerIndex >= playerBoardTexts.Length) return;
        EnsurePlayerBoardVisible(playerIndex);
        if (playerBoardTexts[playerIndex] == null)
        {
            Debug.LogWarning(
                $"PlayerTransactionFeedback: playerBoardTexts[{playerIndex}] is not assigned in the Inspector (player boards).",
                this);
            return;
        }

        if (_boardRoutines[playerIndex] != null)
        {
            StopCoroutine(_boardRoutines[playerIndex]);
            _boardRoutines[playerIndex] = null;
        }

        var txt = playerBoardTexts[playerIndex];
        EnsureFontOnTmp(txt, playerIndex);
        txt.text = richText;
        txt.alpha = 1f;
        var c = txt.color;
        txt.color = new Color(c.r, c.g, c.b, 1f);
        txt.ForceMeshUpdate();
    }

    /// <summary>
    /// Intro-safe variant used before gameplay starts (e.g. apartment upgrade voting).
    /// Unlike <see cref="SetPlayerBoardMessage"/>, this works while _gameStarted is false.
    /// </summary>
    public void SetPlayerBoardMessageIntro(int playerIndex, string richText)
    {
        if (playerIndex < 0 || playerIndex >= playerBoardTexts.Length) return;
        EnsurePlayerBoardVisible(playerIndex);
        if (playerBoardTexts[playerIndex] == null) return;

        if (_boardRoutines[playerIndex] != null)
        {
            StopCoroutine(_boardRoutines[playerIndex]);
            _boardRoutines[playerIndex] = null;
        }

        var txt = playerBoardTexts[playerIndex];
        EnsureFontOnTmp(txt, playerIndex);
        txt.text = richText;
        txt.alpha = 1f;
        var c = txt.color;
        txt.color = new Color(c.r, c.g, c.b, 1f);
        txt.ForceMeshUpdate();
    }

    /// <summary>
    /// Clears intro voting copy from all player boards.
    /// </summary>
    public void ClearAllPlayerBoardMessagesIntro()
    {
        for (int i = 0; i < playerBoardTexts.Length; i++)
            ClearBoardTextLine(i);
    }

    /// <summary>
    /// Big Grocery menu image only: drawn on the root canvas, centered under this player's resource panel,
    /// at native pixel size, or set <see cref="groceryMenuWidthPx"/> for a fixed width. Choice text belongs on
    /// the board via <see cref="SetPlayerBoardMessage"/> — not inside this overlay.
    /// Pass <paramref name="artSprite"/> if the PNG is imported as Sprite (2D); use <paramref name="art"/> when import type is Default (Texture2D).
    /// </summary>
    public void ShowGroceryMenuOverlay(int playerIndex, Texture2D art, Sprite artSprite = null)
    {
        if (!_gameStarted || IntroUiShouldBlockStationOverlays()) return;
        if (playerIndex < 0 || playerIndex >= playerPanels.Length) return;
        EnsurePlayerBoardVisible(playerIndex);
        if (playerPanels[playerIndex] == null || _rootCanvas == null) return;

        HideGroceryMenuOverlay(playerIndex);

        if (artSprite == null && art == null) return;

        var canvasRt = _rootCanvas.GetComponent<RectTransform>();
        var panel = playerPanels[playerIndex];
        RectTransform anchorUnder = panel;
        if (groceryMenuAnchorToBoardText &&
            playerIndex < playerBoardTexts.Length &&
            playerBoardTexts[playerIndex] != null)
            anchorUnder = playerBoardTexts[playerIndex].rectTransform;

        RectTransform artRt;
        if (artSprite != null)
        {
            var imgGo = new GameObject("GroceryMenuOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imgGo.transform.SetParent(_rootCanvas.transform, false);
            imgGo.transform.SetAsLastSibling();
            artRt = imgGo.GetComponent<RectTransform>();
            artRt.anchorMin = artRt.anchorMax = new Vector2(0.5f, 0.5f);
            artRt.pivot = new Vector2(0.5f, 1f);
            var image = imgGo.GetComponent<Image>();
            image.sprite = artSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.SetNativeSize();
        }
        else
        {
            var imgGo = new GameObject("GroceryMenuOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            imgGo.transform.SetParent(_rootCanvas.transform, false);
            imgGo.transform.SetAsLastSibling();
            artRt = imgGo.GetComponent<RectTransform>();
            artRt.anchorMin = artRt.anchorMax = new Vector2(0.5f, 0.5f);
            artRt.pivot = new Vector2(0.5f, 1f);
            var raw = imgGo.GetComponent<RawImage>();
            raw.texture = art;
            raw.raycastTarget = false;
            raw.SetNativeSize();
        }

        if (groceryMenuWidthPx > 0f)
            ApplyGroceryMenuFixedWidth(artRt, groceryMenuWidthPx);
        else
        {
            ClampGroceryArtSize(artRt, groceryMenuMaxWidthPx);
            ApplyGroceryMenuRelativeScale(artRt, groceryMenuRelativeScale);
        }

        PlaceGroceryArtBelowPanel(canvasRt, anchorUnder, artRt);

        _groceryMenuOverlays[playerIndex] = artRt.gameObject;
        Canvas.ForceUpdateCanvases();
    }

    static void ApplyGroceryMenuFixedWidth(RectTransform artRt, float widthPx)
    {
        if (artRt == null || widthPx <= 0f) return;
        float w = artRt.rect.width > 0f ? artRt.rect.width : artRt.sizeDelta.x;
        if (w <= 0f) return;
        // Scale to target width; avoids Image/Canvas resetting sizeDelta after SetNativeSize().
        float s = widthPx / w;
        artRt.localScale = new Vector3(s, s, 1f);
    }

    static void ClampGroceryArtSize(RectTransform artRt, float maxWidthPx)
    {
        if (artRt == null || maxWidthPx <= 0f) return; // 0 = do not clamp; keep native sprite dimensions
        var w = artRt.sizeDelta.x;
        var h = artRt.sizeDelta.y;
        if (w <= 0f || h <= 0f) return;
        if (w <= maxWidthPx) return;
        float s = maxWidthPx / w;
        artRt.sizeDelta = new Vector2(w * s, h * s);
    }

    static void ApplyGroceryMenuRelativeScale(RectTransform artRt, float relativeScale)
    {
        if (artRt == null || relativeScale <= 0f) return;
        if (Mathf.Approximately(relativeScale, 1f)) return;
        // sizeDelta can be reset on the same frame after SetNativeSize() when the canvas rebuilds; localScale persists.
        artRt.localScale = new Vector3(relativeScale, relativeScale, 1f);
    }

    void PlaceGroceryArtBelowPanel(RectTransform canvasRt, RectTransform anchorRect, RectTransform artRt)
    {
        Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _rootCanvas.worldCamera;

        // Bottom-center of the anchor in canvas space: TransformPoint matches the visual Resource Manager
        // frame better than averaging world corners when parents apply non-uniform scale.
        Rect r = anchorRect.rect;
        Vector3 localBottomCenter = new Vector3(r.center.x, r.yMin, 0f);
        Vector3 worldBottomCenter = anchorRect.TransformPoint(localBottomCenter);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldBottomCenter);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRt, screen, cam, out Vector2 canvasLocalBottomCenter);

        float gap = groceryMenuGapPx;
        // Pivot is top-center: place top edge slightly below the anchor bottom.
        artRt.anchoredPosition = new Vector2(canvasLocalBottomCenter.x, canvasLocalBottomCenter.y - gap);

        // rect is unscaled; multiply by lossyScale so placement matches the visually scaled menu.
        float artHalfW = artRt.rect.width * artRt.lossyScale.x * 0.5f;
        float artH = artRt.rect.height * artRt.lossyScale.y;
        var canvasRect = canvasRt.rect;

        float posX = artRt.anchoredPosition.x;
        if (groceryMenuClampHorizontalToCanvas)
        {
            float minX = canvasRect.xMin + artHalfW;
            float maxX = canvasRect.xMax - artHalfW;
            posX = Mathf.Clamp(posX, minX, maxX);
        }

        float minTopY = canvasRect.yMin + artH;
        float posY = Mathf.Max(artRt.anchoredPosition.y, minTopY);
        artRt.anchoredPosition = new Vector2(posX, posY);
    }

    public void HideGroceryMenuOverlay(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= _groceryMenuOverlays.Length) return;
        if (_groceryMenuOverlays[playerIndex] == null) return;
        Destroy(_groceryMenuOverlays[playerIndex]);
        _groceryMenuOverlays[playerIndex] = null;
    }

    /// <summary>
    /// Clears only the board line TMP (does not remove grocery menu overlays). Use when dismissing
    /// temporary copy such as event-card votes so open station overlays stay intact.
    /// </summary>
    public void ClearBoardTextLine(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerBoardTexts.Length) return;
        if (playerBoardTexts[playerIndex] == null) return;

        if (_boardRoutines[playerIndex] != null)
        {
            StopCoroutine(_boardRoutines[playerIndex]);
            _boardRoutines[playerIndex] = null;
        }

        var txt = playerBoardTexts[playerIndex];
        txt.alpha = 0f;
        txt.text  = string.Empty;
    }

    /// <summary>
    /// Call when a player exits a station trigger zone.
    /// Clears the station prompt from the player's board.
    /// </summary>
    public void HideStationPrompt(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= playerBoardTexts.Length) return;
        if (playerBoardTexts[playerIndex] == null) return;

        if (_boardRoutines[playerIndex] != null)
        {
            StopCoroutine(_boardRoutines[playerIndex]);
            _boardRoutines[playerIndex] = null;
        }

        HideGroceryMenuOverlay(playerIndex);

        var txt   = playerBoardTexts[playerIndex];
        txt.alpha = 0f;
        txt.text  = string.Empty;
    }

    public void HideAllStationPromptsAndMenus()
    {
        for (int i = 0; i < _groceryMenuOverlays.Length; i++)
            HideGroceryMenuOverlay(i);

        for (int i = 0; i < playerBoardTexts.Length; i++)
            ClearBoardTextLine(i);
    }

    /// <summary>
    /// Ensures a font is set so TMP can generate a mesh (fixes Editor/scene "No Font Asset" spam).
    /// </summary>
    private void EnsureFontOnTmp(TextMeshProUGUI tmp, int preferPlayerIndex)
    {
        if (tmp == null || tmp.font != null) return;
        var fa = ResolveTmpFontAsset(preferPlayerIndex, out Material sharedMat);
        if (fa == null) return;
        tmp.font = fa;
        if (sharedMat != null)
            tmp.fontSharedMaterial = sharedMat;
        else if (fa.material != null)
            tmp.fontSharedMaterial = fa.material;
    }

    /// <summary>
    /// Runtime-created TMP has no font until assigned; avoids "No Font Asset has been assigned" mesh errors.
    /// </summary>
    private void ApplyFontToDynamicTmp(TextMeshProUGUI tmp, int preferPlayerIndex)
    {
        var fa = ResolveTmpFontAsset(preferPlayerIndex, out Material sharedMat);
        if (fa == null)
        {
            Debug.LogError(
                "PlayerTransactionFeedback: Could not resolve any TMP Font Asset. Assign Fallback Font For Dynamic Text or fix TMP Settings / board text fonts.",
                this);
            return;
        }

        tmp.font = fa;
        if (sharedMat != null)
            tmp.fontSharedMaterial = sharedMat;
        else if (fa.material != null)
            tmp.fontSharedMaterial = fa.material;
    }

    private TMP_FontAsset _cachedResolvedFont;
    private Material _cachedResolvedFontMaterial;

    /// <summary>Resolves a font for popups and missing board TMPs: board copy → inspector fallback → TMP Settings → Resources.</summary>
    private TMP_FontAsset ResolveTmpFontAsset(int preferPlayerIndex, out Material sharedMat)
    {
        sharedMat = null;

        if (preferPlayerIndex >= 0 && preferPlayerIndex < playerBoardTexts.Length && playerBoardTexts[preferPlayerIndex] != null)
        {
            var src = playerBoardTexts[preferPlayerIndex];
            if (src.font != null)
            {
                sharedMat = src.fontSharedMaterial;
                return src.font;
            }
        }

        for (int i = 0; i < playerBoardTexts.Length; i++)
        {
            if (playerBoardTexts[i] == null || playerBoardTexts[i].font == null) continue;
            sharedMat = playerBoardTexts[i].fontSharedMaterial;
            return playerBoardTexts[i].font;
        }

        if (fallbackFontForDynamicText != null)
            return fallbackFontForDynamicText;

        if (_cachedResolvedFont != null)
        {
            sharedMat = _cachedResolvedFontMaterial;
            return _cachedResolvedFont;
        }

        TMP_FontAsset fa = TMP_Settings.defaultFontAsset;

        if (fa == null)
            fa = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fa == null)
            fa = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF - Fallback");
        if (fa == null)
            fa = Resources.Load<TMP_FontAsset>("LiberationSans SDF");

        if (fa == null)
        {
            var inFolder = Resources.LoadAll<TMP_FontAsset>("Fonts & Materials");
            if (inFolder != null && inFolder.Length > 0)
                fa = inFolder[0];
        }

        if (fa != null)
        {
            _cachedResolvedFont = fa;
            _cachedResolvedFontMaterial = fa.material;
        }

        return fa;
    }

    // -------------------------------------------------------------------------
    private Vector2 GetCanvasLocalPos(RectTransform target)
    {
        Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _rootCanvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, target.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rootCanvas.GetComponent<RectTransform>(), screenPoint, cam, out Vector2 local);
        return local;
    }
}
