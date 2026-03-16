using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility that discovers and caches ScriptableObject assets by type.
/// Used by the Database Inspector window to list all entries for each database type.
/// </summary>
public static class SA_DatabaseManager
{
    private static readonly Dictionary<Type, List<ScriptableObject>> _cache = new Dictionary<Type, List<ScriptableObject>>();

    /// <summary>
    /// Known database types with display names for the inspector dropdown.
    /// Add new ScriptableObject types here when they are introduced.
    /// </summary>
    public static readonly (Type Type, string DisplayName)[] DatabaseTypes =
    {
        (typeof(Resource), "Resources"),
        (typeof(LootTable), "Loot Tables"),
        (typeof(LevelDataSO), "Levels"),
        (typeof(ResourceGoalSO), "Resource Goals"),
        (typeof(ColorPaletteSO), "Color Palettes"),
        (typeof(ResourceManager_SO), "Resource Managers"),
        (typeof(RuleSO), "Rules"),
        (typeof(RulesSessionSO), "Rules Sessions"),
        (typeof(StationDataSO), "Station Data"),
    };

    /// <summary>
    /// Gets all ScriptableObject assets of the given type. Results are cached until Refresh is called.
    /// </summary>
    public static IReadOnlyList<ScriptableObject> GetEntries(Type type)
    {
        if (type == null || !typeof(ScriptableObject).IsAssignableFrom(type))
            return Array.Empty<ScriptableObject>();

        if (_cache.TryGetValue(type, out var list))
            return list;

        Refresh(type);
        return _cache.TryGetValue(type, out list) ? list : Array.Empty<ScriptableObject>();
    }

    /// <summary>
    /// Refreshes the cache for the given type by searching the project for all assets of that type.
    /// </summary>
    public static void Refresh(Type type)
    {
        if (type == null || !typeof(ScriptableObject).IsAssignableFrom(type))
            return;

        var results = new List<ScriptableObject>();
        string typeName = type.Name;
        string[] guids = AssetDatabase.FindAssets($"t:{typeName}");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (asset != null && type.IsInstanceOfType(asset))
                results.Add(asset);
        }

        results.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        _cache[type] = results;
    }

    /// <summary>
    /// Clears the cache for a specific type or all types.
    /// </summary>
    public static void ClearCache(Type type = null)
    {
        if (type == null)
            _cache.Clear();
        else
            _cache.Remove(type);
    }
}
