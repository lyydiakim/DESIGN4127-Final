using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// When two players share one keyboard, player 0 uses the "Keyboard" scheme (WASD + E/R/F/T/G)
/// and player 1 uses "KeyboardP2" (Arrow keys + Insert/Home/PageUp etc.) so inputs do not overlap.
/// Attach is automatic: runs after scene load and adds this to the <see cref="PlayerInputManager"/> object if present.
/// <para>
/// <see cref="PlayerInputManager"/>'s default join mode listens for <i>unpaired</i> devices. One physical keyboard
/// is a single device, so after player 0 pairs it there is no second keyboard to "join" with — arrow keys alone
/// will not add player 1. While at least one player is in the game and player index 1 is free, pressing any
/// arrow key (or F2) calls <see cref="PlayerInputManager.JoinPlayer"/> to spawn player 1 on the same keyboard
/// with the KeyboardP2 scheme.
/// </para>
/// </summary>
[DefaultExecutionOrder(-100)]
public sealed class PlayerKeyboardSplitJoin : MonoBehaviour
{
    const string SchemeKeyboard = "Keyboard";
    const string SchemeKeyboardP2 = "KeyboardP2";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var pim = Object.FindFirstObjectByType<PlayerInputManager>();
        if (pim == null) return;
        if (pim.gameObject.GetComponent<PlayerKeyboardSplitJoin>() != null) return;
        pim.gameObject.AddComponent<PlayerKeyboardSplitJoin>();
    }

    void OnEnable()
    {
        if (PlayerInputManager.instance != null)
            PlayerInputManager.instance.onPlayerJoined += OnPlayerJoined;

        foreach (var pi in PlayerInput.all)
            ApplySecondPlayerScheme(pi);
    }

    void OnDisable()
    {
        if (PlayerInputManager.instance != null)
            PlayerInputManager.instance.onPlayerJoined -= OnPlayerJoined;
    }

    void OnPlayerJoined(PlayerInput player)
    {
        ApplySecondPlayerScheme(player);
    }

    void Update()
    {
        var pim = PlayerInputManager.instance;
        if (pim == null || !pim.joiningEnabled) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (pim.maxPlayerCount >= 0 && PlayerInput.all.Count >= pim.maxPlayerCount) return;

        // Ensure keyboard player 0 can join even when join behavior is manual.
        if (PlayerInput.all.Count < 1)
        {
            bool joinP1 =
                kb.wKey.wasPressedThisFrame ||
                kb.aKey.wasPressedThisFrame ||
                kb.sKey.wasPressedThisFrame ||
                kb.dKey.wasPressedThisFrame ||
                kb.upArrowKey.wasPressedThisFrame ||
                kb.downArrowKey.wasPressedThisFrame ||
                kb.leftArrowKey.wasPressedThisFrame ||
                kb.rightArrowKey.wasPressedThisFrame ||
                kb.eKey.wasPressedThisFrame ||
                kb.xKey.wasPressedThisFrame ||
                kb.f2Key.wasPressedThisFrame;

            if (!joinP1) return;

            var joinedP1 = pim.JoinPlayer(playerIndex: 0, controlScheme: SchemeKeyboard, pairWithDevice: kb);
            if (joinedP1 == null)
                Debug.LogWarning("PlayerKeyboardSplitJoin: Could not join player 0 with Keyboard.", this);
            return;
        }

        if (PlayerWithIndexExists(1)) return;

        var joinP2 =
            kb.upArrowKey.wasPressedThisFrame ||
            kb.downArrowKey.wasPressedThisFrame ||
            kb.leftArrowKey.wasPressedThisFrame ||
            kb.rightArrowKey.wasPressedThisFrame ||
            kb.f2Key.wasPressedThisFrame;

        if (!joinP2) return;

        var joined = pim.JoinPlayer(playerIndex: 1, controlScheme: SchemeKeyboardP2, pairWithDevice: kb);
        if (joined == null)
            Debug.LogWarning("PlayerKeyboardSplitJoin: Could not join player 1 with KeyboardP2 (check max players and joining).", this);
    }

    static bool PlayerWithIndexExists(int playerIndex)
    {
        foreach (var pi in PlayerInput.all)
        {
            if (pi != null && pi.playerIndex == playerIndex)
                return true;
        }

        return false;
    }

    static void ApplySecondPlayerScheme(PlayerInput player)
    {
        if (player == null || player.playerIndex != 1) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        // Only force KeyboardP2 when this player is actually keyboard-controlled.
        // If player index 1 joined on a gamepad, switching to KeyboardP2 removes gamepad bindings.
        {
            var devices = player.devices;
            bool hasGamepad = false;
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i] is Gamepad)
                {
                    hasGamepad = true;
                    break;
                }
            }

            if (hasGamepad) return;
        }
        player.SwitchCurrentControlScheme(SchemeKeyboardP2, kb);
    }
}
