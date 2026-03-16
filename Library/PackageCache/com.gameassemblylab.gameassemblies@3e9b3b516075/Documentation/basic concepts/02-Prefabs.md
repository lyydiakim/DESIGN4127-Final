# Prefabs

This foundational guide explains **Prefabs** in Unity and how they're used throughout the Game Assemblies library. Understanding prefabs is essential for creating reusable game objects, managing scene content, and building efficient game systems.

## What is a Prefab?

A **Prefab** is a saved GameObject (and all its components and children) that can be reused multiple times in your scenes. Think of it as a **template** or **blueprint** for creating objects.

### Prefab vs GameObject

| Aspect | Prefab | GameObject |
|--------|--------|------------|
| **Location** | Project window (asset) | Scene (instance) |
| **Purpose** | Template/blueprint | Actual object in scene |
| **Reusability** | Can be used many times | One specific instance |
| **Editing** | Edit prefab → all instances update | Edit instance → only that object changes |
| **When to use** | Reusable objects (players, enemies, items) | Scene-specific objects (level geometry) |

**Key Insight**: Prefabs are **templates** stored in your project. When you place a prefab in a scene, you create an **instance** of that template.

---

## Why Use Prefabs?

### 1. **Reusability**

Create once, use many times:

- **Player prefab**: Use the same player setup in multiple scenes
- **Resource prefab**: Spawn the same resource object many times
- **Station prefab**: Place the same station type in different locations

### 2. **Consistency**

All instances share the same:
- Components and settings
- Hierarchy structure
- Scripts and values

**Example**: 100 "Wood" resource objects all behave identically because they're instances of the same prefab.

### 3. **Easy Updates**

Edit the prefab once, all instances update:

1. Select prefab in Project window
2. Make changes in Inspector
3. Click "Apply" button
4. All instances in all scenes update automatically

### 4. **Scene Management**

- Keep scenes clean (prefabs live in Project, not cluttering scenes)
- Easy to organize (prefabs in folders)
- Version control friendly (prefabs are files)

---

## Creating Prefabs

### Method 1: Drag and Drop

1. **Create GameObject** in scene with desired components
2. **Configure** it (set values, add scripts, etc.)
3. **Drag** from Hierarchy to Project window
4. **Name** the prefab (e.g., "Player", "Wood Resource")

**Result**: A `.prefab` file is created in your Project window.

### Method 2: Create Empty Prefab

1. **Right-click** in Project window
2. **Create → Prefab**
3. **Name** it
4. **Select** the prefab
5. **Add components** in Inspector

### Method 3: Programmatically (Editor Scripts)

```csharp
using UnityEngine;
using UnityEditor;

// Create prefab from GameObject in scene
GameObject instance = GameObject.Find("MyObject");
string prefabPath = "Assets/Prefabs/MyPrefab.prefab";
PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
```

---

## Using Prefabs

### Placing Prefabs in Scene

**Method 1: Drag from Project**
- Drag prefab from Project window into Scene view or Hierarchy
- Creates an **instance** of the prefab

**Method 2: Instantiate in Code**

```csharp
// Load prefab
GameObject prefab = Resources.Load<GameObject>("MyPrefab");

// Instantiate at position
GameObject instance = Instantiate(prefab, new Vector3(0, 0, 0), Quaternion.identity);

// Instantiate as child
GameObject instance = Instantiate(prefab, parentTransform);
```

**Example from Game Assemblies** (`GoalManager.cs`):

```csharp
// Instantiate goal tracker prefab
GameObject gTracker = Instantiate(goalTracker);
gTracker.transform.parent = goalTrackerGrid.transform;
allGoalTrackers.Add(gTracker);
```

### Prefab Instances

When you place a prefab in a scene, you create an **instance**:

- **Prefab Connection**: Instance is linked to prefab (blue text in Hierarchy)
- **Overrides**: You can modify instance without affecting prefab
- **Revert**: Reset instance to match prefab
- **Apply**: Save instance changes back to prefab

---

## Prefab Variants

**Prefab Variants** allow you to create variations of a prefab:

1. **Base Prefab**: Original template (e.g., "Station")
2. **Variant**: Modified version (e.g., "Oven Station", "Cutting Station")

**Creating a Variant**:
1. Right-click base prefab
2. **Create → Prefab Variant**
3. Modify the variant
4. Changes only affect the variant, not the base

**Use Cases**:
- Different station types (all based on "Station" prefab)
- Player variations (different sprites, same controller)
- Resource variations (different icons, same behavior)

---

## Prefab Connection States

### Connected Prefab

- **Blue text** in Hierarchy
- Instance is linked to prefab
- Changes to prefab update instance
- Can have **overrides** (local modifications)

### Broken Prefab

- **Black text** in Hierarchy
- Connection to prefab is lost
- Changes to prefab don't affect instance
- Instance is now independent

### Missing Prefab

- **Red text** in Hierarchy
- Prefab file was deleted or moved
- Instance exists but has no template
- Usually indicates an error

---

## Working with Prefabs in Code

