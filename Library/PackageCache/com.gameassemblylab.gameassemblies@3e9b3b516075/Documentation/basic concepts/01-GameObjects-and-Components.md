# GameObjects and Components

This foundational guide explains **GameObjects** and **Components** in Unity - the fundamental building blocks of everything in your game. Understanding this relationship is essential for working with Unity and the Game Assemblies library.

## What is a GameObject?

A **GameObject** is the basic entity in Unity. **Almost everything** in your Unity scene is a GameObject:

- **Players** — GameObjects with movement scripts
- **Stations** — GameObjects with station components
- **Resources** — GameObjects with resource components
- **Cameras** — GameObjects with Camera components
- **Lights** — GameObjects with Light components
- **UI Elements** — GameObjects with UI components
- **Empty Objects** — GameObjects with no components (except Transform)

**Key Insight**: In Unity, a GameObject is like an **empty container**. By itself, it does nothing. **Components** give it functionality.

---

## What are Components?

**Components** are pieces of functionality that you **attach** to GameObjects. Think of a GameObject as a car chassis, and components as the engine, wheels, and steering wheel.

### Component Types

**Built-in Unity Components**:
- **Transform** — Position, rotation, scale (mandatory)
- **SpriteRenderer** — Renders 2D sprites
- **Rigidbody2D** — Physics body for 2D
- **Collider2D** — Collision detection
- **Camera** — Renders the view
- **AudioSource** — Plays sounds
- **Animator** — Controls animations

**Custom Script Components** (MonoBehaviour):
- **playerController** — Player movement and interaction
- **Station** — Station behavior
- **ResourceManager** — Resource tracking
- **GoalManager** — Goal management

---

## The Transform Component: Always Present

### Why Transform is Mandatory

**Every GameObject in a scene MUST have a Transform component**. You cannot remove it, and Unity automatically adds it.

**Transform stores**:
- **Position** (x, y, z) — Where the object is
- **Rotation** (x, y, z) — Which way it's facing
- **Scale** (x, y, z) — How big it is

### Transform in 2D vs 3D

**In 2D games**:
- Position: (x, y) — z is usually 0
- Rotation: z-axis rotation (spinning in 2D plane)
- Scale: (x, y) — z is usually 1

**In 3D games**:
- Position: (x, y, z) — full 3D space
- Rotation: (x, y, z) — rotation on all axes
- Scale: (x, y, z) — scaling on all axes

**Example**:

```csharp
// Access Transform component
Transform myTransform = transform;  // Shorthand for GetComponent<Transform>()

// Get position
Vector3 position = transform.position;

// Set position
transform.position = new Vector3(5, 3, 0);

// Move object
transform.position += new Vector3(1, 0, 0);  // Move right
```

**Example from Game Assemblies** (`playerController.cs`):

```csharp
// Transform is always available
transform.position = Vector3.zero;  // Set to origin
carryPosition = transform;  // Reference to this object's transform
```

---

## Empty GameObjects

An **Empty GameObject** has **only** the Transform component (which is mandatory). It has no other functionality.

### When to Use Empty GameObjects

**1. Organization** — Group related objects:

```
Scene
├── Managers (Empty GameObject)
│   ├── ResourceManager
│   ├── GoalManager
│   └── LevelManager
├── Players (Empty GameObject)
│   ├── Player 1
│   └── Player 2
└── Stations (Empty GameObject)
    ├── Oven
    └── Cutting Board
```

**2. Spawn Points** — Mark locations:

```csharp
// Empty GameObject at spawn location
// Only has Transform (position)
// Scripts can find it and spawn objects there
GameObject spawnPoint = GameObject.Find("SpawnPoint");
Instantiate(prefab, spawnPoint.transform.position, Quaternion.identity);
```

**3. Parent Objects** — Hold child objects:

```csharp
// Empty GameObject as parent
GameObject container = new GameObject("ResourceContainer");
resourceObject.transform.parent = container.transform;
```

**4. Script Holders** — Attach scripts without visual representation:

```csharp
// Empty GameObject with manager script
GameObject manager = new GameObject("GameManager");
manager.AddComponent<GameManager>();
```

