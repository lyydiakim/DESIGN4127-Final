# Static References

This foundational guide explains **static references** and the **singleton pattern** used throughout the Game Assemblies library. Understanding static references is essential for accessing managers, sharing data across objects, and writing efficient game code.

## What is a Static Reference?

A **static reference** is a way to access an object or value **without needing a direct reference** to a specific instance. In Unity, this is commonly implemented using the **singleton pattern**.

### Static vs Instance

| Aspect | Static | Instance |
|--------|--------|----------|
| **Access** | `ClassName.MemberName` | `objectInstance.MemberName` |
| **Belongs to** | The class itself | A specific object |
| **Number** | One per class (shared) | One per object |
| **Example** | `ResourceManager.Instance` | `myResourceManager.globalCapital` |

**Key Insight**: Static members belong to the **class**, not to individual **objects**.

---

## The Singleton Pattern

A **singleton** is a design pattern that ensures **only one instance** of a class exists, and provides **global access** to that instance.

### Basic Singleton Structure

```csharp
public class ResourceManager : MonoBehaviour
{
    // Static property that holds the single instance
    public static ResourceManager Instance { get; private set; }
    
    // Instance data
    public int globalCapital = 0;
    public List<ResourceObject> allResources = new List<ResourceObject>();
    
    private void Awake()
    {
        // Ensure only one instance exists
        if (Instance == null)
        {
            Instance = this;  // This is the first instance
        }
        else
        {
            Destroy(gameObject);  // Duplicate instance, destroy it
        }
    }
}
```

### How It Works

1. **First instance created**: `Instance` is `null`, so we set `Instance = this`
2. **Second instance created**: `Instance` is not `null`, so we destroy the duplicate
3. **Result**: Only one instance exists, accessible via `ResourceManager.Instance`

### Accessing the Singleton

```csharp
// From anywhere in your code:
ResourceManager.Instance.globalCapital = 100;
int capital = ResourceManager.Instance.globalCapital;
ResourceManager.Instance.AddResource(myResource);
```

**No need to**:
- Find the GameObject
- Get the component
- Pass references around
- Store a reference

---

## Singletons in Game Assemblies

The Game Assemblies library uses singletons for **manager classes** that need global access:

### ResourceManager

**Purpose**: Tracks all resources in the game and global capital

```csharp
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }
    public int globalCapital = 0;
    public List<ResourceObject> allResources = new List<ResourceObject>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }
    
    public int GetResourceCount(Resource resourceType)
    {
        // Count resources of a specific type
        return allResources.Count(r => r.resourceType == resourceType);
    }
}
```

**Usage**:
```csharp
// Add resource
ResourceManager.Instance.AddResource(myResource);

// Get resource count
int woodCount = ResourceManager.Instance.GetResourceCount(woodResource);

// Access global capital
int score = ResourceManager.Instance.globalCapital;
```

### GoalManager

**Purpose**: Manages active goals and goal tracking

```csharp
public class GoalManager : MonoBehaviour
{
    public static GoalManager Instance { get; private set; }
    public List<ResourceGoalSO> activeGoals = new List<ResourceGoalSO>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void goalContribution(Resource rType)
    {
        // Check if any goal matches this resource
        foreach (ResourceGoalSO goal in activeGoals)
        {
            goal.UpdateGoalObjective(rType);
        }
    }
}
```

**Usage**:
```csharp
// Notify goal completion
GoalManager.Instance.goalContribution(breadResource);

// Add a new goal
GoalManager.Instance.AddGoal(newGoal);

// Check active goals
int activeCount = GoalManager.Instance.activeGoals.Count;
```

### LevelManager

**Purpose**: Manages level progression and goal spawning

```csharp
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    public List<LevelDataSO> levelDataList = new List<LevelDataSO>();
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
```

**Usage**:
```csharp
// Check if level manager exists
if (LevelManager.Instance != null)
{
    // Access level data
    var currentLevel = LevelManager.Instance.levelDataList[0];
}
```

### GameManager

**Purpose**: Manages game state (Menu, Playing, Paused, etc.)

```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameState CurrentState { get; private set; } = GameState.Menu;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
```

**Usage**:
```csharp
// Check game state
if (GameManager.Instance.CurrentState == GameState.Playing)
{
    // Game is active
}

// Change game state
GameManager.Instance.ChangeState(GameState.Paused);
```

---

## Why Use Singletons?

### 1. **Global Access**

Any script can access the manager without:
- Finding the GameObject
- Getting the component
- Storing references
- Passing references through multiple objects

