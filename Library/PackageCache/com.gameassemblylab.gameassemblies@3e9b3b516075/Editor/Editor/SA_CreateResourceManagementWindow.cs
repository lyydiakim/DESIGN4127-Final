using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Editor window for creating the Resource Management System with customization options.
/// </summary>
public class SA_CreateResourceManagementWindow : EditorWindow
{
    private bool enableTimer = true;
    private bool enableScore = true;

    [MenuItem("Game Assemblies/Systems/Create Resource Management System")]
    public static void ShowWindow()
    {
        GetWindow<SA_CreateResourceManagementWindow>("Create Resource Management System");
    }

    private void OnGUI()
    {
        GUILayout.Space(20);

        EditorGUILayout.LabelField("Resource Management System", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "The Resource Management System adds the core components for tracking resources, goals, and scoring in your game:\n\n" +
            "• ResourceManager — Tracks all ResourceObjects in the scene and drives resource counts in the UI\n" +
            "• GoalManager — Manages goals, spawns goal trackers, and updates the global score when goals complete or fail\n" +
            "• ResourceManager_Canvas — UI canvas with resource panels, goal tracker area, and global score display\n\n" +
            "Configure the options below before creating the system.",
            MessageType.Info);

        GUILayout.Space(20);

        EditorGUILayout.LabelField("UI Options", EditorStyles.boldLabel);
        enableTimer = EditorGUILayout.Toggle("Enable Overall Timer", enableTimer);
        enableScore = EditorGUILayout.Toggle("Enable Overall Score", enableScore);
        EditorGUILayout.HelpBox("Timer: visible countdown for time-based goals. Score: displays global capital. Disable either for games that don't use them.", MessageType.None);

        GUILayout.Space(30);

        var createRect = GUILayoutUtility.GetRect(0, 50);
        createRect.x += 20;
        createRect.width -= 40;
        if (GUI.Button(createRect, "Create Resource Management System"))
        {
            CreateSystem();
        }
    }

    private void CreateSystem()
    {
        GameObject rm_prefab = SA_AssetPathHelper.FindPrefab("Samples/Prefabs/Managers/ResourceManager.prefab");
        GameObject gm_prefab = SA_AssetPathHelper.FindPrefab("Samples/Prefabs/Managers/GoalManager.prefab");
        GameObject rmc_prefab = SA_AssetPathHelper.FindPrefab("Samples/Prefabs/UI Prefabs/ResourceManager_Canvas.prefab");

        if (rm_prefab == null)
        {
            Debug.LogError("Prefab not found: ResourceManager.prefab");
            return;
        }

        if (gm_prefab == null)
        {
            Debug.LogError("Prefab not found: GoalManager.prefab");
            return;
        }

        if (rmc_prefab == null)
        {
            Debug.LogError("Prefab not found: ResourceManager_Canvas.prefab");
            return;
        }

        if (!SceneManager.GetActiveScene().IsValid())
        {
            Debug.LogError("No active scene. Open a scene first.");
            return;
        }

        GameObject gm_instance = (GameObject)PrefabUtility.InstantiatePrefab(gm_prefab, SceneManager.GetActiveScene());
        GameObject rm_instance = (GameObject)PrefabUtility.InstantiatePrefab(rm_prefab, SceneManager.GetActiveScene());
        GameObject rmc_instance = (GameObject)PrefabUtility.InstantiatePrefab(rmc_prefab, SceneManager.GetActiveScene());

        gm_instance.transform.position = Vector3.zero;
        rm_instance.transform.position = Vector3.zero;
        rmc_instance.transform.position = Vector3.zero;

        var canvas = rmc_instance.GetComponent<resourceManagerCanvas>();
        if (canvas != null)
        {
            if (canvas.timerModule != null)
                canvas.timerModule.SetActive(enableTimer);
            if (canvas.globalScoreModule != null)
                canvas.globalScoreModule.gameObject.SetActive(enableScore);
        }

        GameObject gtg = canvas?.goalTrackerModule;
        TMP_Text gs = canvas?.globalScoreModule;

        var goalManager = gm_instance.GetComponent<GoalManager>();
        if (goalManager != null)
        {
            if (gtg != null) goalManager.goalTrackerGrid = gtg;
            if (gs != null) goalManager.scoreText = gs;
        }

        Debug.Log("Resource Management System Created");
    }
}
