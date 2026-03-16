# Tutorial 05: Levels and Level Editor

This tutorial explains the **Levels** system in Game Assemblies and how to use the **Create Level** editor window to design structured gameplay experiences. You'll learn what a level is, how to configure level types, and how the LevelManager uses level data to spawn goals dynamically.

## Prerequisites

- Completed [Tutorial 01](01-Creating-Character-and-Canvas.md) (Character and Canvas)
- Completed [Tutorial 02](02-Stations-and-Resources.md) (Stations and Resources)
- Completed [Tutorial 03](03-Resource-Manager-and-Goals.md) (Resource Manager and Goals)
- Completed [Tutorial 04](04-Goals-and-Goal-Tracker.md) (Goals and Goal Tracker)
- At least one `ResourceGoalSO` goal asset created (see Tutorial 04)

## Overview

In this tutorial, you'll learn:

1. **What is a Level?** — Understanding `LevelDataSO` and how levels structure gameplay
2. **Opening the Create Level Window** — How to access the level editor
3. **Level Configuration Options** — All settings available in the editor window
4. **Sequential Mode** — Goals appear one after another in order
5. **Random Interval Mode** — Goals spawn randomly from a pool at intervals
6. **Common Settings** — Timing, completion conditions, and level limits
7. **Using Levels with LevelManager** — How levels integrate into your game

---

## Part 1: What is a Level?

A **Level** (`LevelDataSO`) is a ScriptableObject that defines a structured gameplay experience by:

- **Organizing goals** — Determines which goals appear and in what order
- **Controlling timing** — Sets delays, intervals, and time limits
- **Defining completion** — Specifies when a level ends (all goals complete, time runs out, etc.)

Levels are **data assets** that the **LevelManager** reads at runtime to dynamically spawn goals into the game. This allows you to:

- Design multiple levels with different goal sequences
- Create replayable experiences with varied goal patterns
- Separate level design from scene setup
- Easily iterate on level difficulty and pacing

### Level vs Goal

| Concept | Purpose |
|---------|---------|
| **Goal** (`ResourceGoalSO`) | A single objective: "Collect 3 Bread in 30 seconds" |
| **Level** (`LevelDataSO`) | A collection of goals with timing rules: "Show Goal A, then Goal B, then Goal C" |

A level **contains** goals and defines **how** they appear. Goals define **what** the player must do.

---

## Part 2: Opening the Create Level Window

### Accessing the Editor Window

1. **Navigate to the menu**:
   ```
   Game Assemblies → Levels → Create Level
   ```

2. **The window opens** showing:
   - Level name field
   - Level type dropdown
   - Common settings
   - Mode-specific settings (Sequential or Random)
   - Create button

### Window Layout

The Create Level window is organized into sections:

```
┌─────────────────────────────────────┐
│ Create a New Level                  │
├─────────────────────────────────────┤
│ Level Name: [New Level]              │
│ Level Type: [Sequential ▼]          │
├─────────────────────────────────────┤
│ Common Settings                     │
│ Initial Delay: [5]                  │
│ End Level When Complete: [✓]        │
│ Time Limit: [0]                      │
├─────────────────────────────────────┤
│ [Sequential/Random Mode Settings]    │
│ ...                                  │
├─────────────────────────────────────┤
│ Welcome message and path info        │
│ [Create Level] button                │
└─────────────────────────────────────┘
```

---

## Part 3: Level Configuration Options

### Basic Information

#### Level Name
- **Field**: Text input at the top
- **Purpose**: Name for the level asset (e.g., "Level 1", "Tutorial", "Boss Level")
- **Default**: "New Level"
- **Note**: This becomes the filename: `{LevelName}.asset`

#### Level Type
- **Field**: Dropdown menu
- **Options**:
  - **Sequential** — Goals appear one after another in order
  - **Random Interval** — Goals spawn randomly from a pool at set intervals
- **Default**: Sequential
- **Impact**: Changes which settings section appears below

---

## Part 4: Common Settings

These settings apply to **both** Sequential and Random Interval modes.

### Initial Delay (seconds)

- **Purpose**: Time to wait **before the first goal appears** when the level starts
- **Default**: `5.0` seconds
- **Use Cases**:
  - Give players time to orient themselves
  - Allow stations/resources to spawn
  - Show tutorial text or instructions
  - Create dramatic pauses

**Example**: Set to `10` to give players 10 seconds before any goals appear.

### End Level When Complete

- **Purpose**: Whether the level should **automatically end** when all goals are completed
- **Default**: `true` (checked)
- **Behavior**:
  - **`true`**: Level ends immediately when all goals are done
  - **`false`**: Level continues running (useful for sandbox modes or continuous gameplay)

