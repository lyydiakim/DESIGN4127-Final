# Common Data Structures: Lists and Dictionaries

This foundational guide explains **Lists** and **Dictionaries** - two of the most commonly used data structures in Unity and the Game Assemblies library. Understanding these collections is essential for managing groups of objects, resources, goals, and other game data.

## What are Data Structures?

**Data structures** are ways to organize and store multiple pieces of data. Instead of having separate variables for each item, you can store them in a collection.

**Common Collections in C#**:
- **Arrays** — Fixed-size collections
- **Lists** — Dynamic-size collections (most common)
- **Dictionaries** — Key-value pairs (like a phone book)

---

## Lists

A **List** is a dynamic collection that can grow or shrink. It's like an expandable array.

### Why Use Lists?

- **Dynamic size** — Add or remove items as needed
- **Easy iteration** — Loop through all items
- **Type-safe** — All items are the same type
- **Common operations** — Add, remove, find, count

### Creating Lists

```csharp
using System.Collections.Generic;

// Create empty list
List<int> scores = new List<int>();

// Create list with initial values
List<string> names = new List<string> { "Alice", "Bob", "Charlie" };

// Create list of GameObjects
List<GameObject> players = new List<GameObject>();

// Create list of custom types
List<Resource> resources = new List<Resource>();
```

### Adding Items

```csharp
List<int> numbers = new List<int>();

// Add single item
numbers.Add(10);
numbers.Add(20);
numbers.Add(30);

// Add multiple items
numbers.AddRange(new int[] { 40, 50, 60 });

// Insert at specific position
numbers.Insert(0, 5);  // Insert 5 at index 0
```

**Example from Game Assemblies** (`GoalManager.cs`):

```csharp
public List<ResourceGoalSO> activeGoals = new List<ResourceGoalSO>();

// Add goal to list
activeGoals.Add(newGoal);

// Add goal at specific position
activeGoals.Insert(0, firstGoal);
```

### Accessing Items

```csharp
List<string> names = new List<string> { "Alice", "Bob", "Charlie" };

// Access by index (0-based)
string first = names[0];      // "Alice"
string second = names[1];     // "Bob"

// Get count
int count = names.Count;      // 3

// Check if contains item
bool hasAlice = names.Contains("Alice");  // true

// Find index
int index = names.IndexOf("Bob");  // 1
```

### Removing Items

```csharp
List<string> names = new List<string> { "Alice", "Bob", "Charlie" };

// Remove by value
names.Remove("Bob");  // Removes "Bob"

// Remove at index
names.RemoveAt(0);    // Removes first item ("Alice")

// Remove all matching
names.RemoveAll(name => name.StartsWith("A"));  // Removes all starting with "A"

// Clear entire list
names.Clear();  // Removes all items
```

**Example from Game Assemblies** (`GoalManager.cs`):

```csharp
// Remove completed goal
for (int i = activeGoals.Count - 1; i >= 0; i--)
{
    if (activeGoals[i].isCompleted)
    {
        activeGoals.RemoveAt(i);  // Remove at index
        allGoalTrackers.RemoveAt(i);
    }
}
```

**Important**: When removing items, iterate **backwards** to avoid index shifting issues!

### Iterating Through Lists

**Foreach Loop** (most common):

```csharp
List<Resource> resources = new List<Resource>();

foreach (Resource resource in resources)
{
    Debug.Log(resource.resourceName);
}
```

**For Loop** (when you need index):

```csharp
for (int i = 0; i < resources.Count; i++)
{
    Debug.Log($"Resource {i}: {resources[i].resourceName}");
}
```

**For Loop (Backwards)** (when removing items):

```csharp
for (int i = resources.Count - 1; i >= 0; i--)
{
    if (shouldRemove)
    {
        resources.RemoveAt(i);  // Safe to remove
    }
}
```

**Example from Game Assemblies** (`GoalManager.cs`):

