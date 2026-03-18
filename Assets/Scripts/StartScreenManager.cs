using UnityEngine;
using UnityEngine.InputSystem;

public class StartScreenManager : MonoBehaviour
{
    public GameObject startScreenOverlay;

    private bool gameStarted = false;

    void Start()
    {
        if (startScreenOverlay != null)
            startScreenOverlay.SetActive(true);

        Time.timeScale = 0f;
    }

    void Update()
    {
        if (gameStarted) return;

        bool keyPressed = Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame;
        bool controllerPressed = Gamepad.current != null && (
            Gamepad.current.buttonSouth.wasPressedThisFrame ||
            Gamepad.current.buttonNorth.wasPressedThisFrame ||
            Gamepad.current.buttonEast.wasPressedThisFrame ||
            Gamepad.current.buttonWest.wasPressedThisFrame ||
            Gamepad.current.startButton.wasPressedThisFrame
        );

        if (keyPressed || controllerPressed)
        {
            gameStarted = true;

            if (startScreenOverlay != null)
                startScreenOverlay.SetActive(false);

            Time.timeScale = 1f;

            if (GameManager.Instance != null)
                GameManager.Instance.StartGame();

            EventCardManager.Instance?.StartTimer();
            PlayerTransactionFeedback.Instance?.OnGameStarted();
            PlayerInstructionDisplay.Instance?.OnGameStarted();
        }
    }
}
