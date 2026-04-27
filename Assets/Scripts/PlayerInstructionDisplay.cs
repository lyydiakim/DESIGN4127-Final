using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shows the instruction sprite for each player index as they join,
/// and hides it again if they leave. Attach this to any persistent
/// GameObject in the scene (e.g. a Canvas or Game Manager).
///
/// In the Inspector, assign playerInstructionSprites[0] = Player 1 sprite GO,
/// [1] = Player 2, [2] = Player 3, [3] = Player 4.
/// </summary>
public class PlayerInstructionDisplay : MonoBehaviour
{
    public static PlayerInstructionDisplay Instance { get; private set; }

    [Tooltip("Assign the instruction sprite GameObjects in order: index 0 = Player 1, index 1 = Player 2, etc.")]
    [SerializeField] private GameObject[] playerInstructionSprites = new GameObject[4];

    private bool _gameStarted = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        foreach (var sprite in playerInstructionSprites)
            if (sprite != null) sprite.SetActive(false);
    }

    /// <summary>
    /// Call this when the start screen is dismissed to reveal boards for
    /// players who have already joined.
    /// </summary>
    public void OnGameStarted()
    {
        _gameStarted = true;

        foreach (var pi in PlayerInput.all)
        {
            int index = pi.playerIndex;
            if (index >= 0 && index < playerInstructionSprites.Length && playerInstructionSprites[index] != null)
                playerInstructionSprites[index].SetActive(true);
        }
    }

    private void OnEnable()
    {
        if (PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.onPlayerJoined += OnPlayerJoined;
            PlayerInputManager.instance.onPlayerLeft  += OnPlayerLeft;
        }
    }

    private void OnDisable()
    {
        if (PlayerInputManager.instance != null)
        {
            PlayerInputManager.instance.onPlayerJoined -= OnPlayerJoined;
            PlayerInputManager.instance.onPlayerLeft  -= OnPlayerLeft;
        }
    }

    private void OnPlayerJoined(PlayerInput player)
    {
        // Only reveal the board once the start screen has been dismissed.
        if (!_gameStarted) return;
        int index = player.playerIndex;
        if (index >= 0 && index < playerInstructionSprites.Length && playerInstructionSprites[index] != null)
            playerInstructionSprites[index].SetActive(true);
    }

    private void OnPlayerLeft(PlayerInput player)
    {
        int index = player.playerIndex;
        if (index >= 0 && index < playerInstructionSprites.Length && playerInstructionSprites[index] != null)
            playerInstructionSprites[index].SetActive(false);
    }

    private void Update()
    {
        // Keep instruction popups hidden until the intro flow has fully ended.
        if (!StartScreenManager.IsGameplayStarted)
        {
            for (int i = 0; i < playerInstructionSprites.Length; i++)
            {
                var go = playerInstructionSprites[i];
                if (go != null && go.activeSelf)
                    go.SetActive(false);
            }

            _gameStarted = false;
            return;
        }

        if (!_gameStarted)
            OnGameStarted();
    }
}
