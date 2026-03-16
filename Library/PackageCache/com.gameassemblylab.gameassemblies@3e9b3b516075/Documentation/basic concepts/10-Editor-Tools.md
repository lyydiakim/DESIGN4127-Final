# Editor Tools

This foundational guide explains how to create **Unity Editor Tools** - custom windows and menu items that extend Unity's editor functionality. Editor tools allow you to automate repetitive tasks, create content quickly, and build custom workflows for your game development.

## What are Editor Tools?

**Editor Tools** are custom scripts that run in Unity's **Editor** (not in the game at runtime). They allow you to:

- Create custom windows with buttons and input fields
- Add menu items to Unity's top menu bar
- Automate content creation (prefabs, ScriptableObjects, GameObjects)
- Build custom workflows for your team
- Speed up repetitive tasks

### Editor Scripts vs Runtime Scripts

| Aspect | Editor Scripts | Runtime Scripts |
|--------|----------------|-----------------|
| **Location** | `Editor/` folder | `Runtime/` or `Scripts/` folder |
| **Namespace** | `UnityEditor` | `UnityEngine` |
| **Runs** | Only in Unity Editor | In builds and editor |
| **Purpose** | Tool creation, automation | Game logic, behavior |
| **Example** | Create prefab tool | Player movement script |

**Key Rule**: Editor scripts must be in a folder named `Editor` (or have `.Editor` in the assembly definition).

---

## Basic Structure: Editor Window

An **Editor Window** is a custom window that appears in Unity's editor interface.

### Minimal Editor Window

```csharp
using UnityEngine;
using UnityEditor;

public class MyEditorWindow : EditorWindow
{
    // Menu item to open the window
    [MenuItem("My Tools/Open My Window")]
    public static void ShowWindow()
    {
        GetWindow<MyEditorWindow>("My Window Title");
    }
    
    // Draw the window's GUI
    private void OnGUI()
    {
        GUILayout.Label("Hello, Editor Window!");
        
        if (GUILayout.Button("Click Me"))
        {
            Debug.Log("Button clicked!");
        }
    }
}
```

**What this does**:
1. Adds menu item `My Tools → Open My Window`
2. Opens a window titled "My Window Title"
3. Displays a label and button
4. Logs to console when button is clicked

---

## Menu Items

The `[MenuItem]` attribute adds items to Unity's top menu bar.

### Basic Menu Item

```csharp
[MenuItem("Game Assemblies/Systems/Create Resource Management System")]
public static void CreateResourceManagementSystem()
{
    Debug.Log("Creating Resource Management System...");
    // Your logic here
}
```

**Menu Path Format**: `"Top Level/Sub Menu/Item Name"`

**Examples from Game Assemblies**:
- `"Game Assemblies/Systems/Create Resource Management System"`
- `"Game Assemblies/Resources/Create Resource"`
- `"Game Assemblies/Environment/Create White Canvas"`

### Menu Item with Validation

Add a second method with `%` prefix to enable/disable the menu item:

```csharp
[MenuItem("My Tools/Create Object", true)]
public static bool ValidateCreateObject()
{
    // Only enable if a scene is open
    return UnityEditor.SceneManagement.SceneManager.GetActiveScene().isLoaded;
}

[MenuItem("My Tools/Create Object")]
public static void CreateObject()
{
    // Create logic here
}
```

---

## Editor Window GUI Elements

### Labels

```csharp
private void OnGUI()
{
    // Simple label
    GUILayout.Label("My Label");
    
    // Bold label
    GUILayout.Label("Bold Label", EditorStyles.boldLabel);
    
    // Label with custom style
    GUILayout.Label("Custom Style", EditorStyles.helpBox);
}
```

### Text Fields

```csharp
private string myText = "Default Value";

private void OnGUI()
{
    // Single-line text field
    myText = EditorGUILayout.TextField("Name", myText);
    
    // Multi-line text area
    myText = EditorGUILayout.TextArea(myText, GUILayout.Height(100));
}
```

