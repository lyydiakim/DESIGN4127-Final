using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Level", menuName = "Game Assemblies/Level Data")]
public class LevelDataSO : ScriptableObject
{
    [Header("Level Information")]
    public string levelName = "New Level";
    public LevelType levelType = LevelType.Sequential;
    
    [Header("Sequential Mode Settings")]
    [Tooltip("Goals that will appear one after another")]
    public List<ResourceGoalSO> sequentialGoals = new List<ResourceGoalSO>();
    
    [Header("Random Mode Settings")]
    [Tooltip("Pool of goals to randomly select from")]
    public List<ResourceGoalSO> randomGoalPool = new List<ResourceGoalSO>();
    [Tooltip("Time between generating random goals (seconds)")]
    public float goalInterval = 30f;
    [Tooltip("Maximum number of goals active at once")]
    public int maxActiveGoals = 3;
    
    [Header("Common Settings")]
    [Tooltip("Time before the first goal appears (seconds)")]
    public float initialDelay = 5f;
    [Tooltip("Whether the level should end after all goals are completed")]
    public bool endLevelWhenComplete = true;
    //[Tooltip("Time limit for the level (0 = no limit)")]
    //public float timeLimit = 0f;

    [Header("Time Settings")]
    [Tooltip("Time to complete the level (seconds)")]
    public float timeToComplete = 10f;
    
}
