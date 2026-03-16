using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Resource Builder window that guides users through creating resources with templates.
/// Resources define what flows through the chain: Source (gather) → Transform (craft) → Sink (deliver).
/// </summary>
public class SA_ResourceBuilderWindow : EditorWindow
{
    private const string DefaultTemplatePrefabPath = "Samples/Prefabs/Resources/resource_obj_template.prefab";

    private GameObject prefabTemplate;
    private string resourceName = "New Resource";
    private Sprite resourceIcon;
    private Color resourceColor = Color.white;
    private float resourceScale = 0.4f;
    private Resource.ResourceBehavior typeOfBehavior = Resource.ResourceBehavior.Static;
    private float lifespan = 10f;
    private Vector2 scrollPosition;

    private enum ResourceTemplate
    {
        None,
        Static,
        Decays,
        Consumable
    }
    private ResourceTemplate selectedTemplate = ResourceTemplate.None;

    private static readonly string[] TemplateDisplayNames = new[]
    {
        "None",
        "Static (permanent, grab & move)",
        "Decays (loses value over time)",
        "Consumable (instant collect on contact)"
    };

    private static readonly string[] TemplateDescriptions = new[]
    {
        "",
        "A permanent store of value. Players grab and move these into station input areas. They persist until consumed or delivered. Use for wood, ore, planks, ingots—anything that flows through the chain.",
        "A resource that loses value over time. Players must grab and move it into stations before it expires. Use for food that spoils, fuel that burns off, or time-sensitive materials.",
        "Instantly collected upon contact. No grab-and-carry—players absorb these on touch. Use for coins, power-ups, or resources that are consumed immediately."
    };

    [MenuItem("Game Assemblies/Resources/Resource Builder")]
    public static void ShowWindow()
    {
        var window = GetWindow<SA_ResourceBuilderWindow>("Resource Builder");
        window.minSize = new Vector2(450, 420);
    }

    private void OnEnable()
    {
        if (prefabTemplate == null)
        {
            prefabTemplate = SA_AssetPathHelper.FindPrefab(DefaultTemplatePrefabPath);
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        DrawTemplates();
        DrawFields();
        DrawCreateButton();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(8);
        GUILayout.Label("Resource Builder", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Resources flow through the chain: players gather from stations, carry to other stations, and deliver to complete goals. " +
            "Behavior (Static, Decays, Consumable) defines how the resource is picked up and whether it persists over time.",
            MessageType.Info);
        EditorGUILayout.Space(4);
    }

    private void DrawTemplates()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Templates", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Selecting a template updates Behavior, Lifespan, and suggested name. You can customize all values after.",
            MessageType.None);
        EditorGUILayout.BeginHorizontal();
        int templateIndex = EditorGUILayout.Popup(
            new GUIContent("Template", "Predefined resource types. Selecting a template updates Behavior and Lifespan automatically."),
            (int)selectedTemplate,
            TemplateDisplayNames);
        ResourceTemplate newSelection = (ResourceTemplate)templateIndex;
        if (newSelection != selectedTemplate)
        {
            selectedTemplate = newSelection;
            ApplyTemplate(selectedTemplate);
        }
        else
        {
            selectedTemplate = newSelection;
        }
        if (GUILayout.Button("Apply Template", GUILayout.Width(120)))
        {
            ApplyTemplate(selectedTemplate);
        }
        EditorGUILayout.EndHorizontal();
        if (selectedTemplate != ResourceTemplate.None && (int)selectedTemplate < TemplateDescriptions.Length)
        {
            EditorGUILayout.Space(2);
            EditorGUILayout.HelpBox(TemplateDescriptions[(int)selectedTemplate], MessageType.None);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void ApplyTemplate(ResourceTemplate template)
    {
        if (template == ResourceTemplate.None) return;

        switch (template)
        {
            case ResourceTemplate.Static:
                resourceName = "Wood";
                typeOfBehavior = Resource.ResourceBehavior.Static;
                lifespan = 10f;
                break;
            case ResourceTemplate.Decays:
                resourceName = "Fresh Food";
                typeOfBehavior = Resource.ResourceBehavior.Decays;
                lifespan = 30f;
                break;
            case ResourceTemplate.Consumable:
                resourceName = "Coins";
                typeOfBehavior = Resource.ResourceBehavior.Consumable;
                lifespan = 10f;
                break;
        }
    }

    private void DrawFields()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Prefab Template", EditorStyles.boldLabel);
        prefabTemplate = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Resource Prefab Template", "Prefab used as the base for new resources. Must have ResourceObject and SpriteRenderer."),
            prefabTemplate,
            typeof(GameObject),
            false);
        if (prefabTemplate == null)
        {
            EditorGUILayout.HelpBox("No template selected. The default resource_obj_template prefab will be used.", MessageType.Warning);
        }
        EditorGUILayout.Space(4);

