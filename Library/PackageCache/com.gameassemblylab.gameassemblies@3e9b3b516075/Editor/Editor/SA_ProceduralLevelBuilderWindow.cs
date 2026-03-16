using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Procedural Level Builder window for generating levels from prefabs and rules.
/// </summary>
public class SA_ProceduralLevelBuilderWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private int _selectedToolIndex;

    // Scatter Prefab Tool
    private List<ScatterEntry> _scatterEntries = new List<ScatterEntry>();
    private int _scatterTotalInstances = 50;
    private AxisPlane _scatterAxisPlane = AxisPlane.XY;
    private float _scatterWidth = 10f;
    private float _scatterHeight = 10f;
    private Vector3 _scatterCenter = Vector3.zero;

    // Grid of Prefabs Tool
    private List<GridEntry> _gridEntries = new List<GridEntry>();
    private AxisPlane _gridAxisPlane = AxisPlane.XY;
    private int _gridColumns = 5;
    private int _gridRows = 5;
    private float _gridSpacing = 1f;
    private Vector3 _gridCenter = Vector3.zero;
    private float _gridGapPercent = 0f;
    private float _gridOffsetPercent = 0f;
    private float _gridOffsetX = 0.2f;
    private float _gridOffsetY = 0.2f;

    // Perlin Noise Scatter Tool (grid placement with noise mask)
    private List<PerlinScatterEntry> _perlinEntries = new List<PerlinScatterEntry>();
    private int _perlinColumns = 20;
    private int _perlinRows = 20;
    private float _perlinSpacing = 0.3f;
    private AxisPlane _perlinAxisPlane = AxisPlane.XY;
    private Vector3 _perlinCenter = Vector3.zero;
    private float _perlinNoiseMin = 0.5f;
    private float _perlinNoiseMax = 1.0f;
    private float _perlinNoiseScale = 0.25f;
    private float _perlinOffsetX = 0f;
    private float _perlinOffsetY = 0f;
    private bool _perlinUseRandomScale = false;
    private float _perlinScaleMin = 0.8f;
    private float _perlinScaleMax = 1.2f;
    private float _perlinRandomOffsetX = 0.2f;
    private float _perlinRandomOffsetY = 0.2f;

    private enum AxisPlane { XY, XZ, YZ }

    [System.Serializable]
    private class ScatterEntry
    {
        public GameObject prefab;
        public float percent = 50f;
    }

    [System.Serializable]
    private class GridEntry
    {
        public GameObject prefab;
        public float percent = 50f;
    }

    [System.Serializable]
    private class PerlinScatterEntry
    {
        public GameObject prefab;
        public float percent = 50f;
    }

    [MenuItem("Game Assemblies/Environment/Procedural Level Builder")]
    public static void ShowWindow()
    {
        var window = GetWindow<SA_ProceduralLevelBuilderWindow>("Procedural Level Builder");
        window.minSize = new Vector2(400, 500);
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(8);
        GUILayout.Label("Procedural Level Builder", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Tools for procedurally generating level content. Select a tool below to place prefabs, tiles, or structures in the scene.",
            MessageType.Info);
        EditorGUILayout.Space(8);

        // Tool selector
        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
        string[] toolNames = { "Scatter Prefab", "Grid of Prefabs", "Perlin Noise Scatter", "Room / Wave (planned)", "Tilemap from Noise (planned)", "Path / Walkway (planned)", "Spawn Points (planned)", "Boundary / Fence (planned)", "Layered Placement (planned)" };
        _selectedToolIndex = EditorGUILayout.Popup("Active Tool", _selectedToolIndex, toolNames);
        EditorGUILayout.Space(8);

        if (_selectedToolIndex == 0)
        {
            DrawScatterPrefabTool();
        }
        else if (_selectedToolIndex == 1)
        {
            DrawGridPrefabsTool();
        }
        else if (_selectedToolIndex == 2)
        {
            DrawPerlinNoiseScatterTool();
        }
        else
        {
            DrawPlannedPlaceholder();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawScatterPrefabTool()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Scatter Prefab Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Place prefabs across an area with configurable probability for each. Use for rocks, bushes, trees, or debris.",
            MessageType.None);
        EditorGUILayout.Space(4);

        // Prefabs to scatter (List) with % for each
        EditorGUILayout.LabelField("Prefabs to Scatter", EditorStyles.boldLabel);
        for (int i = 0; i < _scatterEntries.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            _scatterEntries[i].prefab = (GameObject)EditorGUILayout.ObjectField(_scatterEntries[i].prefab, typeof(GameObject), false, GUILayout.Width(180));
            EditorGUILayout.LabelField("%", GUILayout.Width(14));
            _scatterEntries[i].percent = Mathf.Max(0f, EditorGUILayout.FloatField(_scatterEntries[i].percent, GUILayout.Width(50)));
            if (GUILayout.Button("−", GUILayout.Width(22)))
            {
                _scatterEntries.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Add Prefab"))
        {
            _scatterEntries.Add(new ScatterEntry { prefab = null, percent = 50f });
        }
        EditorGUILayout.Space(4);

        // Total number of instances
        _scatterTotalInstances = Mathf.Max(1, EditorGUILayout.IntField("Total Instances", _scatterTotalInstances));
        EditorGUILayout.Space(4);

        // Axis plane
        _scatterAxisPlane = (AxisPlane)EditorGUILayout.EnumPopup("Axis Plane", _scatterAxisPlane);
        EditorGUILayout.Space(4);

        // Bounds (-width, width, -height, height)
        EditorGUILayout.LabelField("Bounds", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Range: X from -Width to +Width, Y/Z from -Height to +Height (based on plane).", MessageType.None);
        EditorGUILayout.BeginHorizontal();
        _scatterWidth = Mathf.Max(0.01f, EditorGUILayout.FloatField("Width (±)", _scatterWidth));
        _scatterHeight = Mathf.Max(0.01f, EditorGUILayout.FloatField("Height (±)", _scatterHeight));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);

        // Center
        _scatterCenter = EditorGUILayout.Vector3Field("Center", _scatterCenter);
        EditorGUILayout.Space(8);

        // Scatter button
        bool canScatter = _scatterEntries.Count > 0 && _scatterEntries.Exists(e => e.prefab != null);
        EditorGUI.BeginDisabledGroup(!canScatter);
        if (GUILayout.Button("Scatter Prefabs", GUILayout.Height(28)))
        {
            ExecuteScatter();
        }
        EditorGUI.EndDisabledGroup();
        if (!canScatter)
        {
            EditorGUILayout.HelpBox("Add at least one prefab to scatter.", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    private void ExecuteScatter()
    {
        var validEntries = new List<ScatterEntry>();
        float totalPercent = 0f;
        foreach (var e in _scatterEntries)
        {
            if (e.prefab != null && e.percent > 0f)
            {
                validEntries.Add(e);
                totalPercent += e.percent;
            }
        }
        if (validEntries.Count == 0)
        {
            Debug.LogWarning("Procedural Level Builder: No valid prefabs to scatter.");
            return;
        }
        if (totalPercent <= 0f)
        {
            Debug.LogWarning("Procedural Level Builder: Total percent must be > 0.");
            return;
        }

        var parent = new GameObject("Scattered Prefabs");
        Undo.RegisterCreatedObjectUndo(parent, "Scatter Prefabs");

        for (int i = 0; i < _scatterTotalInstances; i++)
        {
            float r = Random.Range(0f, totalPercent);
            GameObject chosenPrefab = null;
            foreach (var e in validEntries)
            {
                r -= e.percent;
                if (r <= 0f)
                {
                    chosenPrefab = e.prefab;
                    break;
                }
            }
            if (chosenPrefab == null) chosenPrefab = validEntries[validEntries.Count - 1].prefab;

            float a = Random.Range(-_scatterWidth, _scatterWidth);
            float b = Random.Range(-_scatterHeight, _scatterHeight);
            Vector3 pos = _scatterCenter;
            switch (_scatterAxisPlane)
            {
                case AxisPlane.XY: pos.x += a; pos.y += b; break;
                case AxisPlane.XZ: pos.x += a; pos.z += b; break;
                case AxisPlane.YZ: pos.y += a; pos.z += b; break;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(chosenPrefab);
            instance.transform.SetParent(parent.transform);
            instance.transform.position = pos;
            Undo.RegisterCreatedObjectUndo(instance, "Scatter Prefabs");
        }

        Selection.activeGameObject = parent;
        EditorGUIUtility.PingObject(parent);
        Debug.Log($"Procedural Level Builder: Scattered {_scatterTotalInstances} prefabs.");
    }

    private void DrawGridPrefabsTool()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Grid of Prefabs Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Fill a grid with prefabs. Support gaps (empty cells) and random offset for a less uniform look.",
            MessageType.None);
        EditorGUILayout.Space(4);

        // Prefabs to instantiate (List) with % for each
        EditorGUILayout.LabelField("Prefabs to Instantiate", EditorStyles.boldLabel);
        for (int i = 0; i < _gridEntries.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            _gridEntries[i].prefab = (GameObject)EditorGUILayout.ObjectField(_gridEntries[i].prefab, typeof(GameObject), false, GUILayout.Width(180));
            EditorGUILayout.LabelField("%", GUILayout.Width(14));
            _gridEntries[i].percent = Mathf.Max(0f, EditorGUILayout.FloatField(_gridEntries[i].percent, GUILayout.Width(50)));
            if (GUILayout.Button("−", GUILayout.Width(22)))
            {
                _gridEntries.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Add Prefab"))
        {
            _gridEntries.Add(new GridEntry { prefab = null, percent = 50f });
        }
        EditorGUILayout.Space(4);

        // Axis
        _gridAxisPlane = (AxisPlane)EditorGUILayout.EnumPopup("Axis Plane", _gridAxisPlane);
        EditorGUILayout.Space(4);

        // Columns and rows
        EditorGUILayout.BeginHorizontal();
        _gridColumns = Mathf.Max(1, EditorGUILayout.IntField("Columns", _gridColumns));
        _gridRows = Mathf.Max(1, EditorGUILayout.IntField("Rows", _gridRows));
        EditorGUILayout.EndHorizontal();
        _gridSpacing = Mathf.Max(0.01f, EditorGUILayout.FloatField("Spacing", _gridSpacing));
        EditorGUILayout.Space(4);

        // Center
        _gridCenter = EditorGUILayout.Vector3Field("Center", _gridCenter);
        EditorGUILayout.Space(4);

        // % of gaps
        _gridGapPercent = Mathf.Clamp(EditorGUILayout.Slider("% of Gaps", _gridGapPercent, 0f, 100f), 0f, 100f);
        EditorGUILayout.Space(4);

        // % of units with random offset
        _gridOffsetPercent = Mathf.Clamp(EditorGUILayout.Slider("% with Random Offset", _gridOffsetPercent, 0f, 100f), 0f, 100f);
        EditorGUILayout.Space(4);

        // Offset range for units "misbehaving" (+/- in x and y)
        EditorGUILayout.LabelField("Offset Range (±)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        _gridOffsetX = Mathf.Max(0f, EditorGUILayout.FloatField("X", _gridOffsetX));
        _gridOffsetY = Mathf.Max(0f, EditorGUILayout.FloatField("Y", _gridOffsetY));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("Applied to the two axes of the grid plane when a unit gets random offset.", MessageType.None);
        EditorGUILayout.Space(8);

        // Generate button
        bool canGenerate = _gridEntries.Count > 0 && _gridEntries.Exists(e => e.prefab != null);
        EditorGUI.BeginDisabledGroup(!canGenerate);
        if (GUILayout.Button("Generate Grid", GUILayout.Height(28)))
        {
            ExecuteGrid();
        }
        EditorGUI.EndDisabledGroup();
        if (!canGenerate)
        {
            EditorGUILayout.HelpBox("Add at least one prefab to generate.", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    private void ExecuteGrid()
    {
        var validEntries = new List<GridEntry>();
        float totalPercent = 0f;
        foreach (var e in _gridEntries)
        {
            if (e.prefab != null && e.percent > 0f)
            {
                validEntries.Add(e);
                totalPercent += e.percent;
            }
        }
        if (validEntries.Count == 0)
        {
            Debug.LogWarning("Procedural Level Builder: No valid prefabs for grid.");
            return;
        }
        if (totalPercent <= 0f)
        {
            Debug.LogWarning("Procedural Level Builder: Total percent must be > 0.");
            return;
        }

        var parent = new GameObject("Grid Prefabs");
        Undo.RegisterCreatedObjectUndo(parent, "Grid Prefabs");

        int placed = 0;
        for (int row = 0; row < _gridRows; row++)
        {
            for (int col = 0; col < _gridColumns; col++)
            {
                if (Random.Range(0f, 100f) < _gridGapPercent)
                    continue;

                float a = (col - (_gridColumns - 1) * 0.5f) * _gridSpacing;
                float b = (row - (_gridRows - 1) * 0.5f) * _gridSpacing;

                if (Random.Range(0f, 100f) < _gridOffsetPercent)
                {
                    a += Random.Range(-_gridOffsetX, _gridOffsetX);
                    b += Random.Range(-_gridOffsetY, _gridOffsetY);
                }

                Vector3 pos = _gridCenter;
                switch (_gridAxisPlane)
                {
                    case AxisPlane.XY: pos.x += a; pos.y += b; break;
                    case AxisPlane.XZ: pos.x += a; pos.z += b; break;
                    case AxisPlane.YZ: pos.y += a; pos.z += b; break;
                }

                float r = Random.Range(0f, totalPercent);
                GameObject chosenPrefab = null;
                foreach (var e in validEntries)
                {
                    r -= e.percent;
                    if (r <= 0f)
                    {
                        chosenPrefab = e.prefab;
                        break;
                    }
                }
                if (chosenPrefab == null) chosenPrefab = validEntries[validEntries.Count - 1].prefab;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(chosenPrefab);
                instance.transform.SetParent(parent.transform);
                instance.transform.position = pos;
                Undo.RegisterCreatedObjectUndo(instance, "Grid Prefabs");
                placed++;
            }
        }

        Selection.activeGameObject = parent;
        EditorGUIUtility.PingObject(parent);
        Debug.Log($"Procedural Level Builder: Generated grid with {placed} prefabs ({_gridColumns}x{_gridRows}, {100f - _gridGapPercent:F0}% fill).");
    }

    private void DrawPerlinNoiseScatterTool()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Perlin Noise Scatter Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Grid placement with Perlin noise as a mask. Prefabs are placed on a regular grid—only where noise falls within the specified range (white areas). Empty cells are masked out. Use for terrain tiles, vegetation, or patterned placement.",
            MessageType.None);
        EditorGUILayout.Space(4);

        // Prefabs to scatter (List) with % for each
        EditorGUILayout.LabelField("Prefabs to Scatter", EditorStyles.boldLabel);
        for (int i = 0; i < _perlinEntries.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            _perlinEntries[i].prefab = (GameObject)EditorGUILayout.ObjectField(_perlinEntries[i].prefab, typeof(GameObject), false, GUILayout.Width(180));
            EditorGUILayout.LabelField("%", GUILayout.Width(14));
            _perlinEntries[i].percent = Mathf.Max(0f, EditorGUILayout.FloatField(_perlinEntries[i].percent, GUILayout.Width(50)));
            if (GUILayout.Button("−", GUILayout.Width(22)))
            {
                _perlinEntries.RemoveAt(i);
                i--;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+ Add Prefab"))
        {
            _perlinEntries.Add(new PerlinScatterEntry { prefab = null, percent = 50f });
        }
        EditorGUILayout.Space(4);

        // Grid
        EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        _perlinColumns = Mathf.Max(1, EditorGUILayout.IntField("Columns", _perlinColumns));
        _perlinRows = Mathf.Max(1, EditorGUILayout.IntField("Rows", _perlinRows));
        EditorGUILayout.EndHorizontal();
        _perlinSpacing = Mathf.Max(0.01f, EditorGUILayout.FloatField("Spacing", _perlinSpacing));
        EditorGUILayout.Space(4);

        // Axis plane
        _perlinAxisPlane = (AxisPlane)EditorGUILayout.EnumPopup("Axis Plane", _perlinAxisPlane);
        EditorGUILayout.Space(4);

        // Center
        _perlinCenter = EditorGUILayout.Vector3Field("Center", _perlinCenter);
        EditorGUILayout.Space(4);

        // Perlin noise range
        EditorGUILayout.LabelField("Noise Value Range (White Areas)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Prefabs are placed only where noise (0–1) falls within this range. Think of it as black (0) to white (1)—only 'white' areas get prefabs. Narrow range = tighter clusters.", MessageType.None);
        EditorGUILayout.BeginHorizontal();
        _perlinNoiseMin = Mathf.Clamp(EditorGUILayout.FloatField("Min", _perlinNoiseMin), 0f, 1f);
        _perlinNoiseMax = Mathf.Clamp(EditorGUILayout.FloatField("Max", _perlinNoiseMax), 0f, 1f);
        EditorGUILayout.EndHorizontal();
        if (_perlinNoiseMin > _perlinNoiseMax)
        {
            float swap = _perlinNoiseMin;
            _perlinNoiseMin = _perlinNoiseMax;
            _perlinNoiseMax = swap;
        }
        EditorGUILayout.Space(4);

        // Noise scale (size of features)
        _perlinNoiseScale = EditorGUILayout.Slider("Noise Scale", _perlinNoiseScale, 0f, 2f);
        EditorGUILayout.HelpBox("Larger = bigger blobs/features. Smaller = finer detail. Uses multi-octave noise to avoid mirroring and repetition.", MessageType.None);
        EditorGUILayout.Space(4);

        // Noise offset (move the pattern)
        EditorGUILayout.LabelField("Noise Offset", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Moves the noise pattern in X and Y. Use to get different placements without changing seed.", MessageType.None);
        EditorGUILayout.BeginHorizontal();
        _perlinOffsetX = EditorGUILayout.FloatField("X", _perlinOffsetX);
        _perlinOffsetY = EditorGUILayout.FloatField("Y", _perlinOffsetY);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);

        // Random position offset per element
        EditorGUILayout.LabelField("Random Position Offset", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Optional random offset (±) applied to each element's position for a less rigid grid.", MessageType.None);
        EditorGUILayout.BeginHorizontal();
        _perlinRandomOffsetX = Mathf.Max(0f, EditorGUILayout.FloatField("X (±)", _perlinRandomOffsetX));
        _perlinRandomOffsetY = Mathf.Max(0f, EditorGUILayout.FloatField("Y (±)", _perlinRandomOffsetY));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);

        // Random scale
        _perlinUseRandomScale = EditorGUILayout.Toggle("Random Scale per Object", _perlinUseRandomScale);
        if (_perlinUseRandomScale)
        {
            EditorGUILayout.BeginHorizontal();
            _perlinScaleMin = Mathf.Max(0.01f, EditorGUILayout.FloatField("Scale Min", _perlinScaleMin));
            _perlinScaleMax = Mathf.Max(0.01f, EditorGUILayout.FloatField("Scale Max", _perlinScaleMax));
            EditorGUILayout.EndHorizontal();
            if (_perlinScaleMin > _perlinScaleMax)
            {
                float swap = _perlinScaleMin;
                _perlinScaleMin = _perlinScaleMax;
                _perlinScaleMax = swap;
            }
        }
        EditorGUILayout.Space(8);

        // Execute button
        bool canScatter = _perlinEntries.Count > 0 && _perlinEntries.Exists(e => e.prefab != null);
        EditorGUI.BeginDisabledGroup(!canScatter);
        if (GUILayout.Button("Generate Perlin Noise Grid", GUILayout.Height(28)))
        {
            ExecutePerlinNoiseScatter();
        }
        EditorGUI.EndDisabledGroup();
        if (!canScatter)
        {
            EditorGUILayout.HelpBox("Add at least one prefab to scatter.", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    private static float SampleMultiOctaveNoise(float x, float y)
    {
        // Multi-octave noise breaks symmetry and repetition inherent in single Perlin noise.
        // Each octave samples a different frequency and offset within the noise space.
        float n1 = Mathf.PerlinNoise(x, y);
        float n2 = Mathf.PerlinNoise(x * 2f + 100f, y * 2f + 200f);
        float n3 = Mathf.PerlinNoise(x * 4f + 300f, y * 4f + 400f);
        return Mathf.Clamp01(n1 * 0.5f + n2 * 0.3f + n3 * 0.2f);
    }

    private void ExecutePerlinNoiseScatter()
    {
        var validEntries = new List<PerlinScatterEntry>();
        float totalPercent = 0f;
        foreach (var e in _perlinEntries)
        {
            if (e.prefab != null && e.percent > 0f)
            {
                validEntries.Add(e);
                totalPercent += e.percent;
            }
        }
        if (validEntries.Count == 0)
        {
            Debug.LogWarning("Procedural Level Builder: No valid prefabs for Perlin noise grid.");
            return;
        }
        if (totalPercent <= 0f)
        {
            Debug.LogWarning("Procedural Level Builder: Total percent must be > 0.");
            return;
        }

        var parent = new GameObject("Perlin Noise Grid Prefabs");
        Undo.RegisterCreatedObjectUndo(parent, "Perlin Noise Grid");

        int placed = 0;
        for (int row = 0; row < _perlinRows; row++)
        {
            for (int col = 0; col < _perlinColumns; col++)
            {
                float a = (col - (_perlinColumns - 1) * 0.5f) * _perlinSpacing;
                float b = (row - (_perlinRows - 1) * 0.5f) * _perlinSpacing;

                float sampleX = a * _perlinNoiseScale + _perlinOffsetX;
                float sampleY = b * _perlinNoiseScale + _perlinOffsetY;
                float noiseValue = SampleMultiOctaveNoise(sampleX, sampleY);

                if (noiseValue < _perlinNoiseMin || noiseValue > _perlinNoiseMax)
                    continue;

                float offsetA = a + Random.Range(-_perlinRandomOffsetX, _perlinRandomOffsetX);
                float offsetB = b + Random.Range(-_perlinRandomOffsetY, _perlinRandomOffsetY);

                Vector3 pos = _perlinCenter;
                switch (_perlinAxisPlane)
                {
                    case AxisPlane.XY: pos.x += offsetA; pos.y += offsetB; break;
                    case AxisPlane.XZ: pos.x += offsetA; pos.z += offsetB; break;
                    case AxisPlane.YZ: pos.y += offsetA; pos.z += offsetB; break;
                }

                float r = Random.Range(0f, totalPercent);
                GameObject chosenPrefab = null;
                foreach (var e in validEntries)
                {
                    r -= e.percent;
                    if (r <= 0f)
                    {
                        chosenPrefab = e.prefab;
                        break;
                    }
                }
                if (chosenPrefab == null) chosenPrefab = validEntries[validEntries.Count - 1].prefab;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(chosenPrefab);
                instance.transform.SetParent(parent.transform);
                instance.transform.position = pos;

                if (_perlinUseRandomScale)
                {
                    float scale = Random.Range(_perlinScaleMin, _perlinScaleMax);
                    instance.transform.localScale = new Vector3(scale, scale, scale);
                }

                Undo.RegisterCreatedObjectUndo(instance, "Perlin Noise Grid");
                placed++;
            }
        }

        Selection.activeGameObject = parent;
        EditorGUIUtility.PingObject(parent);
        Debug.Log($"Procedural Level Builder: Perlin noise grid placed {placed} prefabs ({_perlinColumns}x{_perlinRows} grid, noise mask applied).");
    }

    private void DrawPlannedPlaceholder()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.HelpBox("This tool is planned for future implementation.", MessageType.Info);
        EditorGUILayout.EndVertical();
    }
}
