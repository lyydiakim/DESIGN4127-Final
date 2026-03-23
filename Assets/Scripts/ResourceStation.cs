using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

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

    /// <summary>
    /// Big Grocery / Art Store / Deli / Factory / Farmers Market / Pet Shelter (and similar) may use a child trigger + minigame interaction.
    /// The parent still receives 2D trigger callbacks when the Rigidbody2D is on the parent, so a stray
    /// <see cref="ResourceStation"/> on the root would fight that flow. Defer entirely if present.
    /// </summary>
    private GroceryStationInteraction _deferToGrocery;
    private ArtStoreStationInteraction _deferToArtStore;
    private HomeStationInteraction _deferToHome;
    private ParkStationInteraction _deferToPark;
    private DeliStationInteraction _deferToDeli;
    private FactoryStationInteraction _deferToFactory;
    private FarmersMarketStationInteraction _deferToFarmersMarket;
    private PetShelterStationInteraction _deferToPetShelter;

    private void Awake()
    {
        _deferToGrocery = GetComponentInChildren<GroceryStationInteraction>(true);
        _deferToArtStore = GetComponentInChildren<ArtStoreStationInteraction>(true);
        _deferToHome = GetComponentInChildren<HomeStationInteraction>(true);
        _deferToPark = GetComponentInChildren<ParkStationInteraction>(true);
        _deferToDeli = GetComponentInChildren<DeliStationInteraction>(true);
        _deferToFactory = GetComponentInChildren<FactoryStationInteraction>(true);
        _deferToFarmersMarket = GetComponentInChildren<FarmersMarketStationInteraction>(true);
        _deferToPetShelter = GetComponentInChildren<PetShelterStationInteraction>(true);
    }

    private bool DeferToMinigameStation =>
        _deferToGrocery != null || _deferToArtStore != null || _deferToHome != null || _deferToPark != null
        || _deferToDeli != null || _deferToFactory != null || _deferToFarmersMarket != null
        || _deferToPetShelter != null;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (DeferToMinigameStation) return;

        playerController pc = other.GetComponentInParent<playerController>();
        if (pc == null || _listeners.ContainsKey(pc)) return;

        Debug.Log($"{gameObject.name}: player {pc.playerID} entered, listening for button A");

        // Capture pc in a closure so the callback knows which player fired it.
        UnityAction action = () => OnPlayerInteract(pc);
        _listeners[pc] = action;
        pc.onPlayerButton_A.AddListener(action);

        // Show station info on this player's board (trade key / face button from bindings when available).
        PlayerTransactionFeedback.Instance?.ShowStationPrompt(
            PlayerTransactionFeedback.BoardIndexForPlayer(pc), DisplayName, costs, rewards,
            PlayerResourceBindingPrompts.ResolvePlayerInput(pc));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (DeferToMinigameStation) return;

        playerController pc = other.GetComponentInParent<playerController>();
        if (pc == null || !_listeners.TryGetValue(pc, out var action)) return;

        pc.onPlayerButton_A.RemoveListener(action);
        _listeners.Remove(pc);

        // Clear station info from this player's board.
        PlayerTransactionFeedback.Instance?.HideStationPrompt(PlayerTransactionFeedback.BoardIndexForPlayer(pc));
    }

    private void OnPlayerInteract(playerController pc)
    {
        Debug.Log($"{gameObject.name}: OnPlayerInteract called by player {pc?.playerID}");

        int ui = PlayerTransactionFeedback.BoardIndexForPlayer(pc);

        if (bank.TrySpendAll(costs))
        {
            foreach (var reward in rewards)
                bank.Add(reward.resource, reward.amount);

            if (pc != null)
                PlayerTransactionFeedback.Instance?.ShowTransaction(ui, costs, rewards);

            ActivateStation();
        }
        else
        {
            if (pc != null)
                PlayerTransactionFeedback.Instance?.ShowInsufficientFeedback(ui, costs, bank);
        }
    }

    void ActivateStation()
    {
        Debug.Log($"{gameObject.name}: exchange complete.");
        onSuccessfulTrade?.Invoke();
    }
}
