using UnityEngine;

[CreateAssetMenu(fileName = "NewResourceGoal", menuName = "Game Assemblies/Goals/Create Resource Goal")]

public class ResourceGoalSO : ScriptableObject
{
    [Header("Goal Definition")]
    // The resource to collect (ensure your Resource class has a resourceName field)
    public Resource resourceType;
    // How many of the resource are required to complete the goal
    public int requiredCount = 1;
    // The total time allowed to meet the goal (in seconds)
    public float timeLimit = 20f;

    [Header("Rewards")]
    // Points awarded when this goal is completed
    public int rewardPoints = 0;
    public int penalty = 1;

    [Header("Runtime State (Not Serialized)")]
    [System.NonSerialized] public float remainingTime;
    [System.NonSerialized] public bool isCompleted;
    [System.NonSerialized] public bool isFailed;

    // Resets the runtime state of the goal
    public void ResetGoal()
    {
        remainingTime = timeLimit;
        isCompleted = false;
        isFailed = false;
    }

    // Updates the goal status based on elapsed time and collected resources
    public void UpdateGoal(float deltaTime)
    {
        if (isCompleted || isFailed)
            return;

        remainingTime -= deltaTime;

        // Check current progress from the ResourceManager
        int currentCount = ResourceManager.Instance.GetResourceCount(resourceType);
        if (currentCount >= requiredCount)
        {
            isCompleted = true;
            // Optionally, you could trigger reward distribution here
        } else if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isFailed = true;
        }
    }

    public bool UpdateGoalObjective(Resource providedResource)
    {
        // If already completed or failed, simply return the state.
        if (isCompleted || isFailed)
            return isCompleted;

        // Check if the provided resource is the one needed for this goal.
        if (providedResource == resourceType)
        {
            isCompleted = true;
        } 
        return isCompleted;
    }


    public void UpdateGoaltime(float deltaTime)
    {
        if (isCompleted || isFailed)
            return;

        remainingTime -= deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isFailed = true;
        }
    }

}
