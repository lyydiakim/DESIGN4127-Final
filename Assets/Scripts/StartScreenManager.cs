using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pauses the game until any key / face button. Ensures the start overlay is the topmost UI sibling
/// so it covers <c>playerBoards</c> and other canvas content used as an instructional splash.
/// </summary>
[DefaultExecutionOrder(500)]
public class StartScreenManager : MonoBehaviour
{
    public GameObject startScreenOverlay;

    private bool gameStarted = false;

    private IEnumerator Start()
    {
        // This component must be on an active GameObject. If startScreenOverlay is inactive in the
        // hierarchy, Unity will not run Start() here.
        Time.timeScale = 0f;

        if (startScreenOverlay != null)
        {
            startScreenOverlay.SetActive(true);
            // Sibling order = draw order (later = in front). Scene order may place playerBoards after
            // the start screen; wait one frame then move overlay to the top.
            yield return null;
            if (startScreenOverlay != null)
                startScreenOverlay.transform.SetAsLastSibling();
        }
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
