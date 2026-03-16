# Game Assemblies Documentation

Welcome to the Game Assemblies documentation. This folder contains tutorials, guides, and API reference for using the Game Assemblies package.

---

## Outline

1. [Getting Started](#getting-started)
2. [Basic Concepts](#basic-concepts)
3. [Tutorials](#tutorials)
4. [Documentation Structure](#documentation-structure)
5. [Contributing](#contributing)

---

## Getting Started

Start with the main [README.md](../README.md) in the root directory for installation and basic usage instructions.

### Learning Path

**New to Unity or C#?** Start with the **Programming Fundamentals** (01–05) to build a solid foundation:
1. [01 – Game Objects and Components](basic%20concepts/01-GameObjects-and-Components.md) — Understand Unity's fundamental building blocks
2. [02 – Prefabs](basic%20concepts/02-Prefabs.md) — Understand Unity's prefab system
3. [03 – Syntax](basic%20concepts/03-Syntax.md) — Learn essential C# programming concepts
4. [04 – Vector Math](basic%20concepts/04-Vector-Math.md) — Learn vector operations for game development
5. [05 – Data Structures](basic%20concepts/05-Data-Structures.md) — Master Lists and Dictionaries

**Familiar with Unity?** Jump to the **Core Concepts** (06–10):
1. [06 – Static References](basic%20concepts/06-Static-References.md) — Learn about singletons and global manager access
2. [07 – Scriptable Objects](basic%20concepts/07-Scriptable-Objects.md) — Understanding data assets used throughout Game Assemblies
3. [08 – Compressed Syntax](basic%20concepts/08-Compressed-Syntax.md) — Advanced C# patterns and shorthand
4. [09 – Input System](basic%20concepts/09-Input-System.md) — Create Input Actions assets, bindings, and connect to PlayerInput
5. [10 – Editor Tools](basic%20concepts/10-Editor-Tools.md) — Create custom Unity editor windows and menu items

**Ready to build?** Follow the **Step-by-Step Tutorials** in order:
1. [Tutorial 01: Creating a Character and Canvas](tutorials/01-Creating-Character-and-Canvas.md) — Set up your scene
2. [Tutorial 02: Stations and Resources](tutorials/02-Stations-and-Resources.md) — Add resources and production
3. [Tutorial 03: Resource Manager and Goals](tutorials/03-Resource-Manager-and-Goals.md) — Set up resource tracking and UI
4. [Tutorial 04: Goals and Goal Tracker](tutorials/04-Goals-and-Goal-Tracker.md) — Implement the Goals system
5. [Tutorial 05: Levels and Level Editor](tutorials/05-Levels-and-Level-Editor.md) — Create structured gameplay experiences
6. [Tutorial 06: Creating Environment Objects](tutorials/06-Creating-Environment-Objects.md) — Create backgrounds, obstacles, ground tiles, and foliage
7. [Tutorial 07: Procedural Level Builder](tutorials/07-Procedural-Level-Builder.md) — Use Scatter, Grid, and Perlin Noise tools to generate level content; roadmap for planned tools
8. [Tutorial 08: Database Inspector and Data Management](tutorials/08-Database-Inspector-and-Data-Management.md) — Understand the ScriptableObject-as-database paradigm and use the Database Inspector to browse, edit, validate, and manage all project data

---

## Basic Concepts

Foundational tutorials that explain core Unity and Game Assemblies concepts. Read in order (01–10) for the best learning experience.

### Programming Fundamentals (01–05)

Essential programming concepts for Unity and C# development. Start here if you're new to programming or Unity.

| # | Concept | Description |
|---|---------|-------------|
| 01 | [Game Objects and Components](basic%20concepts/01-GameObjects-and-Components.md) | Understanding Unity's fundamental building blocks: what GameObjects are, how components add functionality, why Transform is mandatory, and common component patterns. Essential for working with Unity. |
| 02 | [Prefabs](basic%20concepts/02-Prefabs.md) | Understanding Unity prefabs: what they are, how to create and use them, prefab instances, variants, and working with prefabs in code. Essential for reusable game objects. |
| 03 | [Syntax](basic%20concepts/03-Syntax.md) | Essential C# programming concepts: variables, data types, methods, classes, control flow, operators, and common patterns used in Unity and Game Assemblies. |
| 04 | [Vector Math](basic%20concepts/04-Vector-Math.md) | Vector operations for game development: Vector2 and Vector3, basic operations, magnitude, normalization, distance calculations, and movement patterns. |
| 05 | [Data Structures](basic%20concepts/05-Data-Structures.md) | Working with Lists and Dictionaries: when to use each, common operations, iteration patterns, and practical examples from Game Assemblies code. |

### Core Concepts (06–10)

Game Assemblies-specific concepts and Unity editor tools. Read these to understand how the library works.

| # | Concept | Description |
|---|---------|-------------|
| 06 | [Static References](basic%20concepts/06-Static-References.md) | Understand the singleton pattern and how static references provide global access to managers like ResourceManager, GoalManager, and LevelManager. |
| 07 | [Scriptable Objects](basic%20concepts/07-Scriptable-Objects.md) | Learn what ScriptableObjects are, why they're used in Game Assemblies, and how to create and use them. Essential for understanding resources, goals, levels, and other data assets. |
| 08 | [Compressed Syntax](basic%20concepts/08-Compressed-Syntax.md) | Advanced C# patterns: lambdas, LINQ, null-conditional operators, and shorthand used in Game Assemblies. |
| 09 | [Input System](basic%20concepts/09-Input-System.md) | Create Input Actions assets, define actions and bindings, connect to PlayerInput, and control your player with the new Input System. Covers action maps, Invoke Unity Events, and action map switching. |
| 10 | [Editor Tools](basic%20concepts/10-Editor-Tools.md) | Learn how to create custom Unity editor windows and menu items. Covers EditorWindow, menu items, GUI elements, and creating prefabs and ScriptableObjects programmatically. |

---

## Tutorials

Step-by-step guides for common workflows. Work through them in order for the best experience.

| # | Tutorial | Description |
|---|----------|-------------|
| 01 | [Creating a Character and Canvas](tutorials/01-Creating-Character-and-Canvas.md) | Set up a basic scene with a playable character and background canvas using the Game Assemblies editor tools. Covers creating an empty white canvas and a local multiplayer player system with customizable character sprites. |
| 02 | [Stations and Resources](tutorials/02-Stations-and-Resources.md) | Learn the core concepts of **Stations** and **Resources**: what they are, how to create resource types and stations, and how to build conversion chains for gathering, producing, and transforming resources. |
| 03 | [Resource Manager and Goals](tutorials/03-Resource-Manager-and-Goals.md) | Use the **Resource Manager** system: add the Resource Management System to your scene, configure resource tracking and UI panels, and understand **global capital** and how it connects to the Goals system. |
| 04 | [Goals and Goal Tracker](tutorials/04-Goals-and-Goal-Tracker.md) | Create **ResourceGoalSO** goals, configure the **GoalManager** and **Goal Tracker** UI, and use **stations as goal-completing modules** with `completesGoals_consumption` and `completesGoals_production`. |
| 05 | [Levels and Level Editor](tutorials/05-Levels-and-Level-Editor.md) | Create **LevelDataSO** levels using the Create Level editor window. Learn about Sequential and Random Interval modes, configure level settings, and understand how LevelManager uses level data to spawn goals dynamically. |
| 06 | [Creating Environment Objects](tutorials/06-Creating-Environment-Objects.md) | Create **Environment objects** using the Create Environment Object tool: **Obstacles** (solid, block movement), **Ground Tiles** (walkable surfaces), and **Foliage** (decorative, wiggle on contact). Also covers Create White Canvas and Create Stage Background. |
| 07 | [Procedural Level Builder](tutorials/07-Procedural-Level-Builder.md) | Use the **Procedural Level Builder** to generate level content: **Scatter Prefab**, **Grid of Prefabs**, and **Perlin Noise Scatter**. Includes development status note and roadmap for planned tools (Room/Wave, Tilemap, Path, Spawn Points, Boundary, Layered Placement). |
| 08 | [Database Inspector and Data Management](tutorials/08-Database-Inspector-and-Data-Management.md) | Understand the **ScriptableObject-as-database** paradigm (data-driven design, single source of truth, version control). Use the **Database Inspector** to browse, search, edit, validate, and manage all project data (Resources, Goals, Levels, Stations, Loot Tables, etc.) in one place. |

---

## Documentation Structure

- **`README.md`** — This file; overview and navigation
- **`basic concepts/`** — Foundational tutorials (01–10) explaining core concepts
- **`tutorials/`** — Step-by-step tutorials (01–08) for common workflows
- **`API/`** — API reference documentation
- **`Guides/`** — Detailed guides for specific features

---

## Contributing

When adding new documentation:

- Use numbered filenames for ordering (e.g., `01-Topic.md`, `02-Topic.md`)
- Include code examples when applicable
- Keep tutorials focused on specific tasks
- Update this README when adding new sections or tutorials
