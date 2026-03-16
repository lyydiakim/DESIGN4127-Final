# Tutorial 01: Creating a Character and Empty Canvas

This tutorial will guide you through using the Game Assemblies editor tools to quickly set up a basic scene with a playable character and a background canvas. These tools automate the creation of prefabs and their required components, saving you time and ensuring proper setup.

## Prerequisites

- Unity 2021.3 LTS or later
- Game Assemblies library imported into your project
- A new or existing Unity scene

## Overview

In this tutorial, you'll learn to:
1. Create an empty white canvas (background) for your game
2. Create a local multiplayer player system with customizable character sprites
3. Understand what prefabs and scripts are automatically created

---

## Step 1: Create an Empty Canvas

The first step is to create a clean background for your game scene.

### Instructions

1. **Open the Editor Menu**: In Unity's top menu bar, navigate to:
   ```
   Game Assemblies → Environment → Create White Canvas
   ```

2. **What Happens**: 
   - A prefab called `BackgroundPlane` is instantiated in your scene at position (0, 0, 0)
   - The object is automatically selected in the Hierarchy

3. **What Gets Created**:
   - **GameObject**: `BackgroundPlane` - A simple plane/sprite renderer that serves as your game's background
   - This is a basic visual element with no scripts attached - just a clean white canvas for your game

### Understanding the Background Canvas

The `BackgroundPlane` prefab is located at:
```
Assets/Simulated Assemblies/Prefabs/Canvas/BackgroundPlane.prefab
```

This prefab is intentionally simple - it's just a visual background element. You can:
- Change its color/material in the Inspector
- Scale it to fit your game's viewport
- Replace the sprite/material with your own artwork

---

## Step 2: Create Local Multiplayer Player System

Now let's add playable characters to your scene. The editor tool will create a complete player management system.

### Instructions

1. **Open the Player Creation Window**: Navigate to:
   ```
   Game Assemblies → Players → Create Local Multiplayer System
   ```

2. **Configure Player Sprites**:
   - The window will open showing four sprite fields:
     - Player 1 Sprite
     - Player 2 Sprite
     - Player 3 Sprite
     - Player 4 Sprite
   - Drag and drop your character sprites into these fields (or leave them empty to use defaults)
   - You can assign sprites for 1-4 players depending on your game's needs

3. **Create the System**: Click the button:
   ```
   "Create Local Multiplayer Environment"
   ```

4. **What Happens**:
   - A `PlayerManager` GameObject is created in your scene
   - The player prefab is automatically configured with your sprites
   - The system is ready for local multiplayer gameplay

### Understanding the Player System

When you create the local multiplayer system, the following prefabs and scripts are automatically set up:

#### PlayerManager GameObject

**Location**: `Assets/Simulated Assemblies/Prefabs/Managers/PlayerManager.prefab`

**Key Components**:

1. **PlayerInputManager** (Unity Input System)
   - Manages multiple players joining the game
   - Handles player spawning and input device assignment
   - Automatically assigns gamepads/keyboards to players
   - **Key Properties**:
     - `Player Prefab`: References the player prefab to spawn
     - `Max Player Count`: Maximum number of players (-1 = unlimited)
     - `Allow Joining`: Whether new players can join during gameplay

2. **playersInfo** Script
   - Tracks all players in the game
   - Manages player data (colors, names, capital)
   - Maintains lists of all player controllers
   - **Key Properties**:
     - `allPlayers`: List of all player GameObjects
     - `allControllers`: List of all playerController components
     - `playerColors`: Color scheme for each player
     - `playerNames`: Display names for each player

#### Player Prefab

**Location**: `Assets/Simulated Assemblies/Prefabs/Players/Player_Drawn.prefab`

**Key Components**:

1. **playerController** Script
   - Core player movement and interaction system
   - Handles input, movement, grabbing, and labor actions
   - **Key Features**:
     - **Movement**: 2D top-down movement with configurable speed
     - **Grab System**: Pick up and carry objects/resources
     - **Labor System**: Work at stations to produce resources
     - **Input Actions**: Button mappings for A, B, X, Y, Start, LB, RB
     - **Sprite Management**: Supports multiple character sprites
   
   **Important Properties**:
   - `playerSpeed`: Movement speed (default: 2.0)
   - `characterSprites`: List of sprites for character selection
   - `sprite1-4`: Individual sprite fields (fallback if list is empty)
   - `playerID`: Unique identifier for this player
   - `isCarryingObject`: Whether player is currently carrying something
   - `maxObjectsToCarry`: Maximum number of objects (default: 2)

