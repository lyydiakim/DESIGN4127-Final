using UnityEngine;

/// <summary>
/// Scene singleton: walkable area = background floor + left/right edges, capped upward by the
/// highest station trigger base among <see cref="ResourceStation"/> instances (Collider2D.bounds.min.y).
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerMovementBoundsDefinition : MonoBehaviour
{
    public static PlayerMovementBoundsDefinition Instance { get; private set; }

    [Header("Toggle")]
    [SerializeField] private bool enableBounds = true;

    [Header("Background (world bounds for floor + left/right)")]
    [Tooltip("Assign the ScreenBackground mesh/sprite renderer, or leave empty and use name below.")]
    [SerializeField] private Renderer backgroundRenderer;
    [Tooltip("Optional 2D collider on the board.")]
    [SerializeField] private Collider2D backgroundCollider2D;
    [Tooltip("Optional: if set, caps max Y (overrides station calculation when lower).")]
    [SerializeField] private Transform walkableTop;

    [Header("Padding")]
    [SerializeField] private float horizontalPadding = 0.12f;
    [SerializeField] private float bottomPadding = 0.05f;
    [SerializeField] private float topPadding = 0.02f;

    [Header("Manual fallback")]
    [Tooltip("If on, only manual min/max are used.")]
    [SerializeField] private bool useManualBoundsOnly = false;
    [SerializeField] private float manualMinX = -6f;
    [SerializeField] private float manualMaxX = 6f;
    [SerializeField] private float manualMinY = -5.5f;
    [SerializeField] private float manualMaxY = -0.5f;

    [Header("Auto: background by name + station bases")]
    [SerializeField] private string screenBackgroundObjectName = "ScreenBackground";
    [Tooltip("Extra world Y above the highest station trigger base (Collider2D.bounds.min.y).")]
    [SerializeField] private float paddingAboveStationBases = 0.35f;
    [Tooltip("Never allow walking above the background's top edge (world Y).")]
    [SerializeField] private bool clampMaxYToBackgroundTop = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool IsEnabled => enableBounds && enabled && gameObject.activeInHierarchy;

    private Renderer ResolveBackgroundRenderer()
    {
        if (backgroundRenderer != null) return backgroundRenderer;
        if (!string.IsNullOrEmpty(screenBackgroundObjectName))
        {
            var go = GameObject.Find(screenBackgroundObjectName);
            if (go != null)
                return go.GetComponentInChildren<Renderer>();
        }
        return null;
    }

    private static bool TryGetMaxYFromResourceStations(float paddingAbove, out float maxYFromStations)
    {
        maxYFromStations = float.NegativeInfinity;
        var stations = FindObjectsByType<ResourceStation>(FindObjectsSortMode.None);
        if (stations == null || stations.Length == 0) return false;

        bool any = false;
        foreach (var st in stations)
        {
            if (st == null) continue;
            any = true;
            var col = st.GetComponent<Collider2D>();
            if (col != null && col.enabled)
                maxYFromStations = Mathf.Max(maxYFromStations, col.bounds.min.y + paddingAbove);
            else
                maxYFromStations = Mathf.Max(maxYFromStations, st.transform.position.y + paddingAbove);
        }

        return any && maxYFromStations > float.NegativeInfinity;
    }

    public bool TryGetBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        minX = maxX = minY = maxY = 0f;
        if (!IsEnabled) return false;

        if (useManualBoundsOnly)
        {
            minX = manualMinX;
            maxX = manualMaxX;
            minY = manualMinY;
            maxY = manualMaxY;
            Normalize(ref minY, ref maxY);
            return true;
        }

        var br = ResolveBackgroundRenderer();
        Bounds b = default;
        bool hasBg = false;
        if (br != null)
        {
            b = br.bounds;
            hasBg = true;
        }
        else if (backgroundCollider2D != null)
        {
            b = backgroundCollider2D.bounds;
            hasBg = true;
        }

        if (!hasBg)
        {
            minX = manualMinX;
            maxX = manualMaxX;
            minY = manualMinY;
            maxY = manualMaxY;
            Normalize(ref minY, ref maxY);
            return true;
        }

        minX = b.min.x + horizontalPadding;
        maxX = b.max.x - horizontalPadding;
        minY = b.min.y + bottomPadding;

        float maxFromBg = b.max.y - topPadding;
        float maxCandidate;

        if (TryGetMaxYFromResourceStations(paddingAboveStationBases, out float maxFromStations))
            maxCandidate = maxFromStations;
        else if (walkableTop != null)
            maxCandidate = walkableTop.position.y - topPadding;
        else
            maxCandidate = maxFromBg;

        if (clampMaxYToBackgroundTop)
            maxY = Mathf.Min(maxFromBg, maxCandidate);
        else
            maxY = maxCandidate;

        if (walkableTop != null)
            maxY = Mathf.Min(maxY, walkableTop.position.y - topPadding);

        Normalize(ref minY, ref maxY);
        return true;
    }

    private static void Normalize(ref float minY, ref float maxY)
    {
        if (maxY < minY)
        {
            float t = maxY;
            maxY = minY;
            minY = t;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!TryGetBounds(out float minX, out float maxX, out float minY, out float maxY)) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.9f);
        Vector3 center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        Vector3 size = new Vector3(maxX - minX, maxY - minY, 0.01f);
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