**Example**: A station can notify GoalManager directly:
```csharp
// Station script
public class Station : MonoBehaviour
{
    void OnResourceProduced()
    {
        // Direct access, no reference needed
        GoalManager.Instance.goalContribution(producedResource);
    }
}
```

### 2. **Single Source of Truth**

Only one instance exists, so:
- No confusion about which manager to use
- Data is consistent across the game
- Changes affect the entire system

**Example**: `ResourceManager.Instance.globalCapital` is the same value everywhere.

### 3. **Easy Communication**

Objects can communicate without direct references:

```csharp
// Station produces resource
ResourceManager.Instance.AddResource(newResource);

// Goal checks completion
int count = ResourceManager.Instance.GetResourceCount(goalResource);

// UI updates display
scoreText.text = ResourceManager.Instance.globalCapital.ToString();
```

All three systems use the same `ResourceManager` instance without knowing about each other.

### 4. **Scene-Independent**

If you use `DontDestroyOnLoad()`, the singleton persists across scenes:

```csharp
private void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);  // Persist across scenes
    }
    else
    {
        Destroy(gameObject);
    }
}
```

---

## Static Properties vs Static Methods

### Static Properties (Singletons)

```csharp
public static ResourceManager Instance { get; private set; }
```

**Usage**: Access the single instance
```csharp
ResourceManager.Instance.DoSomething();
```

### Static Methods

```csharp
public static int getGlobalCapital()
{
    return Instance.globalCapital;
}
```

**Usage**: Call method without instance reference
```csharp
int capital = ResourceManager.getGlobalCapital();
```

**Note**: Static methods can still use `Instance` internally to access instance data.

---

## Null Checking

**Always check if the singleton exists** before using it:

### Safe Access Pattern

```csharp
if (ResourceManager.Instance != null)
{
    ResourceManager.Instance.AddResource(myResource);
}
else
{
    Debug.LogWarning("ResourceManager not found in scene!");
}
```

### Why Null Checks Matter

- Singleton might not exist in the scene
- Singleton might be destroyed
- Script might run before singleton's `Awake()`

### Example: GoalManager Null Check

```csharp
// In GoalManager.Update()
private void Update()
{
    // Check if GameManager exists before accessing it
    bool shouldProcessGoals = GameManager.Instance == null || 
                              GameManager.Instance.CurrentState == GameState.Playing;
    
    if (shouldProcessGoals)
    {
        // Process goals
    }
}
```

---

## Common Patterns

### Pattern 1: Manager Singleton

**Purpose**: Central manager for a system

```csharp
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }
    
    // System data
    public int globalCapital = 0;
    public List<ResourceObject> allResources = new List<ResourceObject>();
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }
    
    // Public methods
    public void AddResource(ResourceObject resource) { ... }
    public int GetResourceCount(Resource type) { ... }
}
```

### Pattern 2: Static Utility Class

**Purpose**: Helper methods that don't need an instance

```csharp
public static class ResourceUtils
{
    public static int CalculateTotalValue(List<ResourceObject> resources)
    {
        // Static method, no instance needed
        int total = 0;
        foreach (var resource in resources)
        {
            total += resource.value;
        }
        return total;
    }
}

// Usage:
int total = ResourceUtils.CalculateTotalValue(resourceList);
```

### Pattern 3: Static Data

**Purpose**: Shared data that doesn't need an instance

```csharp
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    // Static data (shared across all instances, though there's only one)
    public static float elapsedTime = 0;
    public static float currentLevelTimeLeft = 0f;
}
```

**Usage**:
```csharp
// Access static data directly
float time = LevelManager.elapsedTime;

// Or through instance
float time = LevelManager.Instance.elapsedTime;  // Same value
```

---

## Best Practices

### 1. **Always Null Check**

```csharp
if (ResourceManager.Instance != null)
{
    // Use singleton
}
```

### 2. **Use Private Setter**

```csharp
public static ResourceManager Instance { get; private set; }
```

Prevents external code from overwriting the instance.

### 3. **Destroy Duplicates**

```csharp
private void Awake()
{
    if (Instance == null)
        Instance = this;
    else
        Destroy(gameObject);  // Destroy duplicate GameObject
}
```

### 4. **Initialize in Awake**

Set up singleton in `Awake()` so it's ready before `Start()`:

```csharp
private void Awake()
{
    if (Instance == null)
        Instance = this;
    else
        Destroy(gameObject);
}

private void Start()
{
    // Instance is guaranteed to be set here
    Initialize();
}
```

### 5. **Use DontDestroyOnLoad Sparingly**

Only use `DontDestroyOnLoad()` if you need the singleton to persist across scenes:

```csharp
private void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);  // Only if needed
    }
    else
    {
        Destroy(gameObject);
    }
}
```

