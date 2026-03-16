# Basic Vector Math

This foundational guide explains **Vector Math** in Unity - essential for positioning objects, calculating movement, distances, and directions. Understanding vectors is crucial for game development, especially for 2D games like those built with Game Assemblies.

## What is a Vector?

A **Vector** represents a **direction and magnitude** (length). In game development, vectors are used to represent:

- **Positions** — Where something is (x, y) or (x, y, z)
- **Directions** — Which way something is facing or moving
- **Distances** — How far apart things are
- **Velocities** — Speed and direction of movement

### Vector2 vs Vector3

| Type | Dimensions | Use Case |
|------|------------|----------|
| **Vector2** | x, y | 2D games, UI positions, screen coordinates |
| **Vector3** | x, y, z | 3D games, but also used in 2D (z often = 0) |

**In Unity 2D**: You'll use both, but `Vector2` is more common for 2D-specific operations.

---

## Creating Vectors

### Vector2

```csharp
// Create Vector2 with x and y
Vector2 position = new Vector2(5.0f, 3.0f);

// Common shortcuts
Vector2 zero = Vector2.zero;        // (0, 0)
Vector2 one = Vector2.one;          // (1, 1)
Vector2 up = Vector2.up;            // (0, 1)
Vector2 down = Vector2.down;        // (0, -1)
Vector2 left = Vector2.left;        // (-1, 0)
Vector2 right = Vector2.right;      // (1, 0)

// Access components
float x = position.x;  // 5.0
float y = position.y;  // 3.0
```

### Vector3

```csharp
// Create Vector3 with x, y, z
Vector3 position = new Vector3(5.0f, 3.0f, 0.0f);

// Common shortcuts
Vector3 zero = Vector3.zero;        // (0, 0, 0)
Vector3 one = Vector3.one;           // (1, 1, 1)
Vector3 up = Vector3.up;             // (0, 1, 0)
Vector3 down = Vector3.down;         // (0, -1, 0)
Vector3 forward = Vector3.forward;   // (0, 0, 1)
Vector3 back = Vector3.back;        // (0, 0, -1)
Vector3 left = Vector3.left;        // (-1, 0, 0)
Vector3 right = Vector3.right;      // (1, 0, 0)

// Access components
float x = position.x;  // 5.0
float y = position.y;  // 3.0
float z = position.z;  // 0.0
```

**Note**: In 2D games, `Vector3` is often used with `z = 0` because Unity's Transform uses `Vector3` for position.

---

## Basic Vector Operations

### Addition and Subtraction

```csharp
Vector2 a = new Vector2(5, 3);
Vector2 b = new Vector2(2, 1);

// Addition (move position)
Vector2 sum = a + b;        // (7, 4)

// Subtraction (get direction/distance)
Vector2 difference = a - b;  // (3, 2)

// Example: Move object
transform.position += new Vector3(1, 0, 0);  // Move right
```

**Example from Game Assemblies** (`move.cs`):

```csharp
Vector2 moveDirection = Vector2.zero;
rb.linearVelocity = new Vector2(
    moveDirection.x * moveSpeed, 
    moveDirection.y * moveSpeed
);
```

### Multiplication and Division

```csharp
Vector2 v = new Vector2(3, 4);

// Multiply by scalar (scale vector)
Vector2 scaled = v * 2.0f;   // (6, 8)
Vector2 scaled2 = 2.0f * v;  // Same result

// Divide by scalar
Vector2 half = v / 2.0f;     // (1.5, 2)

// Example: Scale movement by speed
Vector2 movement = direction * speed;
```

**Example from Game Assemblies** (`Wanderer.cs`):

```csharp
// Move object with speed
transform.position += (Vector3)(currentDirection * moveSpeed * Time.deltaTime);
```

### Dot Product

The **dot product** tells you:
- How similar two vectors are (direction)
- If vectors are perpendicular (result = 0)
- If vectors point same direction (positive) or opposite (negative)

