# Tutorial 06: Creating Environment Objects

This tutorial explains how to create environment objects in Game Assemblies, including backgrounds, obstacles, ground tiles, and foliage. These tools help you quickly build the visual play space for your game.

## Prerequisites

- Unity 2021.3 LTS or later
- Game Assemblies library imported into your project
- A new or existing Unity scene
- (Optional) Sprite assets for your environment objects

## Overview

In this tutorial, you'll learn to:
1. Create a White Canvas or Stage Background for your scene
2. Use the Create Environment Object tool to make Obstacles, Ground Tiles, and Foliage
3. Understand the differences between each environment object subtype

---

## Environment Menu Options

All environment-related tools are under:
```
Game Assemblies → Environment
```

| Menu Item | Purpose |
|-----------|---------|
| **Create White Canvas** | Adds a simple flat background plane at z=1 |
| **Create Stage Background** | Adds a screen-sized background at z=0 |
| **Create Environment Object** | Opens a window to create Obstacles, Ground Tiles, or Foliage prefabs |
| **Procedural Level Builder** | Builds levels procedurally with tiles and obstacles |

---

## Part 1: Background Canvases

### Create White Canvas

Use this when you need a simple, flat background for your game.

1. **Open the menu**: `Game Assemblies → Environment → Create White Canvas`
2. **What happens**: A `BackgroundPlane` prefab is instantiated at position (0, 0, 1).
3. **Why z=1**: Placed behind sprites at z=0 so it doesn't overlap with gameplay objects.

**Use cases**: Minimalist backgrounds, prototyping, testing, or a blank canvas you'll customize later.

### Create Stage Background

Use this when you need a full screen background for your stage.

1. **Open the menu**: `Game Assemblies → Environment → Create Stage Background`
2. **What happens**: A `ScreenBackground` prefab is instantiated at position (0, 0, 0).
3. **Difference from White Canvas**: The Stage Background is typically sized for the screen and may use different prefab configuration.

**Use cases**: Kitchen floors, outdoor arenas, or any scene where the background is part of the stage itself.

---

## Part 2: Create Environment Object

The **Create Environment Object** tool lets you build prefabs from sprites with three different subtypes. Each subtype has different behavior and placement in the scene.

### Opening the Tool

1. **Open the window**: `Game Assemblies → Environment → Create Environment Object`
2. The window shows:
   - **Type** dropdown (Obstacle, Ground Tile, Foliage)
   - **Object Details** (name, sprite, tint, scale)
   - **Prefab Template** (optional custom template per type)

---

## Environment Object Subtypes

### Obstacle

**Purpose**: Solid objects that block player movement.

| Property | Description |
|----------|-------------|
| **Collision** | Has solid colliders; players cannot pass through |
| **Parent** | Objects are placed under an `Environment` folder in the scene |
| **Output folder** | `Game Assemblies/Prefabs/Environment/Obstacles` |
| **Collider** | Resized automatically to match the sprite bounds |

**Use cases**: Walls, rocks, barrels, furniture, counters, or any object that should block the player.

**Template**: Uses `Obstacle_Template` by default. The template includes a collider that is resized to fit your sprite when you create the prefab.

---

### Ground Tile

**Purpose**: Floor textures and surfaces that players walk over.

| Property | Description |
|----------|-------------|
| **Collision** | No collision; players walk directly over tiles |
| **Parent** | Objects are placed under a `Tiles` folder in the scene |
| **Output folder** | `Game Assemblies/Prefabs/Environment/Tiles` |
| **Collider** | None (decorative only) |

**Use cases**: Floor textures, paths, dirt, grass, wood planks, or any decorative surface.

**Template**: Uses `Groundtile_Template` by default. Tiles are purely visual—no gameplay interaction.

---

### Foliage

**Purpose**: Decorative elements that react to the player (e.g., wiggle animation).

| Property | Description |
|----------|-------------|
| **Collision** | Does not block movement; players pass through |
| **Parent** | Objects are placed under an `Environment` folder in the scene |
| **Output folder** | `Game Assemblies/Prefabs/Environment/Foliage` |
| **Behavior** | Triggers a wiggle animation when the player passes through |

**Use cases**: Grass, bushes, flowers, reeds, or any decorative element that should react to the player.

**Template**: Uses `Folliage_Template` by default. The template includes the wiggle animation trigger and a collider sized to the sprite (used for trigger detection, not blocking).

---

## Part 3: Creating an Environment Object Step-by-Step

### Step 1: Choose the Type

Select **Obstacle**, **Ground Tile**, or **Foliage** from the Type dropdown. The help box below updates to describe the selected type.

### Step 2: Assign a Sprite

Drag a sprite into the **Sprite** field. The sprite defines the visual appearance and (for Obstacles and Foliage) the collider shape.

### Step 3: Configure Optional Settings

- **Object Name**: Name for the prefab (e.g., `Rock`, `GrassTile`, `Bush`)
- **Sprite Tint**: Color tint applied to the sprite (default: white for no tint)
- **Scale**: Uniform scale for the prefab (0.1 to 5.0)

### Step 4: Create the Prefab

Click **Create Prefab**. The tool will:

1. Copy the appropriate template to `Assets/Game Assemblies/Prefabs/Environment/{Obstacles|Tiles|Foliage}/{objectName}.prefab`
2. Assign your sprite and tint to the SpriteRenderer
3. For Obstacles and Foliage: resize colliders to match the sprite bounds
4. Apply the scale to the prefab
5. Select the new prefab in the Project window

### Step 5: Place in the Scene

Drag the new prefab from the Project window into the scene Hierarchy. For Obstacles and Foliage, consider parenting them under an `Environment` GameObject; for Ground Tiles, under a `Tiles` GameObject.

---

## Summary

| Type | Collision | Use For |
|------|-----------|---------|
| **Obstacle** | Solid (blocks movement) | Walls, rocks, furniture |
| **Ground Tile** | None | Floors, paths, surfaces |
| **Foliage** | Trigger only (no blocking) | Grass, bushes, flowers |

- **Create White Canvas** and **Create Stage Background** add instant scene objects.
- **Create Environment Object** creates reusable prefabs you can place multiple times.

For procedural level building with tiles and obstacles, see [Tutorial 07: Procedural Level Builder](07-Procedural-Level-Builder.md).
