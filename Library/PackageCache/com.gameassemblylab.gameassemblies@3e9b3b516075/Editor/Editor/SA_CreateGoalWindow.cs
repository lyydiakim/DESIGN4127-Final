using UnityEngine;
using UnityEditor;

public class SA_CreateGoalWindow : EditorWindow
{
    private string goalName = "NewGoal";
    //private Sprite objectSprite;
    private Resource resourceType;
    private float timeLimit = 20f;
    private int requiredCount = 1;

    // Tutorial image
    //private Texture2D tutorialImage;

    // Adds a menu item under Tools -> Create Resource
    [MenuItem("Game Assemblies/Goals/Create Goal")]
    public static void ShowWindow()
    {
        // Opens the window with the title "Create Resource"
        GetWindow<SA_CreateGoalWindow>("Create Goal");
    }

    private void OnGUI()
    {

        GUILayout.Label("Create a New Goal", EditorStyles.boldLabel);
        GUILayout.Space(20);
        // Text field for the name of the new resource
        goalName = EditorGUILayout.TextField("Goal Name", goalName);
        // Sprite field for assigning a sprite to the ScriptableObject's field
        //objectSprite = (Sprite)EditorGUILayout.ObjectField("Resource Sprite", objectSprite, typeof(Sprite), false);
        GUILayout.Space(20);
        timeLimit = EditorGUILayout.FloatField("Time Limit", timeLimit);
        GUILayout.Space(20);
        resourceType = EditorGUILayout.ObjectField("Resource to Obtain", resourceType, typeof(Resource), true) as Resource;
        //tower = EditorGUILayout.ObjectField("", tower, typeof(TowerObj), true) as TowerObj;

        GUILayout.Space(20);
        GUILayout.Label("Welcome to the Goal creator. Input the name of your Goal. "); //TEXT LABEL
        GUILayout.Label("This panel will create a goal scriptable object and store it in the database.");
        //"Assets/Game Assemblies/Databases/Resources/Apples_data.asset"
        GUILayout.Space(20);


        // When the Create button is pressed, execute CreateResource()
        if (GUILayout.Button("Create"))
        {
            CreateGoal();
        }
    }

    private void CreateGoal()
    {
        SA_AssetPathHelper.EnsureAssetPathDirectories("Game Assemblies/Databases/Goals");

        ResourceGoalSO newAsset = ScriptableObject.CreateInstance<ResourceGoalSO>();
        newAsset.name = goalName;
        newAsset.timeLimit = timeLimit;
        newAsset.resourceType = resourceType;

        string assetPath = $"Assets/Game Assemblies/Databases/Goals/{goalName}.asset";
        AssetDatabase.CreateAsset(newAsset, assetPath);
        AssetDatabase.SaveAssets();

        Selection.activeObject = newAsset;
        EditorGUIUtility.PingObject(newAsset);

        Debug.Log("Goal created: " + goalName);
    }
}