```csharp
// Update all active goals
foreach (ResourceGoalSO goal in activeGoals)
{
    goal.UpdateGoaltime(Time.deltaTime);
}

// Check for completed goals (iterate backwards)
for (int i = activeGoals.Count - 1; i >= 0; i--)
{
    if (activeGoals[i].isCompleted)
    {
        activeGoals.RemoveAt(i);
    }
}
```

### Common List Operations

```csharp
List<int> numbers = new List<int> { 5, 2, 8, 1, 9 };

// Count
int count = numbers.Count;  // 5

// Contains
bool hasFive = numbers.Contains(5);  // true

// Find
int found = numbers.Find(n => n > 5);  // 8 (first number > 5)

// Find all
List<int> large = numbers.FindAll(n => n > 5);  // [8, 9]

// Sort
numbers.Sort();  // [1, 2, 5, 8, 9]

// Reverse
numbers.Reverse();  // [9, 8, 5, 2, 1]
```

---

## Dictionaries

A **Dictionary** stores key-value pairs. Like a phone book: you look up a name (key) to get a phone number (value).

### Why Use Dictionaries?

- **Fast lookup** — Find values by key quickly
- **Unique keys** — Each key appears only once
- **Key-value pairs** — Associate data with identifiers
- **Flexible** — Keys and values can be different types

### Creating Dictionaries

```csharp
using System.Collections.Generic;

// Create empty dictionary
Dictionary<string, int> scores = new Dictionary<string, int>();

// Create with initial values
Dictionary<string, string> colors = new Dictionary<string, string>
{
    { "red", "#FF0000" },
    { "green", "#00FF00" },
    { "blue", "#0000FF" }
};

// Dictionary with different types
Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();
Dictionary<string, Resource> resources = new Dictionary<string, Resource>();
```

### Adding Items

```csharp
Dictionary<string, int> scores = new Dictionary<string, int>();

// Add key-value pair
scores["Alice"] = 100;
scores["Bob"] = 200;
scores["Charlie"] = 150;

// Or use Add method
scores.Add("David", 300);

// Check before adding (to avoid errors)
if (!scores.ContainsKey("Eve"))
{
    scores["Eve"] = 250;
}
```

### Accessing Items

```csharp
Dictionary<string, int> scores = new Dictionary<string, int>
{
    { "Alice", 100 },
    { "Bob", 200 }
};

// Access by key
int aliceScore = scores["Alice"];  // 100

// Check if key exists
if (scores.ContainsKey("Bob"))
{
    int bobScore = scores["Bob"];
}

// Safe access (returns default if key doesn't exist)
int score = scores.ContainsKey("Charlie") ? scores["Charlie"] : 0;

// Or use TryGetValue (recommended)
if (scores.TryGetValue("Charlie", out int charlieScore))
{
    Debug.Log($"Charlie's score: {charlieScore}");
}
```

### Removing Items

```csharp
Dictionary<string, int> scores = new Dictionary<string, int>();

// Remove by key
scores.Remove("Alice");

// Check before removing
if (scores.ContainsKey("Bob"))
{
    scores.Remove("Bob");
}

// Clear all
scores.Clear();
```

### Iterating Through Dictionaries

**Foreach Loop** (iterates key-value pairs):

```csharp
Dictionary<string, int> scores = new Dictionary<string, int>
{
    { "Alice", 100 },
    { "Bob", 200 }
};

// Iterate all key-value pairs
foreach (KeyValuePair<string, int> pair in scores)
{
    Debug.Log($"{pair.Key}: {pair.Value}");
}

// Or iterate keys only
foreach (string name in scores.Keys)
{
    Debug.Log($"Player: {name}");
}

// Or iterate values only
foreach (int score in scores.Values)
{
    Debug.Log($"Score: {score}");
}
```

### Common Dictionary Operations