        GUILayout.Label("Resource Details", EditorStyles.boldLabel);
        resourceName = EditorGUILayout.TextField(
            new GUIContent("Resource Name", "Display name used in stations, goals, and UI."),
            resourceName);
        resourceIcon = (Sprite)EditorGUILayout.ObjectField(
            new GUIContent("Icon", "Sprite shown in UI, info panels, and goal trackers."),
            resourceIcon,
            typeof(Sprite),
            false);
        resourceColor = EditorGUILayout.ColorField(
            new GUIContent("Sprite Color", "Color tint applied to the resource sprite. Use white for no tint."),
            resourceColor);
        resourceScale = EditorGUILayout.Slider(
            new GUIContent("Scale", "Uniform scale applied to the resource prefab. Helps reduce pixelation of pixel-art sprites on screen."),
            resourceScale,
            0.1f,
            2f);
        EditorGUILayout.HelpBox(
            "A scale of 0.4 helps reduce pixelation of pixel-art sprites when displayed on screen.",
            MessageType.Info);
        typeOfBehavior = (Resource.ResourceBehavior)EditorGUILayout.EnumPopup(
            new GUIContent("Behavior", "Static: permanent, grab & move. Decays: expires after lifespan. Consumable: instant collect on contact."),
            typeOfBehavior);
        EditorGUI.BeginDisabledGroup(typeOfBehavior != Resource.ResourceBehavior.Decays);
        lifespan = EditorGUILayout.FloatField(
            new GUIContent("Lifespan (seconds)", "How long before this resource decays and is destroyed. Only used when Behavior is Decays."),
            Mathf.Max(0.1f, lifespan));
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.HelpBox(
            "A prefab will be created from the resource template and linked automatically. " +
            "Assign the icon; the prefab's sprite will match.",
            MessageType.None);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawCreateButton()
    {
        EditorGUILayout.Space(4);
        var createRect = GUILayoutUtility.GetRect(0, 36);
        createRect.x += 20;
        createRect.width -= 40;

        bool canCreate = !string.IsNullOrWhiteSpace(resourceName);

        EditorGUI.BeginDisabledGroup(!canCreate);
        if (GUI.Button(createRect, "Create Resource"))
        {
            CreateResource();
        }
        EditorGUI.EndDisabledGroup();

        if (!canCreate)
        {
            EditorGUILayout.HelpBox("Enter a resource name to create.", MessageType.Warning);
        }

        EditorGUILayout.Space(4);
    }

