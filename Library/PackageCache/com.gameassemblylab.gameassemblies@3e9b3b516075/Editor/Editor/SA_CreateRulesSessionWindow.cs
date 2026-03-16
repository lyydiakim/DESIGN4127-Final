using UnityEngine;
using UnityEditor;

public class SA_CreateRulesSessionWindow : EditorWindow
{
    private string sessionName = "NewSession";
    private string description = "";

    [MenuItem("Game Assemblies/Rules/Create Rules Session")]
    public static void ShowWindow()
    {
        GetWindow<SA_CreateRulesSessionWindow>("Create Rules Session");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create a New Rules Session", EditorStyles.boldLabel);
        GUILayout.Space(10);

        sessionName = EditorGUILayout.TextField("Session Name", sessionName);
        description = EditorGUILayout.TextArea(description, GUILayout.Height(60));

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Rules Sessions define starting rules and the pool of rules available for unlock. " +
            "Assign starting rules and available pool in the Inspector after creation.",
            MessageType.Info);
        GUILayout.Space(10);

        if (GUILayout.Button("Create"))
        {
            CreateSession();
        }
    }

    private void CreateSession()
    {
        SA_AssetPathHelper.EnsureAssetPathDirectories("Game Assemblies/Databases/Rules Sessions");

        RulesSessionSO newAsset = ScriptableObject.CreateInstance<RulesSessionSO>();
        newAsset.name = sessionName;
        newAsset.sessionName = sessionName;
        newAsset.description = description;

        string assetPath = $"Assets/Game Assemblies/Databases/Rules Sessions/{sessionName}.asset";
        AssetDatabase.CreateAsset(newAsset, assetPath);
        AssetDatabase.SaveAssets();

        Selection.activeObject = newAsset;
        EditorGUIUtility.PingObject(newAsset);

        Debug.Log($"Rules Session created: {sessionName}");
    }
}
