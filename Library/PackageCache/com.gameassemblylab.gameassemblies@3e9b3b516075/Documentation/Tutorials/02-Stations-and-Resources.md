# Tutorial 02: Stations and Resources

This tutorial explains the core concepts of **Stations** and **Resources** in Game Assemblies. You'll learn how stations create, consume, and transform resources, and how to set up your own resource types and production chains.

## Prerequisites

- Completed Tutorial 01 (Character and Canvas setup)
- Basic understanding of Unity's Inspector and Prefabs
- A scene with at least a player character

## Overview

In this tutorial, you'll learn:
1. **What is a Resource?** - Understanding the resource system
2. **What is a Station?** - Understanding stations and their capabilities
3. **Creating a New Resource** - Using editor tools to create resource types
4. **Creating a Station** - Setting up stations that produce resources
5. **Station Functions** - Key methods in the Station code
6. **Resource Transformation** - Creating conversion chains

---

## Part 1: Understanding Resources

### What is a Resource?

A **Resource** is a ScriptableObject that represents a type of item in your game. Resources are the building blocks of your game's economy - they can be gathered, produced, consumed, and transformed.

### Resource Components

Resources consist of two main parts:

1. **Resource ScriptableObject** (`Resource.cs`)
   - **Location**: `Assets/Simulated Assemblies/Scripts/Resource.cs`
   - **Purpose**: Defines the resource type (data only)
   - **Key Properties**:
     - `resourceName`: Display name (e.g., "Wood", "Iron", "Bread")
     - `icon`: Sprite for UI display
     - `resourcePrefab`: The physical GameObject prefab that represents this resource in the game world

2. **ResourceObject Component** (`ResourceObject.cs`)
   - **Location**: `Assets/Simulated Assemblies/Scripts/ResourceObject.cs`
   - **Purpose**: Attached to prefabs to make them interactable resources
   - **Key Properties**:
     - `resourceType`: Reference to the Resource ScriptableObject
     - `amount`: Quantity this object represents (usually 1)
     - `owner`: Which player owns this resource (optional)
     - `typeOfBehavior`: Static, Decays, or Consumable
     - `lifespan`: How long before decay (if applicable)

### Resource Lifecycle

1. **Creation**: Resource ScriptableObject defines the type
2. **Instantiation**: Resource prefab is spawned in the game world
3. **Interaction**: Players can grab, carry, and deliver resources
4. **Consumption**: Resources are consumed by stations or goals
5. **Tracking**: ResourceManager tracks all resources globally

---

## Part 2: Understanding Stations

### What is a Station?

A **Station** is an interactive GameObject that can:
- **Create Resources**: Produce new resources automatically or when worked
- **Consume Resources**: Take in resources as input
- **Transform Resources**: Convert input resources into different output resources

Stations are the heart of resource conversion chains in Game Assemblies games.

### Station Capabilities

Stations can operate in three main modes:

#### 1. **Production Mode** (`produceResource = true`)
   - Creates new resources
   - Can be automatic, player-worked, or triggered by consumption
   - Examples: Tree (produces Wood), Mine (produces Ore), Oven (produces Bread)

#### 2. **Consumption Mode** (`consumeResource = true`)
   - Takes in resources as input
   - Can be automatic, player-worked, or cycle-based
   - Examples: Furnace (consumes Ore), Delivery Box (consumes finished products)

#### 3. **Transformation Mode** (Both production and consumption)
   - Takes input resources and converts them to output resources
   - Examples: Crafting Table (Wood → Planks), Oven (Flour → Bread)

### Station Interaction Types

Stations can operate in different ways:

- **`automatic`**: Works continuously without player input
- **`whenWorked`**: Requires player to work at the station (hold X button)
- **`whenResourcesConsumed`**: Triggers when resources are consumed
- **`cycle`**: Operates on a timer cycle

---

## Part 3: Creating a New Resource

Let's create a new resource type called "Wood" that we'll use in our examples.

### Step-by-Step: Create Resource

1. **Open the Resource Creator**:
   ```
   Game Assemblies → Resources → Create Resource
   ```