**Example**: 
- **Campaign level**: `true` — Level ends when player completes all goals
- **Endless mode**: `false` — Goals keep spawning, level never ends

### Time Limit (seconds)

- **Purpose**: Maximum time allowed for the **entire level**
- **Default**: `0` (no limit)
- **Behavior**:
  - **`0`**: No time limit — level runs until goals complete or manually ended
  - **`> 0`**: Level ends when time runs out (success or failure depends on your game logic)

**Example**: Set to `300` for a 5-minute level with a hard time limit.

**Note**: This is separate from individual goal time limits. Goals can still fail individually if their `timeLimit` expires.

---

## Part 5: Sequential Mode

**Sequential Mode** presents goals **one at a time** in a **fixed order**. Each goal must be completed (or failed) before the next one appears.

### When to Use Sequential Mode

- **Tutorial levels** — Teach mechanics step-by-step
- **Story progression** — Goals that build on each other
- **Difficulty curves** — Start easy, get harder
- **Narrative structure** — Goals tell a story

### Sequential Mode Settings

#### Sequential Goals List

- **Purpose**: Ordered list of goals that appear one after another
- **Display**: Scrollable list showing "Goal 1", "Goal 2", etc.
- **How to Add Goals**:
  1. Drag a `ResourceGoalSO` asset into the **"New Goal"** field at the bottom
  2. Click **"Add Goal"** button
  3. The goal appears in the list above

#### Managing the Goals List

Each goal in the list has controls:

- **↑ (Up Arrow)** — Move goal earlier in sequence
- **↓ (Down Arrow)** — Move goal later in sequence
- **X (Remove)** — Remove goal from sequence

**Example Workflow**:
1. Add "Collect 3 Wood" → appears as Goal 1
2. Add "Deliver 1 Bread" → appears as Goal 2
3. Add "Produce 2 Planks" → appears as Goal 3
4. Use ↑ on Goal 3 to move it to position 2
5. Final order: Wood → Planks → Bread

### How Sequential Mode Works

When the level starts:

1. **Initial Delay** elapses (e.g., 5 seconds)
2. **First goal** in the list is added to `GoalManager.activeGoals`
3. Player works on that goal
4. When goal **completes or fails**, the next goal in sequence is added
5. Process repeats until all goals are shown
6. If **End Level When Complete** is `true`, level ends when the last goal finishes

**Important**: Only **one goal is active at a time** in Sequential mode (unless goals overlap due to timing).

---

## Part 6: Random Interval Mode

**Random Interval Mode** spawns goals **randomly** from a **pool** at **set time intervals**. Multiple goals can be active simultaneously.

### When to Use Random Interval Mode

- **Endless/survival modes** — Continuous challenge
- **Variety** — Different goal combinations each playthrough
- **Difficulty scaling** — Increase goal frequency over time
- **Chaotic gameplay** — Multiple simultaneous objectives

### Random Interval Mode Settings

#### Goal Interval (seconds)

- **Purpose**: Time **between** spawning new goals from the pool
- **Default**: `30.0` seconds
- **Behavior**: Every X seconds, a random goal from the pool is added

**Example**: 
- `15` seconds → New goal every 15 seconds
- `60` seconds → New goal every minute
- `5` seconds → Very fast-paced, goals spawn rapidly

#### Max Active Goals

- **Purpose**: **Maximum number** of goals that can be active **at the same time**
- **Default**: `3`
- **Behavior**:
  - If `maxActiveGoals = 3` and 3 goals are already active, **no new goal spawns** until one completes/fails
  - Prevents goal overload
  - Creates pacing — players must complete goals to make room for new ones

**Example**:
- `1` → Only one goal at a time (like Sequential, but random)
- `5` → Up to 5 simultaneous goals (very challenging)
- `10` → Many goals active (chaotic mode)

#### Random Goal Pool

- **Purpose**: List of goals that can be randomly selected
- **Display**: Scrollable list
- **How to Add Goals**:
  1. Drag a `ResourceGoalSO` asset into the **"New Goal"** field
  2. Click **"Add to Pool"** button
  3. Goal is added to the pool

**Important**: Goals in the pool can be **selected multiple times**. If you want variety, add many different goals. If you want repetition, add fewer goals.

**Example Pools**:
- **Varied**: 10 different goals → High variety, unlikely to repeat quickly
- **Focused**: 3 goals → Same goals appear frequently, but order is random
- **Mixed**: 5 easy goals + 2 hard goals → Mostly easy, occasional challenge