```csharp
Dictionary<string, int> scores = new Dictionary<string, int>();

// Count
int count = scores.Count;

// Contains key
bool hasAlice = scores.ContainsKey("Alice");

// Contains value
bool hasScore100 = scores.ContainsValue(100);

// Get all keys
List<string> names = new List<string>(scores.Keys);

// Get all values
List<int> allScores = new List<int>(scores.Values);
```

---

## Lists vs Dictionaries: When to Use Which?

### Use Lists When:

- ✅ You need to **iterate through all items**
- ✅ Order matters (items have positions)
- ✅ You access items by **index** (0, 1, 2, ...)
- ✅ You frequently **add/remove** items
- ✅ Items don't need unique identifiers

**Examples**:
- List of active goals
- List of players
- List of resources to process
- Queue of actions

### Use Dictionaries When:

- ✅ You need to **look up values by key**
- ✅ Keys are **unique identifiers** (names, IDs)
- ✅ Fast lookup is important
- ✅ You associate data with identifiers

**Examples**:
- Player scores by player name
- Resource counts by resource type
- GameObject references by ID
- Settings by setting name

---

## Practical Examples from Game Assemblies

### Example 1: List of Active Goals

From `GoalManager.cs`:

```csharp
public List<ResourceGoalSO> activeGoals = new List<ResourceGoalSO>();

// Add goal
public void AddGoal(ResourceGoalSO goal)
{
    activeGoals.Add(goal);
}

// Update all goals
foreach (ResourceGoalSO goal in activeGoals)
{
    goal.UpdateGoaltime(Time.deltaTime);
}

// Remove completed goals (iterate backwards!)
for (int i = activeGoals.Count - 1; i >= 0; i--)
{
    if (activeGoals[i].isCompleted)
    {
        activeGoals.RemoveAt(i);
    }
}
```

### Example 2: List of Resources

From `ResourceManager.cs`:

```csharp
public List<ResourceObject> allResources = new List<ResourceObject>();

// Add resource
public void AddResource(ResourceObject resource)
{
    allResources.Add(resource);
}

// Count resources of specific type
public int GetResourceCount(Resource resourceType)
{
    int count = 0;
    foreach (ResourceObject resource in allResources)
    {
        if (resource.resourceType == resourceType)
        {
            count++;
        }
    }
    return count;
}
```

### Example 3: Dictionary for Resource Counts

**Hypothetical example** (more efficient than iterating list):

```csharp
Dictionary<Resource, int> resourceCounts = new Dictionary<Resource, int>();

// Increment count
public void AddResource(Resource resourceType)
{
    if (resourceCounts.ContainsKey(resourceType))
    {
        resourceCounts[resourceType]++;
    }
    else
    {
        resourceCounts[resourceType] = 1;
    }
}

// Get count (fast lookup)
public int GetResourceCount(Resource resourceType)
{
    return resourceCounts.ContainsKey(resourceType) 
        ? resourceCounts[resourceType] 
        : 0;
}
```

### Example 4: List of Sequential Goals

From `LevelDataSO.cs`:

```csharp
public List<ResourceGoalSO> sequentialGoals = new List<ResourceGoalSO>();

// Add goals in order
sequentialGoals.Add(goal1);
sequentialGoals.Add(goal2);
sequentialGoals.Add(goal3);

// Process in order
for (int i = 0; i < sequentialGoals.Count; i++)
{
    ResourceGoalSO goal = sequentialGoals[i];
    // Process goal at position i
}
```

---

## Best Practices

### 1. **Always Check Before Accessing**

```csharp
// ✅ Good: Check if key exists
if (dictionary.ContainsKey("key"))
{
    int value = dictionary["key"];
}

// ✅ Better: Use TryGetValue
if (dictionary.TryGetValue("key", out int value))
{
    // Use value
}
```

### 2. **Iterate Backwards When Removing**