### Buttons

```csharp
private void OnGUI()
{
    // Simple button
    if (GUILayout.Button("Create"))
    {
        CreateSomething();
    }
    
    // Button with custom width
    if (GUILayout.Button("Create", GUILayout.Width(200)))
    {
        CreateSomething();
    }
    
    // Button with custom height
    if (GUILayout.Button("Create", GUILayout.Height(50)))
    {
        CreateSomething();
    }
}
```

### Object Fields

```csharp
private GameObject myPrefab;
private Sprite mySprite;
private Resource myResource;

private void OnGUI()
{
    // GameObject field
    myPrefab = (GameObject)EditorGUILayout.ObjectField(
        "Prefab", 
        myPrefab, 
        typeof(GameObject), 
        false  // false = only assets, true = scene objects too
    );
    
    // Sprite field
    mySprite = (Sprite)EditorGUILayout.ObjectField(
        "Sprite", 
        mySprite, 
        typeof(Sprite), 
        false
    );
    
    // ScriptableObject field
    myResource = (Resource)EditorGUILayout.ObjectField(
        "Resource", 
        myResource, 
        typeof(Resource), 
        true  // true = allow scene objects (though ScriptableObjects are assets)
    ) as Resource;
}
```

### Other Common Fields

```csharp
private float myFloat = 5.0f;
private int myInt = 10;
private bool myBool = true;
private Color myColor = Color.white;
private Vector3 myVector = Vector3.zero;

private void OnGUI()
{
    // Float field
    myFloat = EditorGUILayout.FloatField("Float Value", myFloat);
    
    // Int field
    myInt = EditorGUILayout.IntField("Int Value", myInt);
    
    // Toggle (checkbox)
    myBool = EditorGUILayout.Toggle("Enable Feature", myBool);
    
    // Color field
    myColor = EditorGUILayout.ColorField("Color", myColor);
    
    // Vector3 field
    myVector = EditorGUILayout.Vector3Field("Position", myVector);
    
    // Enum dropdown
    LevelType levelType = (LevelType)EditorGUILayout.EnumPopup("Level Type", levelType);
}
```

### Layout Helpers

```csharp
private void OnGUI()
{
    // Add space
    GUILayout.Space(20);
    
    // Horizontal layout (elements side by side)
    EditorGUILayout.BeginHorizontal();
    GUILayout.Label("Left");
    GUILayout.Button("Right");
    EditorGUILayout.EndHorizontal();
    
    // Vertical layout (default, but can be explicit)
    EditorGUILayout.BeginVertical("box");
    GUILayout.Label("Inside box");
    EditorGUILayout.EndVertical();
    
    // Scroll view
    scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
    // ... content here ...
    EditorGUILayout.EndScrollView();
}
```

---

## Creating Prefabs in the Scene

### Loading a Prefab

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class MyEditorWindow : EditorWindow
{
    [MenuItem("My Tools/Create Prefab in Scene")]
    public static void CreatePrefabInScene()
    {
        // Method 1: Load by path
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Samples/Prefabs/MyPrefab.prefab"
        );
        
        if (prefab == null)
        {
            Debug.LogError("Prefab not found!");
            return;
        }
        
        // Instantiate in the active scene
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
            prefab, 
            SceneManager.GetActiveScene()
        );
        
        // Set position
        instance.transform.position = Vector3.zero;
        
        // Select the new object
        Selection.activeObject = instance;
        
        Debug.Log("Prefab created in scene!");
    }
}
```

### Using PrefabUtility

`PrefabUtility.InstantiatePrefab()` is the **correct** way to instantiate prefabs in editor scripts:

```csharp
// ✅ Correct: Maintains prefab connection
GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
    prefab, 
    SceneManager.GetActiveScene()
);

