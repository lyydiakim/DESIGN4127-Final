using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum LevelType
{
    Sequential,
    RandomInterval
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Configuration")]
    [SerializeField] private List<LevelDataSO> levelDataList = new List<LevelDataSO>();
    [SerializeField] private int currentLevelIndex = 0;

    private static LevelDataSO currentLevel;
    private int sequentialGoalIndex = 0;
    private float nextGoalTime = 0f;
    private float levelStartTime = 0f;
    private bool levelActive = false;

    [Header("Events")]
    public UnityEvent onLevelStart;
    public UnityEvent onLevelComplete;
    public UnityEvent onAllLevelsComplete;

    [Header("Timer")]
    //public CountdownTimer currentTimer;
    public static float elapsedTime = 0;
    public static float currentLevelTimeLeft = 0f;
    private bool IsTimerRunning = false;

    [Header("Suppress Goal Spawining - keep it manual by other entities")]
    public bool manualGoals = false;

    [Header("Delay Complete")]
    public float delayTimeToComplete = 2.0f;
    private float delayCount = 0f;

    [Header("Audio")]
    public AudioSource outputSound;
    public AudioClip successComplete;
    public AudioClip failureComplete;

    [Header("Score Brackets - (5 scores for stars)")]
    public List<int> scoreBrakets;

    public bool debug = false;

    private void Awake()
    {   
        
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        } else
        {
            Destroy(gameObject);
        }
        
    }

    private void Start()
    {
        //currentTimer = GameObject.FindAnyObjectByType<CountdownTimer>();
        //if (currentTimer != null)
        //{
            //currentTimer.startingTime = currentLevel.timeToComplete; 
        //}

        // Initialize with the first level
        if (levelDataList.Count > 0 && currentLevelIndex < levelDataList.Count)
        {
            StartLevel(currentLevelIndex);
        } else
        {
            Debug.LogWarning("No levels configured in LevelManager.");
        }
    }

    public static LevelDataSO getCurrentLevel()
    {
        return currentLevel;
    }
    public static float getTimeToComplete()
    {
        return currentLevel.timeToComplete;
    }
    public static float getTimeRemaining()
    {
        return currentLevelTimeLeft;
    }

    private void Update()
    {
        if (!levelActive) return;

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

        //Progress time
        passTime();
        //if (IsTimerRunning)
        //{
            //elapsedTime = Time.time - levelStartTime;
        //}

        // Check for level time limit
        if (currentLevel.timeToComplete > 0 &&
            currentLevelTimeLeft == 0)
        {
            EndCurrentLevel();
            AdvanceToNextLevel();
            return;
        }

        // Handle goal creation based on level type
        if (currentLevel.levelType == LevelType.Sequential)
        {
            UpdateSequentialLevel();
        } else // Random interval mode
        {
            UpdateRandomIntervalLevel();
        }

        // Check if all sequential goals are completed
        if (currentLevel.levelType == LevelType.Sequential &&
            sequentialGoalIndex >= currentLevel.sequentialGoals.Count &&
            GoalManager.Instance.activeGoals.Count == 0 &&
            currentLevel.endLevelWhenComplete)
        {
            delayCount += Time.deltaTime; //delay the completion of a level
            if(delayCount >= delayTimeToComplete)
            {

                //PLAY SUCCESS SOUND
                if (outputSound != null && successComplete != null)
                {
                    outputSound.clip = successComplete;
                    outputSound.Play();
                }

                Debug.Log("All sequential goals completed. Advancing to next level.");
                EndCurrentLevel();
                AdvanceToNextLevel();

                
            }
        }
    }

    public void passTime()
    {
        //Progress time
        if (IsTimerRunning)
        {
            currentLevelTimeLeft -= Time.deltaTime;
            if (currentLevelTimeLeft <= 0f)
            {
                currentLevelTimeLeft = 0f;
                IsTimerRunning = false;
            }
        }
        
    }

    public void StartLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelDataList.Count)
        {
            Debug.LogError($"Invalid level index: {levelIndex}");
            return;
        }

        // End current level if one is active
        if (levelActive)
        {
            //EndCurrentLevel();
        }

        // Set up the new level
        currentLevelIndex = levelIndex;
        currentLevel = levelDataList[currentLevelIndex];
        sequentialGoalIndex = 0;
        nextGoalTime = Time.time + currentLevel.initialDelay;
        levelStartTime = Time.time;
        currentLevelTimeLeft = currentLevel.timeToComplete;
        levelActive = true;

        if (debug) Debug.Log($"Starting level: {currentLevel.levelName} ({currentLevel.levelType})");

        // Clear any existing goals
        if (GoalManager.Instance != null)
        {
            GoalManager.Instance.ClearAllGoals();
        }

        // Trigger level start event
        onLevelStart.Invoke();
    }
    public void EndCurrentLevel()
    {
        if (!levelActive) return;

        levelActive = false;

        // Clear all active goals
        if (GoalManager.Instance != null)
        {
            GoalManager.Instance.ClearAllGoals();
        }

        Debug.Log($"Ending level: {currentLevel.levelName}");

        // Trigger level complete event
        onLevelComplete.Invoke();
    }
    public void AdvanceToNextLevel()
    {
        int nextLevelIndex = currentLevelIndex + 1;
        if (nextLevelIndex < levelDataList.Count)
        {
            StartLevel(nextLevelIndex);
        } else
        {
            Debug.Log("No more levels available. Game completed!");
            // Trigger game completion event
            onAllLevelsComplete.Invoke();
        }
    }
    private void UpdateSequentialLevel()
    {
        // Check if it's time to introduce the next goal and we still have goals to create
        if (Time.time >= nextGoalTime && sequentialGoalIndex < currentLevel.sequentialGoals.Count)
        {
            // Create the next goal in the sequence
            CreateGoal(currentLevel.sequentialGoals[sequentialGoalIndex]);
            sequentialGoalIndex++;

            // Update the time for the next goal
            nextGoalTime = Time.time + currentLevel.goalInterval;

            // Check if all sequential goals have been created
            if (sequentialGoalIndex >= currentLevel.sequentialGoals.Count)
            {
                Debug.Log("All sequential goals have been created for this level.");
            }
        }
    }
    private void UpdateRandomIntervalLevel()
    {
        // Check if it's time to introduce a new random goal
        if (Time.time >= nextGoalTime && currentLevel.randomGoalPool.Count > 0)
        {
            // Check if we're under the max active goals limit
            if (GoalManager.Instance != null &&
                GoalManager.Instance.activeGoals.Count < currentLevel.maxActiveGoals)
            {

                if(manualGoals == false)
                {
                    // Select a random goal from the pool
                    int randomIndex = Random.Range(0, currentLevel.randomGoalPool.Count);
                    CreateGoal(currentLevel.randomGoalPool[randomIndex]);

                    // Update the time for the next goal
                    nextGoalTime = Time.time + currentLevel.goalInterval;
                }

            }
        }
    }
    public void CreateGoal(ResourceGoalSO goalTemplate)
    {
        if (GoalManager.Instance == null)
        {
            Debug.LogError("GoalManager instance not found!");
            return;
        }

        // Create a runtime copy of the goal
        ResourceGoalSO runtimeGoal = Instantiate(goalTemplate);
        runtimeGoal.ResetGoal();

        // Add the goal to the GoalManager
        GoalManager.Instance.AddGoal(runtimeGoal);

        if (debug) Debug.Log($"Created new goal: Collect {runtimeGoal.requiredCount} of {runtimeGoal.resourceType.resourceName}");
    }
    // Public methods to manually control level flow
    public void RestartCurrentLevel()
    {
        StartLevel(currentLevelIndex);
    }
    public void SkipToLevel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < levelDataList.Count)
        {
            StartLevel(levelIndex);
        }
    }
    public void CreateRandomGoalNow()
    {
        if (currentLevel.randomGoalPool.Count > 0)
        {
            int randomIndex = Random.Range(0, currentLevel.randomGoalPool.Count);
            CreateGoal(currentLevel.randomGoalPool[randomIndex]);
        }
    }
    public void ForceCreateNextSequentialGoal()
    {
        if (currentLevel.levelType == LevelType.Sequential &&
            sequentialGoalIndex < currentLevel.sequentialGoals.Count)
        {
            CreateGoal(currentLevel.sequentialGoals[sequentialGoalIndex]);
            sequentialGoalIndex++;
        }
    }
    // Helper methods
    public float GetLevelProgress()
    {
        if (!levelActive || currentLevel.timeToComplete <= 0) return 0;

        elapsedTime = Time.time - levelStartTime;
        return Mathf.Clamp01(elapsedTime / currentLevel.timeToComplete);
    }
    public int GetRemainingSequentialGoals()
    {
        if (!levelActive || currentLevel.levelType != LevelType.Sequential) return 0;

        return currentLevel.sequentialGoals.Count - sequentialGoalIndex;
    }
}