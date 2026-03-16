using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor window that lists all ScriptableObject assets of a selected database type.
/// Left panel: compact scrollable list. Right panel: editable inspector with icon when available.
/// </summary>
public class SA_DatabaseInspectorWindow : EditorWindow
{
    private const string SplitPrefKey = "SA_DatabaseInspector_SplitPosition";
    private const float MinListWidth = 150f;
    private const float MinInspectorWidth = 200f;
    private const float DefaultListWidth = 240f;

    private int _selectedDatabaseIndex;
    private Vector2 _listScrollPosition;
    private Vector2 _inspectorScrollPosition;
    private string _searchFilter = "";
    private ScriptableObject _selectedAsset;
    private Editor _cachedEditor;
    private float _listWidth = DefaultListWidth;
    private bool _isDraggingSplitter;
    private Dictionary<ScriptableObject, int> _validationResults = new Dictionary<ScriptableObject, int>();

    // Optional fields that are commonly left empty - excluded from "missing reference" validation
    private static readonly HashSet<string> OptionalFieldNames = new HashSet<string>
    {
        "previewImage", "preview", "sprite", "resourcePrefab", "icon"
    };

    // Unity internal serialization fields - never declared in user ScriptableObjects, skip these
    private static readonly HashSet<string> UnityInternalFieldNames = new HashSet<string>
    {
        "m_Script", "m_CorrespondingSourceObject", "m_PrefabInstance", "m_PrefabAsset",
        "m_GameObject", "m_FileID", "m_PathID", "m_Component", "m_LocalEulerAnglesHint"
    };

    [MenuItem("Game Assemblies/Databases/Database Inspector")]
    public static void ShowWindow()
    {
        var window = GetWindow<SA_DatabaseInspectorWindow>("Database Inspector");
        window.minSize = new Vector2(400, 300);
    }

    private void OnEnable()
    {
        SA_DatabaseManager.ClearCache();
        _listWidth = EditorPrefs.GetFloat(SplitPrefKey, DefaultListWidth);
    }

    private void OnDisable()
    {
        DestroyCachedEditor();
    }

    private void DestroyCachedEditor()
    {
        if (_cachedEditor != null)
        {
            DestroyImmediate(_cachedEditor);
            _cachedEditor = null;
        }
    }

    private void OnGUI()
    {
        DrawToolbar();

        EditorGUILayout.BeginVertical();
        EditorGUILayout.BeginHorizontal();

        // Left panel: list
        DrawListPanel();

        // Resizable splitter
        DrawSplitter();

        // Right panel: inspector
        DrawInspectorPanel();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Database Type", GUILayout.Width(80));
        int newIndex = EditorGUILayout.Popup(_selectedDatabaseIndex, GetDisplayNames());
        if (newIndex != _selectedDatabaseIndex)
        {
            _selectedDatabaseIndex = newIndex;
            _selectedAsset = null;
            _validationResults.Clear();
            DestroyCachedEditor();
        }

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Search", GUILayout.Width(40));
        _searchFilter = EditorGUILayout.TextField(_searchFilter, GUILayout.Width(120));

        if (GUILayout.Button("Validate", GUILayout.Width(55)))
        {
            ValidateAllDatabases();
        }
        if (GUILayout.Button("Refresh", GUILayout.Width(50)))
        {
            var refreshType = SA_DatabaseManager.DatabaseTypes[_selectedDatabaseIndex].Type;
            SA_DatabaseManager.Refresh(refreshType);
            _validationResults.Clear();
            Repaint();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);
    }

