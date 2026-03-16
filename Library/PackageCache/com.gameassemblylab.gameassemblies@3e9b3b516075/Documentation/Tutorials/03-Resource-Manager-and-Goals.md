# Tutorial 03: Resource Manager and Goals

This tutorial explains how to use the **Resource Manager** system in Game Assemblies. You'll learn how to add the Resource Management System to your scene, how it tracks resources and drives the resource UI, and how it connects to **global capital** and the **Goals** system. The Goals system is introduced briefly here and will be fully explained in Tutorial 04.

## Prerequisites

- Completed [Tutorial 01](01-Creating-Character-and-Canvas.md) (Character and Canvas setup)
- Completed [Tutorial 02](02-Stations-and-Resources.md) (Stations and Resources)
- A scene with players, resources, and at least one station
- GA Samples imported (so the ResourceManager, GoalManager, and ResourceManager_Canvas prefabs are available)

## Overview

In this tutorial, you'll learn:

1. **What is the Resource Manager?** — The `ResourceManager` singleton and how it tracks resources
2. **Creating the Resource Management System** — Using the editor menu to add ResourceManager, GoalManager, and the UI canvas
3. **Configuring Resource Tracking** — Binding resources to UI panels so counts update in real time
4. **Global Capital and Goals** — What `globalCapital` is, how the GoalManager uses it, and a short preview of the Goals system (Tutorial 04)

---

## Part 1: What is the Resource Manager?

The **Resource Manager** is a singleton `MonoBehaviour` that keeps a live count of every `ResourceObject` in the scene. It is the central place for:

- **Tracking** — Which resources exist in the world (by `Resource` type)
- **UI** — Driving the on-screen resource counts via `resourcesToTrack`
- **Capital** — Storing `globalCapital`, a score that goals and other systems can add to or subtract from

### How Resources Get Registered

`ResourceObject` automatically registers with the `ResourceManager`:

- **On `Start()`**: It finds the `ResourceManager` in the scene and calls `AddResource(this)`.
- **On `OnDestroy()`**: It calls `RemoveResource(this)` so the count stays correct when resources are consumed, destroyed, or delivered.

You don’t need to register or unregister resources manually. As long as your pickups and station outputs use the `ResourceObject` component with a valid `resourceType`, they are tracked.

### What the ResourceManager Tracks

| Field / concept        | Purpose |
|------------------------|---------|
| `allResources`         | List of every `ResourceObject` currently in the scene (read-only in normal use) |
| `resourcesToTrack`     | List of `ResourceUIBinding`: each links a `Resource` type to a `resourceInfoManager` UI panel |
| `globalCapital`        | Integer score; goals and other logic add (rewards) or subtract (penalties) from this |

### Key Methods (for reference)

- `GetResourceCount(Resource resourceType)` — Number of `ResourceObject`s of that type in the scene
- `GetResourceCount2(Resource resourceType)` — Same, alternative implementation
- `GetAllResourceCounts()` — `Dictionary<Resource, int>` of all resource counts
- `getGlobalCapital()` — Static access to `globalCapital`

---

## Part 2: Creating the Resource Management System

The easiest way to add the Resource Manager to your game is with the built-in menu. This creates the **ResourceManager**, **GoalManager**, and **ResourceManager_Canvas** in one step and wires the GoalManager to the canvas.

### Step-by-Step: Create Resource Management System

1. **Open the menu**  
   In Unity’s top menu bar, go to:
   ```
   Game Assemblies → Systems → Create Resource Management System
   ```

2. **What appears in the scene**
   - **ResourceManager** — The `ResourceManager` component (singleton). Holds `allResources`, `resourcesToTrack`, and `globalCapital`.
   - **GoalManager** — Manages goal templates, active goals, and connects to the goal tracker UI and score text. (Tutorial 04 covers goals in detail.)
   - **ResourceManager_Canvas** — A Canvas with:
     - **Resource panels** — UI for each resource you want to display (each with a `resourceInfoManager`).
     - **Goal Tracker** — Area where `GoalManager` spawns goal tracker UI elements.
     - **Global Score** — Text that shows `ResourceManager.globalCapital` (driven by `GoalManager` when goals complete or fail).

3. **Linking done for you**  
   The menu links:
   - `ResourceManager_Canvas.goalTrackerModule` → `GoalManager.goalTrackerGrid`
   - `ResourceManager_Canvas.globalScoreModule` → `GoalManager.scoreText`

You still need to configure **ResourceManager.resourcesToTrack** so that each resource type you care about is shown in the UI. That’s the next part.

---

## Part 3: Configuring Resource Tracking

The ResourceManager only updates the resource UI for entries in **`resourcesToTrack`**. Each entry is a **ResourceUIBinding** with:

- **`resourceType`** — The `Resource` ScriptableObject (e.g. Wood, Planks, Bread).
- **`resourceUIPanel`** — A `resourceInfoManager` component on a UI panel.

### The resourceInfoManager Component

`resourceInfoManager` is a small component with two `TextMeshProUGUI` references:

- **`resourceName`** — Typically the resource’s display name (e.g. `"Wood"`). You can set it once or leave it to be overridden by `ResourceManager.updatePanels()`.
- **`resourceAmount`** — The count. **ResourceManager** sets this every frame in `updatePanels()` to `GetResourceCount2(resourceType)`.

