# Compressed C# Syntax

This guide explains **compressed syntax** you may encounter in C# code—especially when AI or experienced developers write compact one-liners. Each section shows the **long, legible form** first, then **two intermediate steps** that lead to the compressed version. Use this when you see unfamiliar syntax in the Game Assemblies project or generated code.

---

## 1. Ternary Operator (`? :`)

The **ternary operator** is a shorthand for a simple if-else that assigns a value.

### Long Form (Most Readable)

```csharp
float lifespan;

if (resourceType != null)
{
    lifespan = resourceType.lifespan;
}
else
{
    lifespan = 2f;
}
```

### Intermediate Step 1: Combine into One Assignment

```csharp
float lifespan;

if (resourceType != null)
    lifespan = resourceType.lifespan;
else
    lifespan = 2f;
```

### Intermediate Step 2: Use Ternary Syntax

```csharp
float lifespan = resourceType != null ? resourceType.lifespan : 2f;
```

### Compressed Form (One Line)

```csharp
float lifespan = resourceType != null ? resourceType.lifespan : 2f;
```

**Structure**: `condition ? valueIfTrue : valueIfFalse`

**Example from Game Assemblies** (`ResourceObject.cs`):

```csharp
float lifespan = resourceType != null ? resourceType.lifespan : 2f;
```

---

## 2. Expression-Bodied Members (`=>`)

**Expression-bodied** syntax replaces a method or property body with a single expression.

### Long Form: Property Getter

```csharp
public bool produceResource
{
    get
    {
        if (stationData != null)
        {
            return stationData.produceResource;
        }
        else
        {
            return false;
        }
    }
}
```

### Intermediate Step 1: Simplify with Ternary

```csharp
public bool produceResource
{
    get
    {
        return stationData != null ? stationData.produceResource : false;
    }
}
```

### Compressed Form: Expression-Bodied Property

```csharp
public bool produceResource => stationData != null && stationData.produceResource;
```

**Structure**: `public Type Name => expression;` — the expression is evaluated and returned.

**Example from Game Assemblies** (`Station.cs`):

```csharp
public bool produceResource => stationData != null && stationData.produceResource;
public float workDuration => stationData != null ? stationData.workDuration : 5f;
```

---

## 3. Null-Conditional Operator (`?.`)

The **null-conditional** operator short-circuits: if the left side is `null`, the whole expression returns `null` (or `false` for booleans) without evaluating the right side.

### Long Form

```csharp
Sprite sprite;

if (stationData != null && stationData.stationGraphic != null)
{
    sprite = stationData.stationGraphic;
}
else
{
    sprite = null;
}
```

### Intermediate Step 1: Use Ternary

```csharp
Sprite sprite = (stationData != null && stationData.stationGraphic != null) 
    ? stationData.stationGraphic 
    : null;
```

### Compressed Form: Null-Conditional Chain

```csharp
Sprite sprite = stationData?.stationGraphic;
```

**Structure**: `object?.Property` — if `object` is null, the result is null; otherwise, returns `object.Property`.

**Chained example**:

```csharp
// Long form
string name = null;
if (player != null && player.inventory != null && player.inventory.selectedItem != null)
{
    name = player.inventory.selectedItem.itemName;
}

// Compressed
string name = player?.inventory?.selectedItem?.itemName;
```

---

## 4. Lambda Expressions (`=>` in Delegates)

**Lambdas** are inline functions, often used with `List.Exists`, `List.Find`, `foreach`-style methods, etc.

### Long Form: Separate Method

```csharp
private bool HasValidResource(Resource r)
{
    return r != null;
}

// Usage
bool hasConsumeResources = !consumeResource || consumeResources.Exists(HasValidResource);
```

### Intermediate Step 1: Anonymous Method

```csharp
bool hasConsumeResources = !consumeResource || consumeResources.Exists(delegate (Resource r) 
{ 
    return r != null; 
});
```

### Compressed Form: Lambda

```csharp
bool hasConsumeResources = !consumeResource || consumeResources.Exists(r => r != null);
```