```csharp
Vector2 a = new Vector2(1, 0);  // Right
Vector2 b = new Vector2(0, 1);  // Up

float dot = Vector2.Dot(a, b);  // 0 (perpendicular)

Vector2 c = new Vector2(1, 0);
float dot2 = Vector2.Dot(a, c);  // 1 (same direction)
```

**Use Cases**:
- Check if object is in front of another
- Calculate angle between vectors
- Project one vector onto another

### Cross Product (Vector3 only)

The **cross product** gives a vector perpendicular to both input vectors:

```csharp
Vector3 a = new Vector3(1, 0, 0);  // Right
Vector3 b = new Vector3(0, 1, 0);  // Up

Vector3 cross = Vector3.Cross(a, b);  // (0, 0, 1) - Forward
```

**Use Cases**:
- Calculate normal vectors
- Determine rotation axis
- Calculate torque

---

## Vector Properties

### Magnitude (Length)

The **magnitude** is the length of the vector:

```csharp
Vector2 v = new Vector2(3, 4);

// Get magnitude
float length = v.magnitude;           // 5.0 (3-4-5 triangle)
float length2 = Vector2.Distance(Vector2.zero, v);  // Same

// Get squared magnitude (faster, no square root)
float sqrLength = v.sqrMagnitude;     // 25.0
```

**Why use `sqrMagnitude`?**
- Faster (no square root calculation)
- Use for comparisons (if `a.sqrMagnitude > b.sqrMagnitude`, then `a.magnitude > b.magnitude`)

### Normalized Vector (Direction)

A **normalized** vector has magnitude of 1 (unit vector). It represents direction only:

```csharp
Vector2 v = new Vector2(3, 4);

// Get normalized vector (direction)
Vector2 direction = v.normalized;     // (0.6, 0.8) - same direction, length = 1

// Normalize in place
v.Normalize();  // Modifies v directly
```

**Use Cases**:
- Get direction without magnitude
- Consistent movement speed
- Calculate directions between points

**Example**:

```csharp
// Get direction from A to B
Vector2 direction = (pointB - pointA).normalized;

// Move towards target
transform.position += (Vector3)(direction * speed * Time.deltaTime);
```

---

## Common Vector Operations

### Distance Between Points

```csharp
Vector2 pointA = new Vector2(0, 0);
Vector2 pointB = new Vector2(3, 4);

// Calculate distance
float distance = Vector2.Distance(pointA, pointB);  // 5.0

// Or manually
float distance2 = (pointB - pointA).magnitude;  // Same result

// Squared distance (faster for comparisons)
float sqrDistance = (pointB - pointA).sqrMagnitude;  // 25.0
```

**Example from Game Assemblies** (`playerController.cs`):

```csharp
// Calculate distance for grab range
float distance = Vector2.Distance(transform.position, objectPosition);
if (distance < grabRange)
{
    // Can grab object
}
```

### Direction Between Points

```csharp
Vector2 start = new Vector2(0, 0);
Vector2 end = new Vector2(3, 4);

// Get direction (normalized)
Vector2 direction = (end - start).normalized;  // (0.6, 0.8)

// Get direction with magnitude
Vector2 toTarget = end - start;  // (3, 4) - includes distance
```

### Lerp (Linear Interpolation)

**Lerp** smoothly moves from one value to another:

```csharp
Vector2 start = new Vector2(0, 0);
Vector2 end = new Vector2(10, 10);
float t = 0.5f;  // 0 to 1 (0 = start, 1 = end)

// Interpolate
Vector2 middle = Vector2.Lerp(start, end, t);  // (5, 5)

// Example: Smooth movement
transform.position = Vector3.Lerp(
    transform.position, 
    targetPosition, 
    Time.deltaTime * speed
);
```

**Use Cases**:
- Smooth camera following
- Gradual color changes
- Smooth object movement

### Move Towards

**MoveTowards** moves towards a target without overshooting:

```csharp
Vector2 current = new Vector2(0, 0);
Vector2 target = new Vector2(10, 10);
float maxDistance = 2.0f;

// Move towards target
Vector2 newPos = Vector2.MoveTowards(current, target, maxDistance);
// Moves 2 units towards target, stops at target
```

**Example**:

```csharp
// Move towards target each frame
transform.position = Vector3.MoveTowards(
    transform.position,
    targetPosition,
    speed * Time.deltaTime
);
```

---

## Vector Math in Game Development

### Movement

```csharp
// Move in direction
Vector2 direction = new Vector2(1, 0);  // Right
float speed = 5.0f;

// Move each frame
transform.position += (Vector3)(direction * speed * Time.deltaTime);

// Or with input
Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
transform.position += (Vector3)(input.normalized * speed * Time.deltaTime);
```

**Example from Game Assemblies** (`move.cs`):

```csharp
Vector2 moveDirection = Vector2.zero;
// ... get input ...

// Apply movement
rb.linearVelocity = new Vector2(
    moveDirection.x * moveSpeed, 
    moveDirection.y * moveSpeed
);
```

### Following a Target

```csharp
public Transform target;
public float followSpeed = 2.0f;

void Update()
{
    if (target != null)
    {
        // Get direction to target
        Vector2 direction = (target.position - transform.position).normalized;
        
        // Move towards target
        transform.position += (Vector3)(direction * followSpeed * Time.deltaTime);
        
        // Or use MoveTowards
        transform.position = Vector3.MoveTowards(
            transform.position,
            target.position,
            followSpeed * Time.deltaTime
        );
    }
}
```

### Rotation Towards Target

```csharp
Vector2 direction = (target.position - transform.position).normalized;
float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
transform.rotation = Quaternion.Euler(0, 0, angle);
```

### Boundary Checking

**Example from Game Assemblies** (`Wanderer.cs`):

```csharp
void EnforceBoundaries()
{
    Vector3 position = transform.position;
    bool needsNewDirection = false;

    // Check X boundaries
    if (position.x < minX)
    {
        position.x = minX;
        needsNewDirection = true;
    }
    else if (position.x > maxX)
    {
        position.x = maxX;
        needsNewDirection = true;
    }

    // Check Y boundaries
    if (position.y < minY)
    {
        position.y = minY;
        needsNewDirection = true;
    }
    else if (position.y > maxY)
    {
        position.y = maxY;
        needsNewDirection = true;
    }

    transform.position = position;
}
```

### Spawning in Area

```csharp
// Spawn at random position in area
Vector2 minBounds = new Vector2(-10, -10);
Vector2 maxBounds = new Vector2(10, 10);

Vector2 randomPos = new Vector2(
    Random.Range(minBounds.x, maxBounds.x),
    Random.Range(minBounds.y, maxBounds.y)
);

Instantiate(prefab, randomPos, Quaternion.identity);
```

---

## Common Patterns

### Pattern 1: Normalized Movement

```csharp
// Get input direction
Vector2 input = new Vector2(
    Input.GetAxis("Horizontal"),
    Input.GetAxis("Vertical")
);

// Normalize to ensure consistent speed
Vector2 direction = input.normalized;

// Apply movement
transform.position += (Vector3)(direction * speed * Time.deltaTime);
```

### Pattern 2: Distance Check

```csharp
// Check if within range
float distance = Vector2.Distance(transform.position, target.position);
if (distance < interactionRange)
{
    // Can interact
}
```

### Pattern 3: Direction to Target

```csharp
// Get direction to target
Vector2 toTarget = (target.position - transform.position).normalized;

// Move towards target
transform.position += (Vector3)(toTarget * speed * Time.deltaTime);
```

### Pattern 4: Clamp Position

```csharp
// Keep position within bounds
Vector2 position = transform.position;
position.x = Mathf.Clamp(position.x, minX, maxX);
position.y = Mathf.Clamp(position.y, minY, maxY);
transform.position = position;
```

---

## Best Practices

### 1. **Use Time.deltaTime for Movement**