### Loading Prefabs

**Method 1: Resources.Load** (if in Resources folder)

```csharp
GameObject prefab = Resources.Load<GameObject>("Prefabs/MyPrefab");
GameObject instance = Instantiate(prefab);
```

**Method 2: AssetDatabase** (Editor scripts only)

```csharp
using UnityEditor;

GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
    "Assets/Prefabs/MyPrefab.prefab"
);
```

**Method 3: Reference in Inspector**

```csharp
public class Spawner : MonoBehaviour
{
    public GameObject prefabToSpawn;  // Assign in Inspector
    
    void Spawn()
    {
        Instantiate(prefabToSpawn, transform.position, Quaternion.identity);
    }
}
```

### Instantiating Prefabs

```csharp
// Basic instantiation
GameObject instance = Instantiate(prefab);

// At specific position
GameObject instance = Instantiate(prefab, new Vector3(5, 0, 0), Quaternion.identity);

// As child of another object
GameObject instance = Instantiate(prefab, parentTransform);

// Store reference
GameObject newPlayer = Instantiate(playerPrefab);
newPlayer.name = "Player " + playerCount;
```

### PrefabUtility (Editor Scripts)

**Important**: In editor scripts, use `PrefabUtility.InstantiatePrefab()` instead of `Instantiate()`:

```csharp
using UnityEditor;
using UnityEngine.SceneManagement;

// ✅ Correct: Maintains prefab connection
GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
    prefab, 
    SceneManager.GetActiveScene()
);

// ❌ Wrong: Breaks prefab connection
GameObject instance = Instantiate(prefab);
```

**Example from Game Assemblies** (`SA_Menu.cs`):

```csharp
GameObject rm_prefab = SA_AssetPathHelper.FindPrefab(
    "Samples/Prefabs/Managers/ResourceManager.prefab"
);

GameObject rm_instance = (GameObject)PrefabUtility.InstantiatePrefab(
    rm_prefab, 
    SceneManager.GetActiveScene()
);
```

---

## Prefabs in Game Assemblies

The Game Assemblies library uses prefabs extensively:

### Manager Prefabs

Located at `Samples/Prefabs/Managers/`:

- **ResourceManager.prefab** — Resource tracking system
- **GoalManager.prefab** — Goal management system
- **LevelManager.prefab** — Level progression system
- **PlayerManager.prefab** — Local multiplayer system

**Usage**: Created via editor menu items:
- `Game Assemblies → Systems → Create Resource Management System`
- `Game Assemblies → Players → Create Local Multiplayer System`

### Resource Prefabs

Located at `Samples/Prefabs/Resources/`:

- **resource_obj_template.prefab** — Template for creating resource objects
- Individual resource prefabs (Wood, Bread, etc.)

**Usage**: Created via `Game Assemblies → Resources → Create Resource`

### UI Prefabs

Located at `Samples/Prefabs/UI Prefabs/`:

- **ResourceManager_Canvas.prefab** — UI for resource tracking
- **GoalTracker.prefab** — Individual goal tracker UI element

### Player Prefabs

Located at `Samples/Prefabs/Players/`:

- **Player_Drawn.prefab** — Player character with controller

---

## Prefab Workflow

### Creating a New Prefab

1. **Design in Scene**: Create GameObject with all components
2. **Test**: Verify it works correctly
3. **Save as Prefab**: Drag to Project window
4. **Organize**: Place in appropriate folder
5. **Delete from Scene**: Remove original (prefab is saved)

### Modifying Prefabs

**Edit Prefab** (affects all instances):
1. Select prefab in Project window
2. Make changes in Inspector
3. Click "Apply" button (if needed)
4. All instances update

**Edit Instance** (affects only that instance):
1. Select instance in Hierarchy
2. Make changes in Inspector
3. Changes show as **overrides** (bold text)
4. Can **Revert** to match prefab or **Apply** to save to prefab

### Updating Multiple Instances

**Method 1: Edit Prefab**
- Changes apply to all instances automatically

**Method 2: Select All Instances**
- Select multiple instances in Hierarchy
- Make changes (applies to all selected)

**Method 3: Find References**
- Right-click prefab → **Find References in Scene**
- Selects all instances of that prefab

---

## Best Practices

### 1. **Organize Prefabs in Folders**

```
Assets/
  Prefabs/
    Players/
      Player.prefab
    Resources/
      Wood.prefab
      Bread.prefab
    Stations/
      Oven.prefab
      CuttingBoard.prefab
```

### 2. **Use Descriptive Names**

- ✅ Good: `Player_Drawn.prefab`, `ResourceManager.prefab`
- ❌ Bad: `prefab1.prefab`, `object.prefab`

### 3. **Keep Prefabs Simple**

- One prefab = one purpose
- Use prefab variants for variations
- Compose complex objects from multiple prefabs

### 4. **Test Before Saving**

- Verify prefab works in scene before saving
- Test all components and scripts
- Check that references are correct

### 5. **Use PrefabUtility in Editor Scripts**

