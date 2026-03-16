# Tutorial 04: Goals and Goal Tracker

This tutorial explains the **Goals** system and **Goal Tracker** UI in Game Assemblies. You'll learn how to create goals, configure the GoalManager, understand the goal tracker display, and—importantly—how **stations can act as goal-completing modules** by reporting deliveries and production to the GoalManager.

## Prerequisites

- Completed [Tutorial 01](01-Creating-Character-and-Canvas.md) (Character and Canvas)
- Completed [Tutorial 02](02-Stations-and-Resources.md) (Stations and Resources)
- Completed [Tutorial 03](03-Resource-Manager-and-Goals.md) (Resource Manager and Goals)
- A scene with the Resource Management System (ResourceManager, GoalManager, ResourceManager_Canvas) and at least one station

## Overview

In this tutorial, you'll learn:

1. **ResourceGoalSO** — What a goal asset is and how to create one
2. **GoalManager and Goal Tracker** — How the GoalManager spawns tracker UI and checks completion/failure
3. **Goal Tracker UI** — What the tracker shows (icon, time slider, color ramp) and how to customize it
4. **Stations as Goal-Completing Modules** — How to use `completesGoals_consumption` and `completesGoals_production` so that stations complete goals when they consume or produce resources

---

## Part 1: ResourceGoalSO — What is a Goal?

A **ResourceGoalSO** is a ScriptableObject that defines a single objective: **collect a certain resource** (by type) **within a time limit**. When the goal is met, the player gets **reward points**; when time runs out without meeting it, they get a **penalty**.

### Goal Properties

| Property        | Purpose |
|----------------|---------|
| `resourceType` | The `Resource` the player must deliver or produce (e.g. Bread, Wood) |
| `requiredCount` | How many of that resource are needed. For **station-based completion** (see Part 4), one matching delivery or production completes the goal. `requiredCount` can be used for display or by other systems. |
| `timeLimit`     | Time in seconds to complete the goal. The tracker shows a countdown. |
| `rewardPoints`  | Added to `ResourceManager.globalCapital` when the goal is completed |
| `penalty`       | Subtracted from `globalCapital` when the goal fails (time runs out) |

### Creating a Goal (Editor)

1. **Open the Create Goal window**  
   In Unity’s top menu:
   ```
   Game Assemblies → Goals → Create Goal
   ```

2. **Set the fields**
   - **Goal Name** — Name for the asset (e.g. `"Deliver Bread"`).
   - **Time Limit** — Seconds allowed (e.g. `30`).
   - **Resource to Obtain** — Assign the `Resource` ScriptableObject (e.g. your Bread asset).

3. **Click Create**  
   A `ResourceGoalSO` is saved at:
   ```
   Assets/Simulated Assemblies/Databases/Goals/{GoalName}.asset
   ```

4. **Adjust in the Inspector** (optional)  
   After creation, select the asset and set:
   - **Required Count** — Default is `1`. For station-based completion, one matching event completes the goal; you can still use this for UI or logic.
   - **Reward Points** — e.g. `10`.
   - **Penalty** — e.g. `5`.

You can also create goals via **Assets → Create → Game Assemblies → Goals → Create Resource Goal** and then move or edit them as needed.

---

## Part 2: GoalManager and the Goal Tracker

The **GoalManager** is a singleton that:

- Holds **goal templates** and **active goals** at runtime
- Spawns a **Goal Tracker** UI for each active goal
- Each frame: updates each goal’s **remaining time**, checks **completion** and **failure**, applies **rewards/penalties**, and removes finished goals

### Configuring the GoalManager

The **Create Resource Management System** menu (Tutorial 03) adds the GoalManager and links:

- `goalTrackerGrid` ← from `ResourceManager_Canvas.goalTrackerModule` (where trackers are parented)
- `scoreText` ← from `ResourceManager_Canvas.globalScoreModule` (displays `globalCapital`)

The **GoalManager** prefab also needs:

- **Goal Templates** — Drag your `ResourceGoalSO` assets into this list. At `Start`, the GoalManager instantiates each template, resets it, and adds it to `activeGoals`.
- **Goal Tracker** — Prefab to instantiate for each active goal (the sample `GoalTracker` prefab with `GoalTrackerUI`). This is usually already set on the GoalManager prefab.

### How Goals Are Activated

- **From templates** — Goals in **Goal Templates** are created and activated when the scene starts.
- **At runtime** — You can add goals with `GoalManager.Instance.AddGoal(ResourceGoalSO)`. The LevelManager and other systems use this to spawn goals during a level.

### How Goals Are Completed or Failed

