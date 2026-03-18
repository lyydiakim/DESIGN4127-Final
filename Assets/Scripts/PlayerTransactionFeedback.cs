using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerTransactionFeedback : MonoBehaviour
{
    public static PlayerTransactionFeedback Instance { get; private set; }

    [Header("Player Panels (index = player ID)")]
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

    [Header("Colors")]
    [SerializeField] private Color costColor   = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color rewardColor = new Color(0.35f, 1f, 0.35f, 1f);

    [Header("Debug")]
    [SerializeField] private bool showAllPanels = false;

    private Canvas       _rootCanvas;
    private Coroutine[]  _popupRoutines = new Coroutine[4];
    private Coroutine[]  _boardRoutines = new Coroutine[4];
    private bool         _gameStarted   = false;

    // -------------------------------------------------------------------------
    private void Awake()
    {
        Instance    = this;
        _rootCanvas = GetComponentInParent<Canvas>();
        if (_rootCanvas == null) _rootCanvas = FindObjectOfType<Canvas>();

        // Keep all panels hidden until the start screen is dismissed.
        for (int i = 0; i < playerPanels.Length; i++)
            if (playerPanels[i] != null)
                playerPanels[i].gameObject.SetActive(false);

        // Also force board texts invisible so they don't flash on startup.
        for (int i = 0; i < playerBoardTexts.Length; i++)
            if (playerBoardTexts[i] != null)
            {
                playerBoardTexts[i].alpha = 0f;
                playerBoardTexts[i].text  = string.Empty;
            }
    }

    private void Start()
    {
        // showAllPanels is a debug shortcut — skip the start-screen gate.
        if (showAllPanels)
        {
            _gameStarted = true;
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

    // -------------------------------------------------------------------------
    public void ShowTransaction(int playerIndex,
                                List<ResourceCost> costs,
                                List<ResourceCost> rewards)
    {
        if (playerIndex < 0 || playerIndex >= playerPanels.Length) return;
        if (playerPanels[playerIndex] == null) return;
        if (_rootCanvas == null) return;

        string lines = BuildTransactionString(costs, rewards);

        // --- flying popup at screen centre ---
        if (_popupRoutines[playerIndex] != null)
            StopCoroutine(_popupRoutines[playerIndex]);
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
        if (playerIndex < 0 || playerIndex >= playerPanels.Length) return;
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
    // Spawns a text label at screen centre, flies it to the player board, fades out.
    private IEnumerator RunPopup(int playerIndex, string lines)
    {
        // Build a simple text object at the canvas centre.
        var go  = new GameObject("TxPopup", typeof(RectTransform));
        go.transform.SetParent(_rootCanvas.transform, false);

        var rt       = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(popupStartX, popupStartY);
        rt.localScale       = Vector3.one * 2f;

        var cg  = go.AddComponent<CanvasGroup>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
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
        _popupRoutines[playerIndex] = null;
    }

    // -------------------------------------------------------------------------
    // Updates the text field already placed on the player board, then fades it out.
    private IEnumerator ShowBoardText(int playerIndex, string lines)
    {
        var txt   = playerBoardTexts[playerIndex];
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
    public void ShowStationPrompt(int playerIndex, string stationName,
                                  List<ResourceCost> costs,
                                  List<ResourceCost> rewards)
    {
        if (playerIndex < 0 || playerIndex >= playerBoardTexts.Length) return;
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

        var txt   = playerBoardTexts[playerIndex];
        txt.text  = $"<b>{stationName}</b>  [A] trade\n{tradeLine}";
        txt.alpha = 1f;
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

        var txt   = playerBoardTexts[playerIndex];
        txt.alpha = 0f;
        txt.text  = string.Empty;
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