### How Random Interval Mode Works

When the level starts:

1. **Initial Delay** elapses
2. **First random goal** from pool is added (if `maxActiveGoals` allows)
3. Every **Goal Interval** seconds:
   - If active goals < `maxActiveGoals`, pick a **random goal** from the pool
   - Add it to `GoalManager.activeGoals`
   - If active goals = `maxActiveGoals`, **skip** this interval
4. Goals complete/fail independently
5. Process continues until level ends (time limit, manual end, or **End Level When Complete** = `true` and all goals finish)

**Note**: Random selection is **truly random** — the same goal can appear multiple times, and order is unpredictable.

---

## Part 7: Creating a Level — Step by Step

### Example 1: Tutorial Level (Sequential)

**Goal**: Create a simple tutorial that teaches players to collect resources step-by-step.

1. **Open Create Level Window**: `Game Assemblies → Levels → Create Level`

2. **Set Basic Info**:
   - Level Name: `"Tutorial Level 1"`
   - Level Type: `Sequential`

3. **Configure Common Settings**:
   - Initial Delay: `5` (give players time to see the scene)
   - End Level When Complete: `true` (tutorial ends when done)
   - Time Limit: `0` (no overall time limit)

4. **Add Sequential Goals**:
   - Drag "Collect 3 Wood" goal → Click "Add Goal"
   - Drag "Deliver 1 Bread" goal → Click "Add Goal"
   - Drag "Produce 2 Planks" goal → Click "Add Goal"

5. **Click "Create Level"**

**Result**: A level that shows goals one at a time, teaching mechanics progressively.

### Example 2: Survival Mode (Random Interval)

**Goal**: Create an endless survival mode with random challenges.

1. **Open Create Level Window**: `Game Assemblies → Levels → Create Level`

2. **Set Basic Info**:
   - Level Name: `"Survival Mode"`
   - Level Type: `Random Interval`

3. **Configure Common Settings**:
   - Initial Delay: `3` (quick start)
   - End Level When Complete: `false` (never ends automatically)
   - Time Limit: `300` (5-minute survival challenge)

4. **Configure Random Settings**:
   - Goal Interval: `20` (new goal every 20 seconds)
   - Max Active Goals: `4` (up to 4 simultaneous goals)

5. **Add Goals to Pool**:
   - Add 8-10 different goals (mix of easy and hard)
   - Examples: "Deliver Bread", "Produce Planks", "Collect Wood", "Deliver Soup", etc.

6. **Click "Create Level"**

**Result**: A level that continuously spawns random goals, creating varied, challenging gameplay.

### Example 3: Boss Level (Sequential with Time Limit)

**Goal**: Create a challenging boss level with a strict time limit.

1. **Open Create Level Window**: `Game Assemblies → Levels → Create Level`

2. **Set Basic Info**:
   - Level Name: `"Boss Level - Final Challenge"`
   - Level Type: `Sequential`

3. **Configure Common Settings**:
   - Initial Delay: `2` (quick start for challenge)
   - End Level When Complete: `true` (level ends when all goals done)
   - Time Limit: `180` (3-minute hard limit)

4. **Add Challenging Sequential Goals**:
   - Add 5-6 difficult goals in order
   - Each goal should have short time limits (20-30 seconds)
   - Goals should build in difficulty

5. **Click "Create Level"**

**Result**: A high-pressure level where players must complete goals quickly or fail the level.

---

## Part 8: Where Levels Are Saved

When you click **"Create Level"**, the level asset is saved at:

```
Assets/Game Assemblies/Databases/Levels/{LevelName}.asset
```

**Example**: If you name your level "Tutorial Level 1", it saves as:
```
Assets/Game Assemblies/Databases/Levels/Tutorial Level 1.asset
```

The folder `Game Assemblies/Databases/Levels/` is **automatically created** if it doesn't exist.

### Editing Levels After Creation

After creating a level, you can:

1. **Select the asset** in the Project window
2. **Edit in Inspector** — Change any property (level type, goals, settings)
3. **Changes are saved automatically** when you modify values

**Note**: Changing a level asset affects **all scenes** that use it. If you want to test variations, create multiple level assets with different names.

---

## Part 9: Using Levels with LevelManager

Levels are **data assets** — they don't do anything by themselves. The **LevelManager** reads level data and spawns goals at runtime.

### Setting Up LevelManager

1. **Create Level System**: Use the menu:
   ```
   Game Assemblies → Systems → Create Levels System and Menu
   ```
   This creates:
   - `LevelManager` GameObject
   - `GameStateManagerAndCanvas` (for level selection UI)