**Example from Game Assemblies**: Manager objects are often empty GameObjects with just scripts attached.

---

## GameObjects with Components

### Adding Functionality

Components give GameObjects their behavior and appearance:

**Example: Player GameObject**

```
Player (GameObject)
├── Transform (mandatory - position, rotation, scale)
├── SpriteRenderer (visual appearance)
├── Rigidbody2D (physics)
├── Collider2D (collision detection)
└── playerController (custom script - movement, interaction)
```

**Example: Station GameObject**

```
Station (GameObject)
├── Transform (mandatory)
├── SpriteRenderer (visual appearance)
├── Collider2D (interaction area)
└── Station (custom script - resource conversion)
```

### Component Stacking

You can add **multiple components** of the same type (though usually not necessary):

```csharp
// Add multiple colliders for complex shapes
GameObject player = new GameObject("Player");
player.AddComponent<CircleCollider2D>();  // Body collider
player.AddComponent<BoxCollider2D>();     // Grab area collider
```

---

## Working with Components in Code

### Getting Components

```csharp
// Get component (returns null if not found)
SpriteRenderer sprite = GetComponent<SpriteRenderer>();

// Get component with null check
if (GetComponent<SpriteRenderer>() != null)
{
    SpriteRenderer sprite = GetComponent<SpriteRenderer>();
}

// Get component (throws error if not found)
SpriteRenderer sprite = GetComponent<SpriteRenderer>();
if (sprite == null)
{
    Debug.LogError("SpriteRenderer not found!");
}
```

### Adding Components

```csharp
// Add component
SpriteRenderer sprite = gameObject.AddComponent<SpriteRenderer>();
sprite.sprite = mySprite;

// Add multiple components
Rigidbody2D rb = gameObject.AddComponent<Rigidbody2D>();
Collider2D col = gameObject.AddComponent<CircleCollider2D>();
```

### Removing Components

```csharp
// Remove component
SpriteRenderer sprite = GetComponent<SpriteRenderer>();
if (sprite != null)
{
    Destroy(sprite);  // Remove at runtime
    // Or DestroyImmediate(sprite);  // Remove in editor
}
```

### Finding Components

```csharp
// Find component on this GameObject
SpriteRenderer sprite = GetComponent<SpriteRenderer>();

// Find component on child objects
SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();

// Find component on parent
SpriteRenderer sprite = GetComponentInParent<SpriteRenderer>();

// Find component anywhere
SpriteRenderer sprite = FindObjectOfType<SpriteRenderer>();
```

**Example from Game Assemblies** (`playerController.cs`):

```csharp
// Get components
Rigidbody2D rb = GetComponent<Rigidbody2D>();
SpriteRenderer playerSprite = GetComponent<SpriteRenderer>();
```

---

## Common Component Patterns in Game Assemblies

### Pattern 1: Manager GameObject

**Empty GameObject with Script Component**:

```
ResourceManager (GameObject)
├── Transform (mandatory)
└── ResourceManager (MonoBehaviour script)
    - Tracks all resources
    - Manages global capital
    - Updates UI
```

**Code Example**:

```csharp
public class ResourceManager : MonoBehaviour
{
    // MonoBehaviour = component that can be attached to GameObject
    public static ResourceManager Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;  // Set singleton
    }
}
```

### Pattern 2: Player GameObject

**GameObject with Multiple Components**:

```
Player_Drawn (GameObject)
├── Transform (mandatory)
├── SpriteRenderer (visual)
├── Rigidbody2D (physics)
├── Collider2D (collision)
└── playerController (MonoBehaviour script)
    - Movement logic
    - Input handling
    - Interaction system
```

### Pattern 3: Station GameObject

**GameObject with Station Functionality**:

```
Oven (GameObject)
├── Transform (mandatory)
├── SpriteRenderer (visual)
├── Collider2D (interaction area)
└── Station (MonoBehaviour script)
    - Resource conversion
    - Input/output areas
    - Production logic
```

### Pattern 4: UI GameObject

**GameObject with UI Components**:

```
Canvas (GameObject)
├── Transform (mandatory)
├── Canvas (UI rendering)
├── CanvasScaler (UI scaling)
└── GraphicRaycaster (UI interaction)
    └── GoalTracker (GameObject child)
        ├── Transform
        ├── Image (background)
        ├── TextMeshProUGUI (text)
        └── GoalTrackerUI (MonoBehaviour script)
```

---

## Component Lifecycle

### MonoBehaviour Callbacks

When a component (MonoBehaviour) is attached to a GameObject, Unity calls these methods automatically:

```csharp
public class MyComponent : MonoBehaviour
{
    void Awake()        // Called when object is created (before Start)
    {
        // Initialize variables
        // Set up references
    }
    
    void OnEnable()     // Called when object becomes active
    {
        // Subscribe to events
    }
    
    void Start()        // Called before first frame update
    {
        // Setup that depends on other objects
    }
    
    void Update()       // Called every frame
    {
        // Game logic
    }
    
    void FixedUpdate()  // Called at fixed intervals (for physics)
    {
        // Physics-related code
    }
    
    void OnDisable()    // Called when object becomes inactive
    {
        // Unsubscribe from events
    }
    
    void OnDestroy()    // Called when object is destroyed
    {
        // Cleanup
    }
}
```

**Example from Game Assemblies** (`GoalManager.cs`):

```csharp
public class GoalManager : MonoBehaviour
{
    private void Awake()
    {
        // Set up singleton
        if (Instance == null)
        {
            Instance = this;
        }
    }
    
    private void Start()
    {
        // Initialize goals after all objects are created
        allGoalTrackers = new List<GameObject>();
    }
    
    private void Update()
    {
        // Process goals every frame
        foreach (ResourceGoalSO goal in activeGoals)
        {
            goal.UpdateGoaltime(Time.deltaTime);
        }
    }
}
```

---

## Component Dependencies

### Requiring Components

You can specify that a component **requires** another component:

```csharp
[RequireComponent(typeof(Rigidbody2D))]
public class playerController : MonoBehaviour
{
    // Rigidbody2D is automatically added if missing
    // Cannot remove Rigidbody2D while this script is attached
}
```

**Benefits**:
- Ensures required components exist
- Automatically adds missing components
- Prevents accidental removal

### Component References

Components often reference other components:

```csharp
public class Station : MonoBehaviour
{
    // Reference to other components
    private SpriteRenderer spriteRenderer;
    private Collider2D interactionArea;
    
    void Start()
    {
        // Get component references
        spriteRenderer = GetComponent<SpriteRenderer>();
        interactionArea = GetComponent<Collider2D>();
    }
}
```

---

## Best Practices

### 1. **Use Empty GameObjects for Organization**

```csharp
// ✅ Good: Organize scene hierarchy
GameObject managers = new GameObject("Managers");
GameObject players = new GameObject("Players");
GameObject stations = new GameObject("Stations");
```

### 2. **Cache Component References**

```csharp
// ✅ Good: Cache in Awake/Start
private SpriteRenderer spriteRenderer;

void Awake()
{
    spriteRenderer = GetComponent<SpriteRenderer>();
}

void Update()
{
    spriteRenderer.color = Color.red;  // Use cached reference
}

// ❌ Bad: Get component every frame
void Update()
{
    GetComponent<SpriteRenderer>().color = Color.red;  // Slow!
}
```

### 3. **Check for Null Before Using**

```csharp
// ✅ Good: Always check
SpriteRenderer sprite = GetComponent<SpriteRenderer>();
if (sprite != null)
{
    sprite.color = Color.red;
}

// ❌ Bad: Can crash if component missing
GetComponent<SpriteRenderer>().color = Color.red;
```

### 4. **Use RequireComponent When Needed**

```csharp
// ✅ Good: Ensure dependencies
[RequireComponent(typeof(Rigidbody2D))]
public class Movement : MonoBehaviour
{
    // Rigidbody2D guaranteed to exist
}
```

### 5. **Name GameObjects Clearly**

```csharp
// ✅ Good: Descriptive names
GameObject playerManager = new GameObject("PlayerManager");
GameObject resourceSpawner = new GameObject("ResourceSpawner");

// ❌ Bad: Unclear names
GameObject obj1 = new GameObject("Object");
GameObject thing = new GameObject("Thing");
```

