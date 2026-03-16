using UnityEngine;
using UnityEditor;

public class SA_CreateRuleWindow : EditorWindow
{
    private string ruleName = "NewRule";
    private string description = "";
    private RuleCategory category = RuleCategory.Custom;
    private Sprite icon;

    [MenuItem("Game Assemblies/Rules/Create Rule")]
    public static void ShowWindow()
    {
        GetWindow<SA_CreateRuleWindow>("Create Rule");
    }

    private void OnGUI()
    {
        GUILayout.Label("Create a New Rule", EditorStyles.boldLabel);
        GUILayout.Space(10);

        ruleName = EditorGUILayout.TextField("Rule Name", ruleName);
        description = EditorGUILayout.TextArea(description, GUILayout.Height(60));
        category = (RuleCategory)EditorGUILayout.EnumPopup("Category", category);
        icon = (Sprite)EditorGUILayout.ObjectField("Icon", icon, typeof(Sprite), false);

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Rules define governing structures (property, taxes, government, etc.). " +
            "Add dependencies and parameters in the Inspector after creation.",
            MessageType.Info);
        GUILayout.Space(10);

        if (GUILayout.Button("Create"))
        {
            CreateRule();
        }
    }

    private void CreateRule()
    {
        SA_AssetPathHelper.EnsureAssetPathDirectories("Game Assemblies/Databases/Rules");

        RuleSO newAsset = ScriptableObject.CreateInstance<RuleSO>();
        newAsset.name = ruleName;
        newAsset.ruleName = ruleName;
        newAsset.description = description;
        newAsset.category = category;
        newAsset.icon = icon;

        string assetPath = $"Assets/Game Assemblies/Databases/Rules/{ruleName}.asset";
        AssetDatabase.CreateAsset(newAsset, assetPath);
        AssetDatabase.SaveAssets();

        Selection.activeObject = newAsset;
        EditorGUIUtility.PingObject(newAsset);

        Debug.Log($"Rule created: {ruleName}");
    }
}
