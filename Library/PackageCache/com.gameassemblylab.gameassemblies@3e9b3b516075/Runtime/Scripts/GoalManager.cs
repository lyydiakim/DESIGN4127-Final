using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GoalManager : MonoBehaviour
{
    public static GoalManager Instance { get; private set; }

    [Header("Goal Templates")]
    // Drag-and-drop your ResourceGoalSO assets into this list in the Inspector.
    [SerializeField] private List<ResourceGoalSO> goalTemplates = new List<ResourceGoalSO>();

    // This list will hold runtime instances of the goals.
    public List<ResourceGoalSO> activeGoals = new List<ResourceGoalSO>();

    //[Header("Global Score")]
    //public int globalScore = 0;

    [Header("UI Elements")]
    // Assign your TextMesh Pro UI element (for displaying the global score) in the Inspector.
    public TMP_Text scoreText;

    public GameObject goalTracker;
    public GameObject goalTrackerGrid;
    public List<GameObject> allGoalTrackers;

    public bool debug = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        } else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        allGoalTrackers = new List<GameObject>();

        if (goalTracker == null || goalTrackerGrid == null)
        {
            Debug.LogWarning("GoalManager: goalTracker or goalTrackerGrid is not assigned. Goal trackers will not be created.");
            UpdateScoreUI();
            return;
        }

        // For each goal template, create a runtime copy and reset its state.
        foreach (var goalTemplate in goalTemplates)
        {
            if (goalTemplate == null) continue;

            // Use Instantiate to clone the ScriptableObject.
            ResourceGoalSO runtimeGoal = Instantiate(goalTemplate);
            runtimeGoal.ResetGoal();
            activeGoals.Add(runtimeGoal);

            GameObject gTracker = Instantiate(goalTracker);
            gTracker.transform.parent = goalTrackerGrid.transform;
            allGoalTrackers.Add(gTracker);

            gTracker.GetComponent<GoalTrackerUI>().Initialize(runtimeGoal);
        }

        UpdateScoreUI();
    }

    private void Update()
    {
        // Check if GameManager exists and if game state is Playing
        // If GameManager doesn't exist, process goals anyway (for standalone Resource Management System)
        bool shouldProcessGoals = GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing;
        
        if (shouldProcessGoals)
        {

            float dt = Time.deltaTime;
            // Update each active goal.
            foreach (ResourceGoalSO goal in activeGoals)
            {
                goal.UpdateGoaltime(dt);
            }

            // Check for completed goals.
            // Iterate backwards to safely remove items from the list.
            for (int i = activeGoals.Count - 1; i >= 0; i--)
            {
                if (activeGoals[i].isCompleted)
                {
                    // Add the reward to the global score.
                    //ResourceManager.Instance.globalCapital += activeGoals[i].rewardPoints;
                    if(debug)Debug.Log($"Goal Completed: Collect {activeGoals[i].requiredCount} of {activeGoals[i].resourceType.resourceName}. " +
                              $"Reward: {activeGoals[i].rewardPoints}. Global Score: {ResourceManager.Instance.globalCapital}");

                    ResourceManager.Instance.globalCapital += activeGoals[i].rewardPoints;

                    // Remove the completed goal.
                    activeGoals.RemoveAt(i);
                    Destroy(allGoalTrackers[i]);
                    allGoalTrackers.RemoveAt(i);

                    UpdateScoreUI();
                }else if (activeGoals[i].isFailed){
                    if(debug)Debug.Log($"Goal Failed: Collect {activeGoals[i].requiredCount} of {activeGoals[i].resourceType.resourceName}. " +
                              $"Reward: {activeGoals[i].rewardPoints}. Global Score: {ResourceManager.Instance.globalCapital}");

                    ResourceManager.Instance.globalCapital -= activeGoals[i].penalty;

                    // Remove the completed goal.
                    activeGoals.RemoveAt(i);
                    Destroy(allGoalTrackers[i]);
                    allGoalTrackers.RemoveAt(i);

                    UpdateScoreUI();
                }

            }
        }

        // (Optional) Call DebugActiveGoals() on a key press or at regular intervals.
        UpdateScoreUI();
        //DebugActiveGoals();
        
    }

    public void goalContribution(Resource rType)
    {
        if(debug) Debug.Log("GOAL CONTRIBUTION CALLED WITH " + rType.resourceName);

        foreach (ResourceGoalSO goal in activeGoals)
        {
            if (goal.UpdateGoalObjective(rType) == true)
            {
                Debug.Log("GOAL COMPLETED!!!");
                break;
            }
        }
    }

    // Add this method to allow the LevelManager to add new goals
    public void AddGoal(ResourceGoalSO goal)
    {
        if (goal == null) return;
        if (goalTracker == null || goalTrackerGrid == null)
        {
            Debug.LogWarning("GoalManager: goalTracker or goalTrackerGrid is not assigned. Cannot add goal tracker.");
            activeGoals.Add(goal);
            return;
        }

        // Add the goal to active goals
        activeGoals.Add(goal);

        // Create a tracker UI for the goal
        GameObject gTracker = Instantiate(goalTracker);
        gTracker.transform.parent = goalTrackerGrid.transform;
        allGoalTrackers.Add(gTracker);

        // Initialize the UI
        gTracker.GetComponent<GoalTrackerUI>().Initialize(goal);

        // Log goal creation
        if (debug) Debug.Log($"Added new goal: {goal.resourceType.resourceName} x{goal.requiredCount}");
    }

    // Add this method to clear all active goals (useful for level transitions)
    public void ClearAllGoals()
    {
        // Destroy all goal trackers
        foreach (GameObject tracker in allGoalTrackers)
        {
            Destroy(tracker);
        }

        // Clear the lists
        allGoalTrackers.Clear();
        activeGoals.Clear();

        if (debug) Debug.Log("Cleared all active goals");
    }

    /// <summary>
    /// Updates the TextMesh Pro UI element to display the current global score.
    /// </summary>
    public void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + ResourceManager.Instance.globalCapital.ToString(); 
        } else
        {
            Debug.LogWarning("Score Text UI element is not assigned in GoalManager.");
        }
    }

    // A debug function to print the active goals and their progress.
    public void DebugActiveGoals()
    {
        Debug.Log("----- Active Goals -----");
        foreach (ResourceGoalSO goal in activeGoals)
        {
            int currentCount = ResourceManager.Instance.GetResourceCount(goal.resourceType);
            string goalName = goal.resourceType.resourceName; // adjust as necessary
            float progressPercent = ((float)currentCount / goal.requiredCount) * 100f;
            string status = goal.isCompleted ? "Completed" : (goal.isFailed ? "Failed" : "Active");

            Debug.Log($"Goal: Collect {goal.requiredCount} of {goalName} " +
                      $"(Current: {currentCount} - {progressPercent:F1}%) | " +
                      $"Time Remaining: {goal.remainingTime:F1}s | Status: {status}");
        }
    }
}
