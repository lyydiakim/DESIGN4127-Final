# Scriptable Objects

This foundational guide explains **ScriptableObjects** in Unity and how they're used throughout the Game Assemblies library. Understanding ScriptableObjects is essential for working with resources, goals, levels, and other data-driven systems.

## What is a ScriptableObject?

A **ScriptableObject** is a Unity class that allows you to create **data assets** that exist independently of GameObjects in your scene. Think of them as **data containers** that can be:

- **Created as assets** in your project (`.asset` files)
- **Edited in the Inspector** like any other asset
- **Referenced by multiple objects** without duplicating data
- **Modified at design time** without affecting runtime performance
- **Shared across scenes** without needing to be in the scene hierarchy

### ScriptableObject vs MonoBehaviour

| Aspect | ScriptableObject | MonoBehaviour |
|--------|------------------|---------------|
| **Lives in** | Project (as `.asset` file) | Scene (attached to GameObject) |
| **Purpose** | Data storage | Behavior/logic |
| **Can have** | Data fields, methods | Data fields, methods, Unity callbacks (`Update`, `Start`, etc.) |
| **Instance** | One asset, many references | One instance per GameObject |
| **When to use** | Game data, configurations, templates | Game logic, components, behaviors |

**Key Insight**: ScriptableObjects are for **data**, MonoBehaviours are for **behavior**.

---

## Why Use ScriptableObjects?

### 1. **Data-Driven Design**

ScriptableObjects separate **data** from **logic**. You can:

- Design game content (resources, goals, levels) without writing code
- Iterate on game balance by editing asset values
- Create variations easily (e.g., "Bread" resource vs "Wheat" resource)

### 2. **Memory Efficiency**

- **One asset** can be referenced by **many objects**
- No duplication of data in memory
- Example: 100 stations can all reference the same "Wood" Resource asset

### 3. **Easy Content Creation**

- Designers can create content using Unity's Inspector
- No code changes needed to add new resources, goals, or levels
- Visual workflow: drag, drop, configure

### 4. **Version Control Friendly**

- Assets are files that can be tracked in Git/SVN
- Easy to see what changed (diff shows data changes)
- Can be organized in folders (e.g., `Databases/Resources/`)

### 5. **Runtime Flexibility**

