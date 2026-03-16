using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SA_CreateEnvironmentWindow : EditorWindow
{
    private enum EnvironmentObjectType
    {
        Obstacle,
        GroundTile,
        Foliage
    }

    private EnvironmentObjectType objectType = EnvironmentObjectType.Obstacle;
    private string objectName = "NewEnvironmentObject";
    private Sprite objectSprite;
    private Color spriteTint = Color.white;
    private float prefabScale = 1f;
    
    // Prefab templates
    private GameObject obstaclePrefabTemplate;
    private GameObject groundTilePrefabTemplate;
    private GameObject foliagePrefabTemplate;
    
    // Default template paths
    private const string DEFAULT_OBSTACLE_PREFAB_PATH = "Samples/Prefabs/Obstacle_Template.prefab";
    private const string DEFAULT_GROUND_TILE_PREFAB_PATH = "Samples/Prefabs/Groundtile_Template.prefab";
    private const string DEFAULT_FOLIAGE_PREFAB_PATH = "Samples/Prefabs/Folliage_Template.prefab";

    [MenuItem("Game Assemblies/Environment/Create Environment Object")]
    public static void ShowWindow()
    {
        GetWindow<SA_CreateEnvironmentWindow>("Create Environment Object");
    }

    private void OnEnable()
    {
        // Load default templates when window opens
        if (obstaclePrefabTemplate == null)
        {
            obstaclePrefabTemplate = SA_AssetPathHelper.FindPrefab(DEFAULT_OBSTACLE_PREFAB_PATH);
        }
        if (groundTilePrefabTemplate == null)
        {
            groundTilePrefabTemplate = SA_AssetPathHelper.FindPrefab(DEFAULT_GROUND_TILE_PREFAB_PATH);
        }
        if (foliagePrefabTemplate == null)
        {
            foliagePrefabTemplate = SA_AssetPathHelper.FindPrefab(DEFAULT_FOLIAGE_PREFAB_PATH);
        }
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Create Environment Object", EditorStyles.boldLabel);
        GUILayout.Label("Create obstacles, ground tiles, or foliage from sprites.");
        GUILayout.Space(20);

        // Object Type Selection
        EditorGUILayout.LabelField("Object Type", EditorStyles.boldLabel);
        objectType = (EnvironmentObjectType)EditorGUILayout.EnumPopup("Type", objectType);
        
        GUILayout.Space(5);
        
        // Show description based on type
        switch (objectType)
        {
            case EnvironmentObjectType.Obstacle:
                EditorGUILayout.HelpBox(
                    "Obstacles have solid colliders that block player movement. Use for walls, rocks, or any object players cannot pass through. Parented under 'Environment' in the scene.",
                    MessageType.Info);
                break;
            case EnvironmentObjectType.GroundTile:
                EditorGUILayout.HelpBox(
                    "Ground Tiles have no collision—players walk directly over them. Use for floor textures, paths, or decorative surfaces. Parented under 'Tiles' in the scene.",
                    MessageType.Info);
                break;
            case EnvironmentObjectType.Foliage:
                EditorGUILayout.HelpBox(
                    "Foliage does not block movement; players pass through and trigger a wiggle animation. Use for grass, bushes, flowers, or other decorative elements that react to the player.",
                    MessageType.Info);
                break;
        }

        GUILayout.Space(15);

        // Prefab Template Section
        EditorGUILayout.LabelField("Prefab Template", EditorStyles.boldLabel);
        
        if (objectType == EnvironmentObjectType.Obstacle)
        {
            obstaclePrefabTemplate = (GameObject)EditorGUILayout.ObjectField("Obstacle Template", obstaclePrefabTemplate, typeof(GameObject), false);
            if (obstaclePrefabTemplate == null)
            {
                EditorGUILayout.HelpBox("No template selected. The default Obstacle_Template prefab will be used.", MessageType.Warning);
            }
        }
        else if (objectType == EnvironmentObjectType.GroundTile)
        {
            groundTilePrefabTemplate = (GameObject)EditorGUILayout.ObjectField("Ground Tile Template", groundTilePrefabTemplate, typeof(GameObject), false);
            if (groundTilePrefabTemplate == null)
            {
                EditorGUILayout.HelpBox("No template selected. The default Groundtile_Template prefab will be used.", MessageType.Warning);
            }
        }
        else // Foliage
        {
            foliagePrefabTemplate = (GameObject)EditorGUILayout.ObjectField("Foliage Template", foliagePrefabTemplate, typeof(GameObject), false);
            if (foliagePrefabTemplate == null)
            {
                EditorGUILayout.HelpBox("No template selected. The default Folliage_Template prefab will be used.", MessageType.Warning);
            }
        }

        GUILayout.Space(15);

        // Object Details Section
        EditorGUILayout.LabelField("Object Details", EditorStyles.boldLabel);
        objectName = EditorGUILayout.TextField("Object Name", objectName);
        objectSprite = (Sprite)EditorGUILayout.ObjectField("Sprite", objectSprite, typeof(Sprite), false);
        spriteTint = EditorGUILayout.ColorField("Sprite Tint", spriteTint);
        prefabScale = EditorGUILayout.Slider("Scale", prefabScale, 0.1f, 5f);

        GUILayout.Space(10);

        // Output path info
        string outputFolder = GetOutputFolderForType(objectType);
        EditorGUILayout.LabelField("Output Path:", $"Assets/{outputFolder}/{objectName}.prefab", EditorStyles.miniLabel);

        GUILayout.Space(20);

        // Validation
        bool canCreate = true;
        if (string.IsNullOrWhiteSpace(objectName))
        {
            EditorGUILayout.HelpBox("Please enter a name for the object.", MessageType.Error);
            canCreate = false;
        }
        if (objectSprite == null)
        {
            EditorGUILayout.HelpBox("Please assign a sprite for the object.", MessageType.Error);
            canCreate = false;
        }

        // Create Button
        EditorGUI.BeginDisabledGroup(!canCreate);
        if (GUILayout.Button("Create Prefab", GUILayout.Height(30)))
        {
            CreatePrefab();
        }
        EditorGUI.EndDisabledGroup();
    }

    private static string GetOutputFolderForType(EnvironmentObjectType type)
    {
        return type switch
        {
            EnvironmentObjectType.Obstacle => "Game Assemblies/Prefabs/Environment/Obstacles",
            EnvironmentObjectType.GroundTile => "Game Assemblies/Prefabs/Environment/Tiles",
            EnvironmentObjectType.Foliage => "Game Assemblies/Prefabs/Environment/Foliage",
            _ => "Game Assemblies/Prefabs/Environment"
        };
    }

    private void CreatePrefab()
    {
        // Get the appropriate template
        GameObject templatePrefab = objectType switch
        {
            EnvironmentObjectType.Obstacle => obstaclePrefabTemplate,
            EnvironmentObjectType.GroundTile => groundTilePrefabTemplate,
            EnvironmentObjectType.Foliage => foliagePrefabTemplate,
            _ => obstaclePrefabTemplate
        };

        // Fall back to defaults if no template selected
        if (templatePrefab == null)
        {
            string defaultPath = objectType switch
            {
                EnvironmentObjectType.Obstacle => DEFAULT_OBSTACLE_PREFAB_PATH,
                EnvironmentObjectType.GroundTile => DEFAULT_GROUND_TILE_PREFAB_PATH,
                EnvironmentObjectType.Foliage => DEFAULT_FOLIAGE_PREFAB_PATH,
                _ => DEFAULT_OBSTACLE_PREFAB_PATH
            };
            templatePrefab = SA_AssetPathHelper.FindPrefab(defaultPath);
        }

        if (templatePrefab == null)
        {
            Debug.LogError("Template prefab not found. Please assign a template or ensure default templates exist.");
            return;
        }

        string templatePrefabPath = AssetDatabase.GetAssetPath(templatePrefab);

        // Determine output folder
        string outputFolder = GetOutputFolderForType(objectType);

        // Ensure directories exist
        SA_AssetPathHelper.EnsureAssetPathDirectories(outputFolder);

        string newPrefabPath = $"Assets/{outputFolder}/{objectName}.prefab";

        // Check if prefab already exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath) != null)
        {
            if (!EditorUtility.DisplayDialog("Prefab Exists", 
                $"A prefab named '{objectName}' already exists at this location. Do you want to overwrite it?", 
                "Overwrite", "Cancel"))
            {
                return;
            }
            AssetDatabase.DeleteAsset(newPrefabPath);
        }

        // Copy the template prefab
        bool copySuccess = AssetDatabase.CopyAsset(templatePrefabPath, newPrefabPath);
        if (!copySuccess)
        {
            Debug.LogError("Failed to copy prefab template to " + newPrefabPath);
            return;
        }

        // Load and modify the new prefab
        GameObject newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath);
        if (newPrefab != null)
        {
            // Update the sprite and tint
            SpriteRenderer spriteRenderer = newPrefab.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = objectSprite;
                spriteRenderer.color = spriteTint;
            }
            else
            {
                // Try to find SpriteRenderer in children
                spriteRenderer = newPrefab.GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = objectSprite;
                    spriteRenderer.color = spriteTint;
                }
                else
                {
                    Debug.LogWarning("No SpriteRenderer found on the prefab. Sprite was not assigned.");
                }
            }

            // For obstacles and foliage, resize colliders to match sprite bounds
            if ((objectType == EnvironmentObjectType.Obstacle || objectType == EnvironmentObjectType.Foliage) 
                && objectSprite != null && spriteRenderer != null)
            {
                ResizeCollidersToSprite(newPrefab, objectSprite, spriteRenderer);
            }

            // Rename the prefab's root object
            newPrefab.name = objectName;

            // Apply uniform scale to X and Y
            newPrefab.transform.localScale = new Vector3(prefabScale, prefabScale, 1f);

            // Mark as dirty and save
            EditorUtility.SetDirty(newPrefab);
            AssetDatabase.SaveAssets();
        }

        Debug.Log($"{objectType} prefab created at: {newPrefabPath}");
        
        // Select the new prefab in the Project window
        Selection.activeObject = newPrefab;
        EditorGUIUtility.PingObject(newPrefab);
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
                // Use inscribed circle (smaller dimension) so it fits within the sprite
                float radius = Mathf.Min(size.x, size.y) * 0.5f;
                circle.radius = radius;
                circle.offset = center;
            }
            else if (collider is PolygonCollider2D poly)
            {
                // Use sprite's physics shape if available, otherwise create box from bounds
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
