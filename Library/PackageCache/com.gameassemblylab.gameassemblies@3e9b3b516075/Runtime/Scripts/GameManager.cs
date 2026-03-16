using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Collections;

// Define the possible game states
public enum GameState
{
    Menu,
    Playing,
    Paused,
    Success,
    Fail,
    Results,
    Tutorial
}

public class GameManager : MonoBehaviour
{
    // Singleton instance
    public static GameManager Instance { get; private set; }

    // Current game state
    public GameState CurrentState { get; private set; } = GameState.Menu;

    // Optional event to notify listeners when the game state changes
    public delegate void GameStateChanged(GameState newState);
    public event GameStateChanged OnGameStateChanged;

    public GameObject MainMenu;
    public GameObject PauseMenu;
    public GameObject EndOfChallengeMenu;

    public GameObject tutorialMenu;
    public float timeToShowTutorial = 5.0f;
    public float tutorialCount = 0;

    public LevelManager lvlManager;
    
    private int timesStarted = 0;

    public TMP_Text finalTitle;
    public TMP_Text finalScoreTitle;
    public TMP_Text finalScore;
    public TMP_Text topScore;
    public int playingLevelID = 0;

    private int highScore_1 = 0;
    private int highScore_2 = 0;
    public string suffix = "MURAL";

    public List <GameObject> fullStars;
    //public List <int> scoreBrakets;

    public List<GameObject> LevelGroups;

    public bool debug = false;

    private void Awake()
    {
        // Ensure a single instance of GameManager persists between scenes if needed
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        } else
        {
            Destroy(gameObject);
        }

