using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Resolves human-readable control labels for the player resource boards from
/// <c>playerControls</c> bindings (Joystick = gamepad face buttons, Keyboard / KeyboardP2).
/// For local multiplayer (up to four <see cref="PlayerInput"/> instances), each player’s
/// paired <see cref="Gamepad"/> is detected so prompts show Xbox-style <b>A</b>/<b>B</b>/<b>X</b>/<b>Y</b>
/// for Fire 1–4 when that player is on a controller.
/// </summary>
public static class PlayerResourceBindingPrompts
{
    public const string GroupJoystick = "Joystick";
    public const string GroupKeyboard = "Keyboard";
    public const string GroupKeyboardP2 = "KeyboardP2";

    public const string ActionFire1 = "Player/Fire 1";
    public const string ActionFire2 = "Player/Fire 2";
    public const string ActionFire3 = "Player/Fire 3";
    public const string ActionFire4 = "Player/Fire 4";

    /// <summary>
    /// When the prompt group is <see cref="GroupJoystick"/>, use fixed letters for face-button actions
    /// (<see cref="ActionFire1"/> = South → A, <see cref="ActionFire2"/> = East → B, <see cref="ActionFire3"/> = West → X,
    /// <see cref="ActionFire4"/> = North → Y). Matches Xbox controllers and this project’s <c>playerControls</c> gamepad layout.
    /// Set to false to use Unity’s per-device display names from bindings (e.g. if you add PlayStation prompts later).
    /// </summary>
    public static bool UseXboxStyleFaceButtonNamesForGamepad = true;

    /// <summary>
    /// Finds <see cref="PlayerInput"/> on the player (field, same GameObject, or children).
    /// </summary>
    public static PlayerInput ResolvePlayerInput(playerController pc)
    {
        if (pc == null) return null;
        if (pc.playerInputComponent != null) return pc.playerInputComponent;
        var pi = pc.GetComponent<PlayerInput>();
        if (pi != null) return pi;
        return pc.GetComponentInChildren<PlayerInput>(true);
    }

    /// <summary>
    /// Which binding group to show for UI: follows <see cref="PlayerInput.currentControlScheme"/>,
    /// or infers gamepad when no scheme is set yet but this player has a paired <see cref="Gamepad"/>.
    /// </summary>
    public static string ResolvePromptGroup(PlayerInput playerInput)
    {
        if (playerInput == null) return GroupKeyboard;

        string scheme = playerInput.currentControlScheme;
        if (!string.IsNullOrEmpty(scheme))
        {
            if (IsJoystickSchemeName(scheme)) return GroupJoystick;
            if (string.Equals(scheme, GroupKeyboardP2, StringComparison.Ordinal))
                return GroupKeyboardP2;
            if (string.Equals(scheme, GroupKeyboard, StringComparison.Ordinal))
                return GroupKeyboard;
        }

        // Per-player gamepad (four players = four gamepads): show face-button prompts even if scheme
        // has not switched yet (e.g. before first input this frame).
        if (playerInput.GetDevice<Gamepad>() != null)
            return GroupJoystick;

        foreach (var d in playerInput.devices)
        {
            if (d is Gamepad)
                return GroupJoystick;
        }

        return GroupKeyboard;
    }

    /// <summary>
    /// True when this player is using the split-keyboard P2 scheme (arrow keys + Insert, etc.).
    /// </summary>
    public static bool IsKeyboardP2Player(playerController pc)
    {
        var pi = ResolvePlayerInput(pc);
        return pi != null && string.Equals(pi.currentControlScheme, GroupKeyboardP2, StringComparison.Ordinal);
    }

    static bool IsJoystickSchemeName(string scheme)
    {
        if (string.IsNullOrEmpty(scheme)) return false;
        if (string.Equals(scheme, GroupJoystick, StringComparison.OrdinalIgnoreCase)) return true;
        // Some templates name the scheme "Gamepad"; ours is "Joystick" but both mean face buttons here.
        if (string.Equals(scheme, "Gamepad", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>
    /// Display string for one action (e.g. "A", "E | R", "Insert | E") for the given scheme group.
    /// Joystick + <see cref="UseXboxStyleFaceButtonNamesForGamepad"/>: face-button actions show A/B/X/Y.
    /// </summary>
    public static string BindingLabel(PlayerInput playerInput, string actionPath, string schemeGroup)
    {
        if (playerInput?.actions == null || string.IsNullOrEmpty(actionPath))
            return "?";

        var action = playerInput.actions.FindAction(actionPath, throwIfNotFound: false);
        if (action == null)
            return "?";

        if (schemeGroup == GroupJoystick && UseXboxStyleFaceButtonNamesForGamepad)
        {
            string xb = XboxStyleLetterForFaceButtonAction(actionPath);
            if (xb != null)
                return xb;
        }

        string text = action.GetBindingDisplayString(group: schemeGroup);
        return string.IsNullOrEmpty(text) ? "?" : text;
    }

    /// <summary>
    /// Maps <c>Player/Fire 1–4</c> to A/B/X/Y per <c>playerControls</c> (South/East/West/North). Returns null if not a mapped action.
    /// </summary>
    static string XboxStyleLetterForFaceButtonAction(string actionPath)
    {
        if (actionPath == ActionFire1) return "A";
        if (actionPath == ActionFire2) return "B";
        if (actionPath == ActionFire3) return "X";
        if (actionPath == ActionFire4) return "Y";
        return null;
    }

    /// <summary>
    /// Convenience: label using <see cref="ResolvePromptGroup"/>.
    /// </summary>
    public static string BindingLabelForActiveScheme(PlayerInput playerInput, string actionPath)
    {
        return BindingLabel(playerInput, actionPath, ResolvePromptGroup(playerInput));
    }

    /// <summary>
    /// TMP-friendly fragment: <c>&lt;b&gt;[Label]&lt;/b&gt;</c> with the binding text inside brackets.
    /// </summary>
    public static string BoldBracketLabel(PlayerInput playerInput, string actionPath, string schemeGroup)
    {
        return $"<b>[{BindingLabel(playerInput, actionPath, schemeGroup)}]</b>";
    }
}