    private void DrawListPanel()
    {
        var type = SA_DatabaseManager.DatabaseTypes[_selectedDatabaseIndex].Type;
        var entries = SA_DatabaseManager.GetEntries(type);

        string filter = _searchFilter?.Trim().ToLowerInvariant() ?? "";
        var filtered = new List<ScriptableObject>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrEmpty(filter) || (entry != null && entry.name.ToLowerInvariant().Contains(filter)))
                filtered.Add(entry);
        }

        EditorGUILayout.BeginVertical(GUILayout.Width(_listWidth), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField($"{filtered.Count} entries", EditorStyles.miniLabel);
        EditorGUILayout.Space(2);

        _listScrollPosition = EditorGUILayout.BeginScrollView(_listScrollPosition, GUILayout.ExpandHeight(true));

        for (int i = 0; i < filtered.Count; i++)
        {
            var asset = filtered[i];
            if (asset == null) continue;

            bool isSelected = asset == _selectedAsset;
            var bgColor = isSelected ? new Color(0.3f, 0.5f, 0.8f, 0.4f) : (i % 2 == 0 ? new Color(1, 1, 1, 0.03f) : Color.clear);
            var prevBg = GUI.backgroundColor;
            GUI.backgroundColor = bgColor;

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(22));

            // Validation status indicator (centered vertically in row)
            EditorGUILayout.BeginVertical(GUILayout.Width(12), GUILayout.Height(22));
            GUILayout.Space(5);
            DrawValidationIndicator(asset);
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
            GUILayout.Space(4);

            if (GUILayout.Button(asset.name, EditorStyles.label, GUILayout.ExpandWidth(true), GUILayout.Height(20)))
            {
                _selectedAsset = asset;
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
                DestroyCachedEditor();
            }

            var pingContent = EditorGUIUtility.IconContent("d_Project");
            if (pingContent == null) pingContent = new GUIContent("→", "Select in Project");
            else pingContent.tooltip = "Select in Project";
            if (GUILayout.Button(pingContent, GUILayout.Width(22), GUILayout.Height(22)))
            {
                _selectedAsset = asset;
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = prevBg;
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void ValidateAllDatabases()
    {
        _validationResults.Clear();
        var log = new System.Text.StringBuilder();
        log.AppendLine("[Database Inspector] Validation Report");
        log.AppendLine("========================================");

        foreach (var (type, displayName) in SA_DatabaseManager.DatabaseTypes)
        {
            SA_DatabaseManager.Refresh(type);
            var entries = SA_DatabaseManager.GetEntries(type);
            log.AppendLine();
            log.AppendLine($"Database: {displayName} ({entries.Count} entries)");

            foreach (var entry in entries)
            {
                if (entry == null) continue;

                var missing = GetMissingReferences(entry);
                _validationResults[entry] = missing.Count;

                if (missing.Count == 0)
                {
                    log.AppendLine($"  [OK] {entry.name}");
                }
                else
                {
                    log.AppendLine($"  [MISSING] {entry.name}:");
                    foreach (var path in missing)
                        log.AppendLine($"    - {path}");
                }
            }
        }

        log.AppendLine();
        log.AppendLine("========================================");
        log.AppendLine("Validation complete.");
        Debug.Log(log.ToString());
        Repaint();
    }

    private List<string> GetMissingReferences(ScriptableObject obj)
    {
        var missing = new List<string>();
        if (obj == null) return missing;

        var so = new SerializedObject(obj);
        var prop = so.GetIterator();
        prop.Next(true);

        do
        {
            var fieldName = GetLeafFieldName(prop.propertyPath);
            if (UnityInternalFieldNames.Contains(fieldName)) continue;
            if (prop.propertyType != SerializedPropertyType.ObjectReference) continue;
            if (prop.objectReferenceValue != null) continue;
            if (OptionalFieldNames.Contains(fieldName)) continue;

            missing.Add(prop.propertyPath);
        }
        while (prop.Next(true));

        return missing;
    }

    private static string GetLeafFieldName(string propertyPath)
    {
        var parts = propertyPath.Split('.');
        return parts.Length > 0 ? parts[parts.Length - 1] : propertyPath;
    }

    private void DrawValidationIndicator(ScriptableObject asset)
    {
        const int size = 12;
        var rect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
        string tooltip;

        if (!_validationResults.TryGetValue(asset, out int missingCount))
        {
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            tooltip = "Click Validate to check for missing references";
        }
        else if (missingCount == 0)
        {
            var greenIcon = EditorGUIUtility.IconContent("d_greenLight");
            if (greenIcon != null && greenIcon.image != null)
                GUI.DrawTexture(rect, greenIcon.image, ScaleMode.ScaleToFit);
            else
                EditorGUI.DrawRect(rect, new Color(0.2f, 0.8f, 0.2f, 0.9f));
            tooltip = "Valid - no missing references";
        }
        else
        {
            var redIcon = EditorGUIUtility.IconContent("d_redLight");
            if (redIcon != null && redIcon.image != null)
                GUI.DrawTexture(rect, redIcon.image, ScaleMode.ScaleToFit);
            else
                EditorGUI.DrawRect(rect, new Color(0.9f, 0.2f, 0.2f, 0.9f));
            tooltip = $"{missingCount} missing reference(s) - needs fixing";
        }

        EditorGUI.LabelField(rect, new GUIContent("", tooltip));
    }

    private Texture2D GetThumbnail(ScriptableObject obj)
    {
        if (obj == null) return null;

        var so = new SerializedObject(obj);
        var iterator = so.GetIterator();
        iterator.Next(true);

        while (iterator.NextVisible(false))
        {
            if (iterator.propertyType == SerializedPropertyType.ObjectReference)
            {
                var refObj = iterator.objectReferenceValue;
                if (refObj is Sprite sprite)
                {
                    var preview = AssetPreview.GetAssetPreview(sprite);
                    if (preview != null) return preview;
                    var tex = sprite.texture;
                    if (tex != null) return AssetPreview.GetAssetPreview(tex) ?? tex;
                }
                if (refObj is Texture2D tex2d)
                    return AssetPreview.GetAssetPreview(tex2d) ?? tex2d;
            }
        }

        return AssetPreview.GetAssetPreview(obj);
    }

    private void DrawSplitter()
    {
        var splitterRect = GUILayoutUtility.GetRect(6, 0, GUILayout.ExpandHeight(true), GUILayout.Width(6));

        var evt = Event.current;
        if (evt.type == EventType.Repaint)
        {
            var bgColor = _isDraggingSplitter ? new Color(0.4f, 0.6f, 1f, 0.3f) : new Color(0.5f, 0.5f, 0.5f, 0.3f);
            EditorGUI.DrawRect(splitterRect, bgColor);
        }

        EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

        if (evt.type == EventType.MouseDown && splitterRect.Contains(evt.mousePosition))
        {
            _isDraggingSplitter = true;
            evt.Use();
        }
        else if (_isDraggingSplitter && evt.type == EventType.MouseUp)
        {
            _isDraggingSplitter = false;
            evt.Use();
        }
        else if (_isDraggingSplitter && evt.type == EventType.MouseDrag)
        {
            _listWidth += evt.delta.x;
            _listWidth = Mathf.Clamp(_listWidth, MinListWidth, position.width - MinInspectorWidth);
            EditorPrefs.SetFloat(SplitPrefKey, _listWidth);
            evt.Use();
            Repaint();
        }
    }

    private void DrawInspectorPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        if (_selectedAsset == null)
        {
            EditorGUILayout.Space(40);
            var msg = "Select an item from the list to view and edit its properties.";
            var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true };
            EditorGUILayout.LabelField(msg, style, GUILayout.ExpandWidth(true));
        }
        else
        {
            // Header with large thumbnail
            EditorGUILayout.Space(8);
            DrawInspectorHeader();

            // Resource-specific tools
            if (_selectedAsset is Resource resource)
            {
                DrawResourceAssetTools(resource);
            }

            EditorGUILayout.Space(8);

            // Inspector
            _inspectorScrollPosition = EditorGUILayout.BeginScrollView(_inspectorScrollPosition);

            if (_cachedEditor == null || _cachedEditor.target != _selectedAsset)
            {
                DestroyCachedEditor();
                _cachedEditor = Editor.CreateEditor(_selectedAsset);
            }

            if (_cachedEditor != null)
            {
                EditorGUI.BeginChangeCheck();
                _cachedEditor.OnInspectorGUI();
                if (EditorGUI.EndChangeCheck())
                    _cachedEditor.serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawInspectorHeader()
    {
        var headerThumb = GetThumbnail(_selectedAsset);

        EditorGUILayout.BeginHorizontal();
        if (headerThumb != null)
        {
            var thumbRect = GUILayoutUtility.GetRect(64f, 64f);
            GUI.DrawTexture(thumbRect, headerThumb, ScaleMode.ScaleToFit);
        }

        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(_selectedAsset.name, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(_selectedAsset.GetType().Name), EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawResourceAssetTools(Resource resource)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Prefab Tools", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        bool hasPrefab = resource.resourcePrefab != null;
        EditorGUI.BeginDisabledGroup(!hasPrefab);

        if (GUILayout.Button(new GUIContent("Update Sprite", "Copies the Resource icon to the SpriteRenderer on the linked prefab.")))
        {
            UpdateResourcePrefabSprite(resource);
        }
        if (GUILayout.Button(new GUIContent("Update Collider", "Resizes the prefab's Collider2D to match the sprite bounds.")))
        {
            UpdateResourcePrefabCollider(resource);
        }

        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        if (!hasPrefab)
            EditorGUILayout.HelpBox("Assign a prefab to this resource to use these tools.", MessageType.Info);

        EditorGUILayout.EndVertical();
    }

    private void UpdateResourcePrefabSprite(Resource resource)
    {
        if (resource == null || resource.resourcePrefab == null)
        {
            Debug.LogWarning("Database Inspector: Resource has no prefab assigned.");
            return;
        }
        if (resource.icon == null)
        {
            Debug.LogWarning("Database Inspector: Resource has no icon assigned.");
            return;
        }

        var sr = resource.resourcePrefab.GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogWarning($"Database Inspector: Prefab '{resource.resourcePrefab.name}' has no SpriteRenderer.");
            return;
        }

        Undo.RecordObject(sr, "Update Resource Prefab Sprite");
        sr.sprite = resource.icon;
        EditorUtility.SetDirty(resource.resourcePrefab);
        AssetDatabase.SaveAssets();
        Debug.Log($"Database Inspector: Updated sprite on prefab '{resource.resourcePrefab.name}' to match resource icon.");
    }

    private void UpdateResourcePrefabCollider(Resource resource)
    {
        if (resource == null || resource.resourcePrefab == null)
        {
            Debug.LogWarning("Database Inspector: Resource has no prefab assigned.");
            return;
        }

        var sr = resource.resourcePrefab.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            Debug.LogWarning($"Database Inspector: Prefab '{resource.resourcePrefab.name}' has no SpriteRenderer with a sprite.");
            return;
        }

        var bounds = sr.sprite.bounds;
        var collider = resource.resourcePrefab.GetComponentInChildren<Collider2D>();
        if (collider == null)
        {
            Debug.LogWarning($"Database Inspector: Prefab '{resource.resourcePrefab.name}' has no Collider2D.");
            return;
        }

        if (collider is CircleCollider2D circle)
        {
            Undo.RecordObject(circle, "Update Resource Prefab Collider");
            circle.offset = bounds.center;
            float radius = Mathf.Sqrt(bounds.extents.x * bounds.extents.x + bounds.extents.y * bounds.extents.y);
            circle.radius = radius;
            EditorUtility.SetDirty(resource.resourcePrefab);
        }
        else if (collider is BoxCollider2D box)
        {
            Undo.RecordObject(box, "Update Resource Prefab Collider");
            box.offset = bounds.center;
            box.size = bounds.size;
            EditorUtility.SetDirty(resource.resourcePrefab);
        }
        else
        {
            Debug.LogWarning($"Database Inspector: Collider type '{collider.GetType().Name}' is not supported. Use CircleCollider2D or BoxCollider2D.");
            return;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Database Inspector: Updated collider on prefab '{resource.resourcePrefab.name}' to match sprite bounds.");
    }

    private string[] GetDisplayNames()
    {
        var types = SA_DatabaseManager.DatabaseTypes;
        var names = new string[types.Length];
        for (int i = 0; i < types.Length; i++)
            names[i] = types[i].DisplayName;
        return names;
    }
}
