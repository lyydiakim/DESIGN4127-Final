using UnityEngine;
using UnityEngine.UI;

public class GoalTrackerUI : MonoBehaviour
{
    [Header("UI References")]
    // Assign the Image component that will show the resource icon.
    [SerializeField] private Image resourceIcon;
    // Assign the Slider that will represent the remaining time.
    [SerializeField] private Slider timeSlider;

    // The goal that this UI tracker is currently displaying.
    private ResourceGoalSO currentGoal;

    

    [Header("Color Settings")]
    // The Image used as the slider's fill area whose color will change.
    [SerializeField] private Image sliderFillImage;
    // A gradient to define the color ramp (e.g., green when full, red when nearly out).
    [SerializeField] private Gradient timeColorRamp;

    /// <summary>
    /// Initializes the GoalTrackerUI with a specific goal.
    /// </summary>
    /// <param name="goal">The ResourceGoalSO to track.</param>
    public void Initialize(ResourceGoalSO goal)
    {
        currentGoal = goal;

        // If the resource type has an associated icon, assign it to the UI Image.
        if (goal.resourceType != null && resourceIcon != null)
        {
            resourceIcon.sprite = goal.resourceType.icon;
            resourceIcon.color = GetIconTint(goal.resourceType);
        }

        // Set up the slider. We set the maximum value to the goal's time limit
        // and initialize it with the remaining time.
        timeSlider.maxValue = goal.timeLimit;
        timeSlider.value = goal.remainingTime;

        // Update the fill color based on the current time.
        float ratio = currentGoal.remainingTime / currentGoal.timeLimit;
        sliderFillImage.color = timeColorRamp.Evaluate(ratio);
    }

    private static Color GetIconTint(Resource r)
    {
        if (r == null) return Color.white;
        return r.iconTint.a < 0.01f ? Color.white : r.iconTint;
    }

    private void Update()
    {
        if (currentGoal != null)
        {
            // Update the slider to represent the remaining time.
            timeSlider.value = currentGoal.remainingTime;

            // Calculate the ratio of remaining time to total time.
            float ratio = currentGoal.remainingTime / currentGoal.timeLimit;
            // Update the fill color using the gradient.
            sliderFillImage.color = timeColorRamp.Evaluate(ratio);
        }
    }
}
