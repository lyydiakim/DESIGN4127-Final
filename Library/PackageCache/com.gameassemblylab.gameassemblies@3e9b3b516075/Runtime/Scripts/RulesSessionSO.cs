using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a rules configuration for a session (e.g., a roguelike run).
/// Specifies starting rules and the pool of rules that can be unlocked during gameplay.
/// </summary>
[CreateAssetMenu(fileName = "New Rules Session", menuName = "Game Assemblies/Rules/Create Rules Session")]
public class RulesSessionSO : ScriptableObject
{
    [Header("Session Identity")]
    public string sessionName = "New Session";
    [TextArea(2, 4)]
    public string description = "";

    [Header("Starting Rules")]
    [Tooltip("Rules that are active at the start of the session")]
    public List<RuleSO> startingRules = new List<RuleSO>();

    [Header("Available Rule Pool (Roguelike)")]
    [Tooltip("Rules that can be unlocked/activated during gameplay")]
    public List<RuleSO> availableRulePool = new List<RuleSO>();

    [Header("Unlock Settings")]
    [Tooltip("Maximum number of rules that can be active at once (0 = no limit)")]
    public int maxActiveRules = 0;
    [Tooltip("Whether rules from the pool can only be activated once per session")]
    public bool oneTimeUnlock = false;
}