```csharp
// ✅ Good: Iterate backwards
for (int i = list.Count - 1; i >= 0; i--)
{
    if (shouldRemove)
    {
        list.RemoveAt(i);
    }
}

// ❌ Bad: Iterate forwards (skips items)
for (int i = 0; i < list.Count; i++)
{
    if (shouldRemove)
    {
        list.RemoveAt(i);  // Index shifts!
    }
}
```

### 3. **Initialize Lists and Dictionaries**

```csharp
// ✅ Good: Initialize when declaring
List<int> numbers = new List<int>();
Dictionary<string, int> scores = new Dictionary<string, int>();

// ❌ Bad: Null reference
List<int> numbers;  // null!
numbers.Add(5);     // Crash!
```

### 4. **Use Appropriate Type**

```csharp
// ✅ Good: Specific type
List<Resource> resources = new List<Resource>();

// ❌ Bad: Generic object
List<object> resources = new List<object>();
```

### 5. **Check for Null/Empty**

```csharp
// Check if list is empty
if (list.Count > 0)
{
    // Process items
}

// Or use null-conditional
list?.ForEach(item => Process(item));
```

---

## Common Mistakes

### Mistake 1: Accessing Dictionary Key That Doesn't Exist

```csharp
// ❌ Bad: Crashes if key doesn't exist
int score = scores["UnknownPlayer"];  // KeyNotFoundException!

// ✅ Good: Check first
if (scores.ContainsKey("UnknownPlayer"))
{
    int score = scores["UnknownPlayer"];
}

// ✅ Better: Use TryGetValue
if (scores.TryGetValue("UnknownPlayer", out int score))
{
    // Use score
}
```

### Mistake 2: Removing from List While Iterating Forwards

```csharp
// ❌ Bad: Skips items
for (int i = 0; i < list.Count; i++)
{
    if (shouldRemove)
    {
        list.RemoveAt(i);  // Next item shifts to current index!
    }
}

// ✅ Good: Iterate backwards
for (int i = list.Count - 1; i >= 0; i--)
{
    if (shouldRemove)
    {
        list.RemoveAt(i);  // Safe!
    }
}
```

### Mistake 3: Not Initializing Collection

```csharp
// ❌ Bad: Null reference
List<int> numbers;
numbers.Add(5);  // NullReferenceException!

// ✅ Good: Initialize
List<int> numbers = new List<int>();
numbers.Add(5);
```

### Mistake 4: Using Wrong Collection Type

```csharp
// ❌ Bad: Using List when Dictionary is better
List<Player> players = new List<Player>();
// Need to search through list to find player by name

// ✅ Good: Use Dictionary for lookup
Dictionary<string, Player> players = new Dictionary<string, Player>();
Player player = players["Alice"];  // Fast lookup!
```

---

## Summary

**Lists** and **Dictionaries** are essential data structures:

- ✅ **Lists** — Dynamic collections, accessed by index
- ✅ **Dictionaries** — Key-value pairs, fast lookup by key
- ✅ **Lists** — Use when order matters, iterating all items
- ✅ **Dictionaries** — Use when looking up by unique key

**Key Takeaways**:
- Always iterate backwards when removing from lists
- Check if dictionary key exists before accessing
- Use `TryGetValue()` for safe dictionary access
- Initialize collections when declaring
- Choose the right collection for your use case

**Common Operations**:
- **Lists**: `Add()`, `Remove()`, `RemoveAt()`, `Count`, `Contains()`, `foreach`
- **Dictionaries**: `[key]`, `Add()`, `Remove()`, `ContainsKey()`, `TryGetValue()`, `foreach`

---

## Related Documentation

- [03 – Syntax](./03-Syntax.md) — Understanding C# fundamentals
- [06 – Static References](./06-Static-References.md) — How managers use lists and dictionaries
- [Tutorial 03: Resource Manager and Goals](../tutorials/03-Resource-Manager-and-Goals.md) — Using lists in ResourceManager