Each frame (while the game is in a `Playing` state), the GoalManager:

1. Calls `UpdateGoaltime(dt)` on each active goal so `remainingTime` decreases.
2. If `remainingTime <= 0`, the goal is marked `isFailed`.
3. If something has set `isCompleted` (e.g. a station via `goalContribution` — see Part 4), the goal is treated as completed.
4. For completed goals: add `rewardPoints` to `ResourceManager.globalCapital`, remove the goal and its tracker, and refresh the score UI.
5. For failed goals: subtract `penalty` from `globalCapital`, remove the goal and its tracker, and refresh the score UI.

So: **completion** is driven by **goalContribution** (or other logic that sets `isCompleted`). **Failure** is driven only by the **timer**.

---

## Part 3: The Goal Tracker UI

For each active goal, the GoalManager instantiates the **Goal Tracker** prefab, parents it to `goalTrackerGrid`, and calls `GoalTrackerUI.Initialize(ResourceGoalSO)`.

### What the Tracker Shows

The **GoalTrackerUI** component uses:

| Field / element  | Purpose |
|------------------|---------|
| **resourceIcon** | `Image` showing the goal’s `resourceType.icon` (what to collect) |
| **timeSlider**   | `Slider` whose value is the **remaining time** (0 to `timeLimit`). Fills down as time runs out. |
| **sliderFillImage** | `Image` used as the slider’s fill; its color is driven by `timeColorRamp`. |
| **timeColorRamp**   | `Gradient` mapping time ratio to color. Typically green when there’s plenty of time, red when time is almost gone. |

- **Initialize** sets the icon from `goal.resourceType.icon`, the slider range from `timeLimit`, and the initial fill color from the gradient.
- **Update** adjusts the slider value and fill color each frame from `goal.remainingTime` and `goal.timeLimit`.

### Goal Tracker Prefab

The sample **GoalTracker** prefab (`GoalTracker.prefab`) includes:

- A panel with **resource icon** (from the goal’s Resource)
- A **progress bar** (Slider) for the countdown
- A **fill** whose color is driven by the gradient

If you duplicate and edit this prefab, ensure the **GoalTrackerUI** references are still correct: `resourceIcon`, `timeSlider`, `sliderFillImage`, and `timeColorRamp`. The GoalManager’s **Goal Tracker** field must point to your prefab.

---

## Part 4: Stations as Goal-Completing Modules

Stations can **complete goals** by notifying the GoalManager when they **consume** or **produce** a resource. The GoalManager then checks whether any active goal is for that resource type and, if so, marks it complete.

### The `goalContribution` API

**GoalManager** exposes:

```csharp
GoalManager.Instance.goalContribution(Resource rType);
```

- **`goalContribution(Resource rType)`** — Tells the GoalManager: “a resource of this type was just delivered or produced in a way that counts for goals.”
- The GoalManager loops over `activeGoals` and calls `goal.UpdateGoalObjective(rType)`.
- **`UpdateGoalObjective(Resource providedResource)`** — If `providedResource == goal.resourceType`, it sets `goal.isCompleted = true`.
- **One** matching call completes **one** goal. `requiredCount` is not used in this path; it’s one delivery or one production per goal.

So: any script that can call `GoalManager.Instance.goalContribution(resource)` can complete goals. Stations do this when you enable the goal-completion flags.

### Station Flags: `completesGoals_consumption` and `completesGoals_production`

On the **Station** component, under **Goals (SCORE)**:

| Flag | Meaning |
|------|---------|
| **Completes Goals (Consumption)** | When this station **consumes** resources (from its `inputArea`), it calls `GoalManager.goalContribution(consumes[0])`. If an active goal’s `resourceType` matches that resource, that goal is completed. |
| **Completes Goals (Production)** | When this station **produces** resources, it calls `GoalManager.goalContribution(produces[0])`. If an active goal’s `resourceType` matches that produced resource, that goal is completed. |

- For **consumption**, the station uses the **first** entry in its `consumes` list: `consumes[0]`.
- For **production**, it uses the **first** entry in `produces`: `produces[0]`.

So the goal’s `resourceType` must match that first consumed or first produced resource.

### When Does the Station Call `goalContribution`?

**Consumption**  
When the station successfully consumes (e.g. `inputArea.allRequirementsMet` and `RemoveMatchingResources`), it runs `ConsumeResource()`. If `completesGoals_consumption` is true, it then calls:

```csharp
gManager.goalContribution(consumes[0]);
```

This happens for all consumption triggers: `whenWorked`, `whenResourcesConsumed`, `automatic`, etc.