// ❌ Wrong: Breaks prefab connection
GameObject instance = Instantiate(prefab);
```

**Why use PrefabUtility?**
- Maintains prefab connection (changes to prefab update instance)
- Works correctly in both editor and play mode
- Properly handles nested prefabs

### Example: Create Resource Management System

From `SA_Menu.cs`:

```csharp
[MenuItem("Game Assemblies/Systems/Create Resource Management System")]
public static void CreateResourceManagementSystem()
{
    // Find prefab using helper
    GameObject rm_prefab = SA_AssetPathHelper.FindPrefab(
        "Samples/Prefabs/Managers/ResourceManager.prefab"
    );
    
    if (rm_prefab == null)
    {
        Debug.LogError("Prefab not found!");
        return;
    }
    
    // Instantiate in active scene
    GameObject rm_instance = (GameObject)PrefabUtility.InstantiatePrefab(
        rm_prefab, 
        SceneManager.GetActiveScene()
    );
    
    // Set position
    rm_instance.transform.position = Vector3.zero;
    
    // Configure references
    // ... setup code ...
    
    Debug.Log("Resource Management System Created");
}
```

---

## Creating ScriptableObjects

### Basic ScriptableObject Creation

```csharp
using UnityEngine;
using UnityEditor;

public class MyEditorWindow : EditorWindow
{
    private string assetName = "NewAsset";
    
    private void OnGUI()
    {
        assetName = EditorGUILayout.TextField("Asset Name", assetName);
        
        if (GUILayout.Button("Create ScriptableObject"))
        {
            CreateScriptableObject();
        }
    }
    
    private void CreateScriptableObject()
    {
        // Create instance
        Resource newAsset = ScriptableObject.CreateInstance<Resource>();
        newAsset.name = assetName;
        newAsset.resourceName = assetName;
        
        // Ensure folder exists
        string folderPath = "Assets/Game Assemblies/Databases/Resources";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Game Assemblies/Databases", "Resources");
        }
        
        // Create asset file
        string assetPath = $"{folderPath}/{assetName}.asset";
        AssetDatabase.CreateAsset(newAsset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        // Select the new asset
        Selection.activeObject = newAsset;
        
        Debug.Log($"Created: {assetPath}");
    }
}
```

### Creating Folders Programmatically

```csharp
// Create single folder
if (!AssetDatabase.IsValidFolder("Assets/MyFolder"))
{
    AssetDatabase.CreateFolder("Assets", "MyFolder");
}

