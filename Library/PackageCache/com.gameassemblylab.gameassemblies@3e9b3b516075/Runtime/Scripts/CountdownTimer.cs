using UnityEngine;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    // Total time for the countdown (in seconds)
    public float startingTime = 300f; // e.g., 300 seconds (5 minutes)
    private float currentTime;

    // UI Text element using TextMesh Pro
    [SerializeField] private TMP_Text timerText;

    // Public property to check if the timer is running
    public bool IsTimerRunning { get; private set; }

    // Blink speed for the last 10 seconds
    [SerializeField] private float blinkSpeed = 5f; // Adjust for faster or slower blinking

    // Exposed colors for blinking effect
    [SerializeField] private Color primaryBlinkColor = Color.red;
    [SerializeField] private Color secondaryBlinkColor = Color.white;

    // Time threshold when blinking starts
    [SerializeField] private float blinkThreshold = 10f;

    void Start()
    {
        //if (LevelManager.Instance != null)
        //{
        //startingTime = LevelManager.getTimeToComplete();
        //}
        currentTime = startingTime;
        IsTimerRunning = true;
        UpdateTimerDisplay();
    }

    void Update()
    {
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.CurrentState == GameState.Playing)
            {
                IsTimerRunning = true;
            } else
            {
                IsTimerRunning = false;
            }
        }

        if (IsTimerRunning)
        {
            currentTime = LevelManager.getTimeRemaining();
            //Debug.Log("currentTime" +  currentTime);
            /*
            currentTime -= Time.deltaTime;
            if (currentTime <= 0f)
            {
                currentTime = 0f;
                IsTimerRunning = false;
            }
            */
            UpdateTimerDisplay();
        }
    }

    // Formats and updates the timer text in "MM:SS" format
    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        // Check if we're in the last X seconds (configurable via blinkThreshold)
        if (currentTime <= blinkThreshold)
        {
            // Use PingPong to alternate between the two colors
            float t = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            timerText.color = (t < 0.5f) ? primaryBlinkColor : secondaryBlinkColor;
        } else
        {
            // Default color for the timer (using the secondary color as the normal color)
            timerText.color = secondaryBlinkColor;
        }
    }
}