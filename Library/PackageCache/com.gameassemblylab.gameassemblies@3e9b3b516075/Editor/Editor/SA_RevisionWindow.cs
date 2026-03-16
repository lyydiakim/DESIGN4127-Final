using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Scene revision tool that validates the game assembly pipeline:
/// Resources > Stations > Resource Manager > Goals > Levels > Game States
/// Provides feedback, suggestions, and a complexity assessment.
/// </summary>
public class SA_RevisionWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private List<RevisionEntry> entries = new List<RevisionEntry>();
    private string complexityLabel = "";
    private Color complexityColor = Color.white;
    private int chainCount;
    private string suggestion = "";
    private bool hasRun = false;

    private enum EntryStatus { Pass, Warning, Fail, Info }

    private struct RevisionEntry
    {
        public string category;
        public string message;
        public EntryStatus status;
    }

    [MenuItem("Game Assemblies/Revision")]
    public static void ShowWindow()
    {
        var window = GetWindow<SA_RevisionWindow>("Scene Revision");
        window.RunRevision();
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Scene Revision", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool validates the game assembly pipeline in your scene:\n" +
            "Resources > Stations > Resource Manager > Goals > Levels > Game States\n\n" +
            "Click 'Run Revision' to analyze the current scene.",
            MessageType.Info);

        GUILayout.Space(10);
        if (GUILayout.Button("Run Revision", GUILayout.Height(30)))
        {
            RunRevision();
        }

        if (!hasRun) return;

        GUILayout.Space(10);

        // Complexity banner
        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = complexityColor;
        EditorGUILayout.BeginVertical("box");
        GUI.backgroundColor = prevBg;
        EditorGUILayout.LabelField("Scene Complexity", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(complexityLabel, EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        // Suggestion
        if (!string.IsNullOrEmpty(suggestion))
        {
            EditorGUILayout.HelpBox(suggestion, MessageType.None);
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Detailed Results", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        string lastCategory = "";
        foreach (var entry in entries)
        {
            if (entry.category != lastCategory)
            {
                GUILayout.Space(6);
                EditorGUILayout.LabelField(entry.category, EditorStyles.miniBoldLabel);
                lastCategory = entry.category;
            }

            MessageType msgType;
            switch (entry.status)
            {
                case EntryStatus.Pass: msgType = MessageType.Info; break;
                case EntryStatus.Warning: msgType = MessageType.Warning; break;
                case EntryStatus.Fail: msgType = MessageType.Error; break;
                default: msgType = MessageType.None; break;
            }

            EditorGUILayout.HelpBox(StatusIcon(entry.status) + " " + entry.message, msgType);
        }

        EditorGUILayout.EndScrollView();
    }

    private static string StatusIcon(EntryStatus s)
    {
        switch (s)
        {
            case EntryStatus.Pass: return "OK";
            case EntryStatus.Warning: return "WARNING";
            case EntryStatus.Fail: return "MISSING";
            case EntryStatus.Info: return "NOTE";
            default: return "";
        }
    }

    // ─────────────────────────── REVISION LOGIC ───────────────────────────

    private void RunRevision()
    {
        entries.Clear();
        hasRun = true;

        // Gather scene objects
        var stations = Object.FindObjectsByType<Station>(FindObjectsSortMode.None);
        var resourceObjects = Object.FindObjectsByType<ResourceObject>(FindObjectsSortMode.None);
        var resourceManager = Object.FindFirstObjectByType<ResourceManager>();
        var goalManager = Object.FindFirstObjectByType<GoalManager>();
        var levelManager = Object.FindFirstObjectByType<LevelManager>();
        var gameManager = Object.FindFirstObjectByType<GameManager>();
        var playerInputManager = Object.FindFirstObjectByType<UnityEngine.InputSystem.PlayerInputManager>();
        var rmCanvas = Object.FindFirstObjectByType<resourceManagerCanvas>();

        int tier = 0; // how far along the pipeline the scene is

        // ── 1. RESOURCES ──
        CheckResources(resourceObjects, ref tier);

        // ── 2. STATIONS ──
        CheckStations(stations, ref tier);

        // ── 3. RESOURCE MANAGER ──
        CheckResourceManager(resourceManager, rmCanvas, ref tier);

        // ── 4. GOALS ──
        CheckGoalManager(goalManager, ref tier);

        // ── 5. LEVELS ──
        CheckLevelManager(levelManager, ref tier);

        // ── 6. GAME STATES ──
        CheckGameStateManager(gameManager, ref tier);

        // ── PLAYERS ──
        CheckPlayers(playerInputManager);

        // ── COMPLEXITY ──
        chainCount = CountResourceChains(stations);
        AssessComplexity(stations, resourceObjects, chainCount, tier);

        // ── SUGGESTION ──
        BuildSuggestion(tier, stations, resourceManager, goalManager, levelManager, gameManager);

        Repaint();
    }

    // ─────────────────────────── CHECKS ───────────────────────────

    private void CheckResources(ResourceObject[] resourceObjects, ref int tier)
    {
        string cat = "1. Resources";
        // We check for Resource ScriptableObjects referenced by stations/resource objects
        var uniqueTypes = new HashSet<Resource>();
        foreach (var ro in resourceObjects)
        {
            if (ro.resourceType != null) uniqueTypes.Add(ro.resourceType);
        }

        // Also gather from stations
        var stations = Object.FindObjectsByType<Station>(FindObjectsSortMode.None);
        foreach (var s in stations)
        {
            if (s.stationData != null)
            {
                foreach (var r in s.stationData.consumes) if (r != null) uniqueTypes.Add(r);
                foreach (var r in s.stationData.produces) if (r != null) uniqueTypes.Add(r);
            }
        }

        if (uniqueTypes.Count > 0)
        {
            entries.Add(new RevisionEntry { category = cat, message = $"Found {uniqueTypes.Count} unique resource type(s) referenced in the scene.", status = EntryStatus.Pass });

            foreach (var r in uniqueTypes)
            {
                if (r.resourcePrefab == null)
                    entries.Add(new RevisionEntry { category = cat, message = $"Resource '{r.resourceName}' has no prefab assigned. Stations that spawn it will not create physical objects.", status = EntryStatus.Warning });
                if (r.icon == null)
                    entries.Add(new RevisionEntry { category = cat, message = $"Resource '{r.resourceName}' has no icon. UI elements will appear blank.", status = EntryStatus.Warning });
            }

            tier = 1;
        }
        else
        {
            entries.Add(new RevisionEntry { category = cat, message = "No resource types found in the scene. Create resources first using Game Assemblies > Resources > Resource Builder.", status = EntryStatus.Fail });
        }
    }

    private void CheckStations(Station[] stations, ref int tier)
    {
        string cat = "2. Stations";

        if (stations.Length == 0)
        {
            entries.Add(new RevisionEntry { category = cat, message = "No stations found in the scene. Use Game Assemblies > Stations > Station Builder to create one.", status = EntryStatus.Fail });
            return;
        }

        entries.Add(new RevisionEntry { category = cat, message = $"Found {stations.Length} station(s) in the scene.", status = EntryStatus.Pass });
        tier = Mathf.Max(tier, 2);

        int missingData = 0, missingProduce = 0, missingConsume = 0, missingLoot = 0;

        foreach (var s in stations)
        {
            if (s.stationData == null)
            {
                missingData++;
                continue;
            }

            var data = s.stationData;

            if (data.produceResource && data.whatToProduce == Station.productionMode.Resource && (data.produces == null || data.produces.Count == 0 || data.produces.All(r => r == null)))
                missingProduce++;

            if (data.consumeResource && (data.consumes == null || data.consumes.Count == 0 || data.consumes.All(r => r == null)))
                missingConsume++;

            if (data.whatToProduce == Station.productionMode.LootTable && s.produceLootTable == null)
                missingLoot++;
        }

        if (missingData > 0)
            entries.Add(new RevisionEntry { category = cat, message = $"{missingData} station(s) have no StationDataSO assigned. They will not function.", status = EntryStatus.Fail });
        if (missingProduce > 0)
            entries.Add(new RevisionEntry { category = cat, message = $"{missingProduce} station(s) are set to produce resources but have no output resources assigned.", status = EntryStatus.Warning });
        if (missingConsume > 0)
            entries.Add(new RevisionEntry { category = cat, message = $"{missingConsume} station(s) are set to consume resources but have no input resources assigned.", status = EntryStatus.Warning });
        if (missingLoot > 0)
            entries.Add(new RevisionEntry { category = cat, message = $"{missingLoot} station(s) are set to produce from a loot table but have no loot table assigned.", status = EntryStatus.Fail });

        if (missingData == 0 && missingProduce == 0 && missingConsume == 0 && missingLoot == 0)
            entries.Add(new RevisionEntry { category = cat, message = "All stations have valid data and resource assignments.", status = EntryStatus.Pass });
    }

    private void CheckResourceManager(ResourceManager rm, resourceManagerCanvas rmCanvas, ref int tier)
    {
        string cat = "3. Resource Manager";

        if (rm == null)
        {
            entries.Add(new RevisionEntry { category = cat, message = "No ResourceManager found. Use Game Assemblies > Systems > Create Resource Management System.", status = EntryStatus.Fail });
            return;
        }

        entries.Add(new RevisionEntry { category = cat, message = "ResourceManager is present in the scene.", status = EntryStatus.Pass });
        tier = Mathf.Max(tier, 3);

        if (rmCanvas == null)
            entries.Add(new RevisionEntry { category = cat, message = "ResourceManager_Canvas is missing. The resource UI, goal tracker, and score will not display.", status = EntryStatus.Warning });
        else
            entries.Add(new RevisionEntry { category = cat, message = "ResourceManager_Canvas is present.", status = EntryStatus.Pass });
    }

    private void CheckGoalManager(GoalManager gm, ref int tier)
    {
        string cat = "4. Goals";

        if (gm == null)
        {
            entries.Add(new RevisionEntry { category = cat, message = "No GoalManager found. Goals are part of the Resource Management System, or can be added separately.", status = EntryStatus.Fail });
            return;
        }

        entries.Add(new RevisionEntry { category = cat, message = "GoalManager is present in the scene.", status = EntryStatus.Pass });
        tier = Mathf.Max(tier, 4);

        if (gm.goalTracker == null)
            entries.Add(new RevisionEntry { category = cat, message = "GoalManager has no goalTracker prefab assigned. Goal tracker UI will not spawn.", status = EntryStatus.Warning });

        if (gm.goalTrackerGrid == null)
            entries.Add(new RevisionEntry { category = cat, message = "GoalManager has no goalTrackerGrid assigned. Goal trackers have no parent container.", status = EntryStatus.Warning });

        if (gm.scoreText == null)
            entries.Add(new RevisionEntry { category = cat, message = "GoalManager has no scoreText assigned. The global score will not display.", status = EntryStatus.Warning });
    }

    private void CheckLevelManager(LevelManager lm, ref int tier)
    {
        string cat = "5. Levels";

        if (lm == null)
        {
            entries.Add(new RevisionEntry { category = cat, message = "No LevelManager found. Use Game Assemblies > Systems > Create Level Manager to add one.", status = EntryStatus.Fail });
            return;
        }

        entries.Add(new RevisionEntry { category = cat, message = "LevelManager is present in the scene.", status = EntryStatus.Pass });
        tier = Mathf.Max(tier, 5);

        // Check levelDataList via SerializedObject since it's private
        var so = new SerializedObject(lm);
        var levelList = so.FindProperty("levelDataList");
        if (levelList != null)
        {
            int count = levelList.arraySize;
            int nullCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (levelList.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    nullCount++;
            }

            if (count == 0)
                entries.Add(new RevisionEntry { category = cat, message = "LevelManager has no levels loaded. Create levels with Game Assemblies > Levels > Create Level and assign them.", status = EntryStatus.Warning });
            else
            {
                entries.Add(new RevisionEntry { category = cat, message = $"LevelManager has {count} level(s) loaded.", status = EntryStatus.Pass });
                if (nullCount > 0)
                    entries.Add(new RevisionEntry { category = cat, message = $"{nullCount} level slot(s) are empty (null). Remove or assign them.", status = EntryStatus.Warning });
            }
        }
    }

    private void CheckGameStateManager(GameManager gm, ref int tier)
    {
        string cat = "6. Game States";

        if (gm == null)
        {
            entries.Add(new RevisionEntry { category = cat, message = "No GameManager found. Use Game Assemblies > Systems > Create Game State Manager to add one.", status = EntryStatus.Fail });
            return;
        }

        entries.Add(new RevisionEntry { category = cat, message = "GameManager (Game State Manager) is present in the scene.", status = EntryStatus.Pass });
        tier = Mathf.Max(tier, 6);

        if (gm.lvlManager == null)
        {
            var lm = Object.FindFirstObjectByType<LevelManager>();
            if (lm != null)
                entries.Add(new RevisionEntry { category = cat, message = "GameManager.lvlManager is not assigned, but a LevelManager exists in the scene. Link them in the Inspector.", status = EntryStatus.Warning });
        }
        else
        {
            entries.Add(new RevisionEntry { category = cat, message = "GameManager is linked to a LevelManager.", status = EntryStatus.Pass });
        }
    }

    private void CheckPlayers(UnityEngine.InputSystem.PlayerInputManager pim)
    {
        string cat = "Players";

        if (pim == null)
        {
            entries.Add(new RevisionEntry { category = cat, message = "No PlayerInputManager found. Use Game Assemblies > Players > Create Local Multiplayer System to add one.", status = EntryStatus.Info });
            return;
        }

        entries.Add(new RevisionEntry { category = cat, message = "PlayerInputManager (multiplayer) is present.", status = EntryStatus.Pass });

        if (pim.playerPrefab == null)
            entries.Add(new RevisionEntry { category = cat, message = "PlayerInputManager has no player prefab assigned.", status = EntryStatus.Warning });
    }

    // ─────────────────────────── CHAIN ANALYSIS ───────────────────────────

    /// <summary>
    /// Counts distinct resource chains: a chain is a path from a producing station
    /// to a consuming station via a shared resource type.
    /// </summary>
    private int CountResourceChains(Station[] stations)
    {
        var producers = new Dictionary<Resource, int>(); // resource -> number of stations that produce it
        var consumers = new Dictionary<Resource, int>(); // resource -> number of stations that consume it

        foreach (var s in stations)
        {
            if (s.stationData == null) continue;
            var data = s.stationData;

            if (data.produceResource)
            {
                foreach (var r in data.produces)
                {
                    if (r == null) continue;
                    if (!producers.ContainsKey(r)) producers[r] = 0;
                    producers[r]++;
                }
            }

            if (data.consumeResource)
            {
                foreach (var r in data.consumes)
                {
                    if (r == null) continue;
                    if (!consumers.ContainsKey(r)) consumers[r] = 0;
                    consumers[r]++;
                }
            }
        }

        // A chain exists when a resource is both produced and consumed
        int chains = 0;
        foreach (var kvp in producers)
        {
            if (consumers.ContainsKey(kvp.Key))
                chains += Mathf.Min(kvp.Value, consumers[kvp.Key]);
        }

        return chains;
    }

    // ─────────────────────────── COMPLEXITY ───────────────────────────

    private void AssessComplexity(Station[] stations, ResourceObject[] resourceObjects, int chains, int tier)
    {
        int stationCount = stations.Length;

        if (stationCount == 0)
        {
            complexityLabel = "Empty — No stations in the scene yet.";
            complexityColor = new Color(0.6f, 0.6f, 0.6f);
            return;
        }

        if (chains == 0)
        {
            complexityLabel = $"Simple — {stationCount} station(s), no resource chains detected. " +
                              "Stations produce or consume independently. Consider connecting outputs to inputs for a richer gameplay loop.";
            complexityColor = new Color(0.5f, 0.8f, 1f);
        }
        else if (chains <= 2)
        {
            complexityLabel = $"Moderate — {stationCount} station(s), {chains} resource chain(s). " +
                              "Players have a basic production flow. Adding more chains or conversion steps increases depth.";
            complexityColor = new Color(0.5f, 1f, 0.6f);
        }
        else if (chains <= 5)
        {
            complexityLabel = $"Complex — {stationCount} station(s), {chains} resource chain(s). " +
                              "Multiple conversion paths create interesting player decisions and coordination challenges.";
            complexityColor = new Color(1f, 0.9f, 0.4f);
        }
        else
        {
            complexityLabel = $"Highly Complex — {stationCount} station(s), {chains} resource chain(s). " +
                              "The scene has deep production networks. Great for experienced players; consider tutorials for onboarding.";
            complexityColor = new Color(1f, 0.6f, 0.4f);
        }

        if (tier < 3)
            complexityLabel += "\n\nNote: No Resource Manager yet — resource tracking and UI are not active.";
    }

    // ─────────────────────────── SUGGESTIONS ───────────────────────────

    private void BuildSuggestion(int tier, Station[] stations, ResourceManager rm, GoalManager gm, LevelManager lm, GameManager gsm)
    {
        // Suggest the next step based on the highest completed tier
        switch (tier)
        {
            case 0:
                suggestion = "Your scene has no game assembly components yet.\n\n" +
                             "Start by creating resources with Game Assemblies > Resources > Resource Builder, " +
                             "then build stations with Game Assemblies > Stations > Station Builder.";
                break;
            case 1:
                suggestion = "You have resources defined but no stations.\n\n" +
                             "Next step: Create stations that produce or consume your resources using " +
                             "Game Assemblies > Stations > Station Builder. Stations bring resources to life.";
                break;
            case 2:
                suggestion = "You have resources and stations, but no Resource Manager.\n\n" +
                             "Next step: Add the Resource Management System with " +
                             "Game Assemblies > Systems > Create Resource Management System. " +
                             "This enables resource tracking UI and the global score.";
                break;
            case 3:
                suggestion = "Resource Manager is set up. Consider adding goals to give players objectives.\n\n" +
                             "Next step: Create goals with Game Assemblies > Goals > Create Goal, " +
                             "then assign them to the GoalManager's goal templates or to levels.";
                break;
            case 4:
                suggestion = "Goals are configured. Consider adding a Level Manager to structure your game into levels.\n\n" +
                             "Next step: Create levels with Game Assemblies > Levels > Create Level, " +
                             "then add a Level Manager with Game Assemblies > Systems > Create Level Manager.";
                break;
            case 5:
                suggestion = "Levels are set up. The final step is adding a Game State Manager to handle menus, pausing, and win/fail screens.\n\n" +
                             "Next step: Game Assemblies > Systems > Create Game State Manager.";
                break;
            case 6:
                suggestion = "All core systems are present. Your scene has the full pipeline:\n" +
                             "Resources > Stations > Resource Manager > Goals > Levels > Game States.\n\n" +
                             "Fine-tune your stations, goals, and levels. Add players if you haven't already " +
                             "(Game Assemblies > Players > Create Local Multiplayer System).";
                break;
        }
    }
}
