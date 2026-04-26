using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Pauses the game until any key / face button. Keeps the start / instruction overlay the topmost UI
/// sibling so it covers <c>playerBoards</c>, transaction popups, and other canvas content.
/// </summary>
[DefaultExecutionOrder(500)]
public class StartScreenManager : MonoBehaviour
{
    public GameObject startScreenOverlay;
    [SerializeField] private Sprite startScreenSprite;
    [SerializeField] private Sprite instructionScreenSprite;

    private bool gameStarted = false;
    private bool waitingForAdvanceRelease = false;
    private Image overlayImage;
    private enum IntroStage { StartScreen, InstructionScreen }
    private IntroStage currentStage = IntroStage.StartScreen;

    private IEnumerator Start()
    {
        // This component must be on an active GameObject. If startScreenOverlay is inactive in the
        // hierarchy, Unity will not run Start() here.
        Time.timeScale = 0f;
        PlayerGameStats.ResetAll();

        if (startScreenOverlay != null)
        {
            overlayImage = startScreenOverlay.GetComponent<Image>();
            if (overlayImage != null && startScreenSprite != null)
                overlayImage.sprite = startScreenSprite;

            startScreenOverlay.SetActive(true);
            // Sibling order = draw order (later = in front). Scene order may place playerBoards after
            // the start screen; wait one frame then move overlay to the top.
            yield return null;
            if (startScreenOverlay != null)
                startScreenOverlay.transform.SetAsLastSibling();
        }
    }

    private void LateUpdate()
    {
        // Other systems (e.g. player feedback) may add last-sibling UI on the same canvas; keep the
        // intro overlay in front for both start and instruction stages until gameplay begins.
        if (gameStarted || startScreenOverlay == null || !startScreenOverlay.activeInHierarchy) return;
        startScreenOverlay.transform.SetAsLastSibling();
    }

    void Update()
    {
        if (gameStarted) return;

        bool xPressed = IsAdvancePressed();
        if (!xPressed)
            waitingForAdvanceRelease = false;

        if (xPressed && !waitingForAdvanceRelease)
        {
            waitingForAdvanceRelease = true;

            if (currentStage == IntroStage.StartScreen)
            {
                currentStage = IntroStage.InstructionScreen;
                if (overlayImage != null && instructionScreenSprite != null)
                    overlayImage.sprite = instructionScreenSprite;
                return;
            }

            gameStarted = true;
            if (startScreenOverlay != null)
                startScreenOverlay.SetActive(false);

            Time.timeScale = 1f;
            GameManager.Instance?.StartGame();
            EventCardManager.Instance?.StartTimer();
            PlayerTransactionFeedback.Instance?.OnGameStarted();
            PlayerInstructionDisplay.Instance?.OnGameStarted();
        }
    }

    private static bool IsAdvancePressed()
    {
        bool keyboard = Keyboard.current != null && Keyboard.current.xKey.wasPressedThisFrame;
        bool gamepad = Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame;
        return keyboard || gamepad;
    }
}
