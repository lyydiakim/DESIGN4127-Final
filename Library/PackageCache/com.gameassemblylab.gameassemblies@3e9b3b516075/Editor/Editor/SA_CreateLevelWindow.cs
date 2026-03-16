using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class SA_CreateLevelWindow : EditorWindow
{
    private string levelName = "New Level";
    private LevelType levelType = LevelType.Sequential;
    private float initialDelay = 5f;
    private bool endLevelWhenComplete = true;
    private float timeLimit = 0f;

    // Sequential mode settings
    private List<ResourceGoalSO> sequentialGoals = new List<ResourceGoalSO>();
    private ResourceGoalSO newSequentialGoal;

    // Random mode settings
    private List<ResourceGoalSO> randomGoalPool = new List<ResourceGoalSO>();
    private ResourceGoalSO newRandomGoal;
    private float goalInterval = 30f;
    private int maxActiveGoals = 3;

    // Tutorial image
    private Texture2D tutorialImage;

    // Scrolling
    private Vector2 sequentialGoalsScroll = Vector2.zero;
    private Vector2 randomGoalsScroll = Vector2.zero;

    // Adds a menu item under Game Assemblies -> Levels -> Create Level
    [MenuItem("Game Assemblies/Levels/Create Level")]
    public static void ShowWindow()
    {
        // Opens the window with the title "Create Level"
        GetWindow<SA_CreateLevelWindow>("Create Level");
    }

    private void OnEnable()
    {
        tutorialImage = SA_AssetPathHelper.FindAsset<Texture2D>("Samples/2d Assets/Asset02b.png");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create a New Level", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Level basic information
        levelName = EditorGUILayout.TextField("Level Name", levelName);
        levelType = (LevelType)EditorGUILayout.EnumPopup("Level Type", levelType);

        GUILayout.Space(10);
        GUILayout.Label("Common Settings", EditorStyles.boldLabel);
        initialDelay = EditorGUILayout.FloatField("Initial Delay (seconds)", initialDelay);
        endLevelWhenComplete = EditorGUILayout.Toggle("End Level When Complete", endLevelWhenComplete);
        timeLimit = EditorGUILayout.FloatField("Time Limit (0 = no limit)", timeLimit);

        // Display different settings based on level type
        GUILayout.Space(20);
        if (levelType == LevelType.Sequential)
        {
            DrawSequentialSettings();
        } else
        {
            DrawRandomSettings();
        }

        GUILayout.Space(20);
        GUILayout.Label("Welcome to the level creator. Input the details of your level.");
        GUILayout.Label("This panel will create a level scriptable object.");
        GUILayout.Label("Scriptable Object Path: Game Assemblies/Databases/Levels/");
        GUILayout.Space(20);

        // Create button
        if (GUILayout.Button("Create Level"))
        {
            CreateLevel();
        }
    }

    private void DrawSequentialSettings()
    {
        GUILayout.Label("Sequential Mode Settings", EditorStyles.boldLabel);

        // Display current sequential goals
        GUILayout.Label("Sequential Goals (Appear in Order):", EditorStyles.boldLabel);

        sequentialGoalsScroll = EditorGUILayout.BeginScrollView(sequentialGoalsScroll, GUILayout.Height(150));

        for (int i = 0; i < sequentialGoals.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            sequentialGoals[i] = (ResourceGoalSO)EditorGUILayout.ObjectField($"Goal {i + 1}", sequentialGoals[i], typeof(ResourceGoalSO), false);

            if (GUILayout.Button("↑", GUILayout.Width(25)) && i > 0)
            {
                ResourceGoalSO temp = sequentialGoals[i - 1];
                sequentialGoals[i - 1] = sequentialGoals[i];
                sequentialGoals[i] = temp;
            }

            if (GUILayout.Button("↓", GUILayout.Width(25)) && i < sequentialGoals.Count - 1)
            {
                ResourceGoalSO temp = sequentialGoals[i + 1];
                sequentialGoals[i + 1] = sequentialGoals[i];
                sequentialGoals[i] = temp;
            }

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                sequentialGoals.RemoveAt(i);
                GUIUtility.ExitGUI(); // Prevent GUI errors when removing from list
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // Add new goal
        EditorGUILayout.BeginHorizontal();
        newSequentialGoal = (ResourceGoalSO)EditorGUILayout.ObjectField("New Goal", newSequentialGoal, typeof(ResourceGoalSO), false);

        GUI.enabled = newSequentialGoal != null;
        if (GUILayout.Button("Add Goal", GUILayout.Width(100)))
        {
            sequentialGoals.Add(newSequentialGoal);
            newSequentialGoal = null;
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    private void DrawRandomSettings()
    {
        GUILayout.Label("Random Mode Settings", EditorStyles.boldLabel);

        goalInterval = EditorGUILayout.FloatField("Goal Interval (seconds)", goalInterval);
        maxActiveGoals = EditorGUILayout.IntField("Max Active Goals", maxActiveGoals);

        // Display current random goal pool
        GUILayout.Label("Random Goal Pool:", EditorStyles.boldLabel);

        randomGoalsScroll = EditorGUILayout.BeginScrollView(randomGoalsScroll, GUILayout.Height(150));

        for (int i = 0; i < randomGoalPool.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            randomGoalPool[i] = (ResourceGoalSO)EditorGUILayout.ObjectField($"Goal {i + 1}", randomGoalPool[i], typeof(ResourceGoalSO), false);

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                randomGoalPool.RemoveAt(i);
                GUIUtility.ExitGUI(); // Prevent GUI errors when removing from list
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        // Add new goal to pool
        EditorGUILayout.BeginHorizontal();
        newRandomGoal = (ResourceGoalSO)EditorGUILayout.ObjectField("New Goal", newRandomGoal, typeof(ResourceGoalSO), false);

        GUI.enabled = newRandomGoal != null;
        if (GUILayout.Button("Add to Pool", GUILayout.Width(100)))
        {
            randomGoalPool.Add(newRandomGoal);
            newRandomGoal = null;
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    private void CreateLevel()
    {
        SA_AssetPathHelper.EnsureAssetPathDirectories("Game Assemblies/Databases/Levels");
        string assetPath = $"Assets/Game Assemblies/Databases/Levels/{levelName}.asset";

        if (AssetDatabase.LoadAssetAtPath<LevelDataSO>(assetPath) != null)
        {
            if (!EditorUtility.DisplayDialog("Level Already Exists",
                $"A level named '{levelName}' already exists. Do you want to overwrite it?",
                "Yes", "No"))
            {
                return;
            }
        }

        LevelDataSO newLevel = ScriptableObject.CreateInstance<LevelDataSO>();

        newLevel.name = levelName;
        newLevel.levelName = levelName;
        newLevel.levelType = levelType;
        newLevel.initialDelay = initialDelay;
        newLevel.endLevelWhenComplete = endLevelWhenComplete;
        newLevel.timeToComplete = timeLimit;

        if (levelType == LevelType.Sequential)
        {
            newLevel.sequentialGoals = new List<ResourceGoalSO>(sequentialGoals);
        } else
        {
            newLevel.randomGoalPool = new List<ResourceGoalSO>(randomGoalPool);
            newLevel.goalInterval = goalInterval;
            newLevel.maxActiveGoals = maxActiveGoals;
        }

        AssetDatabase.CreateAsset(newLevel, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = newLevel;
        EditorGUIUtility.PingObject(newLevel);

        Debug.Log($"Level created: {levelName} at {assetPath}");
    }
}