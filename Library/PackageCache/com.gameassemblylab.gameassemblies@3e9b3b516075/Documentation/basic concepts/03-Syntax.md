# Basic C# Syntax

This foundational guide covers essential **C# syntax** concepts you'll encounter when working with Unity and the Game Assemblies library. Understanding these fundamentals is crucial for reading and writing game code.

## What is C#?

**C#** (pronounced "C sharp") is a programming language developed by Microsoft. Unity uses C# as its primary scripting language for game development. C# is:

- **Object-oriented** — Code is organized into classes and objects
- **Type-safe** — Variables must have specific types
- **Modern** — Supports features like properties, generics, and LINQ

---

## Variables and Data Types

### Basic Data Types

Variables store data. Each variable has a **type** that determines what kind of data it can hold:

```csharp
// Integers (whole numbers)
int playerScore = 100;
int numberOfPlayers = 4;

// Floats (decimal numbers)
float playerSpeed = 2.5f;  // Note the 'f' suffix
float timeRemaining = 30.5f;

// Booleans (true/false)
bool isGameActive = true;
bool hasCompletedGoal = false;

// Strings (text)
string playerName = "Player 1";
string resourceName = "Wood";

// Characters (single character)
char firstLetter = 'A';
```

### Unity-Specific Types

Unity provides many built-in types:

```csharp
// Vector2 (2D position: x, y)
Vector2 position = new Vector2(5.0f, 3.0f);
Vector2 movement = Vector2.zero;  // (0, 0)

// Vector3 (3D position: x, y, z)
Vector3 worldPosition = new Vector3(0, 0, 0);
Vector3 spawnPoint = Vector3.zero;

// GameObject (any object in the scene)
GameObject player;
GameObject prefab;

// Components (attached to GameObjects)
Transform transform;
SpriteRenderer spriteRenderer;
Rigidbody2D rigidbody;
```

### Variable Declaration

```csharp
// Declare and initialize
int score = 0;

// Declare first, assign later
int score;
score = 0;

// Multiple variables of same type
int player1Score, player2Score, player3Score;
```

---

## Methods (Functions)

**Methods** are blocks of code that perform actions. They can take **parameters** (input) and return **values** (output).

### Basic Method Structure

```csharp
// Method that does something (no return value)
public void DoSomething()
{
    Debug.Log("Doing something!");
}

// Method that returns a value
public int GetScore()
{
    return 100;
}

// Method with parameters
public void SetSpeed(float newSpeed)
{
    playerSpeed = newSpeed;
}

// Method with parameters and return value
public int AddNumbers(int a, int b)
{
    return a + b;
}
```

### Method Access Modifiers

```csharp
public void PublicMethod()     // Can be called from anywhere
{
    // Accessible from other classes
}

private void PrivateMethod()   // Only accessible in this class
{
    // Internal use only
}

protected void ProtectedMethod()  // Accessible in this class and subclasses
{
    // For inheritance
}
```

### Unity Lifecycle Methods

Unity automatically calls these methods at specific times:

```csharp
public class MyScript : MonoBehaviour
{
    void Awake()      // Called when object is created (before Start)
    {
        // Initialize variables
    }
    
    void Start()      // Called before first frame update
    {
        // Setup code
    }
    
    void Update()     // Called every frame
    {
        // Game logic that runs continuously
    }
    
    void FixedUpdate()  // Called at fixed intervals (for physics)
    {
        // Physics-related code
    }
}
```

**Example from Game Assemblies** (`GoalManager.cs`):

```csharp
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

private void Start()
{
    allGoalTrackers = new List<GameObject>();
    // Initialize goals...
}

private void Update()
{
    // Process goals every frame
    foreach (ResourceGoalSO goal in activeGoals)
    {
        goal.UpdateGoaltime(Time.deltaTime);
    }
}
```

---

## Classes and Objects

### What is a Class?

A **class** is a blueprint for creating objects. It defines:
- **Fields** (variables)
- **Properties** (controlled access to data)
- **Methods** (functions)

```csharp
public class Player
{
    // Fields
    public string playerName;
    public int score;
    public float speed;
    
    // Method
    public void Move()
    {
        // Movement logic
    }
}
```

### Creating Objects (Instances)

```csharp
// Create an instance of the Player class
Player player1 = new Player();
player1.playerName = "Alice";
player1.score = 100;
player1.Move();
```

### Unity Classes: MonoBehaviour

In Unity, most scripts inherit from `MonoBehaviour`:

```csharp
using UnityEngine;

public class MyScript : MonoBehaviour
{
    // This script can be attached to a GameObject
    // Unity will call Awake(), Start(), Update(), etc.
}
```

**Key Point**: `MonoBehaviour` scripts are **components** that attach to GameObjects in the scene.

---

## Properties

**Properties** provide controlled access to fields. They can have **getters** and **setters**:

```csharp
public class ResourceManager : MonoBehaviour
{
    private int globalCapital = 0;  // Private field
    
    // Public property with getter and setter
    public int GlobalCapital
    {
        get { return globalCapital; }
        set { globalCapital = value; }
    }
    
    // Auto-property (shorthand)
    public int Score { get; set; }
    
    // Read-only property
    public int MaxScore { get; private set; }
}
```

**Usage**:
```csharp
ResourceManager.Instance.GlobalCapital = 100;  // Set
int capital = ResourceManager.Instance.GlobalCapital;  // Get
```

### Static Properties (Singletons)

```csharp
public class GoalManager : MonoBehaviour
{
    // Static property - belongs to the class, not instances
    public static GoalManager Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;  // Set the singleton instance
    }
}

// Usage: Access without needing a reference
GoalManager.Instance.DoSomething();
```

---

## Control Flow

### If Statements

```csharp
if (score > 100)
{
    Debug.Log("High score!");
}
else if (score > 50)
{
    Debug.Log("Good score!");
}
else
{
    Debug.Log("Keep trying!");
}
```

### Loops

**For Loop** — Repeat a specific number of times:

```csharp
for (int i = 0; i < 10; i++)
{
    Debug.Log($"Count: {i}");
}
```

**Foreach Loop** — Iterate through a collection:

```csharp
List<Resource> resources = new List<Resource>();
foreach (Resource resource in resources)
{
    Debug.Log(resource.resourceName);
}
```

**While Loop** — Repeat while condition is true:

```csharp
int count = 0;
while (count < 5)
{
    Debug.Log(count);
    count++;
}
```

**Example from Game Assemblies** (`GoalManager.cs`):

```csharp
// Iterate backwards to safely remove items
for (int i = activeGoals.Count - 1; i >= 0; i--)
{
    if (activeGoals[i].isCompleted)
    {
        activeGoals.RemoveAt(i);
    }
}
```

---

## Arrays and Collections

### Arrays

Fixed-size collections:

```csharp
// Declare and initialize
int[] scores = new int[5];
scores[0] = 100;
scores[1] = 200;

// Initialize with values
int[] scores = { 100, 200, 300, 400, 500 };
```

### Lists

Dynamic-size collections (most common in Unity):

```csharp
using System.Collections.Generic;

// Declare and initialize
List<int> scores = new List<int>();
scores.Add(100);
scores.Add(200);
scores.Remove(100);

// Initialize with values
List<string> names = new List<string> { "Alice", "Bob", "Charlie" };
```

**Example from Game Assemblies** (`GoalManager.cs`):

```csharp
public List<ResourceGoalSO> activeGoals = new List<ResourceGoalSO>();

// Add to list
activeGoals.Add(newGoal);

// Remove from list
activeGoals.RemoveAt(index);

// Iterate
foreach (ResourceGoalSO goal in activeGoals)
{
    goal.UpdateGoaltime(Time.deltaTime);
}
```

---

## Null and Null Checks

### What is Null?

`null` means "no value" or "nothing". Reference types (objects, GameObjects, etc.) can be `null`.

```csharp
GameObject player = null;  // No object assigned
```

### Null Checks

Always check for `null` before using objects:

```csharp
// ❌ Bad: Crashes if player is null
player.transform.position = Vector3.zero;

// ✅ Good: Check first
if (player != null)
{
    player.transform.position = Vector3.zero;
}

// ✅ Good: Null-conditional operator (C# 6.0+)
player?.transform.position = Vector3.zero;
```

**Example from Game Assemblies** (`GoalManager.cs`):

```csharp
public void UpdateScoreUI()
{
    if (scoreText != null)
    {
        scoreText.text = "Score: " + ResourceManager.Instance.globalCapital.ToString();
    }
    else
    {
        Debug.LogWarning("Score Text UI element is not assigned in GoalManager.");
    }
}
```

---

## Operators

### Arithmetic Operators

```csharp
int a = 10;
int b = 3;

int sum = a + b;        // 13 (addition)
int difference = a - b; // 7 (subtraction)
int product = a * b;    // 30 (multiplication)
int quotient = a / b;   // 3 (division, integer)
int remainder = a % b;  // 1 (modulo)
```

### Comparison Operators

```csharp
int score = 100;

bool isHigh = score > 50;      // true (greater than)
bool isLow = score < 50;        // false (less than)
bool isEqual = score == 100;    // true (equal to)
bool isNotEqual = score != 50;  // true (not equal to)
bool isAtLeast = score >= 100;  // true (greater than or equal)
bool isAtMost = score <= 100;   // true (less than or equal)
```

### Logical Operators

```csharp
bool isActive = true;
bool isComplete = false;

bool both = isActive && isComplete;  // false (AND)
bool either = isActive || isComplete; // true (OR)
bool not = !isActive;                 // false (NOT)
```

---

## String Interpolation

**String interpolation** makes it easy to insert variables into strings:

```csharp
string playerName = "Alice";
int score = 100;

// Old way (concatenation)
string message = "Player " + playerName + " has score " + score;

// New way (interpolation) - preferred
string message = $"Player {playerName} has score {score}";
```

**Example from Game Assemblies** (`GoalManager.cs`):

```csharp
Debug.Log($"Goal Completed: Collect {activeGoals[i].requiredCount} of {activeGoals[i].resourceType.resourceName}. " +
          $"Reward: {activeGoals[i].rewardPoints}. Global Score: {ResourceManager.Instance.globalCapital}");
```

---

## Comments

Comments explain code and are ignored by the compiler:

```csharp
// Single-line comment

/* 
   Multi-line comment
   Can span multiple lines
*/

/// <summary>
/// XML documentation comment
/// Describes what this method does
/// </summary>
public void MyMethod()
{
    // Implementation
}
```

---

## Common Patterns in Game Assemblies

### Pattern 1: Singleton Awake

```csharp
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
```

### Pattern 2: Null-Safe Access

```csharp
if (ResourceManager.Instance != null)
{
    ResourceManager.Instance.AddResource(resource);
}
```

### Pattern 3: List Iteration

```csharp
foreach (ResourceGoalSO goal in activeGoals)
{
    // Process each goal
}
```

### Pattern 4: Time-Based Updates

```csharp
void Update()
{
    float deltaTime = Time.deltaTime;  // Time since last frame
    // Use deltaTime for frame-rate independent movement
    transform.position += movement * deltaTime;
}
```

---

## Best Practices

### 1. **Use Meaningful Names**

```csharp
// ❌ Bad
int x = 10;
void DoStuff() { }

// ✅ Good
int playerScore = 10;
void UpdatePlayerPosition() { }
```

### 2. **Initialize Variables**

```csharp
// ✅ Good: Initialize when declaring
int score = 0;
List<GameObject> objects = new List<GameObject>();
```

### 3. **Check for Null**

```csharp
// Always check before using
if (myObject != null)
{
    myObject.DoSomething();
}
```

### 4. **Use Appropriate Types**

```csharp
// ✅ Good: Use float for positions, speeds
float playerSpeed = 2.5f;
Vector3 position = new Vector3(1.0f, 2.0f, 3.0f);

// ✅ Good: Use int for counts, scores
int playerCount = 4;
int score = 100;
```

### 5. **Comment Complex Logic**

```csharp
// Iterate backwards to safely remove items from list
for (int i = activeGoals.Count - 1; i >= 0; i--)
{
    if (activeGoals[i].isCompleted)
    {
        activeGoals.RemoveAt(i);
    }
}
```

---

## Common Mistakes

### Mistake 1: Forgetting 'f' for Floats

```csharp
// ❌ Bad: Compiler error
float speed = 2.5;

// ✅ Good: Add 'f' suffix
float speed = 2.5f;
```

### Mistake 2: Not Checking Null

```csharp
// ❌ Bad: Crashes if null
GameObject player;
player.transform.position = Vector3.zero;

// ✅ Good: Check first
if (player != null)
{
    player.transform.position = Vector3.zero;
}
```

### Mistake 3: Wrong Loop Direction

```csharp
// ❌ Bad: Can skip items when removing
for (int i = 0; i < list.Count; i++)
{
    if (shouldRemove)
        list.RemoveAt(i);  // Index shifts!
}

// ✅ Good: Iterate backwards
for (int i = list.Count - 1; i >= 0; i--)
{
    if (shouldRemove)
        list.RemoveAt(i);  // Safe!
}
```

### Mistake 4: Using == with Floats

```csharp
// ❌ Bad: Floating point precision issues
if (time == 0.0f) { }

// ✅ Good: Use approximate comparison
if (Mathf.Approximately(time, 0.0f)) { }
// Or use a threshold
if (Mathf.Abs(time) < 0.001f) { }
```

---

## Summary

**C# Syntax Essentials**:

- ✅ **Variables** store data with specific types (int, float, bool, string, etc.)
- ✅ **Methods** are functions that perform actions
- ✅ **Classes** are blueprints for objects
- ✅ **Properties** provide controlled access to data
- ✅ **Control flow** (if, loops) controls program execution
- ✅ **Collections** (Lists, Arrays) store multiple items
- ✅ **Null checks** prevent crashes
- ✅ **String interpolation** makes formatting easy

**Key Takeaways**:
- Always check for `null` before using objects
- Use `float` for decimal numbers (with `f` suffix)
- Use `List<T>` for dynamic collections
- Iterate backwards when removing items from lists
- Use meaningful variable and method names

---

## Related Documentation

- [06 – Static References](./06-Static-References.md) — Understanding static properties and singletons
- [05 – Data Structures](./05-Data-Structures.md) — Lists and Dictionaries in detail
- [02 – Prefabs](./02-Prefabs.md) — Working with Unity prefabs
