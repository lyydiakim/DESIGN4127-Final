using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Categories for organizing rules into governing structures.
/// </summary>
public enum RuleCategory
{
    Property,       // Private property, common property, ownership
    Economy,        // Taxes, trade, markets
    Governance,     // Government, democracy, hierarchy
    Security,       // Policing, enforcement, borders
    Incentives,     // Rewards, penalties, motivation systems
    Social,         // Welfare, equality, community
    Custom
}

/// <summary>
/// A configurable parameter for a rule (e.g., tax rate, enforcement level).
/// </summary>
[Serializable]
public class RuleParameter
{
    public string key = "value";
    public float floatValue;
    public int intValue;
    public bool boolValue;
    public string stringValue;

    public RuleParameter(string key, float value = 0f)
    {
        this.key = key;
        this.floatValue = value;
    }
}

/// <summary>
/// ScriptableObject defining a modular governing rule (private property, taxes, government, etc.).
/// Rules can have dependencies on other rules and configurable parameters.
/// Designed for gradual implementation during gameplay (roguelike-style).
/// </summary>
[CreateAssetMenu(fileName = "New Rule", menuName = "Game Assemblies/Rules/Create Rule")]
public class RuleSO : ScriptableObject
{
    [Header("Identity")]
    public string ruleName = "New Rule";
    [TextArea(2, 4)]
    public string description = "";
    public Sprite icon;

    [Header("Classification")]
    public RuleCategory category = RuleCategory.Custom;
    [Tooltip("Display order within category (lower = earlier)")]
    public int displayOrder = 0;

    [Header("Dependencies")]
    [Tooltip("Rules that must be active before this rule can be activated")]
    public List<RuleSO> requiredRules = new List<RuleSO>();

    [Header("Parameters")]
    [Tooltip("Configurable values (tax rate, enforcement level, etc.)")]
    public List<RuleParameter> parameters = new List<RuleParameter>();

    [Header("Runtime State (Not Serialized)")]
    [System.NonSerialized] public bool isActive;

    /// <summary>
    /// Resets runtime state. Call when starting a new session.
    /// </summary>
    public void ResetState()
    {
        isActive = false;
    }

    /// <summary>
    /// Gets a parameter by key. Returns default if not found.
    /// </summary>
    public float GetFloat(string key, float defaultValue = 0f)
    {
        var p = parameters.Find(x => x.key == key);
        return p != null ? p.floatValue : defaultValue;
    }

    public int GetInt(string key, int defaultValue = 0)
    {
        var p = parameters.Find(x => x.key == key);
        return p != null ? p.intValue : defaultValue;
    }

    public bool GetBool(string key, bool defaultValue = false)
    {
        var p = parameters.Find(x => x.key == key);
        return p != null ? p.boolValue : defaultValue;
    }

    public string GetString(string key, string defaultValue = "")
    {
        var p = parameters.Find(x => x.key == key);
        return p != null ? p.stringValue : defaultValue;
    }
}