`ResourceManager` also sets `resourceName` from `resourceType.resourceName` in `updatePanels()`, so both fields can be driven by the ResourceManager.

### How to Configure resourcesToTrack

1. **Locate the ResourceManager**  
   In the Hierarchy, select the **ResourceManager** GameObject (created by the menu).

2. **Find or create resource panels**  
   On **ResourceManager_Canvas**, the sample layout includes panels for resources. Each panel must have a **`resourceInfoManager`** with:
   - `resourceName` → a `TextMeshProUGUI` for the name
   - `resourceAmount` → a `TextMeshProUGUI` for the count

   If you add your own panels, add the `resourceInfoManager` component and assign those two fields.

3. **Fill the `resourcesToTrack` list**
   - In the Inspector, on the **ResourceManager** component, find **Resources To Track**.
   - Set **Size** to the number of resource types you want to show (e.g. 4 for Wood, Planks, Bread, Ore).
   - For each element:
     - **Resource Type**: Assign the `Resource` asset (from `Assets/Simulated Assemblies/Databases/Resources/` or your project’s equivalent).
     - **Resource UI Panel**: Assign the `resourceInfoManager` on the UI panel for that resource.

4. **Result**  
   At runtime, `ResourceManager.updatePanels()` runs every frame and updates each bound panel’s `resourceName` and `resourceAmount` from the current counts. No extra code is required.

### Tips

- **Order** — The order in `resourcesToTrack` should match the order of the panels on screen if you care about layout.
- **Adding resources later** — When you create new `Resource` types (Tutorial 02), add a new `ResourceUIBinding` and a corresponding UI panel with `resourceInfoManager`.
- **Hide unused slots** — You can disable or hide panels for resources you’re not using in a given level.

---

## Part 4: Global Capital and the Goals System (Preview)

The ResourceManager’s **`globalCapital`** is an integer used as a shared score. It is:

- **Read** by `GameManager` for win/loss and end-of-level screens.
- **Written** by **GoalManager** when goals complete (reward) or fail (penalty).

### How GoalManager Uses the ResourceManager

- **Progress** — Goals use `ResourceManager.Instance.GetResourceCount(goal.resourceType)` to see how many of the required resource exist in the scene.
- **Completion** — When a goal’s `requiredCount` is reached, `GoalManager` adds `rewardPoints` to `ResourceManager.Instance.globalCapital`.
- **Failure** — When a goal’s time runs out without being met, `GoalManager` subtracts `penalty` from `globalCapital`.

So the Resource Manager is the backing data for both **resource counts** (for goals and UI) and **global score** (for goals and game flow).

### Goals System — Covered in Tutorial 04

The **Goals** system is built on top of the Resource Manager:

- **ResourceGoalSO** — ScriptableObject that defines a goal: which `Resource`, how many (`requiredCount`), time limit, `rewardPoints`, and `penalty`.
- **GoalManager** — Holds goal templates, spawns `GoalTrackerUI` instances in the goal tracker area, and checks completion/failure each frame.
- **GoalTrackerUI** — Shows the resource icon and a time slider for each active goal.

Tutorial 04 will walk you through:

- Creating **ResourceGoalSO** assets
- Configuring the **GoalManager** with goal templates
- Understanding the goal tracker UI and how it connects to the ResourceManager and `globalCapital`

For now, it’s enough to know that:

- The **Create Resource Management System** menu sets up both ResourceManager and GoalManager and wires the canvas.
- Goals read from `ResourceManager.GetResourceCount` and write to `ResourceManager.globalCapital`.
- The goal tracker and score on the ResourceManager_Canvas are driven by the GoalManager.

---

## Summary

In this tutorial, you learned:

- The **ResourceManager** is a singleton that tracks all `ResourceObject`s and drives the resource UI.
- **ResourceObject** registers and unregisters itself in `Start` and `OnDestroy`; no manual registration is needed.
- **Create Resource Management System** adds ResourceManager, GoalManager, and ResourceManager_Canvas and links the GoalManager to the canvas.
- **`resourcesToTrack`** binds each `Resource` to a `resourceInfoManager` UI panel so `resourceName` and `resourceAmount` update in real time.
- **`globalCapital`** is the shared score; **GoalManager** adds rewards and subtracts penalties when goals complete or fail.
- The **Goals** system (ResourceGoalSO, GoalManager, GoalTrackerUI) is introduced here and covered in [Tutorial 04](04-Goals-and-Goal-Tracker.md).

### Next Steps

- Add more resource types to `resourcesToTrack` and design the layout of your resource UI.
- Use `ResourceManager.getGlobalCapital()` or `ResourceManager.Instance.globalCapital` in your own game logic (e.g. win conditions, shops).
- Proceed to [Tutorial 04: Goals and Goal Tracker](04-Goals-and-Goal-Tracker.md) to create and configure goals and use stations as goal-completing modules.

---

## Related

- [Tutorial 02: Stations and Resources](02-Stations-and-Resources.md) — Creating resources and stations that produce `ResourceObject`s
- [Tutorial 04: Goals and Goal Tracker](04-Goals-and-Goal-Tracker.md) — Creating ResourceGoalSO assets, the Goal Tracker UI, and stations as goal-completing modules