// Create nested folders
string[] folders = { "Assets", "Game Assemblies", "Databases", "Resources" };
string parent = folders[0];
for (int i = 1; i < folders.Length; i++)
{
    string folderPath = $"{parent}/{folders[i]}";
    if (!AssetDatabase.IsValidFolder(folderPath))
    {
        AssetDatabase.CreateFolder(parent, folders[i]);
    }
    parent = folderPath;
}
```

### Example: Create Goal Window

From `SA_CreateGoalWindow.cs`:

```csharp
private void CreateGoal()
{
    // Ensure folder exists
    SA_AssetPathHelper.EnsureAssetPathDirectories(
        "Game Assemblies/Databases/Goals"
    );
    
    // Create ScriptableObject instance
    ResourceGoalSO newAsset = ScriptableObject.CreateInstance<ResourceGoalSO>();
    newAsset.name = goalName;
    newAsset.timeLimit = timeLimit;
    newAsset.resourceType = resourceType;
    
    // Save as asset
    string assetPath = $"Assets/Game Assemblies/Databases/Goals/{goalName}.asset";
    AssetDatabase.CreateAsset(newAsset, assetPath);
    AssetDatabase.SaveAssets();
    
    Debug.Log("Goal created: " + goalName);
}
```

---

## Complete Example: Simple Prefab Creator

Here's a complete editor window that creates a prefab in the scene:

```csharp
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class SimplePrefabCreator : EditorWindow
{
    private GameObject prefabToCreate;
    private Vector3 spawnPosition = Vector3.zero;
    private string prefabName = "New Object";
    
    [MenuItem("My Tools/Prefab Creator")]
    public static void ShowWindow()
    {
        GetWindow<SimplePrefabCreator>("Prefab Creator");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Create Prefab in Scene", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // Prefab field
        prefabToCreate = (GameObject)EditorGUILayout.ObjectField(
            "Prefab", 
            prefabToCreate, 
            typeof(GameObject), 
            false
        ) as GameObject;
        
        GUILayout.Space(10);
        
        // Name field
        prefabName = EditorGUILayout.TextField("Object Name", prefabName);
        
        // Position field
        spawnPosition = EditorGUILayout.Vector3Field("Spawn Position", spawnPosition);
        
        GUILayout.Space(20);
        
        // Create button (disabled if no prefab selected)
        GUI.enabled = prefabToCreate != null;
        if (GUILayout.Button("Create Prefab", GUILayout.Height(30)))
        {
            CreatePrefab();
        }
        GUI.enabled = true;
        
        // Help box
        if (prefabToCreate == null)
        {
            EditorGUILayout.HelpBox(
                "Please select a prefab to create.", 
                MessageType.Info
            );
        }
    }
    
    private void CreatePrefab()
    {
        // Check if prefab is actually a prefab
        if (PrefabUtility.GetPrefabAssetType(prefabToCreate) == PrefabAssetType.NotAPrefab)
        {
            EditorUtility.DisplayDialog(
                "Error", 
                "Selected object is not a prefab!", 
                "OK"
            );
            return;
        }
        
        // Instantiate prefab
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
            prefabToCreate, 
            SceneManager.GetActiveScene()
        );
        
        // Set name and position
        instance.name = prefabName;
        instance.transform.position = spawnPosition;
        
        // Select the new object
        Selection.activeObject = instance;
        
        // Focus scene view on new object
        SceneView.FrameLastActiveSceneView();
        
        Debug.Log($"Created {prefabName} at {spawnPosition}");
    }
}
```

---

## Editor Window Lifecycle

### OnEnable()

Called when the window is opened or enabled:

```csharp
private void OnEnable()
{
    // Load data
    // Initialize variables
    // Set up references
    
    // Example: Load tutorial image
    tutorialImage = SA_AssetPathHelper.FindAsset<Texture2D>(
        "Samples/2d Assets/Asset02b.png"
    );
}
```

### OnGUI()

Called every frame to draw the window's UI:

```csharp
private void OnGUI()
{
    // Draw all UI elements here
    // Called multiple times per frame
    // Use EditorGUILayout for automatic layout
}
```

### OnDisable()

Called when the window is closed:

```csharp
private void OnDisable()
{
    // Clean up
    // Save data
    // Release references
}
```

---

## Common Patterns

### Pattern 1: Simple Menu Item

For quick actions that don't need a window:

```csharp
[MenuItem("My Tools/Quick Action")]
public static void QuickAction()
{
    // Do something immediately
    Debug.Log("Quick action executed!");
}
```

### Pattern 2: Editor Window with Fields

For tools that need user input:

```csharp
public class MyToolWindow : EditorWindow
{
    private string inputField;
    private GameObject prefabField;
    
    [MenuItem("My Tools/My Tool")]
    public static void ShowWindow()
    {
        GetWindow<MyToolWindow>("My Tool");
    }
    
    private void OnGUI()
    {
        inputField = EditorGUILayout.TextField("Input", inputField);
        prefabField = (GameObject)EditorGUILayout.ObjectField(
            "Prefab", 
            prefabField, 
            typeof(GameObject), 
            false
        );
        
        if (GUILayout.Button("Execute"))
        {
            ExecuteTool();
        }
    }
    
    private void ExecuteTool()
    {
        // Use inputField and prefabField
    }
}
```

### Pattern 3: Window with Multiple Buttons

For tools with multiple actions:

```csharp
private void OnGUI()
{
    if (GUILayout.Button("Create Object"))
    {
        CreateObject();
    }
    
    if (GUILayout.Button("Update Object"))
    {
        UpdateObject();
    }
    
    if (GUILayout.Button("Delete Object"))
    {
        DeleteObject();
    }
}
```

### Pattern 4: Window with Tabs

For complex tools with multiple sections:

```csharp
private int selectedTab = 0;
private string[] tabNames = { "Create", "Edit", "Settings" };