```csharp
// ✅ Good: Frame-rate independent
transform.position += direction * speed * Time.deltaTime;

// ❌ Bad: Frame-rate dependent
transform.position += direction * speed;
```

### 2. **Normalize Direction Vectors**

```csharp
// ✅ Good: Consistent speed
Vector2 direction = input.normalized;
transform.position += (Vector3)(direction * speed * Time.deltaTime);

// ❌ Bad: Speed varies with input magnitude
transform.position += (Vector3)(input * speed * Time.deltaTime);
```

### 3. **Use sqrMagnitude for Distance Comparisons**

```csharp
// ✅ Good: Faster
if ((target.position - transform.position).sqrMagnitude < range * range)
{
    // Within range
}

// ❌ Slower: Unnecessary square root
if (Vector2.Distance(target.position, transform.position) < range)
{
    // Within range
}
```

### 4. **Cache Vector Calculations**

```csharp
// ✅ Good: Calculate once
Vector2 direction = (target.position - transform.position).normalized;
// Use direction multiple times

// ❌ Bad: Recalculate every time
transform.position += (Vector3)((target.position - transform.position).normalized * speed);
```

---

## Common Mistakes

### Mistake 1: Forgetting Time.deltaTime

```csharp
// ❌ Bad: Speed depends on framerate
transform.position += direction * speed;

// ✅ Good: Frame-rate independent
transform.position += direction * speed * Time.deltaTime;
```

### Mistake 2: Not Normalizing Direction

```csharp
// ❌ Bad: Diagonal movement is faster
Vector2 input = new Vector2(1, 1);  // Magnitude = 1.414
transform.position += (Vector3)(input * speed * Time.deltaTime);

// ✅ Good: Consistent speed
Vector2 direction = input.normalized;  // Magnitude = 1.0
transform.position += (Vector3)(direction * speed * Time.deltaTime);
```

### Mistake 3: Using Vector2 in Vector3 Context

```csharp
// ❌ Bad: Type mismatch
Vector2 pos2D = new Vector2(5, 3);
transform.position = pos2D;  // Error!

// ✅ Good: Convert to Vector3
transform.position = new Vector3(pos2D.x, pos2D.y, 0);
// Or cast
transform.position = (Vector3)pos2D;
```

### Mistake 4: Not Checking for Zero Vector

```csharp
// ❌ Bad: Can cause division by zero
Vector2 direction = (target - start).normalized;  // If target == start, direction is (NaN, NaN)

// ✅ Good: Check first
Vector2 toTarget = target - start;
if (toTarget.sqrMagnitude > 0.001f)
{
    Vector2 direction = toTarget.normalized;
}
```

---

## Summary

**Vector Math** is essential for game development:

- ✅ **Vector2** — 2D positions and directions (x, y)
- ✅ **Vector3** — 3D positions and directions (x, y, z)
- ✅ **Operations** — Addition, subtraction, multiplication, dot product
- ✅ **Properties** — Magnitude, normalized, distance
- ✅ **Movement** — Use `Time.deltaTime` and normalize directions

**Key Takeaways**:
- Always use `Time.deltaTime` for frame-rate independent movement
- Normalize direction vectors for consistent speed
- Use `sqrMagnitude` for distance comparisons (faster)
- Check for zero vectors before normalizing
- Convert between Vector2 and Vector3 as needed

**Common Operations**:
- `Vector2.Distance()` — Distance between points
- `.normalized` — Get direction (magnitude = 1)
- `.magnitude` / `.sqrMagnitude` — Get length
- `Vector2.Lerp()` — Smooth interpolation
- `Vector2.MoveTowards()` — Move towards target

---

## Related Documentation

- [03 – Syntax](./03-Syntax.md) — Understanding C# fundamentals
- [02 – Prefabs](./02-Prefabs.md) — Positioning prefabs in scenes
- [Tutorial 01: Creating a Character and Canvas](../tutorials/01-Creating-Character-and-Canvas.md) — Using vectors for player movement