2. **Assign Levels to LevelManager**:
   - Select the `LevelManager` GameObject in the scene
   - In the Inspector, find **"Level Data List"**
   - Drag your `LevelDataSO` assets into this list
   - Set **"Current Level Index"** (usually `0` for the first level)

### How LevelManager Uses Levels

When the level starts:

1. **LevelManager** reads the current level from `levelDataList[currentLevelIndex]`
2. **Initial Delay** elapses
3. **Based on Level Type**:
   - **Sequential**: Adds first goal from `sequentialGoals` list
   - **Random Interval**: Starts timer, spawns goals from `randomGoalPool` at intervals
4. **Goals are added** to `GoalManager.activeGoals` via `GoalManager.Instance.AddGoal()`
5. **Level continues** until:
   - All goals complete (if `endLevelWhenComplete = true`)
   - Time limit expires (if `timeLimit > 0`)
   - Level is manually ended

### Level Progression

The LevelManager can:

- **Progress to next level**: Increment `currentLevelIndex` and load the next level from the list
- **Trigger events**: `onLevelStart`, `onLevelComplete`, `onAllLevelsComplete`
- **Track time**: `elapsedTime`, `currentLevelTimeLeft`
- **Handle completion**: Check if level is complete and trigger next level or end game

---

## Part 10: Tips and Best Practices

### Level Design Tips

1. **Start Simple**: Begin with Sequential mode and 2-3 goals to test your setup
2. **Test Timing**: Adjust `initialDelay` and `goalInterval` based on gameplay feel
3. **Balance Difficulty**: Mix easy and hard goals, especially in Random Interval mode
4. **Use Descriptive Names**: Name levels clearly (e.g., "Tutorial 1", "Level 2 - Forest", "Boss Fight")

### Sequential Mode Tips

- **Order Matters**: Place easier goals first to teach mechanics
- **Build Complexity**: Each goal should introduce something new or increase difficulty
- **Consider Overlap**: Goals can overlap if timing allows (e.g., if Goal 1 takes 30 seconds and Goal 2 appears after 20 seconds)

### Random Interval Mode Tips

- **Pool Size**: More goals in pool = more variety
- **Interval Tuning**: 
  - Too fast (`< 10` seconds) → Overwhelming
  - Too slow (`> 60` seconds) → Boring gaps
  - Sweet spot: `15-30` seconds for most games
- **Max Active Goals**: 
  - `1-2` → Manageable, focused gameplay
  - `3-4` → Challenging, requires multitasking
  - `5+` → Chaotic, survival mode feel

### Common Settings Tips

- **Initial Delay**: 
  - Tutorials: `5-10` seconds (give instructions)
  - Action levels: `0-2` seconds (immediate start)
- **End Level When Complete**: 
  - Campaign: `true` (structured progression)
  - Endless/Sandbox: `false` (continuous play)
- **Time Limit**: 
  - Most levels: `0` (let goals control timing)
  - Challenge modes: `60-300` seconds (add pressure)

---

## Summary

In this tutorial, you learned:

✅ **What a Level is** — A `LevelDataSO` ScriptableObject that organizes goals and timing  
✅ **How to open the Create Level window** — `Game Assemblies → Levels → Create Level`  
✅ **Level configuration options**:
   - **Level Name** — Asset name
   - **Level Type** — Sequential or Random Interval
   - **Common Settings** — Initial delay, completion behavior, time limit
✅ **Sequential Mode** — Goals appear one after another in order  
✅ **Random Interval Mode** — Goals spawn randomly from a pool at intervals  
✅ **How to create levels** — Step-by-step examples  
✅ **Where levels are saved** — `Assets/Game Assemblies/Databases/Levels/`  
✅ **How LevelManager uses levels** — Reads level data and spawns goals dynamically  
✅ **Best practices** — Tips for designing effective levels  

### Next Steps

- Create several levels with different configurations
- Set up LevelManager in your scene and assign levels to it
- Test Sequential and Random Interval modes
- Experiment with timing and difficulty curves
- Design a level progression system (tutorial → easy → medium → hard → boss)

---

## Related Documentation

- [Tutorial 04: Goals and Goal Tracker](04-Goals-and-Goal-Tracker.md) — Creating `ResourceGoalSO` goals
- [Tutorial 03: Resource Manager and Goals](03-Resource-Manager-and-Goals.md) — Setting up GoalManager
- [LevelManager Script Reference](../Runtime/Scripts/LevelManager.cs) — Runtime level management
- [LevelDataSO Script Reference](../Runtime/Scripts/LevelDataSO.cs) — Level data structure