---

## Common Mistakes

### Mistake 1: Forgetting Transform Exists

```csharp
// ❌ Bad: Trying to remove Transform
Destroy(GetComponent<Transform>());  // Error! Cannot remove Transform

// ✅ Good: Transform is always there, just use it
transform.position = Vector3.zero;
```

### Mistake 2: Not Caching Component References

```csharp
// ❌ Bad: Getting component every frame
void Update()
{
    GetComponent<SpriteRenderer>().color = Color.red;  // Slow!
}

// ✅ Good: Cache once, use many times
private SpriteRenderer spriteRenderer;

void Awake()
{
    spriteRenderer = GetComponent<SpriteRenderer>();
}

void Update()
{
    spriteRenderer.color = Color.red;  // Fast!
}
```

### Mistake 3: Not Checking for Null

```csharp
// ❌ Bad: Crashes if component missing
GetComponent<SpriteRenderer>().color = Color.red;

// ✅ Good: Check first
SpriteRenderer sprite = GetComponent<SpriteRenderer>();
if (sprite != null)
{
    sprite.color = Color.red;
}
```

### Mistake 4: Confusing GameObject and Component

```csharp
// ❌ Bad: GameObject doesn't have these properties
GameObject obj = new GameObject();
obj.color = Color.red;  // Error! GameObject has no color property

// ✅ Good: Component has the property
SpriteRenderer sprite = obj.AddComponent<SpriteRenderer>();
sprite.color = Color.red;  // Component has color property
```

---

## GameObject Hierarchy

### Parent-Child Relationships

GameObjects can have **parent-child relationships**:

```
Parent (GameObject)
├── Transform
└── Child 1 (GameObject)
    ├── Transform (relative to parent)
    └── Child 2 (GameObject)
        └── Transform (relative to Child 1)
```

**Transform Inheritance**:
- Child position is **relative** to parent
- Moving parent moves all children
- Rotating parent rotates all children
- Scaling parent scales all children

**Example**:

```csharp
// Create parent
GameObject parent = new GameObject("Parent");
parent.transform.position = new Vector3(5, 0, 0);

// Create child
GameObject child = new GameObject("Child");
child.transform.parent = parent.transform;
child.transform.localPosition = Vector3.zero;  // Relative to parent

// Child's world position is (5, 0, 0) + (0, 0, 0) = (5, 0, 0)
```

**Example from Game Assemblies**: Goal trackers are children of the goal tracker grid:

```csharp
GameObject gTracker = Instantiate(goalTracker);
gTracker.transform.parent = goalTrackerGrid.transform;  // Parent-child relationship
```

---

## Summary

**GameObjects and Components** are Unity's fundamental building blocks:

- ✅ **GameObject** — Basic entity (almost everything in Unity)
- ✅ **Component** — Functionality attached to GameObject
- ✅ **Transform** — Mandatory component (position, rotation, scale)
- ✅ **Empty GameObject** — Only has Transform (for organization)
- ✅ **Components** — Add functionality (visual, physics, scripts)

**Key Takeaways**:
- Almost everything in Unity is a GameObject
- Components give GameObjects functionality
- Transform is mandatory and cannot be removed
- Empty GameObjects are useful for organization
- Cache component references for performance
- Always check for null before using components

**Common Patterns**:
- Manager = Empty GameObject + Script component
- Player = GameObject + Multiple components (SpriteRenderer, Rigidbody2D, Script)
- Station = GameObject + Visual + Collider + Script
- UI = GameObject + UI components + Script

---

## Related Documentation

- [02 – Prefabs](./02-Prefabs.md) — Understanding prefabs (saved GameObjects)
- [03 – Syntax](./03-Syntax.md) — Writing MonoBehaviour scripts
- [06 – Static References](./06-Static-References.md) — How manager GameObjects work
- [Tutorial 01: Creating a Character and Canvas](../tutorials/01-Creating-Character-and-Canvas.md) — Creating GameObjects in Game Assemblies