```csharp
// ✅ Always use PrefabUtility in editor
PrefabUtility.InstantiatePrefab(prefab, scene);

// ❌ Don't use Instantiate() in editor scripts
Instantiate(prefab);  // Breaks prefab connection
```

### 6. **Document Prefabs**

- Add comments in scripts
- Use clear component names
- Organize hierarchy logically

---

## Common Patterns

### Pattern 1: Prefab Spawner

```csharp
public class ResourceSpawner : MonoBehaviour
{
    public GameObject resourcePrefab;
    public float spawnInterval = 2.0f;
    
    void Start()
    {
        InvokeRepeating("SpawnResource", 0, spawnInterval);
    }
    
    void SpawnResource()
    {
        Instantiate(resourcePrefab, transform.position, Quaternion.identity);
    }
}
```

### Pattern 2: Prefab Pool

```csharp
public class PrefabPool : MonoBehaviour
{
    public GameObject prefab;
    private List<GameObject> pool = new List<GameObject>();
    
    public GameObject Get()
    {
        // Reuse existing or create new
        foreach (GameObject obj in pool)
        {
            if (!obj.activeInHierarchy)
            {
                obj.SetActive(true);
                return obj;
            }
        }
        
        GameObject newObj = Instantiate(prefab);
        pool.Add(newObj);
        return newObj;
    }
}
```

### Pattern 3: Prefab Reference

```csharp
public class Station : MonoBehaviour
{
    // Reference to resource prefab
    public GameObject resourcePrefab;
    
    void ProduceResource()
    {
        // Spawn resource at output position
        GameObject resource = Instantiate(
            resourcePrefab, 
            outputPosition, 
            Quaternion.identity
        );
    }
}
```

---

## Common Mistakes

### Mistake 1: Using Instantiate() in Editor Scripts

```csharp
// ❌ Bad: Breaks prefab connection
GameObject instance = Instantiate(prefab);

// ✅ Good: Maintains prefab connection
GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
    prefab, 
    SceneManager.GetActiveScene()
);
```

### Mistake 2: Editing Instance Instead of Prefab

```csharp
// ❌ Bad: Changes only affect one instance
// (Editing instance in scene)

// ✅ Good: Edit prefab to update all instances
// (Select prefab in Project window, make changes)
```

### Mistake 3: Not Organizing Prefabs

```csharp
// ❌ Bad: All prefabs in root folder
Assets/Player.prefab
Assets/Enemy.prefab
Assets/Item.prefab

// ✅ Good: Organized in folders
Assets/Prefabs/Players/Player.prefab
Assets/Prefabs/Enemies/Enemy.prefab
Assets/Prefabs/Items/Item.prefab
```

### Mistake 4: Missing References

```csharp
// ❌ Bad: Reference lost when prefab is moved
public GameObject prefab;  // Missing reference after move

// ✅ Good: Use helper to find prefabs
GameObject prefab = SA_AssetPathHelper.FindPrefab("Path/To/Prefab.prefab");
```

---

## Troubleshooting

### Problem: Prefab Changes Don't Apply

**Cause**: Instance has overrides that conflict.

**Solution**: 
- Select instance → **Revert** to remove overrides
- Or **Apply** instance changes to prefab

### Problem: Missing Prefab Reference

**Cause**: Prefab was moved or deleted.

**Solution**: 
- Find prefab in Project window
- Reassign reference in Inspector
- Or use helper methods to find by path

### Problem: Prefab Connection Broken

**Cause**: Using `Instantiate()` instead of `PrefabUtility.InstantiatePrefab()` in editor.

**Solution**: Always use `PrefabUtility.InstantiatePrefab()` in editor scripts.

### Problem: Changes Affect All Instances Unintentionally

**Cause**: Editing prefab when you meant to edit instance.

**Solution**: 
- Make sure you're selecting instance in Hierarchy (not prefab in Project)
- Check if text is blue (connected) or black (broken)

---

## Summary

**Prefabs** are Unity's way of creating reusable object templates:

- ✅ **Templates** stored in Project window
- ✅ **Instances** placed in scenes
- ✅ **Consistency** across all instances
- ✅ **Easy updates** (edit once, all update)
- ✅ **Organization** and reusability

**Key Takeaways**:
- Prefabs are templates, instances are actual objects
- Edit prefab to update all instances
- Use `PrefabUtility.InstantiatePrefab()` in editor scripts
- Organize prefabs in folders
- Keep prefabs simple and focused

**Game Assemblies Prefabs**:
- Manager prefabs (ResourceManager, GoalManager, etc.)
- Resource prefabs (Wood, Bread, etc.)
- UI prefabs (Canvas, GoalTracker, etc.)
- Player prefabs (Player_Drawn)

---

## Related Documentation

- [03 – Syntax](./03-Syntax.md) — Understanding C# code structure
- [10 – Editor Tools](./10-Editor-Tools.md) — Creating prefabs programmatically
- [Tutorial 01: Creating a Character and Canvas](../tutorials/01-Creating-Character-and-Canvas.md) — Using prefabs in Game Assemblies
