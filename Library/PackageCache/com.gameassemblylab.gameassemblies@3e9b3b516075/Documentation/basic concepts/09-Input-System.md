# Unity Input System

This foundational guide explains how to work with **Unity's Input System** package: creating Input Action assets, defining actions and bindings, and connecting them to your player via the **PlayerInput** component. The Game Assemblies library uses this system for player movement, button actions, and UI navigation.

## Prerequisites

- Unity 2021.3 LTS or later
- **Input System** package installed (Window → Package Manager → Input System)
- If prompted, choose **Yes** when Unity asks to enable the new Input System (disables the legacy Input Manager)

---

## Overview

The Unity Input System uses a **data-driven** approach:

1. **Input Actions Asset** (`.inputactions`) — A JSON file that defines all your input mappings
2. **Action Maps** — Groups of actions (e.g., "Player" for gameplay, "UI" for menus)
3. **Actions** — Named inputs like "Movement" (Vector2) or "Fire" (Button)
4. **Bindings** — Links actions to physical controls (keys, sticks, buttons)
5. **PlayerInput** — Component that reads the asset and invokes your C# methods

**Key Benefit**: You can rebind controls, support multiple devices, and switch action maps (e.g., Player ↔ UI) without changing code.

---

## Step 1: Create an Input Actions Asset

### Instructions

1. In the **Project** window, right-click in the folder where you want the asset
2. Select **Create → Input Actions**
3. Name the asset (e.g., `playerControls`)
4. Double-click the asset to open the **Input Actions** window

### What You Get

- A `.inputactions` file (JSON format)
- An empty asset with no action maps yet
- The Input Actions editor window for visual editing

---

## Step 2: Create Action Maps and Actions

### Add an Action Map

1. In the Input Actions window, click **+** next to "Action Maps"
2. Name it `Player` (for gameplay input)
3. Optionally add a second map named `UI` (for menus, pause screens)

### Add Actions

Inside the **Player** action map:

| Action Name | Action Type | Expected Control Type | Purpose |
|-------------|-------------|------------------------|---------|
| Movement    | Value       | Vector 2               | WASD / left stick movement |
| Fire 1      | Button      | —                      | Primary action (A button, E key) |
| Fire 2      | Button      | —                      | Secondary action (B button) |
| Pause       | Button      | —                      | Open menu (Start, Escape) |

**How to add each action**:
1. Click **+** next to the action map
2. Choose **Add Action**
3. Set the **Name** and **Action Type**
4. For Movement, set **Control Type** to `Vector 2`

### Action Types Explained

| Type    | Use Case                    | Read Value                    |
|---------|-----------------------------|-------------------------------|
| Value   | Continuous input (stick, keys) | `context.ReadValue<Vector2>()` |
| Button  | Press/release (buttons)     | Check `context.started`, `context.performed`, `context.canceled` |
| Pass Through | Raw passthrough       | Same as Value, no processing  |

---

## Step 3: Add Bindings

Bindings connect actions to physical controls. You can add multiple bindings per action (e.g., keyboard + gamepad).

### Movement (Vector2) — Use a Composite

For 2D movement, use a **2D Vector** composite so one action reads up/down/left/right:

1. Click **+** on the Movement action → **Add 2D Vector Composite**
2. Under the composite, add bindings:
   - **Up**: `<Keyboard>/w` (or `<Gamepad>/leftStick/up`)
   - **Down**: `<Keyboard>/s`
   - **Left**: `<Keyboard>/a`
   - **Right**: `<Keyboard>/d`
3. Add a **second** 2D Vector composite for gamepad:
   - **Up**: `<Gamepad>/leftStick/up`
   - **Down**: `<Gamepad>/leftStick/down`
   - **Left**: `<Gamepad>/leftStick/left`
   - **Right**: `<Gamepad>/leftStick/right`

### Button Actions

1. Click **+** on Fire 1 → **Add Binding**
2. Click the binding, then click the **Path** field
3. Press the key or button you want (e.g., E key, or gamepad South button)
4. Repeat for Fire 2, Pause, etc.

**Example bindings** (matching Game Assemblies `playerControls`):

| Action  | Keyboard | Gamepad      |
|---------|----------|--------------|
| Fire 1  | E        | buttonSouth (A) |
| Fire 2  | R        | buttonEast (B)  |
| Pause   | Escape   | start         |

---

## Step 4: Control Schemes (Optional)

Control Schemes let you organize bindings by device:

1. Click **Control Schemes** in the left panel
2. Add schemes: `Keyboard` and `Joystick`
3. Assign devices: `<Keyboard>` to Keyboard, `<Gamepad>` to Joystick
4. In each binding, you can set the **Control Scheme** so it only applies to that device

This helps with multiplayer (each player gets a different scheme) and with clearer rebinding UIs.

---

## Step 5: Add PlayerInput and Assign the Asset

### Add the Component

1. Select your **Player** GameObject (the one with your movement script)
2. **Add Component** → search for **Player Input**
3. In the **Actions** field, assign your Input Actions asset (e.g., `playerControls`)

### Configure Behaviour

Set **Behaviour** to **Invoke Unity Events**. This lets you wire actions to C# methods in the Inspector.

**Other behaviours**:
- **Send Messages** — Calls `On{ActionName}` methods by naming convention
- **Broadcast Messages** — Same, but to children
- **Invoke Unity Events** — Manual wiring in Inspector (recommended for clarity)

### Default Map

Set **Default Map** to `Player` so gameplay input is active when the scene starts.

---

## Step 6: Connect Actions to Your Script

### Method Signatures