        highScore_1 = PlayerPrefs.GetInt(suffix + "_HighScore_" + 0, 0);
        highScore_2 = PlayerPrefs.GetInt(suffix + "_HighScore_" + 1, 0);
        Debug.Log("HIGHSCORE 1: " + highScore_1);
        Debug.Log("HIGHSCORE 2: " + highScore_2);
    }

    private void OnEnable()
    {
      
    }

    private void Start()
    {
        lvlManager = GameObject.FindAnyObjectByType<LevelManager>();

        // Subscribe to the event
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.onLevelComplete.AddListener(HandleLevelComplete);
            Debug.Log("SUBSCRIPTION SUCCESSFULL");
            //HandleLevelComplete();
        } else
        {
            //Debug.LogError("LEVEL MANAGER NULL");
        }

    }

    private void Update()
    {
        if(debug) Debug.Log("Current State: " +  CurrentState);
    }

    public void UpdateScore(int newScore)
    {
        if (playingLevelID == 0)
        {
            // If the new score is higher than the saved high score, update and save it
            if (newScore > highScore_1)
            {
                highScore_1 = newScore;

                // Store the new high score
                PlayerPrefs.SetInt(suffix + "_HighScore_" + 0, highScore_1);

                // Make sure data is written to disk immediately
                PlayerPrefs.Save();
            }
        } else if (playingLevelID == 1)
        {
            // If the new score is higher than the saved high score, update and save it
            if (newScore > highScore_2)
            {
                highScore_2 = newScore;

                // Store the new high score
                PlayerPrefs.SetInt(suffix + "_HighScore_" + 1, highScore_2);

                // Make sure data is written to disk immediately
                PlayerPrefs.Save();
            }
        }

        Debug.Log("HIGHSCORE 1 CHANGED TO: " + highScore_1);
        Debug.Log("HIGHSCORE 2 CHANGED TO: " + highScore_2);

    }

    public void subscribeToLevelComplete()
    {
        // Subscribe to the event
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.onLevelComplete.AddListener(HandleLevelComplete);
            Debug.Log("SUBSCRIPTION SUCCESSFULL");
            //HandleLevelComplete();
        } else
        {
            Debug.LogError("LEVEL MANAGER NULL");
        }
    }

    public void goToGame(int id)
    {
        Application.LoadLevel(id);
    }

    private void HandleLevelComplete()
    {
        if (debug) Debug.Log("LEVEL COMPLETE!!!!");
        UpdateScore(ResourceManager.getGlobalCapital());
        SetState(GameState.Results);

        updateResultsScreen();
        displayStarsBasedOnBrakets();
    }

    public void displayStarsBasedOnBrakets()
    {
        int score = ResourceManager.getGlobalCapital();
        List<int> scoreBrakets = LevelManager.Instance.scoreBrakets;
        for (int i = 0; i < scoreBrakets.Count; i++)
        {
            if (score >= scoreBrakets[i])
            {
                fullStars[i].SetActive(true);
            } else
            {
                fullStars[i].SetActive(false);
            }
        }
    }

    public void updateResultsScreen()
    {
        finalScore.text = "" + ResourceManager.getGlobalCapital();
        if (playingLevelID == 0) topScore.text = highScore_1.ToString();
        if (playingLevelID == 1) topScore.text = highScore_2.ToString();
    }

    // Method to change the game state
    public void SetState(GameState newState)
    {
        // Only change if the new state is different
        if (CurrentState == newState)
            return;

        CurrentState = newState;

        // Handle state-specific logic (for example, pause the game)
        switch (newState)
        {
            case GameState.Paused:
                //Time.timeScale = 0f; // Pause the game
                break;
            default:
                //Time.timeScale = 1f; // Resume the game
                break;
        }

        // Notify any listeners about the state change
        OnGameStateChanged?.Invoke(newState);

        UpdateUIForState(newState);
    }

    private void UpdateUIForState(GameState state)
    {
        // Ensure UI elements exist before trying to access them
        if (MainMenu == null || PauseMenu == null)
        {
            if (debug) Debug.LogWarning("Some UI elements are not assigned in the GameManager!");
            return;
        }

        // Hide all UI elements first
        MainMenu.SetActive(false);
        PauseMenu.SetActive(false);
        EndOfChallengeMenu.SetActive(false);
        tutorialMenu.SetActive(false);

        // Show only the relevant UI for the current state
        switch (state)
        {
            case GameState.Menu:
                MainMenu.SetActive(true);
                break;
            case GameState.Playing:
                break;
            case GameState.Paused:
                PauseMenu.SetActive(true);
                break;
            case GameState.Results:
                EndOfChallengeMenu.SetActive(true);
                break;
            case GameState.Tutorial:
                tutorialMenu.SetActive(true);
                break;
        }
    }


    // Helper method to check if player actions are allowed (in this example, only when Playing)
    public bool CanPlayerAct()
    {
        return CurrentState == GameState.Playing;
    }



    public void StartGameAtLevel(int levelID)
    {
        playingLevelID = levelID;
        SetState(GameState.Tutorial); //show the tutorial window

        StartCoroutine( tutorialCoroutine(levelID) ) ;

    }

    private IEnumerator tutorialCoroutine(int levelID)
    {
            // Wait for the spawn rate duration
            yield return new WaitForSeconds(timeToShowTutorial);

   
            for (int i = 0; i < LevelGroups.Count; i++)
            {
                LevelGroups[i].SetActive(false); //SET ALL LEVELS INACTIVE
            }
            LevelGroups[levelID].SetActive(true); //SET ACTIVE ONLY THE LEVEL TO PLAY

            if (timesStarted == 0)
            {
                SetState(GameState.Playing);
                timesStarted++;
            } else
            {
                RestartGame();
            }

            tutorialCount = 0;

            subscribeToLevelComplete();
    }

    // Start the game - call from Main Menu "Play" button
    public void StartGame()
    {
        if (timesStarted == 0)
        {
            SetState(GameState.Playing);
            timesStarted++;
        } else
        {
            RestartGame();
        }
        // Load the first level scene if needed
        // SceneManager.LoadScene("Level1");
        //UpdateUIForState(GameState.Playing);
    }

    // Pause the game - call from in-game pause button
    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
        {
            SetState(GameState.Paused);
            //UpdateUIForState(GameState.Paused);
        }
    }
    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
        {
            SetState(GameState.Playing);
            //UpdateUIForState(GameState.Playing);
        }
    }
    public void RestartGame()
    {    
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        //SetState(GameState.Playing);
        //if (lvlManager != null) lvlManager.RestartCurrentLevel();
        //LevelManager.RestartCurrentLevel();

    }

    // Toggle between paused and playing - useful for ESC key handling
    public void TogglePause()
    {
        if (CurrentState == GameState.Playing)
        {
            SetState(GameState.Paused);
            //UpdateUIForState(GameState.Paused);
        } else if (CurrentState == GameState.Paused)
        {
            SetState(GameState.Playing);
            //UpdateUIForState(GameState.Playing);
        }
    }

    // Return to the main menu - call from pause menu or end screens
    public void ReturnToMainMenu()
    {
        SetState(GameState.Menu);
        //UpdateUIForState(GameState.Menu);
        // Load the menu scene if needed
        // SceneManager.LoadScene("MainMenu");
    }

    // Complete level successfully - call when player completes a level
    public void CompleteLevel()
    {
        UpdateScore(ResourceManager.getGlobalCapital());
        SetState(GameState.Success);
        updateResultsScreen();
    }

    // Quit the game - call from any menu
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

}