2. **Configure the Resource**:
   - **Resource Name**: Enter "Wood" (or any name you prefer)
   - **Resource Sprite**: Drag and drop a sprite for your resource
     - This sprite will be used for the resource icon and prefab visual

3. **Click "Create"**:
   - The tool automatically:
     - Creates a Resource ScriptableObject at: `Assets/Simulated Assemblies/Databases/Resources/Wood.asset`
     - Creates a Resource prefab at: `Assets/Simulated Assemblies/Prefabs/Resources/Wood.prefab`
     - Links them together automatically

### What Gets Created

**Resource ScriptableObject** (`Wood.asset`):
- Contains the resource definition
- Stores name, icon, and prefab reference
- Can be referenced by stations, goals, and other systems

**Resource Prefab** (`Wood.prefab`):
- Has `ResourceObject` component attached
- Has `SpriteRenderer` with your sprite
- Has `Collider2D` for interaction
- Has `Rigidbody2D` for physics
- Automatically registered with ResourceManager when spawned

### Verify Your Resource

1. Navigate to `Assets/Simulated Assemblies/Databases/Resources/`
2. You should see `Wood.asset`
3. Select it and check the Inspector:
   - Resource Name should be "Wood"
   - Icon should show your sprite
   - Resource Prefab should reference the prefab

---

## Part 4: Creating a Station That Produces Resources

Now let's create a station that produces our "Wood" resource. We'll use the editor tools to create a basic automatic production station.

### Option A: Using Editor Tools (Quick Setup)

The Game Assemblies menu provides several station templates:

1. **Automatic Station**:
   ```
   Game Assemblies → Stations → Create Automatic Station
   ```
   - Creates a station that automatically produces resources
   - No player interaction required
   - Good for resource generation points

2. **Resources When Worked Station**:
   ```
   Game Assemblies → Stations → Create Resources When Worked Station
   ```
   - Requires player to work (hold X button)
   - Produces resources when work is completed
   - Good for active gameplay

3. **Convert Resources On Work Station**:
   ```
   Game Assemblies → Stations → Create Convert Resources on Work Station
   ```
   - Consumes input resources and produces output resources
   - Requires player work
   - Good for crafting/conversion

### Option B: Manual Station Setup (Full Control)

For this tutorial, let's manually configure a station to understand all the components:

1. **Create a Station GameObject**:
   - Right-click in Hierarchy → Create Empty
   - Name it "Tree" (or your station name)
   - Add components:
     - `SpriteRenderer` (for visual)
     - `BoxCollider2D` (for interaction)
     - `Station` script

