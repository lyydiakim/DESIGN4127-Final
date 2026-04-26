using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Big Grocery: the menu opens when the player enters the station trigger; Fire 1 closes the menu when open;
/// Fire 2 combo; Fire 3 meal. Board copy uses <see cref="PlayerResourceBindingPrompts"/>.
/// Place on the same GameObject as the station trigger collider.
/// Uses Input System actions (Player/Fire 1–3) so it works even when playerController never invokes onPlayerButton_X.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GroceryStationInteraction : MonoBehaviour
{
    const string ActionFire1 = "Player/Fire 1";
    const string ActionFire2 = "Player/Fire 2";
    const string ActionFire3 = "Player/Fire 3";

    [SerializeField] ResourceBank bank;

    [Header("Menu")]
    [Tooltip("Use this when the PNG is imported as Sprite (2D and UI). Drag the sprite asset from the Project window.")]
    [SerializeField] Sprite menuArtSprite;
    [Tooltip("Use this when the PNG import type is Default (Texture2D). Leave empty if Menu Art Sprite is set.")]
    [SerializeField] Texture2D menuArt;

    [SerializeField] Resource moneyResource;
    [SerializeField] Resource energyResource;

    [Header("Combo Plate")]
    [SerializeField] int comboCoinCost = 10;
    [SerializeField] int comboEnergyReward = 9;

    [Header("Meal Deal")]
    [SerializeField] int mealCoinCost = 8;
    [SerializeField] int mealEnergyReward = 7;

    [SerializeField] string stationTitle = "Big Grocery";

    private sealed class GroceryHooks
    {
        public Action<InputAction.CallbackContext> Fire1;
        public Action<InputAction.CallbackContext> Fire2;
        public Action<InputAction.CallbackContext> Fire3;
        public UnityAction FallbackA;
        public UnityAction FallbackB;
        public UnityAction FallbackX;
        public bool UsedInputActions;
    }

    private readonly Dictionary<playerController, GroceryHooks> _hooks = new();
    private readonly Dictionary<playerController, bool> _menuOpen = new();

    private void OnTriggerEnter2D(Collider2D other)
    {
        var pc = other.GetComponentInParent<playerController>();
        if (pc == null || _hooks.ContainsKey(pc)) return;
        if (bank == null)
        {
            Debug.LogError($"{name}: GroceryStationInteraction has no ResourceBank assigned.", this);
            return;
        }

        Debug.Log($"{name}: player {pc.name} entered grocery zone (station: {stationTitle}).", this);

        _menuOpen[pc] = false;

        var hook = new GroceryHooks();
        var pi = PlayerResourceBindingPrompts.ResolvePlayerInput(pc);

        if (pi != null && pi.actions != null)
        {
            InputAction f1 = pi.actions.FindAction(ActionFire1, throwIfNotFound: false);
            InputAction f2 = pi.actions.FindAction(ActionFire2, throwIfNotFound: false);
            InputAction f3 = pi.actions.FindAction(ActionFire3, throwIfNotFound: false);

            if (f1 != null && f2 != null && f3 != null)
            {
                hook.Fire1 = _ => OnPressA(pc);
                hook.Fire2 = _ => OnPressB(pc);
                hook.Fire3 = _ => OnPressX(pc);
                f1.performed += hook.Fire1;
                f2.performed += hook.Fire2;
                // Fire 3 is often bound with a Hold interaction on gamepad (labor); `performed` then
                // does not fire on a quick tap. Use `started` so Meal Deal / X matches a single press.
                f3.started += hook.Fire3;
                hook.UsedInputActions = true;
            }
        }

        if (!hook.UsedInputActions)
        {
            hook.FallbackA = () => OnPressA(pc);
            hook.FallbackB = () => OnPressB(pc);
            hook.FallbackX = () => OnPressX(pc);
            pc.onPlayerButton_A.AddListener(hook.FallbackA);
            pc.onPlayerButton_B.AddListener(hook.FallbackB);
            pc.onPlayerButton_X.AddListener(hook.FallbackX);
        }

        _hooks[pc] = hook;

        var ptf = PlayerTransactionFeedback.Instance;
        if (ptf == null)
        {
            Debug.LogError(
                "GroceryStationInteraction: PlayerTransactionFeedback not found in scene. Add it under your UI Canvas (player boards).",
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
                UnsubStarted(pi.actions, ActionFire3, hook.Fire3);
            }
            else
            {
                if (hook.FallbackA != null) pc.onPlayerButton_A.RemoveListener(hook.FallbackA);
                if (hook.FallbackB != null) pc.onPlayerButton_B.RemoveListener(hook.FallbackB);
                if (hook.FallbackX != null) pc.onPlayerButton_X.RemoveListener(hook.FallbackX);
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

    private static void UnsubStarted(InputActionAsset asset, string actionPath, Action<InputAction.CallbackContext> cb)
    {
        if (cb == null) return;
        var a = asset.FindAction(actionPath, throwIfNotFound: false);
        if (a != null)
            a.started -= cb;
    }

    private void OnPressA(playerController pc)
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

        // Menu already open: A cancels and closes the overlay (purchases are B / X only).
        CloseGroceryMenuAndShowOrderPrompt(pc);
    }

    private void OnPressB(playerController pc)
    {
        if (pc == null || !MenuIsOpen(pc)) return;
        TryPurchase(pc, comboCoinCost, comboEnergyReward);
    }

    private void OnPressX(playerController pc)
    {
        if (pc == null || !MenuIsOpen(pc)) return;
        TryPurchase(pc, mealCoinCost, mealEnergyReward);
    }

    private bool MenuIsOpen(playerController pc)
    {
        return _menuOpen.TryGetValue(pc, out bool v) && v;
    }

    private void TryPurchase(playerController pc, int coinCost, int energyGain)
    {
        if (moneyResource == null || energyResource == null)
        {
            Debug.LogError($"{name}: Assign Money and Energy Resource assets on GroceryStationInteraction.", this);
            return;
        }

        var costs = new List<ResourceCost>
        {
            new ResourceCost { resource = moneyResource, amount = coinCost }
        };
        var rewards = new List<ResourceCost>
        {
            new ResourceCost { resource = energyResource, amount = energyGain }
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
        CloseGroceryMenuAndShowOrderPrompt(pc);
    }

    /// <summary>Hides menu art and restores the station prompt. Call after purchase or when player presses A to cancel.</summary>
    private void CloseGroceryMenuAndShowOrderPrompt(playerController pc)
    {
        _menuOpen[pc] = false;
        int ui = PlayerTransactionFeedback.BoardIndexForPlayer(pc);
        var ptf = PlayerTransactionFeedback.Instance;
        if (ptf == null) return;
        ptf.HideGroceryMenuOverlay(ui);
        ptf.SetPlayerBoardMessage(ui, BoardLineOrderPrompt(pc));
    }

    private string BoardLineOrderPrompt(playerController pc)
    {
        var pi = pc != null ? PlayerResourceBindingPrompts.ResolvePlayerInput(pc) : null;
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
        return $"<b>{stationTitle}</b>\nSelect a Meal";
    }
}