private void OnGUI()
{
    selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
    
    switch (selectedTab)
    {
        case 0:
            DrawCreateTab();
            break;
        case 1:
            DrawEditTab();
            break;
        case 2:
            DrawSettingsTab();
            break;
    }
}
```

---

## Best Practices

### 1. **Error Handling**

Always check for null and handle errors gracefully:

```csharp
private void CreatePrefab()
{
    if (prefabToCreate == null)
    {
        EditorUtility.DisplayDialog("Error", "No prefab selected!", "OK");
        return;
    }
    
    // ... create logic ...
}
```

### 2. **User Feedback**

Provide feedback for user actions:

```csharp
// Success message
Debug.Log("Prefab created successfully!");
EditorUtility.DisplayDialog("Success", "Prefab created!", "OK");

// Error message
Debug.LogError("Failed to create prefab!");
EditorUtility.DisplayDialog("Error", "Failed to create prefab!", "OK");

// Warning
Debug.LogWarning("Prefab already exists!");
```

### 3. **Undo Support**

Register actions for Unity's Undo system:

```csharp
using UnityEngine;
using UnityEditor;

GameObject newObject = new GameObject("My Object");
Undo.RegisterCreatedObjectUndo(newObject, "Create My Object");
```

### 4. **Selection Management**

Select created objects so users can see them:

```csharp
GameObject instance = CreatePrefab();
Selection.activeObject = instance;
SceneView.FrameLastActiveSceneView();  // Focus scene view
```

### 5. **Asset Database Refresh**

Refresh the asset database after creating assets:

```csharp
AssetDatabase.CreateAsset(newAsset, path);
AssetDatabase.SaveAssets();
AssetDatabase.Refresh();  // Update Project window
```

### 6. **Folder Organization**

Keep editor scripts organized:

```
Assets/
  Editor/
    MyEditorTools/
      MyEditorWindow.cs
      MyMenuItems.cs
```

### 7. **Conditional Compilation**

Use `#if UNITY_EDITOR` if mixing editor and runtime code:

```csharp
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MyClass
{
    #if UNITY_EDITOR
    [MenuItem("My Tools/Do Something")]
    public static void DoSomething() { }
    #endif
}
```

---

## Common Mistakes

### Mistake 1: Editor Scripts in Wrong Folder

```csharp
// ❌ Bad: Editor script in Runtime folder
Assets/Scripts/MyEditorWindow.cs  // Won't compile!

// ✅ Good: Editor script in Editor folder
Assets/Editor/MyEditorWindow.cs
```

### Mistake 2: Missing Using Statements

```csharp
// ❌ Bad: Missing UnityEditor namespace
using UnityEngine;
public class MyWindow : EditorWindow  // Error!

// ✅ Good: Include UnityEditor
using UnityEngine;
using UnityEditor;
public class MyWindow : EditorWindow  // Works!
```

### Mistake 3: Using Instantiate Instead of PrefabUtility

```csharp
// ❌ Bad: Breaks prefab connection
GameObject instance = Instantiate(prefab);

// ✅ Good: Maintains prefab connection
GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
    prefab, 
    SceneManager.GetActiveScene()
);
```

### Mistake 4: Not Checking for Null

```csharp
// ❌ Bad: Crashes if prefab is null
prefabToCreate.transform.position = Vector3.zero;

// ✅ Good: Check first
if (prefabToCreate != null)
{
    prefabToCreate.transform.position = Vector3.zero;
}
```

### Mistake 5: Forgetting AssetDatabase.Refresh()