**Production**  
When the station produces a resource (in `ProduceResource()`, for `productionMode.Resource`), for each produced item it can call:

```csharp
gManager.goalContribution(produces[0]);
```

only if `completesGoals_production` is true. So each production of that type notifies the GoalManager once per cycle.

### Requirements for Station-Based Goal Completion

1. **GoalManager in the scene** — The Station finds it with `FindAnyObjectByType<GoalManager>()`. If GoalManager is missing, `goalContribution` will fail when the flags are on.
2. **Goal’s `resourceType`** — Must match:
   - `consumes[0]` when using **Completes Goals (Consumption)**,
   - `produces[0]` when using **Completes Goals (Production)**.
3. **Station configuration** — The station must have at least one `consumes` or `produces` entry, and the correct consumption or production logic (e.g. `inputArea`, `outputArea`, `typeOfConsumption`, `typeOfProduction`) so that consume/produce actually runs.

### Example: Delivery Counter (Consumption)

- **Goal**: “Deliver 1 Bread” — `resourceType = Bread`, `timeLimit = 30`, `rewardPoints = 10`, `penalty = 5`.
- **Station**: A “serving counter” or “delivery box”:
  - `consumeResource = true`, `consumes = [Bread]`, `typeOfConsumption = whenResourcesConsumed` (or `automatic` if it pulls from an area).
  - `completesGoals_consumption = true`.
  - `inputArea` set so players can place Bread.

When a Bread is placed and consumed, the station calls `goalContribution(Bread)`. The “Deliver 1 Bread” goal’s `resourceType` matches, so it is marked complete; on the next frame the GoalManager applies the reward and removes the goal and its tracker.

### Example: Oven (Production)

- **Goal**: “Produce 1 Bread” — `resourceType = Bread`, `timeLimit = 45`, `rewardPoints = 15`, `penalty = 5`.
- **Station**: An oven:
  - `produceResource = true`, `produces = [Bread]`, `typeOfProduction = whenResourcesConsumed` (Flour → Bread).
  - `completesGoals_production = true`.

When the oven produces Bread, it calls `goalContribution(Bread)`. The “Produce 1 Bread” goal is completed, and the GoalManager applies the reward.

### Using Both Flags on One Station

A station can have both **Completes Goals (Consumption)** and **Completes Goals (Production)** enabled. For example, a “shop” that consumes raw materials and produces a finished item could complete:

- A goal for the **consumed** resource when it consumes (e.g. “Deliver Wood”),
- A goal for the **produced** resource when it produces (e.g. “Produce Planks”),

as long as the goals’ `resourceType` match `consumes[0]` and `produces[0]` respectively.

---

## Summary

In this tutorial, you learned:

- **ResourceGoalSO** defines a goal: `resourceType`, `timeLimit`, `requiredCount`, `rewardPoints`, `penalty`. Create goals via **Game Assemblies → Goals → Create Goal** and fine-tune in the Inspector.
- **GoalManager** holds goal templates and active goals, spawns a **Goal Tracker** per goal, and each frame updates time, checks completion/failure, and applies rewards/penalties to `ResourceManager.globalCapital`.
- **Goal Tracker UI** shows the resource icon and a time bar (with color ramp). The **GoalTracker** prefab and **GoalTrackerUI** fields must be correctly set on the GoalManager.
- **Stations as goal-completing modules**:
  - **Completes Goals (Consumption)** — On consume, the station calls `goalContribution(consumes[0])`. A goal whose `resourceType` matches is completed.
  - **Completes Goals (Production)** — On produce, the station calls `goalContribution(produces[0])`. A matching goal is completed.
- One matching `goalContribution` completes one goal. The scene must have a **GoalManager**, and the goal’s `resourceType` must match the station’s `consumes[0]` or `produces[0]`.

### Next Steps

- Create several goals (e.g. “Deliver Bread”, “Produce Planks”) and add them to the GoalManager’s **Goal Templates**.
- Turn **Completes Goals (Consumption)** on for a delivery counter and **Completes Goals (Production)** on an oven or crafting station, and test completion and failure.
- Use **LevelManager** or **GoalManager.AddGoal** to spawn goals during play.
- Adjust the **GoalTracker** prefab and **timeColorRamp** to match your game’s look and feel.

---

## Related

- [Tutorial 03: Resource Manager and Goals](03-Resource-Manager-and-Goals.md) — Setting up the Resource Management System and the link to `globalCapital`
- [Tutorial 02: Stations and Resources](02-Stations-and-Resources.md) — Configuring `consumes`, `produces`, `inputArea`, and production/consumption types
