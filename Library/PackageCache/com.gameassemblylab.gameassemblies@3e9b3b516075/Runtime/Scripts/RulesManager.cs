using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages which rules are active in the current session.
/// Supports gradual rule activation (roguelike-style) and dependency checking.
/// </summary>
public class RulesManager : MonoBehaviour
{
    public static RulesManager Instance { get; private set; }

    [Header("Session Configuration")]
    [Tooltip("Optional: rules configuration for this session. If null, use manual setup.")]
    [SerializeField] private RulesSessionSO sessionConfig;

    [Header("Runtime State")]
    [SerializeField] private List<RuleSO> activeRules = new List<RuleSO>();

    [Header("Events")]
    public UnityEvent<RuleSO> onRuleActivated;
    public UnityEvent<RuleSO> onRuleDeactivated;

    [Header("Debug")]
    public bool debug;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (sessionConfig != null)
        {
            InitializeFromSession(sessionConfig);
        }
    }

    /// <summary>
    /// Initializes rules from a session configuration (starting rules).
    /// </summary>
    public void InitializeFromSession(RulesSessionSO config)
    {
        ClearAllRules();

        if (config == null) return;

        foreach (var rule in config.startingRules)
        {
            if (rule != null)
            {
                ActivateRule(rule, skipDependencyCheck: true);
            }
        }

        if (debug) Debug.Log($"[RulesManager] Initialized with {activeRules.Count} starting rules from session config.");
    }

    /// <summary>
    /// Returns true if the given rule is currently active (matched by rule name).
    /// </summary>
    public bool IsRuleActive(RuleSO rule)
    {
        if (rule == null) return false;
        return activeRules.Exists(r => r.ruleName == rule.ruleName);
    }

    /// <summary>
    /// Returns true if all required dependencies for the rule are active.
    /// </summary>
    public bool CanActivateRule(RuleSO rule)
    {
        if (rule == null) return false;
        if (IsRuleActive(rule)) return false;

        foreach (var dep in rule.requiredRules)
        {
            if (dep != null && !IsRuleActive(dep))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Activates a rule. Returns false if dependencies are not met or rule is already active.
    /// </summary>
    /// <param name="skipDependencyCheck">Use when initializing from session config.</param>
    public bool ActivateRule(RuleSO rule, bool skipDependencyCheck = false)
    {
        if (rule == null) return false;
        if (IsRuleActive(rule)) return true;

        if (!skipDependencyCheck && !CanActivateRule(rule))
        {
            if (debug) Debug.LogWarning($"[RulesManager] Cannot activate '{rule.ruleName}': dependencies not met.");
            return false;
        }

        RuleSO runtimeCopy = Instantiate(rule);
        runtimeCopy.isActive = true;
        activeRules.Add(runtimeCopy);

        onRuleActivated?.Invoke(runtimeCopy);
        if (debug) Debug.Log($"[RulesManager] Activated rule: {rule.ruleName}");

        return true;
    }

    /// <summary>
    /// Deactivates a rule. Returns true if the rule was active and is now removed.
    /// </summary>
    public bool DeactivateRule(RuleSO rule)
    {
        if (rule == null) return false;

        int idx = activeRules.FindIndex(r => r.ruleName == rule.ruleName);
        if (idx < 0) return false;

        RuleSO removed = activeRules[idx];
        removed.isActive = false;
        activeRules.RemoveAt(idx);

        onRuleDeactivated?.Invoke(removed);
        if (debug) Debug.Log($"[RulesManager] Deactivated rule: {rule.ruleName}");

        return true;
    }

    /// <summary>
    /// Returns all currently active rules.
    /// </summary>
    public IReadOnlyList<RuleSO> GetActiveRules()
    {
        return activeRules;
    }

    /// <summary>
    /// Returns active rules filtered by category.
    /// </summary>
    public List<RuleSO> GetActiveRulesByCategory(RuleCategory category)
    {
        return activeRules.FindAll(r => r.category == category);
    }

    /// <summary>
    /// Checks if any rule of the given category is active.
    /// </summary>
    public bool HasRuleInCategory(RuleCategory category)
    {
        return activeRules.Exists(r => r.category == category);
    }

    /// <summary>
    /// Clears all active rules. Call when starting a new session.
    /// </summary>
    public void ClearAllRules()
    {
        foreach (var r in activeRules)
        {
            r.isActive = false;
        }
        activeRules.Clear();
        if (debug) Debug.Log("[RulesManager] Cleared all rules.");
    }

    /// <summary>
    /// Attempts to unlock a rule from the session's available pool.
    /// Returns true if the rule was successfully activated.
    /// </summary>
    public bool TryUnlockFromPool(RuleSO rule)
    {
        if (sessionConfig == null || rule == null) return false;
        if (!sessionConfig.availableRulePool.Contains(rule)) return false;
        if (sessionConfig.maxActiveRules > 0 && activeRules.Count >= sessionConfig.maxActiveRules)
            return false;

        return ActivateRule(rule);
    }

    /// <summary>
    /// Returns rules from the session pool that can currently be unlocked (dependencies met).
    /// </summary>
    public List<RuleSO> GetUnlockableRules()
    {
        var result = new List<RuleSO>();
        if (sessionConfig == null) return result;
        if (sessionConfig.maxActiveRules > 0 && activeRules.Count >= sessionConfig.maxActiveRules)
            return result;

        foreach (var rule in sessionConfig.availableRulePool)
        {
            if (rule != null && CanActivateRule(rule))
                result.Add(rule);
        }
        return result;
    }
}