**Structure**: `(parameter) => expression` — a function that takes `parameter` and returns `expression`.

**Example from Game Assemblies** (`SA_StationBuilderWindow.cs`):

```csharp
bool hasConsumeResources = !consumeResource || consumeResources.Exists(r => r != null);
bool hasProduceResources = !produceResource || produceResources.Exists(r => r != null);
```

---

## 5. One-Line If (No Braces)

When an `if` has only one statement, braces are optional. This can make code look compressed.

### Long Form

```csharp
if (resourceType != null)
{
    growOldandDie();
}
```

### Intermediate Step 1: Remove Braces

```csharp
if (resourceType != null)
    growOldandDie();
```

### Compressed Form (One Line)

```csharp
if (resourceType != null) growOldandDie();
```

**Caution**: Adding a second statement without braces only applies to the first one:

```csharp
// ❌ Bug: Only DoFirst() is inside the if!
if (condition) 
    DoFirst();
    DoSecond();  // Always runs!

// ✅ Correct
if (condition)
{
    DoFirst();
    DoSecond();
}
```

---

## 6. Null-Coalescing Operator (`??`)

The **null-coalescing** operator returns the left side unless it is `null`, in which case it returns the right side.

### Long Form

```csharp
string displayName;

if (resourceName != null)
{
    displayName = resourceName;
}
else
{
    displayName = "Unknown";
}
```

### Intermediate Step 1: Ternary

```csharp
string displayName = resourceName != null ? resourceName : "Unknown";
```

### Compressed Form: Null-Coalescing

```csharp
string displayName = resourceName ?? "Unknown";
```

**Structure**: `value ?? fallback` — if `value` is null, use `fallback`.

**Combined with null-conditional**:

```csharp
// Long form
int count = 0;
if (player != null && player.inventory != null)
{
    count = player.inventory.itemCount;
}

// Compressed
int count = player?.inventory?.itemCount ?? 0;
```

---

## 7. Compound Assignment (`+=`, `-=`, etc.)

**Compound assignment** combines an operation with assignment.

### Long Form

```csharp
currentLife = currentLife + Time.deltaTime;
score = score + 10;
count = count - 1;
```

### Compressed Form

```csharp
currentLife += Time.deltaTime;
score += 10;
count -= 1;
```

**Common operators**: `+=`, `-=`, `*=`, `/=`, `%=`

---

## 8. Logical Short-Circuit in Conditions

`&&` and `||` **short-circuit**: evaluation stops as soon as the result is known.

### Long Form

```csharp
bool shouldDecay = false;

if (resourceType != null)
{
    if (resourceType.typeOfBehavior == Resource.ResourceBehavior.Decays)
    {
        shouldDecay = true;
    }
}
```

### Compressed Form

```csharp
bool shouldDecay = resourceType != null && resourceType.typeOfBehavior == Resource.ResourceBehavior.Decays;
```

**Why it's safe**: If `resourceType` is null, the second part is never evaluated, so you avoid a null reference.

---

## Quick Reference Table

| Syntax | Long Form | Compressed |
|--------|-----------|------------|
| Ternary | `if (a) x = b; else x = c;` | `x = a ? b : c;` |
| Expression-bodied | `get { return expr; }` | `=> expr` |
| Null-conditional | `if (a != null) a.Property` | `a?.Property` |
| Null-coalescing | `a != null ? a : b` | `a ?? b` |
| Lambda | `bool F(Resource r) { return r != null; }` | `r => r != null` |
| Compound assign | `x = x + 1` | `x += 1` |

---

## When to Use Compressed vs. Long Form

**Prefer compressed** when:
- The logic is simple and fits on one line
- The pattern is common (e.g. null checks, simple ternaries)
- Readability is not reduced

**Prefer long form** when:
- The logic has multiple steps
- You want to add logging or debug code
- Team members are less familiar with C# shortcuts

---

## Related Documentation

- [03 – Syntax](./03-Syntax.md) — Core C# concepts
- [05 – Data Structures](./05-Data-Structures.md) — Lists and lambdas in context