```csharp
// ❌ Bad: Asset might not appear in Project window
AssetDatabase.CreateAsset(newAsset, path);

// ✅ Good: Refresh to update Project window
AssetDatabase.CreateAsset(newAsset, path);
AssetDatabase.SaveAssets();
AssetDatabase.Refresh();
```

---

## Advanced Topics

### Custom Property Drawers

Create custom inspectors for your components:

```csharp
[CustomEditor(typeof(MyComponent))]
public class MyComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        MyComponent myComp = (MyComponent)target;
        
        if (GUILayout.Button("Custom Button"))
        {
            myComp.DoSomething();
        }
    }
}
```

### Editor Preferences

Store window settings that persist:

```csharp
private void OnEnable()
{
    // Load preference
    mySetting = EditorPrefs.GetFloat("MyTool.Setting", 5.0f);
}

private void OnDisable()
{
    // Save preference
    EditorPrefs.SetFloat("MyTool.Setting", mySetting);
}
```

### Context Menus

Add right-click menu items:

```csharp
[MenuItem("CONTEXT/Transform/Custom Action")]
public static void CustomAction(MenuCommand command)
{
    Transform transform = (Transform)command.context;
    // Do something with transform
}
```

---

## Troubleshooting

### Problem: Menu Item Doesn't Appear

**Causes**:
- Script has compilation errors
- Script is not in Editor folder
- Missing `using UnityEditor;`

**Solution**: Check Console for errors, ensure script compiles.

### Problem: Window Doesn't Open

**Causes**:
- `ShowWindow()` method not static
- Missing `[MenuItem]` attribute
- Wrong method signature

**Solution**: Ensure method is `public static void` and has `[MenuItem]`.

### Problem: Prefab Connection Broken

**Cause**: Using `Instantiate()` instead of `PrefabUtility.InstantiatePrefab()`.

**Solution**: Always use `PrefabUtility.InstantiatePrefab()` in editor scripts.

### Problem: Changes Don't Save

**Cause**: Not calling `AssetDatabase.SaveAssets()`.

**Solution**: Call `AssetDatabase.SaveAssets()` after creating/modifying assets.

### Problem: Asset Doesn't Appear in Project

**Cause**: Not calling `AssetDatabase.Refresh()`.

**Solution**: Call `AssetDatabase.Refresh()` after creating assets.

---

## Summary

**Editor Tools** allow you to:

- ✅ Create custom windows with `EditorWindow`
- ✅ Add menu items with `[MenuItem]`
- ✅ Build user interfaces with `OnGUI()` and `EditorGUILayout`
- ✅ Create prefabs in scenes with `PrefabUtility.InstantiatePrefab()`
- ✅ Create ScriptableObjects with `ScriptableObject.CreateInstance()`
- ✅ Automate repetitive tasks
- ✅ Build custom workflows

**Key Takeaways**:
- Editor scripts must be in `Editor/` folder
- Use `UnityEditor` namespace
- Use `PrefabUtility` for prefabs, not `Instantiate()`
- Always refresh AssetDatabase after creating assets
- Provide user feedback (logs, dialogs)
- Handle errors gracefully

**Essential APIs**:
- `EditorWindow` — Base class for custom windows
- `[MenuItem]` — Adds menu items
- `EditorGUILayout` — UI layout system
- `PrefabUtility` — Prefab operations
- `AssetDatabase` — Asset management
- `ScriptableObject.CreateInstance()` — Create ScriptableObjects

---

## Related Documentation

- [07 – Scriptable Objects](./07-Scriptable-Objects.md) — Creating ScriptableObject assets
- [06 – Static References](./06-Static-References.md) — Accessing singletons from editor tools
- [Tutorial 02: Stations and Resources](../tutorials/02-Stations-and-Resources.md) — Using Create Resource tool
- [Tutorial 04: Goals and Goal Tracker](../tutorials/04-Goals-and-Goal-Tracker.md) — Using Create Goal tool
- [Tutorial 05: Levels and Level Editor](../tutorials/05-Levels-and-Level-Editor.md) — Using Create Level tool
