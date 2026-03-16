using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SA_CreatePlayersWindow : EditorWindow
{
    private Sprite player_1_Sprite;
    private Sprite player_2_Sprite;
    private Sprite player_3_Sprite;
    private Sprite player_4_Sprite;

    private string newPlayerPrefabName = "Player_Custom";
    private float spriteScaleFactor = 0.4f;
    private float playerSpeed = 1f;
    private bool useSnapTarget = true;
    private float snapRadius = 0.1f;
    private float snapAimBias = 0f;
    private GameObject playerPrefabTemplate;
    private const string DEFAULT_PLAYER_PREFAB_PATH = "Samples/Prefabs/Players/Player_Drawn_Small.prefab";
    private const string PLAYER_PREFAB_OUTPUT_FOLDER = "Game Assemblies/Prefabs/Players";

    // Adds a menu item under Tools -> Create Resource
    [MenuItem("Game Assemblies/Players/Create Local Multiplayer System")]
    public static void ShowWindow()
    {
        // Opens the window with the title "Create Resource"
        GetWindow<SA_CreatePlayersWindow>("Create Player System");
    }

    private void OnEnable()
    {
        // Load the default player prefab as placeholder when window opens
        if (playerPrefabTemplate == null)
        {
            playerPrefabTemplate = SA_AssetPathHelper.FindPrefab(DEFAULT_PLAYER_PREFAB_PATH);
        }
    }

    private void OnGUI()
    {
        
        GUILayout.Space(20);
        GUILayout.Label("Welcome to the Player Environment Creator.");
        GUILayout.Label("A new player prefab will be created from the template, with colliders sized to match your sprite.");
        GUILayout.Space(20);

        // Player Prefab Template field
        EditorGUILayout.LabelField("Player Prefab Template", EditorStyles.boldLabel);
        playerPrefabTemplate = (GameObject)EditorGUILayout.ObjectField("Template", playerPrefabTemplate, typeof(GameObject), false);
        
        if (playerPrefabTemplate == null)
        {
            EditorGUILayout.HelpBox("No template selected. The default Player_Drawn_Small prefab will be used.", MessageType.Info);
        }

        GUILayout.Space(5);
        newPlayerPrefabName = EditorGUILayout.TextField("New Prefab Name", newPlayerPrefabName);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Player Speed", EditorStyles.boldLabel);
        playerSpeed = Mathf.Max(0.1f, EditorGUILayout.FloatField("Speed", playerSpeed));
        EditorGUILayout.HelpBox("Movement speed for the player (playerController.playerSpeed).", MessageType.None);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Snap Target", EditorStyles.boldLabel);
        useSnapTarget = EditorGUILayout.Toggle("Use Snap Target", useSnapTarget);
        snapRadius = Mathf.Max(0f, EditorGUILayout.FloatField("Snap Radius", snapRadius));
        snapAimBias = EditorGUILayout.Slider("Snap Aim Bias", snapAimBias, 0f, 1f);
        EditorGUILayout.HelpBox("When enabled, the grab area snaps to the nearest grabbable/workable. Snap radius and aim bias control detection range and direction preference.", MessageType.None);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Sprite Scale Factor", EditorStyles.boldLabel);
        spriteScaleFactor = EditorGUILayout.Slider("Scale", spriteScaleFactor, 0.1f, 2f);
        EditorGUILayout.HelpBox(
            "The prefab template scales the player sprite to avoid pixelation on screen. " +
            "The collider is sized to match this scaled display. Adjust if your template uses a different scale.",
            MessageType.Info);
        
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Player Sprites", EditorStyles.boldLabel);
        player_1_Sprite = (Sprite)EditorGUILayout.ObjectField("Player 1 Sprite", player_1_Sprite, typeof(Sprite), false);
        player_2_Sprite = (Sprite)EditorGUILayout.ObjectField("Player 2 Sprite", player_2_Sprite, typeof(Sprite), false);
        player_3_Sprite = (Sprite)EditorGUILayout.ObjectField("Player 3 Sprite", player_3_Sprite, typeof(Sprite), false);
        player_4_Sprite = (Sprite)EditorGUILayout.ObjectField("Player 4 Sprite", player_4_Sprite, typeof(Sprite), false);


        // When the Create button is pressed, execute CreateResource()
        if (GUILayout.Button("Create Local Multiplayer Environment"))
        {
            CreateEnvironment();
        }
        if (GUILayout.Button("Update Character Icons"))
        {
            UpdateCharacterIcons();
        }
    }

    public void UpdateCharacterIcons()
    {
        // Use selected prefab or fall back to default
        GameObject playerPrefab = playerPrefabTemplate;
        if (playerPrefab == null)
        {
            playerPrefab = SA_AssetPathHelper.FindPrefab(DEFAULT_PLAYER_PREFAB_PATH);
        }

        if (playerPrefab == null)
        {
            Debug.LogError($"Prefab not found: {DEFAULT_PLAYER_PREFAB_PATH}");
            return;
        }

        playerPrefab.GetComponent<playerController>().sprite1 = player_1_Sprite;
        playerPrefab.GetComponent<playerController>().sprite2 = player_2_Sprite;
        playerPrefab.GetComponent<playerController>().sprite3 = player_3_Sprite;
        playerPrefab.GetComponent<playerController>().sprite4 = player_4_Sprite;

        // Log success (optional)
        Debug.Log("Character Icons Updated");
    }

    public void CreateEnvironment()
    {
        string managerPath = "Samples/Prefabs/Managers/PlayerManager.prefab";

        GameObject managerPrefab = SA_AssetPathHelper.FindPrefab(managerPath);
        
        // Use selected template or fall back to default
        GameObject templatePrefab = playerPrefabTemplate;
        if (templatePrefab == null)
        {
            templatePrefab = SA_AssetPathHelper.FindPrefab(DEFAULT_PLAYER_PREFAB_PATH);
        }

        if (managerPrefab == null)
        {
            Debug.LogError($"Prefab not found: {managerPath}");
            return;
        }

        if (templatePrefab == null)
        {
            Debug.LogError($"Player template not found: {DEFAULT_PLAYER_PREFAB_PATH}");
            return;
        }

        if (string.IsNullOrWhiteSpace(newPlayerPrefabName))
        {
            Debug.LogError("Please enter a name for the new player prefab.");
            return;
        }

        // Create new player prefab from template
        SA_AssetPathHelper.EnsureAssetPathDirectories(PLAYER_PREFAB_OUTPUT_FOLDER);
        string newPrefabPath = $"Assets/{PLAYER_PREFAB_OUTPUT_FOLDER}/{newPlayerPrefabName}.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath) != null)
        {
            if (!EditorUtility.DisplayDialog("Prefab Exists",
                $"A prefab named '{newPlayerPrefabName}' already exists. Do you want to overwrite it?",
                "Overwrite", "Cancel"))
            {
                return;
            }
            AssetDatabase.DeleteAsset(newPrefabPath);
        }

        string templatePath = AssetDatabase.GetAssetPath(templatePrefab);
        bool copySuccess = AssetDatabase.CopyAsset(templatePath, newPrefabPath);
        if (!copySuccess)
        {
            Debug.LogError("Failed to create player prefab from template.");
            return;
        }

        // Load and configure the new prefab
        GameObject newPlayerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath);
        if (newPlayerPrefab == null)
        {
            Debug.LogError("Failed to load the newly created player prefab.");
            return;
        }

        // Assign sprites and speed to playerController
        var pc = newPlayerPrefab.GetComponent<playerController>();
        if (pc != null)
        {
            pc.sprite1 = player_1_Sprite;
            pc.sprite2 = player_2_Sprite;
            pc.sprite3 = player_3_Sprite;
            pc.sprite4 = player_4_Sprite;
            pc.playerSpeed = playerSpeed;
            pc.useSnapTarget = useSnapTarget;
            pc.snapRadius = snapRadius;
            pc.snapAimBias = snapAimBias;
        }

        // Apply scale to sprite transform and resize colliders to match the displayed size
        Sprite spriteForCollider = player_1_Sprite ?? player_2_Sprite ?? player_3_Sprite ?? player_4_Sprite;
        SpriteRenderer spriteRenderer = newPlayerPrefab.GetComponent<SpriteRenderer>() ?? newPlayerPrefab.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Apply the scale factor to the sprite's transform (avoids pixelation)
            spriteRenderer.transform.localScale = new Vector3(spriteScaleFactor, spriteScaleFactor, 1f);

            if (spriteForCollider != null)
            {
                ResizeCollidersToSprite(newPlayerPrefab, spriteForCollider, spriteRenderer, spriteScaleFactor);
            }
        }

        newPlayerPrefab.name = newPlayerPrefabName;
        EditorUtility.SetDirty(newPlayerPrefab);
        AssetDatabase.SaveAssets();

        // Instantiate PlayerManager in scene and assign the new prefab
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(managerPrefab, SceneManager.GetActiveScene());
        instance.transform.position = Vector3.zero;
        instance.GetComponent<PlayerInputManager>().playerPrefab = newPlayerPrefab;

        Debug.Log($"Local Multiplayer Environment Created. New player prefab: {newPrefabPath}");
    }

    private static void ResizeCollidersToSprite(GameObject prefab, Sprite sprite, SpriteRenderer spriteRenderer, float scaleFactor = 1f)
    {
        Bounds bounds = sprite.bounds;
        Vector2 size = bounds.size * scaleFactor;
        Vector2 center = bounds.center * scaleFactor;

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
                    for (int i = 0; i < physicsShape.Count; i++)
                        physicsShape[i] *= scaleFactor;
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