### 6. **Provide Fallback Behavior**

Handle the case when singleton doesn't exist:

```csharp
public void DoSomething()
{
    if (GoalManager.Instance != null)
    {
        GoalManager.Instance.goalContribution(resource);
    }
    else
    {
        // Fallback: log warning or use alternative logic
        Debug.LogWarning("GoalManager not available");
    }
}
```

---

## Common Mistakes

### Mistake 1: Not Checking for Null

```csharp
// ❌ Bad: Crashes if Instance is null
ResourceManager.Instance.AddResource(resource);

// ✅ Good: Safe access
if (ResourceManager.Instance != null)
{
    ResourceManager.Instance.AddResource(resource);
}
```

### Mistake 2: Creating Multiple Instances

```csharp
// ❌ Bad: Creates multiple instances
private void Start()
{
    Instance = this;  // Should be in Awake(), and check for null
}

// ✅ Good: Proper singleton pattern
private void Awake()
{
    if (Instance == null)
        Instance = this;
    else
        Destroy(gameObject);
}
```

### Mistake 3: Using Instance Before Awake()

```csharp
// ❌ Bad: Instance might be null
public class SomeScript : MonoBehaviour
{
    void Start()
    {
        ResourceManager.Instance.DoSomething();  // Might be null!
    }
}

// ✅ Good: Check first
public class SomeScript : MonoBehaviour
{
    void Start()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.DoSomething();
        }
    }
}
```

### Mistake 4: Public Setter

```csharp
// ❌ Bad: External code can overwrite instance
public static ResourceManager Instance { get; set; }

// ✅ Good: Private setter protects instance
public static ResourceManager Instance { get; private set; }
```

---

## Static References vs Direct References

### When to Use Static References (Singletons)

✅ **Use for**:
- Manager classes (ResourceManager, GoalManager, LevelManager)
- Systems that need global access
- Single-instance systems
- Cross-scene communication

### When to Use Direct References

✅ **Use for**:
- Components on the same GameObject
- Parent/child relationships
- Specific object interactions
- Temporary references

**Example**:
```csharp
public class Station : MonoBehaviour
{
    // Direct reference (specific to this station)
    public Resource inputResource;
    
    // Static reference (global access)
    void ProduceResource()
    {
        ResourceManager.Instance.AddResource(newResource);
    }
}
```

---

## Troubleshooting

### Problem: "Object reference not set to an instance"

**Cause**: Accessing `Instance` when it's `null`.

**Solution**: 
- Add null check: `if (ResourceManager.Instance != null)`
- Ensure singleton GameObject exists in scene
- Check that singleton's `Awake()` runs before your script

### Problem: Multiple Instances Exist

**Cause**: Singleton pattern not properly implemented.

**Solution**: 
- Ensure `Awake()` checks for existing instance
- Destroy duplicates: `Destroy(gameObject)` if `Instance != null`
- Use `Awake()` not `Start()` for initialization

### Problem: Instance is Null in Start()

**Cause**: Script order issue - your script runs before singleton's `Awake()`.

**Solution**: 
- Use `Start()` or `OnEnable()` instead of `Awake()`
- Add null check and retry logic
- Or use `[DefaultExecutionOrder]` attribute to control execution order

### Problem: Changes Don't Persist Across Scenes

**Cause**: Not using `DontDestroyOnLoad()`.

**Solution**: 
- Add `DontDestroyOnLoad(gameObject)` in `Awake()`
- Or recreate singleton in each scene

---

## Summary

**Static references** (singletons) provide:

- ✅ **Global access** to manager instances
- ✅ **Single source of truth** for system data
- ✅ **Easy communication** between objects
- ✅ **Scene-independent** access (with `DontDestroyOnLoad`)

**Key Takeaways**:
- Use singleton pattern for manager classes
- Always null check before accessing `Instance`
- Initialize in `Awake()`, not `Start()`
- Destroy duplicates to ensure single instance
- Use private setter to protect instance

**Game Assemblies Singletons**:
- `ResourceManager.Instance` — Resource tracking
- `GoalManager.Instance` — Goal management
- `LevelManager.Instance` — Level progression
- `GameManager.Instance` — Game state

---

## Related Documentation

- [07 – Scriptable Objects](./07-Scriptable-Objects.md) — How ScriptableObjects work with singleton managers
- [Tutorial 03: Resource Manager and Goals](../tutorials/03-Resource-Manager-and-Goals.md) — Setting up ResourceManager singleton
- [Tutorial 04: Goals and Goal Tracker](../tutorials/04-Goals-and-Goal-Tracker.md) — Using GoalManager singleton