- Can be modified at runtime (though changes don't persist unless saved)
- Can be instantiated at runtime: `ScriptableObject.CreateInstance<T>()`
- Can be cloned: `Instantiate(scriptableObject)` creates a copy

---

## ScriptableObjects in Game Assemblies

The Game Assemblies library uses ScriptableObjects extensively for game data:

### Resource (`Resource.cs`)

**Purpose**: Defines a resource type (e.g., Wood, Bread, Planks)

```csharp
[CreateAssetMenu(fileName = "New Resource", menuName = "Game Assemblies/Resource")]
public class Resource : ScriptableObject
{
    public string resourceName;
    public Sprite icon;
    public GameObject resourcePrefab;
}
```

**Usage**: 
- Created via `Game Assemblies → Resources → Create Resource`
- Saved at `Assets/Game Assemblies/Databases/Resources/`
- Referenced by stations, goals, and resource objects

### ResourceGoalSO (`ResourceGoalSO.cs`)

**Purpose**: Defines a goal objective (e.g., "Collect 3 Bread in 30 seconds")

```csharp
[CreateAssetMenu(fileName = "NewResourceGoal", menuName = "Game Assemblies/Goals/Create Resource Goal")]
public class ResourceGoalSO : ScriptableObject
{
    public Resource resourceType;
    public int requiredCount = 1;
    public float timeLimit = 20f;
    public int rewardPoints = 0;
    public int penalty = 1;
    
    // Runtime state (not saved)
    [System.NonSerialized] public float remainingTime;
    [System.NonSerialized] public bool isCompleted;
    [System.NonSerialized] public bool isFailed;
}
```

**Usage**:
- Created via `Game Assemblies → Goals → Create Goal`
- Saved at `Assets/Game Assemblies/Databases/Goals/`
- Used by GoalManager and LevelManager

### LevelDataSO (`LevelDataSO.cs`)

**Purpose**: Defines level structure and goal sequencing

```csharp
[CreateAssetMenu(fileName = "New Level", menuName = "Game Assemblies/Level Data")]
public class LevelDataSO : ScriptableObject
{
    public string levelName = "New Level";
    public LevelType levelType = LevelType.Sequential;
    public List<ResourceGoalSO> sequentialGoals = new List<ResourceGoalSO>();
    public List<ResourceGoalSO> randomGoalPool = new List<ResourceGoalSO>();
    public float initialDelay = 5f;
    public bool endLevelWhenComplete = true;
    public float timeToComplete = 10f;
}
```

**Usage**:
- Created via `Game Assemblies → Levels → Create Level`
- Saved at `Assets/Game Assemblies/Databases/Levels/`
- Loaded by LevelManager at runtime

### Other ScriptableObjects

- **ResourceManager_SO**: Configuration for resource management
- **ColorPaletteSO**: Color palette definitions for art tools
- **LootTable**: Defines random loot drops (if used)

---

## Creating ScriptableObjects

### Method 1: Using CreateAssetMenu Attribute

The `[CreateAssetMenu]` attribute adds your ScriptableObject to Unity's **Assets → Create** menu:

```csharp
[CreateAssetMenu(fileName = "New Resource", menuName = "Game Assemblies/Resource")]
public class Resource : ScriptableObject
{
    public string resourceName;
    public Sprite icon;
}
```

**How to use**:
1. Right-click in Project window
2. Navigate to `Create → Game Assemblies → Resource`
3. Unity creates a new `.asset` file
4. Name it (e.g., "Wood")
5. Configure fields in Inspector

### Method 2: Using Editor Windows

Game Assemblies provides editor windows that create ScriptableObjects with proper setup:

- **Create Resource**: `Game Assemblies → Resources → Create Resource`
- **Create Goal**: `Game Assemblies → Goals → Create Goal`
- **Create Level**: `Game Assemblies → Levels → Create Level`

These windows:
- Set up default values
- Create folders if needed
- Wire up references (e.g., prefab to resource)
- Provide a user-friendly interface

### Method 3: Programmatically (Code)

You can create ScriptableObjects in code:

```csharp
// Create instance in memory
Resource newResource = ScriptableObject.CreateInstance<Resource>();
newResource.resourceName = "Wood";
newResource.icon = woodSprite;

// Save as asset
string path = "Assets/Game Assemblies/Databases/Resources/Wood.asset";
AssetDatabase.CreateAsset(newResource, path);
AssetDatabase.SaveAssets();
```

**Note**: This requires `using UnityEditor;` and only works in Editor scripts.

---

## Using ScriptableObjects

### Referencing ScriptableObjects

ScriptableObjects are referenced like any other Unity object:

```csharp
public class Station : MonoBehaviour
{
    public Resource consumes;  // Drag Resource asset here in Inspector
    public Resource produces;   // Drag Resource asset here in Inspector
}
```

**In Inspector**: Drag the Resource asset from Project window into the field.

### Accessing ScriptableObject Data

```csharp
// Get data from ScriptableObject
string name = myResource.resourceName;
Sprite icon = myResource.icon;
GameObject prefab = myResource.resourcePrefab;

// Modify data (runtime changes don't persist unless saved)
myResource.resourceName = "New Name";
```

### Runtime Instantiation

You can create **runtime copies** of ScriptableObjects:

```csharp
// Create a copy (useful for goals that need independent state)
ResourceGoalSO runtimeGoal = Instantiate(goalTemplate);
runtimeGoal.ResetGoal();  // Reset to initial state
```

**Why instantiate?** 
- Original asset stays unchanged
- Each instance can have different runtime state
- Example: GoalManager creates runtime copies so goals can track individual progress

---

## Serialization and NonSerialized

### Serialized Fields (Saved)

By default, **public fields** are serialized (saved to the asset file):

```csharp
public class ResourceGoalSO : ScriptableObject
{
    public float timeLimit = 20f;  // Saved to .asset file
    public int rewardPoints = 10;   // Saved to .asset file
}
```

### NonSerialized Fields (Runtime Only)

Use `[System.NonSerialized]` for fields that shouldn't be saved:

```csharp
public class ResourceGoalSO : ScriptableObject
{
    public float timeLimit = 20f;  // Saved
    
    [System.NonSerialized] 
    public float remainingTime;     // NOT saved, runtime only
    
    [System.NonSerialized] 
    public bool isCompleted;         // NOT saved, runtime only
}
```

**Why use NonSerialized?**
- Runtime state shouldn't persist (e.g., `remainingTime` changes every frame)
- Reduces asset file size
- Prevents accidental saving of temporary data

---

## Best Practices

### 1. **Organize Assets in Folders**

```
Assets/
  Game Assemblies/
    Databases/
      Resources/        (Resource assets)
      Goals/            (ResourceGoalSO assets)
      Levels/           (LevelDataSO assets)
      LootTables/       (LootTable assets)
```

### 2. **Use Descriptive Names**

- ✅ Good: `"Bread"`, `"Tutorial Level 1"`, `"Deliver Bread Goal"`
- ❌ Bad: `"New Resource"`, `"Level"`, `"Goal1"`

### 3. **Set Default Values**

```csharp
public class Resource : ScriptableObject
{
    public string resourceName = "New Resource";  // Default value
    public int value = 0;                          // Default value
}
```

### 4. **Use Tooltips**

```csharp
[Tooltip("Time in seconds to complete the goal")]
public float timeLimit = 20f;
```

### 5. **Validate Data**

Add validation methods:

```csharp
public class ResourceGoalSO : ScriptableObject
{
    public float timeLimit = 20f;
    
    private void OnValidate()
    {
        // Ensure timeLimit is never negative
        if (timeLimit < 0)
            timeLimit = 0;
    }
}
```

### 6. **Don't Store Scene-Specific Data**

ScriptableObjects are shared across scenes. Don't store:
- ❌ GameObject references (they're scene-specific)
- ❌ Transform positions
- ❌ Scene-specific state

Instead, store:
- ✅ Data that applies everywhere (resource names, icons)
- ✅ Configuration (time limits, rewards)
- ✅ Templates (goal definitions, level structures)

---

## Common Patterns

### Pattern 1: Template + Runtime Instance

**Template** (ScriptableObject asset):
- Defines default values
- Created in editor
- Saved to disk

**Runtime Instance** (created from template):
- Copy of template
- Can have different runtime state
- Not saved to disk

**Example**: GoalManager uses goal templates to create runtime instances:

```csharp
// Template (asset)
ResourceGoalSO goalTemplate;  // Created in editor

// Runtime instance (created from template)
ResourceGoalSO runtimeGoal = Instantiate(goalTemplate);
runtimeGoal.ResetGoal();  // Reset to initial state
```

### Pattern 2: Data Container

ScriptableObject holds data, MonoBehaviour uses it:

```csharp
// ScriptableObject (data)
public class Resource : ScriptableObject
{
    public string resourceName;
    public Sprite icon;
}

// MonoBehaviour (behavior)
public class ResourceObject : MonoBehaviour
{
    public Resource resourceType;  // References ScriptableObject
    
    void Start()
    {
        // Use data from ScriptableObject
        GetComponent<SpriteRenderer>().sprite = resourceType.icon;
    }
}
```

### Pattern 3: Configuration Asset

ScriptableObject stores configuration, system reads it:

```csharp
// Configuration (ScriptableObject)
public class LevelDataSO : ScriptableObject
{
    public List<ResourceGoalSO> sequentialGoals;
}

// System (MonoBehaviour)
public class LevelManager : MonoBehaviour
{
    public LevelDataSO currentLevel;  // References configuration
    
    void Start()
    {
        // Read configuration and spawn goals
        foreach (var goal in currentLevel.sequentialGoals)
        {
            GoalManager.Instance.AddGoal(goal);
        }
    }
}
```

---

## Troubleshooting

### Problem: Changes Don't Persist

**Cause**: Modifying ScriptableObject at runtime doesn't save to disk.

**Solution**: 
- Runtime changes are temporary
- Use `AssetDatabase.SaveAssets()` in Editor scripts to save
- Or use runtime instances (`Instantiate()`) for temporary changes

### Problem: "Object reference not set"

**Cause**: ScriptableObject field is null (not assigned in Inspector).

**Solution**: 
- Always assign ScriptableObject references in Inspector
- Add null checks: `if (myResource != null) { ... }`

### Problem: Multiple Objects Share State

**Cause**: All objects reference the same ScriptableObject asset.

**Solution**: 
- Use `Instantiate()` to create copies if you need independent state
- Or design your system to share state intentionally

### Problem: Can't Find ScriptableObject in Menu

**Cause**: Missing `[CreateAssetMenu]` attribute or incorrect namespace.

**Solution**: 
- Add `[CreateAssetMenu(fileName = "...", menuName = "...")]`
- Ensure class inherits from `ScriptableObject`
- Check that script compiles without errors

---

## Summary

**ScriptableObjects** are Unity's way of creating **data assets** that:

- ✅ Store game data independently of GameObjects
- ✅ Can be edited in the Inspector
- ✅ Are shared efficiently across multiple objects
- ✅ Enable data-driven game design
- ✅ Are essential for resources, goals, levels, and configurations

**Key Takeaways**:
- ScriptableObjects = Data containers
- MonoBehaviours = Behavior/logic
- Use `[CreateAssetMenu]` to add to Create menu
- Use `[System.NonSerialized]` for runtime-only fields
- Organize assets in folders
- Use templates + runtime instances pattern for stateful data

---

## Related Documentation

- [Tutorial 02: Stations and Resources](../tutorials/02-Stations-and-Resources.md) — Creating and using Resource ScriptableObjects
- [Tutorial 04: Goals and Goal Tracker](../tutorials/04-Goals-and-Goal-Tracker.md) — Working with ResourceGoalSO
- [Tutorial 05: Levels and Level Editor](../tutorials/05-Levels-and-Level-Editor.md) — Creating LevelDataSO assets
- [06 – Static References](./06-Static-References.md) — How ScriptableObjects work with singleton managers
