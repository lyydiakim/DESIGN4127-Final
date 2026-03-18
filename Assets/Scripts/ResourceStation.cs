using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Requires a Collider2D set to Is Trigger on this GameObject.
// Any player that walks into the trigger can activate this station
// by pressing their A button. No manual wiring needed per station.
[RequireComponent(typeof(Collider2D))]
public class ResourceStation : MonoBehaviour
{
    [SerializeField] ResourceBank bank;
    [Tooltip("Display name shown on the player board when near this station. Defaults to the GameObject name.")]
    [SerializeField] string stationDisplayName;
    [SerializeField] List<ResourceCost> costs;
    [SerializeField] List<ResourceCost> rewards;

    [Tooltip("Fired once every time a trade succeeds at this station.")]
    [SerializeField] private UnityEngine.Events.UnityEvent onSuccessfulTrade;

    private string DisplayName => string.IsNullOrWhiteSpace(stationDisplayName)
        ? gameObject.name
        : stationDisplayName;

    // Each player gets its own closure so we know who pressed A.
    private readonly Dictionary<playerController, UnityAction> _listeners =
        new Dictionary<playerController, UnityAction>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        playerController pc = other.GetComponentInParent<playerController>();
        if (pc == null || _listeners.ContainsKey(pc)) return;

        Debug.Log($"{gameObject.name}: player {pc.playerID} entered, listening for button A");

        // Capture pc in a closure so the callback knows which player fired it.
        UnityAction action = () => OnPlayerInteract(pc);
        _listeners[pc] = action;
        pc.onPlayerButton_A.AddListener(action);

        // Show station info on this player's board.
        PlayerTransactionFeedback.Instance?.ShowStationPrompt(
            pc.playerID, DisplayName, costs, rewards);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        playerController pc = other.GetComponentInParent<playerController>();
        if (pc == null || !_listeners.TryGetValue(pc, out var action)) return;

        pc.onPlayerButton_A.RemoveListener(action);
        _listeners.Remove(pc);

        // Clear station info from this player's board.
        PlayerTransactionFeedback.Instance?.HideStationPrompt(pc.playerID);
    }

    private void OnPlayerInteract(playerController pc)
    {
        Debug.Log($"{gameObject.name}: OnPlayerInteract called by player {pc?.playerID}");

        if (bank.TrySpendAll(costs))
        {
            foreach (var reward in rewards)
                bank.Add(reward.resource, reward.amount);

            if (pc != null)
                PlayerTransactionFeedback.Instance?.ShowTransaction(pc.playerID, costs, rewards);

            ActivateStation();
        }
        else
        {
            if (pc != null)
                PlayerTransactionFeedback.Instance?.ShowInsufficientFeedback(pc.playerID, costs, bank);
        }
    }

    void ActivateStation()
    {
        Debug.Log($"{gameObject.name}: exchange complete.");
        onSuccessfulTrade?.Invoke();
    }
}