2. **PlayerInput** Component
   - Unity's Input System component
   - Handles input mapping and action maps
   - Automatically switches between "Player" and "UI" action maps based on game state

3. **Rigidbody2D** Component
   - Physics body for 2D movement
   - Used for smooth character movement

4. **SpriteRenderer** Component
   - Renders the player's character sprite
   - Handles visual representation

5. **Collider2D** Components
   - Collision detection for interactions
   - Grab areas and interaction zones

### How the Editor Tool Works

The `SA_CreatePlayersWindow` editor tool:

1. **Loads Prefabs**: 
   - Loads `PlayerManager.prefab` from the Prefabs folder
   - Loads `Player_Drawn.prefab` as the player template

2. **Instantiates in Scene**:
   - Creates the PlayerManager GameObject in your active scene
   - Sets it to position (0, 0, 0)

3. **Configures References**:
   - Links the PlayerInputManager's `playerPrefab` field to the player prefab
   - Assigns your sprites to the playerController's sprite fields

4. **Sets Up Sprites**:
   - Assigns `sprite1`, `sprite2`, `sprite3`, `sprite4` on the playerController
   - These can be used individually or as a fallback if `characterSprites` list is empty

### Optional: Update Character Icons

If you want to update the sprites on an existing player prefab without creating a new system:

1. Open the same window: `Game Assemblies → Players → Create Local Multiplayer System`
2. Assign your sprites to the fields
3. Click **"Update Character Icons"**
4. This updates the prefab directly (affects all future player spawns)

---

## Step 3: Test Your Setup

Now that you have a character and canvas, let's verify everything works:

1. **Enter Play Mode**: Press the Play button in Unity
2. **Join as Player**: 
   - Press any button on a connected gamepad, OR
   - Use keyboard input (if configured)
3. **Move Your Character**: 
   - Use the left stick/D-pad on gamepad or WASD on keyboard
   - Your character should move around the white canvas

### Troubleshooting

**Character doesn't move?**
- Check that PlayerInput component has the correct Action Map set to "Player"
- Verify the Input Actions asset is assigned correctly
- Ensure no GameManager is blocking input (the system works without GameManager)

**Character has no sprite?**
- Make sure you assigned sprites in the editor window
- Check the playerController component's sprite fields
- Verify the SpriteRenderer component is on the player GameObject

**Multiple players not working?**
- Ensure PlayerInputManager's "Allow Joining" is enabled
- Check that you have multiple input devices connected
- Verify Max Player Count setting

---

## Understanding the Prefab System

### Why Use Prefabs?

The editor tools create prefabs instead of raw GameObjects because:

1. **Consistency**: Every player/system created is identical
2. **Reusability**: Prefabs can be used across multiple scenes
3. **Maintainability**: Update the prefab once, changes apply everywhere
4. **Version Control**: Prefabs are easier to track in version control

### Prefab Workflow

1. **Editor Tool** → Creates instance in scene from prefab
2. **Prefab Connection** → Scene instance is linked to prefab
3. **Modifications** → You can modify the instance or the prefab
4. **Updates** → Prefab changes propagate to all instances

### Modifying Prefabs

You can customize the created prefabs:

1. **Find the Prefab**: Navigate to `Assets/Simulated Assemblies/Prefabs/`
2. **Select the Prefab**: Click on it in the Project window
3. **Modify in Inspector**: Change properties, add components, etc.
4. **Apply Changes**: Click "Apply" in the Inspector to save changes
5. **All Instances Update**: Changes apply to all existing instances in scenes

---

## Next Steps

Now that you have a character and canvas set up, you can:

- **Add Stations**: Use `Game Assemblies → Stations` menu to create interactive workstations
- **Create Resources**: Use `Game Assemblies → Resources → Create Resource` to define game resources
- **Set Up Goals**: Use `Game Assemblies → Goals → Create Goal` to create objectives
- **Add Systems**: Use `Game Assemblies → Systems` to add resource management and level systems

Check out the next tutorial to learn about creating resources and stations!

---

## Summary

In this tutorial, you learned:

✅ How to create a white canvas background using editor tools  
✅ How to create a local multiplayer player system  
✅ What prefabs and scripts are automatically created  
✅ How the PlayerManager and playerController work together  
✅ How to update character sprites  
✅ How to test your setup  

The editor tools save you time by automatically:
- Instantiating prefabs in the correct locations
- Setting up component references
- Configuring default values
- Organizing your scene hierarchy

---

## Related Documentation

- [Player Systems Documentation](../PlayerSystems.md)
- [Resource Management Documentation](../ResourceManagement.md)
- [Station System Documentation](../StationSystem.md)