2. **Configure the Station Component**:

   **Basic Settings**:
   - `produceResource`: ✅ Check this
   - `consumeResource`: ❌ Uncheck (we're only producing)
   - `WhatToProduce`: Select "Resource"

   **Production Settings**:
   - `typeOfProduction`: Select "automatic" (or "whenWorked" for player interaction)
   - `produces`: Click the "+" button and add your "Wood" resource
   - `productionInterval`: Set to 5.0 (seconds between production)
   - `spawnResourcePrefab`: ✅ Check this (spawns physical resource objects)

   **Output Area** (Optional but Recommended):
   - Create a child GameObject named "OutputArea"
   - Add `Area` component
   - Set `TypeOfArea` to "Output"
   - Add `BoxCollider2D` (set as Trigger)
   - Assign this to Station's `outputArea` field

3. **Test Your Station**:
   - Enter Play Mode
   - The station should automatically produce Wood resources every 5 seconds
   - Resources will spawn at the output area (or around the station)

---

## Part 5: Key Station Functions Explained

Understanding the main functions in the `Station.cs` script will help you customize stations for your game.

### Production Functions

#### `ProduceResource()`
**Purpose**: Core function that creates resources
**When Called**: Automatically by production methods
**What It Does**:
- Loops through the `produces` list
- Adds resources to the station's internal storage
- Spawns resource prefabs in the game world
- Plays production sound
- Contributes to goals if configured

**Key Code**:
```csharp
void ProduceResource()
{
    for (int i = 0; i < produces.Count; i++)
    {
        AddResource(produces[i], 1);  // Add to internal storage
        InstantiateResourcePrefabs(produces[i]);  // Spawn prefab
        playProductionSound();  // Audio feedback
    }
}
```

#### `AutomaticProduction()`
**Purpose**: Produces resources automatically on a timer
**When Called**: Every frame when `typeOfProduction == automatic`
**What It Does**:
- Tracks time with `productionTimer`
- When `productionInterval` is reached, calls `ProduceResource()`
- Awards capital to the station owner

**Key Code**:
```csharp
void AutomaticProduction()
{
    productionTimer += Time.deltaTime;
    if (productionTimer >= productionInterval)
    {
        ProduceResource();
        ProduceCapital(owner);  // Give capital to owner
        productionTimer = 0f;
    }
}
```

#### `ProduceOnWork()`
**Purpose**: Produces resources when player completes work
**When Called**: Every frame when `typeOfProduction == whenWorked` and work is completed
**What It Does**:
- Checks if `workCompleted` flag is true
- If true, produces resources and awards capital to worker
- Resets after production

**Key Code**:
```csharp
void ProduceOnWork()
{
    if (workCompleted)
    {
        ProduceResource();
        ProduceCapital(worker);  // Give capital to worker
    }
}
```

### Consumption Functions

#### `ConsumeResource()`
**Purpose**: Removes resources from the input area
**When Called**: By consumption methods when requirements are met
**What It Does**:
- Checks if input area has all required resources
- Removes matching resources from the area
- Sets `resourcesConsumed` flag
- Contributes to goals if configured
- Can trigger station upgrades

**Key Code**:
```csharp
void ConsumeResource()
{
    if (inputArea.allRequirementsMet)
    {
        inputArea.RemoveMatchingResources();  // Remove from area
        resourcesConsumed = true;
        if (completesGoals_consumption) 
            gManager.goalContribution(consumes[0]);
    }
}
```

#### `ConsumeOnWork()`
**Purpose**: Consumes resources when player completes work
**When Called**: Every frame when `typeOfConsumption == whenWorked` and work is completed
**What It Does**:
- Waits for work to complete
- Consumes required resources
- Charges capital from worker

#### `AutomaticConsumption()`
**Purpose**: Consumes resources automatically
**When Called**: Every frame when `typeOfConsumption == automatic`
**What It Does**:
- Continuously checks input area
- Consumes resources when available
- Charges capital from owner

### Labor/Work Functions

#### `executeLabor(playerController newWorker)`
**Purpose**: Handles player working at the station
**When Called**: By playerController when player holds X button near station
**What It Does**:
- Adds worker to `workerCount` list
- Sets `isBeingWorkedOn` to true
- Increments `workProgress` based on number of workers
- Completes work when `workDuration` is reached

**Key Code**:
```csharp
public void executeLabor(playerController newWorker)
{
    if(!workerCount.Contains(newWorker)) 
        workerCount.Add(newWorker);
    
    isBeingWorkedOn = true;
    workProgress += Time.deltaTime * workerCount.Count;  // Multiple workers = faster
    
    if (workProgress >= workDuration)
    {
        CompleteWork();
    }
}
```

#### `cancelLabor(playerController newWorker)`
**Purpose**: Stops work when player releases X button
**When Called**: By playerController when player stops working
**What It Does**:
- Removes worker from list
- Resets progress if no workers remain
- Prevents progress loss if other workers continue

#### `CompleteWork()`
**Purpose**: Called when work cycle finishes
**When Called**: By `executeLabor()` when progress reaches duration
**What It Does**:
- Sets `workCompleted` flag to true
- Resets `workProgress` to 0
- Triggers production/consumption on next frame

### Resource Spawning Functions

#### `InstantiateResourcePrefabs(Resource rs)`
**Purpose**: Spawns physical resource objects in the game world
**When Called**: By `ProduceResource()` when `spawnResourcePrefab` is true
**What It Does**:
- Gets spawn position from output area (or random around station)
- Instantiates the resource prefab
- Spawns particle effects if configured
- Sets resource owner if applicable

**Key Code**:
```csharp
void InstantiateResourcePrefabs(Resource rs)
{
    if (useOutputArea)
    {
        Vector3 spawnPosition = outputArea.GetPositionWithRandomness(0.1f);
        GameObject resourceInstance = Instantiate(rs.resourcePrefab, spawnPosition, Quaternion.identity);
        // Spawn particles, set owner, etc.
    }
}
```

### Capital Functions

#### `ProduceCapital(playerController pC)`
**Purpose**: Awards capital (money/score) to player
**When Called**: After successful production
**What It Does**:
- Adds `capitalOutputAmount` to player's capital
- Also adds to global capital in ResourceManager

#### `ConsumeCapital(playerController pC)`
**Purpose**: Charges capital from player
**When Called**: When station requires capital to operate
**What It Does**:
- Subtracts `capitalInputAmount` from player
- Also subtracts from global capital

### UI and Feedback Functions

#### `updateSlider()`
**Purpose**: Updates progress bar during work
**When Called**: Every frame when station is being worked
**What It Does**:
- Calculates progress percentage
- Updates slider UI element
- Positions slider above station

#### `updateInfoPanel()`
**Purpose**: Shows/hides station information window
**When Called**: Every frame when station is inspected
**What It Does**:
- Displays resource requirements and outputs
- Positions window above station
- Animates appearance

---

## Part 6: Creating Resource Transformation Chains

Now that you understand stations, let's create a simple transformation chain:

**Example: Wood → Planks → Furniture**

### Step 1: Create Base Resource (Wood)
- Use Resource Creator to create "Wood"
- Create automatic station that produces Wood

### Step 2: Create Intermediate Resource (Planks)
- Create "Planks" resource
- Create station that:
  - **Consumes**: Wood (1x)
  - **Produces**: Planks (1x)
  - **Type**: `whenWorked` (player must work)
  - **Work Duration**: 3 seconds

### Step 3: Create Final Resource (Furniture)
- Create "Furniture" resource
- Create station that:
  - **Consumes**: Planks (2x)
  - **Produces**: Furniture (1x)
  - **Type**: `whenWorked`
  - **Work Duration**: 5 seconds

### Setting Up Input Areas

For stations that consume resources:

1. **Create Input Area**:
   - Create child GameObject "InputArea"
   - Add `Area` component
   - Set `TypeOfArea` to "Input"
   - Add `BoxCollider2D` (set as Trigger)
   - Set size to fit your station

2. **Configure Requirements**:
   - In the `Area` component, add resources to `requirements` list
   - Set quantities needed (e.g., 1x Wood, 2x Planks)

3. **Link to Station**:
   - Assign InputArea to Station's `inputArea` field
   - Set `useInputArea` to true
   - Set `consumeResource` to true

### Testing Your Chain

1. **Gather Base Resource**: Player collects Wood from tree
2. **First Transformation**: Player delivers Wood to Plank station, works, gets Planks
3. **Second Transformation**: Player delivers Planks to Furniture station, works, gets Furniture
4. **Complete Goal**: Deliver Furniture to goal/delivery point

---

## Part 7: Station Configuration Examples

### Example 1: Automatic Resource Generator
**Use Case**: Tree that produces Wood automatically

**Configuration**:
- `produceResource`: ✅
- `consumeResource`: ❌
- `typeOfProduction`: `automatic`
- `productionInterval`: 5.0
- `produces`: [Wood]
- `spawnResourcePrefab`: ✅

### Example 2: Player-Worked Production
**Use Case**: Mine that produces Ore when worked

**Configuration**:
- `produceResource`: ✅
- `consumeResource`: ❌
- `canBeWorked`: ✅
- `typeOfProduction`: `whenWorked`
- `workDuration`: 3.0
- `produces`: [Ore]
- `spawnResourcePrefab`: ✅

### Example 3: Resource Converter
**Use Case**: Oven that converts Flour to Bread

**Configuration**:
- `produceResource`: ✅
- `consumeResource`: ✅
- `canBeWorked`: ✅
- `typeOfProduction`: `whenResourcesConsumed`
- `typeOfConsumption`: `automatic`
- `consumes`: [Flour]
- `produces`: [Bread]
- `inputArea`: Configured with Flour requirement
- `outputArea`: Configured for Bread output

### Example 4: Capital Producer
**Use Case**: Delivery box that converts items to money

**Configuration**:
- `produceResource`: ❌
- `consumeResource`: ✅
- `typeOfConsumption`: `automatic`
- `capitalOutput`: ✅
- `capitalOutputAmount`: 10
- `consumes`: [Any finished product]
- `completesGoals_consumption`: ✅

---

## Part 8: Advanced Station Features

### Capital System
- **Capital Input**: Station requires money to operate
- **Capital Output**: Station generates money
- Useful for economic gameplay

### Goal Contribution
- **`completesGoals_production`**: Output resources count toward goals
- **`completesGoals_consumption`**: Consumed resources count toward goals
- Links stations to the goal system

### Station Upgrades
- **`canBeUpgraded`**: Station can transform into another station
- **`upgradePrefab`**: Reference to upgraded station prefab
- Triggers after resource consumption

### Lifecycle Management
- **`isSingleUse`**: Station can only be used once
- **`canDie`**: Station can be destroyed
- **`canGrow`**: Station changes appearance over time
- **Age System**: Stations can have different sprites at different ages

### Audio and Visual Feedback
- **`workingSound`**: Plays while station is being worked
- **`completeSound`**: Plays when production completes
- **`productionParticles`**: Particle effect on resource spawn
- **Progress Slider**: Visual feedback for work progress

---

## Troubleshooting

### Station Not Producing Resources
- Check `produceResource` is enabled
- Verify `produces` list has resources assigned
- Check `typeOfProduction` matches your intended behavior
- Ensure `spawnResourcePrefab` is true if you want physical objects
- Verify station is `isAlive`

### Resources Not Being Consumed
- Check `consumeResource` is enabled
- Verify `consumes` list has resources assigned
- Ensure `inputArea` is assigned and configured
- Check `inputArea.requirements` matches `consumes` list
- Verify resources are being delivered to input area

### Player Can't Work at Station
- Check `canBeWorked` is enabled
- Verify player is close enough to station
- Check player's `objectToLabor` is set to this station
- Ensure station's `isAlive` is true

### Resources Not Spawning
- Check `spawnResourcePrefab` is enabled
- Verify resource's `resourcePrefab` is assigned in ScriptableObject
- Check `outputArea` is configured (or `spawnRadius` if not using area)
- Verify station has space to spawn

---

## Summary

In this tutorial, you learned:

✅ **Resources** are ScriptableObjects that define item types  
✅ **Stations** can create, consume, and transform resources  
✅ How to create new resources using editor tools  
✅ How to configure stations for different behaviors  
✅ Key functions in the Station script and what they do  
✅ How to create resource transformation chains  
✅ Advanced station features and configuration examples  

### Key Takeaways

- **Resources** = Data (ScriptableObject) + Prefab (GameObject)
- **Stations** = Interactive objects that process resources
- **Production Types**: automatic, whenWorked, whenResourcesConsumed
- **Consumption Types**: automatic, whenWorked, cycle
- **Work System**: Players can work at stations to speed up production
- **Input/Output Areas**: Define where resources enter and exit stations

### Next Steps

- Create multiple resource types
- Build complex transformation chains
- Experiment with different production/consumption types
- Add capital systems to your stations
- Connect stations to goals

Check out [Tutorial 03: Resource Manager and Goals](03-Resource-Manager-and-Goals.md) to set up resource tracking and UI, and [Tutorial 04: Goals and Goal Tracker](04-Goals-and-Goal-Tracker.md) for the Goals system and goal-completing stations.

---

## Related Documentation

- [Resource Management Documentation](../ResourceManagement.md)
- [Station System Documentation](../StationSystem.md)
- [Goal System Documentation](../GoalSystem.md)
