using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Farmers Market minigame: Fire 1 opens the overlay. While open, Fire 1 = job board A (energy to network),
/// Fire 2 = job board B, Fire 4 (Y) closes.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FarmersMarketStationInteraction : MonoBehaviour
{
    const string ActionFire1 = "Player/Fire 1";
    const string ActionFire2 = "Player/Fire 2";
    const string ActionFire4 = "Player/Fire 4";

    [SerializeField] ResourceBank bank;

    [Header("Menu")]
    [SerializeField] Sprite menuArtSprite;
    [SerializeField] Texture2D menuArt;

    [SerializeField] Resource energyResource;
    [SerializeField] Resource networkResource;

    [Header("Open job board — A (Fire 1 when menu open)")]
    [SerializeField] int optionAEnergyCost = 10;
    [SerializeField] int optionANetworkReward = 1;

    [Header("Open job board — B (Fire 2 when menu open)")]
    [SerializeField] int optionBEnergyCost = 8;
    [SerializeField] int optionBNetworkReward = 1;

    [SerializeField] string stationTitle = "Farmers Market";

    private sealed class MarketHooks
    {
        public Action<InputAction.CallbackContext> Fire1;
        public Action<InputAction.CallbackContext> Fire2;
        public Action<InputAction.CallbackContext> Fire4;
        public UnityAction FallbackA;
        public UnityAction FallbackB;
        public UnityAction FallbackY;
        public bool UsedInputActions;
    }

    private readonly Dictionary<playerController, MarketHooks> _hooks = new();
    private readonly Dictionary<playerController, bool> _menuOpen = new();

    private void OnTriggerEnter2D(Collider2D other)
    {
        var pc = other.GetComponentInParent<playerController>();
        if (pc == null || _hooks.ContainsKey(pc)) return;
        if (bank == null)
        {
            Debug.LogError($"{name}: FarmersMarketStationInteraction has no ResourceBank assigned.", this);
            return;
        }

        Debug.Log($"{name}: player {pc.name} entered farmers market zone (station: {stationTitle}).", this);

        _menuOpen[pc] = false;

        var hook = new MarketHooks();
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
                "FarmersMarketStationInteraction: PlayerTransactionFeedback not found in scene. Add it under your UI Canvas (player boards).",
                this);
            return;
        }

        int ui = PlayerTransactionFeedback.BoardIndexForPlayer(pc);
        ptf.SetPlayerBoardMessage(ui, BoardLineClosedPrompt(pc));
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

        TryWork(pc, optionAEnergyCost, optionANetworkReward);
    }

    private void OnPressFire2(playerController pc)
    {
        if (pc == null || !MenuIsOpen(pc)) return;
        TryWork(pc, optionBEnergyCost, optionBNetworkReward);
    }

    private void OnPressFire4(playerController pc)
    {
        if (pc == null || !MenuIsOpen(pc)) return;
        CloseMenuAndShowClosedPrompt(pc);
    }

    private bool MenuIsOpen(playerController pc)
    {
        return _menuOpen.TryGetValue(pc, out bool v) && v;
    }

    private void TryWork(playerController pc, int energyCost, int networkReward)
    {
        if (energyResource == null || networkResource == null)
        {
            Debug.LogError($"{name}: Assign Energy and Network resources on FarmersMarketStationInteraction.", this);
            return;
        }

        var costs = new List<ResourceCost>
        {
            new ResourceCost { resource = energyResource, amount = energyCost }
        };
        var rewards = new List<ResourceCost>
        {
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
            return $"<b>{stationTitle}</b>\n{open} See Open Job Board";
        }

        if (PlayerResourceBindingPrompts.IsKeyboardP2Player(pc))
            return $"<b>{stationTitle}</b>\n<b>[E]</b> or <b>[Insert]</b> See Open Job Board";
        return $"<b>{stationTitle}</b>\n[A] / [E] See Open Job Board";
    }

    private string BoardLineMenuOpen()
    {
        return $"<b>{stationTitle}</b>\nSelect an option";
    }
}