Your C# methods must accept `InputAction.CallbackContext`:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Vector2 movementInput;
    public float playerSpeed = 5f;
    public Rigidbody2D rb;

    // Movement: Value/Vector2 action
    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    // Button: Fire action
    public void OnFire(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // Button was pressed
            Debug.Log("Fire!");
        }
    }

    // Button: Handle started, performed, canceled (e.g., hold to charge)
    public void OnCharge(InputAction.CallbackContext context)
    {
        if (context.started)
            StartCharging();
        else if (context.canceled)
            ReleaseCharge();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = movementInput * playerSpeed;
    }
}
```

### Wiring in the Inspector

1. With **PlayerInput** selected, expand **Player** (or your action map)
2. Each action shows a **+** for listeners
3. Drag your Player GameObject into the object field
4. From the function dropdown, select **PlayerController → OnMove** (or your method name)
5. Repeat for each action you want to use

**Important**: Method names don't have to match exactly when using Invoke Unity Events — you pick them from the dropdown. With **Send Messages**, Unity looks for `On{ActionName}` (e.g., `OnMove`, `OnFire`).

---

## Step 7: Action Map Switching

You can switch which action map is active (e.g., Player vs UI when paused):

```csharp
public PlayerInput playerInputComponent;

void PauseGame()
{
    playerInputComponent.SwitchCurrentActionMap("UI");
}

void ResumeGame()
{
    playerInputComponent.SwitchCurrentActionMap("Player");
}
```

**Example from Game Assemblies** (`playerController.cs`): The controller switches maps based on `GameState`:

```csharp
private void HandleGameStateChanged(GameState newState)
{
    switch (newState)
    {
        case GameState.Menu:
        case GameState.Paused:
        case GameState.Results:
            playerInputComponent.SwitchCurrentActionMap("UI");
            break;
        case GameState.Playing:
            playerInputComponent.SwitchCurrentActionMap("Player");
            break;
    }
}
```

---

## Example: Game Assemblies playerControls

The Game Assemblies package includes a sample Input Actions asset:

**Location**: `Samples/Player Controls/playerControls.inputactions`

### Player Action Map

| Action   | Type  | Bindings                                      |
|----------|-------|-----------------------------------------------|
| Movement | Value | WASD + Left Stick (2D Vector composites)     |
| Fire 1   | Button| E, Gamepad South (A)                          |
| Fire 2   | Button| R, Gamepad East (B)                           |
| Fire 3   | Button| T, Gamepad West (X) — Hold for labor          |
| Fire 4   | Button| Y, Gamepad North (Y) — Hold for absorb        |
| Fire 5   | Button| Gamepad Left Shoulder (LB)                   |
| Fire 6   | Button| Gamepad Right Shoulder (RB)                   |
| Pause    | Button| Escape, Gamepad Start                         |

### UI Action Map

Used when the game is paused or in menus — same movement for UI navigation, plus a Fire action for confirm.

### Connection to playerController

The `playerController` script receives these via Invoke Unity Events:

- `onMove` → stores `movementInput`, updates grab point, flips sprite
- `onFire` → Button A (grab/drop)
- `onFire2` → Button B
- `onFire3` → Button X (start/cancel labor)
- `onFire4` → Button Y (start/stop absorb)
- `onFire5` / `onFire6` → LB/RB (cycle character sprite)
- `onPause` → Pause game, switch to UI map

---

## Alternative: InputActionReference in Code

Instead of Invoke Unity Events, you can reference actions directly in code:

```csharp
public InputActionReference moveAction;

void OnEnable()
{
    moveAction.action.Enable();
    moveAction.action.performed += OnMovePerformed;
    moveAction.action.canceled += OnMoveCanceled;
}

void OnDisable()
{
    moveAction.action.performed -= OnMovePerformed;
    moveAction.action.canceled -= OnMoveCanceled;
}

void OnMovePerformed(InputAction.CallbackContext context)
{
    movementInput = context.ReadValue<Vector2>();
}

void OnMoveCanceled(InputAction.CallbackContext context)
{
    movementInput = Vector2.zero;
}
```

Assign the **Movement** action from your asset to `moveAction` in the Inspector. Remember to enable/disable and unsubscribe to avoid leaks.

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| **"Input System package not found"** | Install via Package Manager: Window → Package Manager → Input System |
| **No input received** | Ensure **Default Map** is set on PlayerInput; check that the correct action map is active |
| **Movement is (0,0)** | Verify Movement action type is Value and Control Type is Vector 2; use a 2D Vector composite for keys |
| **Button fires multiple times** | Use `context.performed` for single press, or `context.started` / `context.canceled` for hold logic |
| **Wrong player receives input** | With PlayerInputManager, each player gets a copy of the actions; don't use `InputSystem.actions` directly |
| **Input in wrong context** | Use `SwitchCurrentActionMap` when changing game state (menu, pause, playing) |

---

## Summary

1. **Create** an Input Actions asset (Create → Input Actions)
2. **Define** action maps (e.g., Player, UI) and actions (Movement, Fire, Pause)
3. **Add bindings** — use 2D Vector composite for movement, direct bindings for buttons
4. **Add PlayerInput** to your player, assign the asset, set Behaviour to Invoke Unity Events
5. **Wire** each action to a C# method with signature `void MethodName(InputAction.CallbackContext context)`
6. **Read values** with `context.ReadValue<Vector2>()` for movement; use `context.started` / `context.performed` / `context.canceled` for buttons
7. **Switch maps** with `playerInput.SwitchCurrentActionMap("MapName")` when game state changes

---

## Related Documentation

- [Tutorial 01: Creating a Character and Canvas](../tutorials/01-Creating-Character-and-Canvas.md) — Sets up PlayerInputManager and player prefab with input
- [01 – Game Objects and Components](./01-GameObjects-and-Components.md) — Understanding the Player GameObject structure
- [Unity Input System Manual](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/) — Official package documentation