    private void CreateResource()
    {
        GameObject templatePrefab = prefabTemplate;
        if (templatePrefab == null)
        {
            templatePrefab = SA_AssetPathHelper.FindPrefab(DefaultTemplatePrefabPath);
        }
        if (templatePrefab == null)
        {
            Debug.LogError($"Resource Builder: Template prefab not found at {DefaultTemplatePrefabPath}");
            return;
        }

        string templatePrefabPath = AssetDatabase.GetAssetPath(templatePrefab);
        SA_AssetPathHelper.EnsureAssetPathDirectories("Game Assemblies/Prefabs/Resources");
        SA_AssetPathHelper.EnsureAssetPathDirectories("Game Assemblies/Databases/Resources");

        string newPrefabPath = $"Assets/Game Assemblies/Prefabs/Resources/{resourceName}.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath) != null)
        {
            Debug.LogError($"Resource Builder: A prefab with this name already exists: {newPrefabPath}");
            return;
        }

        bool copySuccess = AssetDatabase.CopyAsset(templatePrefabPath, newPrefabPath);
        if (!copySuccess)
        {
            Debug.LogError($"Resource Builder: Failed to copy prefab template to {newPrefabPath}");
            return;
        }

        Resource newAsset = ScriptableObject.CreateInstance<Resource>();
        newAsset.name = resourceName;
        newAsset.resourceName = resourceName;
        newAsset.icon = resourceIcon;
        newAsset.iconTint = resourceColor;
        newAsset.typeOfBehavior = typeOfBehavior;
        newAsset.lifespan = lifespan;

        string assetPath = $"Assets/Game Assemblies/Databases/Resources/{resourceName}.asset";
        AssetDatabase.CreateAsset(newAsset, assetPath);
        AssetDatabase.SaveAssets();

        GameObject newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath);
        if (newPrefab != null)
        {
            var resourceObj = newPrefab.GetComponent<ResourceObject>();
            if (resourceObj != null) resourceObj.resourceType = newAsset;
            var sr = newPrefab.GetComponent<SpriteRenderer>() ?? newPrefab.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                if (resourceIcon != null)
                {
                    sr.sprite = resourceIcon;
                    ResizeCollidersToSprite(newPrefab, resourceIcon, sr);
                }
                sr.color = resourceColor;
            }
            newPrefab.transform.localScale = new Vector3(resourceScale, resourceScale, resourceScale);
            newAsset.resourcePrefab = newPrefab;

            // Set MultiTag to match typeOfBehavior so prefab behaves as Resource data specifies.
            var multiTag = newPrefab.GetComponent<MultiTag>();
            if (multiTag != null)
            {
                multiTag.RemoveTag(TagType.Consumable);
                multiTag.RemoveTag(TagType.Grabbable);
                multiTag.AddTag(TagType.Resource);
                if (typeOfBehavior == Resource.ResourceBehavior.Consumable)
                    multiTag.AddTag(TagType.Consumable);
                else
                    multiTag.AddTag(TagType.Grabbable);
            }

            EditorUtility.SetDirty(newPrefab);
            EditorUtility.SetDirty(newAsset);
            AssetDatabase.SaveAssets();
        }

        Selection.activeObject = newAsset;
        EditorGUIUtility.PingObject(newAsset);
        Debug.Log($"Resource Builder: Created '{resourceName}' - SO at {assetPath}, prefab at {newPrefabPath}");
    }

    private static void ResizeCollidersToSprite(GameObject prefab, Sprite sprite, SpriteRenderer spriteRenderer)
    {
        Bounds bounds = sprite.bounds;
        Vector2 size = bounds.size;
        Vector2 center = bounds.center;

        foreach (var collider in prefab.GetComponentsInChildren<Collider2D>())
        {
            if (collider is BoxCollider2D box)
            {
                box.size = size;
                box.offset = center;
            }
            else if (collider is CircleCollider2D circle)
            {
                float radius = Mathf.Min(size.x, size.y) * 0.5f;
                circle.radius = radius;
                circle.offset = center;
            }
            else if (collider is PolygonCollider2D poly)
            {
                List<Vector2> physicsShape = new List<Vector2>();
                if (sprite.GetPhysicsShapeCount() > 0)
                {
                    sprite.GetPhysicsShape(0, physicsShape);
                    poly.points = physicsShape.ToArray();
                }
                else
                {
                    Vector2 extents = size * 0.5f;
                    poly.points = new Vector2[]
                    {
                        center + new Vector2(-extents.x, -extents.y),
                        center + new Vector2(extents.x, -extents.y),
                        center + new Vector2(extents.x, extents.y),
                        center + new Vector2(-extents.x, extents.y)
                    };
                }
            }
        }
    }
}
