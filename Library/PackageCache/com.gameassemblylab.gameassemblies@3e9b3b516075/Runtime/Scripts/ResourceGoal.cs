using UnityEngine;

public class ResourceGoal
{
    // The resource we want to collect (this should match your Resource type from ResourceManager)
    public Resource resourceType;
    // The number of this resource required to complete the goal
    public int requiredCount;
    // Total time allowed (in seconds)
    public float totalTime;
    // Remaining time (in seconds)
    public float remainingTime;
    // Goal status
    public bool isCompleted;
    public bool isFailed;

    // Constructor to set up a new goal
    public ResourceGoal(Resource resourceType, int requiredCount, float timeLimit)
    {
        this.resourceType = resourceType;
        this.requiredCount = requiredCount;
        this.totalTime = timeLimit;
        this.remainingTime = timeLimit;
        isCompleted = false;
        isFailed = false;
    }

    // Call this each frame to update the timer and check the goal status
    public void UpdateGoal(float deltaTime)
    {
        // If already completed or failed, do nothing
        if (isCompleted || isFailed)
            return;

        // Update the remaining time
        remainingTime -= deltaTime;

        // Check if the goal is achieved (enough resources collected)
        int currentCount = ResourceManager.Instance.GetResourceCount(resourceType);
        if (currentCount >= requiredCount)
        {
            isCompleted = true;
        }
        // If time has run out and the goal hasn't been met, mark as failed
        else if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isFailed = true;
        }
    }
}
