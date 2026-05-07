using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Art Store minigame: the menu opens when the player enters the station. While open, Fire 1 = part-time shift,
/// Fire 2 = full-time shift, Fire 4 (Y) = close without working. Uses the same board + overlay path as
/// <see cref="GroceryStationInteraction"/> (<see cref="PlayerTransactionFeedback.ShowGroceryMenuOverlay"/>).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ArtStoreStationInteraction : MonoBehaviour
{
    const string ActionFire1 = "Player/Fire 1";
    const string ActionFire2 = "Player/Fire 2";
    const string ActionFire4 = "Player/Fire 4";

    [SerializeField] ResourceBank bank;

    [Header("Menu")]
    [Tooltip("Art Store minigame image (Sprite import: 2D and UI).")]
    [SerializeField] Sprite menuArtSprite;
    [Tooltip("Optional if the PNG is a Default texture instead of a Sprite.")]
    [SerializeField] Texture2D menuArt;

    [SerializeField] Resource moneyResource;
    [SerializeField] Resource energyResource;
    [SerializeField] Resource networkResource;

    [Header("Part-time shift (Fire 1 when menu is open)")]
    [SerializeField] int partTimeEnergyCost = 15;
    [SerializeField] int partTimeCoinReward = 20;
    [SerializeField] int partTimeNetworkReward = 1;

    [Header("Full-time shift (Fire 2 when menu is open)")]
    [SerializeField] int fullTimeEnergyCost = 20;
    [SerializeField] int fullTimeCoinReward = 25;
    [SerializeField] int fullTimeNetworkReward = 1;

    [SerializeField] string stationTitle = "Art Store";

    private sealed class ArtHooks
    {
        public Action<InputAction.CallbackContext> Fire1;
        public Action<InputAction.CallbackContext> Fire2;
        public Action<InputAction.CallbackContext> Fire4;
        public UnityAction FallbackA;
        public UnityAction FallbackB;
        public UnityAction FallbackY;
        public bool UsedInputActions;
    }

    private readonly Dictionary<playerController, ArtHooks> _hooks = new();
    private readonly Dictionary<playerController, bool> _menuOpen = new();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!StartScreenManager.IsGameplayStarted) return;

        var pc = other.GetComponentInParent<playerController>();
        if (pc == null || _hooks.ContainsKey(pc)) return;
        if (bank == null)
        {
            Debug.LogError($"{name}: ArtStoreStationInteraction has no ResourceBank assigned.", this);
            return;
        }

        Debug.Log($"{name}: player {pc.name} entered art store zone (station: {stationTitle}).", this);

        _menuOpen[pc] = false;

        var hook = new ArtHooks();
        var pi = PlayerResourceBindingPrompts.ResolvePlayerInput(pc);

        if (pi != null && pi.actions != null)
        {
            InputAction f1 = pi.actions.FindAction(ActionFire1, throwIfNotFound: false);
            InputAction f2 = pi.actions.FindAction(ActionFire2, throwIfNotFound: false);
            InputAction f4 = pi.actions.FindAction(ActionFire4, throwIfNotFound: false);

            if (f1 != null && f2 != null && f4 != null)
            {
                hook.Fire1 = _ => OnPressFire1(pc);
                hook.Fire2 = _ => OnPressFire2(pc);
                hook.Fire4 = _ => OnPressFire4(pc);
                f1.performed += hook.Fire1;
                f2.performed += hook.Fire2;
                f4.performed += hook.Fire4;
                hook.UsedInputActions = true;
            }
        }

        if (!hook.UsedInputActions)
        {
            hook.FallbackA = () => OnPressFire1(pc);
            hook.FallbackB = () => OnPressFire2(pc);
            hook.FallbackY = () => OnPressFire4(pc);
            pc.onPlayerButton_A.AddListener(hook.FallbackA);
            pc.onPlayerButton_B.AddListener(hook.FallbackB);
            pc.onPlayerButton_Y.AddListener(hook.FallbackY);
        }

        _hooks[pc] = hook;

        var ptf = PlayerTransactionFeedback.Instance;
        if (ptf == null)
        {
            Debug.LogError(
                "ArtStoreStationInteraction: PlayerTransactionFeedback not found in scene. Add it under your UI Canvas (player boards).",
                this);
            return;
        }

        int ui = PlayerTransactionFeedback.BoardIndexForPlayer(pc);
        _menuOpen[pc] = true;
        ptf.ShowGroceryMenuOverlay(ui, menuArt, menuArtSprite);
        ptf.SetPlayerBoardMessage(ui, BoardLineMenuOpen());
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var pc = other.GetComponentInParent<playerController>();
        if (pc == null) return;

        if (_hooks.TryGetValue(pc, out var hook))
        {
            var pi = PlayerResourceBindingPrompts.ResolvePlayerInput(pc);
            if (hook.UsedInputActions && pi != null && pi.actions != null)
            {
                UnsubPerformed(pi.actions, ActionFire1, hook.Fire1);
                UnsubPerformed(pi.actions, ActionFire2, hook.Fire2);
                UnsubPerformed(pi.actions, ActionFire4, hook.Fire4);
            }
            else
            {
                if (hook.FallbackA != null) pc.onPlayerButton_A.RemoveListener(hook.FallbackA);
                if (hook.FallbackB != null) pc.onPlayerButton_B.RemoveListener(hook.FallbackB);
                if (hook.FallbackY != null) pc.onPlayerButton_Y.RemoveListener(hook.FallbackY);
            }

            _hooks.Remove(pc);
        }

        _menuOpen.Remove(pc);
        int ui = PlayerTransactionFeedback.BoardIndexForPlayer(pc);
        var ptf = PlayerTransactionFeedback.Instance;
        if (ptf == null) return;
        ptf.HideGroceryMenuOverlay(ui);
        ptf.HideStationPrompt(ui);
    }

    private static void UnsubPerformed(InputActionAsset asset, string actionPath, Action<InputAction.CallbackContext> cb)
    {
        if (cb == null) return;
        var a = asset.FindAction(actionPath, throwIfNotFound: false);
        if (a != null)
            a.performed -= cb;
    }

    /// <summary>Fire 1: show menu again when closed; part-time shift when open.</summary>
    private void OnPressFire1(playerController pc)
    {
        if (pc == null) return;
        if (!_menuOpen.TryGetValue(pc, out bool open) || !open)
        {
            _menuOpen[pc] = true;
            int ui = PlayerTransactionFeedback.BoardIndexForPlayer(pc);
            var ptf = PlayerTransactionFeedback.Instance;
            if (ptf == null) return;
            ptf.ShowGroceryMenuOverlay(ui, menuArt, menuArtSprite);
            ptf.SetPlayerBoardMessage(ui, BoardLineMenuOpen());
            return;
        }

        TryPartTimeShift(pc);
    }

    /// <summary>Fire 2: full-time shift (menu must be open).</summary>
    private void OnPressFire2(playerController pc)
    {
        if (pc == null || !MenuIsOpen(pc)) return;
        TryFullTimeShift(pc);
    }

    /// <summary>Fire 4 (Y): close menu without taking a shift.</summary>
    private void OnPressFire4(playerController pc)
    {
        if (pc == null || !MenuIsOpen(pc)) return;
        CloseMenuAndShowClosedPrompt(pc);
    }

    private bool MenuIsOpen(playerController pc)
    {
        return _menuOpen.TryGetValue(pc, out bool v) && v;
    }

    private void TryPartTimeShift(playerController pc)
    {
        TryShift(pc, partTimeEnergyCost, partTimeCoinReward, partTimeNetworkReward);
    }

    private void TryFullTimeShift(playerController pc)
    {
        TryShift(pc, fullTimeEnergyCost, fullTimeCoinReward, fullTimeNetworkReward);
    }

    private void TryShift(playerController pc, int energyCost, int coinReward, int networkReward)
    {
        if (energyResource == null || moneyResource == null || networkResource == null)
        {
            Debug.LogError($"{name}: Assign Energy, Money, and Network Resource assets on ArtStoreStationInteraction.", this);
            return;
        }

        var costs = new List<ResourceCost>
        {
            new ResourceCost { resource = energyResource, amount = energyCost }
        };
        var rewards = new List<ResourceCost>
        {
            new ResourceCost { resource = moneyResource, amount = coinReward },
            new ResourceCost { resource = networkResource, amount = networkReward }
        };

        int ui = PlayerTransactionFeedback.BoardIndexForPlayer(pc);
        var ptf = PlayerTransactionFeedback.Instance;
        if (ptf == null || bank == null) return;

        if (!bank.CanAfford(costs))
        {
            ptf.ShowInsufficientFeedback(ui, costs, bank);
            return;
        }

        if (!bank.TrySpendAll(costs))
            return;

        foreach (var r in rewards)
            if (r.resource != null)
                bank.Add(r.resource, r.amount);

        PlayerGameStats.RecordTransaction(pc, stationTitle, costs, rewards);
        ptf.ShowTransaction(ui, costs, rewards);
        CloseMenuAndShowClosedPrompt(pc);
    }

    private void CloseMenuAndShowClosedPrompt(playerController pc)
    {
        _menuOpen[pc] = false;
        int ui = PlayerTransactionFeedback.BoardIndexForPlayer(pc);
        var ptf = PlayerTransactionFeedback.Instance;
        if (ptf == null) return;
        ptf.HideGroceryMenuOverlay(ui);
        ptf.SetPlayerBoardMessage(ui, BoardLineClosedPrompt(pc));
    }

    private string BoardLineClosedPrompt(playerController pc)
    {
        var pi = PlayerResourceBindingPrompts.ResolvePlayerInput(pc);
        if (pi != null && pi.actions != null)
        {
            string g = PlayerResourceBindingPrompts.ResolvePromptGroup(pi);
            string open = PlayerResourceBindingPrompts.BoldBracketLabel(pi, PlayerResourceBindingPrompts.ActionFire1, g);
            return $"<b>{stationTitle}</b>\n{open} Show menu";
        }

        if (PlayerResourceBindingPrompts.IsKeyboardP2Player(pc))
            return $"<b>{stationTitle}</b>\n<b>[E]</b> or <b>[Insert]</b> Show menu";
        return $"<b>{stationTitle}</b>\n[A] / [E] Show menu";
    }

    private string BoardLineMenuOpen()
    {
        // Menu overlay shows costs/options; board only needs a short cue.
        return $"<b>{stationTitle}</b>\nSelect a Shift";
    }
}
