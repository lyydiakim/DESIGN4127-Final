using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Forces gamepad joining to be explicit: each controller must press X (buttonWest) to join.
/// This prevents accidental joins from stick drift / other buttons and keeps one controller paired per player.
/// </summary>
[DefaultExecutionOrder(-120)]
public sealed class GamepadXJoinManager : MonoBehaviour
{
    const string SchemeJoystick = "Joystick";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var pim = Object.FindFirstObjectByType<PlayerInputManager>();
        if (pim == null) return;
        if (pim.gameObject.GetComponent<GamepadXJoinManager>() != null) return;
        pim.gameObject.AddComponent<GamepadXJoinManager>();
    }

    void Awake()
    {
        var pim = PlayerInputManager.instance;
        if (pim == null) return;

        // Require explicit, script-driven joins so only X on each gamepad can add a player.
        pim.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
        pim.EnableJoining();
    }

    void Update()
    {
        var pim = PlayerInputManager.instance;
        if (pim == null || !pim.joiningEnabled) return;

        foreach (var pad in Gamepad.all)
        {
            if (pad == null || !pad.added) continue;
            if (!pad.buttonWest.wasPressedThisFrame) continue; // X on Xbox layout
            if (IsAlreadyPaired(pad)) continue;
            if (pim.maxPlayerCount >= 0 && PlayerInput.all.Count >= pim.maxPlayerCount) break;

            // Pair this specific gamepad to a newly joined player.
            var joined = pim.JoinPlayer(playerIndex: -1, controlScheme: SchemeJoystick, pairWithDevice: pad);
            if (joined == null)
                Debug.LogWarning($"GamepadXJoinManager: Could not join player from {pad.displayName}.", this);
        }
    }

    static bool IsAlreadyPaired(InputDevice device)
    {
        foreach (var pi in PlayerInput.all)
        {
            if (pi == null) continue;
            var devices = pi.devices;
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i] == device)
                    return true;
            }
        }

        return false;
    }
}
