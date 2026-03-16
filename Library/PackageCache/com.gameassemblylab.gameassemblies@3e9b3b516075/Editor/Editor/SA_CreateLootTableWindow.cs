using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class SA_CreateLootTableWindow : EditorWindow
{
    private string lootTableName = "NewLootTable";
    private Vector2 scrollPosition;
    private List<ResourceEntryEditor> resourceEntries = new List<ResourceEntryEditor>();
    private Texture2D tutorialImage;
    private SerializedObject serializedLootTable;
    private LootTable currentLootTable;
    private bool showExistingLootTables = false;
    private List<LootTable> existingLootTables = new List<LootTable>();
    private int selectedLootTableIndex = -1;

    // Class to handle resource entries in the editor
    [System.Serializable]
    private class ResourceEntryEditor
    {
        public Resource resource;
        public float dropPercentage = 0f;
        public int quantity = 1;
    }

    // Adds a menu item under Game Assemblies -> Loot Tables
    [MenuItem("Game Assemblies/Resources/Create Loot Table")]
    public static void ShowWindow()
    {
        GetWindow<SA_CreateLootTableWindow>("Create Loot Table");
    }

    private void OnEnable()
    {
        tutorialImage = SA_AssetPathHelper.FindAsset<Texture2D>("Samples/2d Assets/Asset02b.png");

        if (resourceEntries.Count == 0)
        {
            resourceEntries.Add(new ResourceEntryEditor());
        }

        RefreshExistingLootTables();
    }

    private void RefreshExistingLootTables()
    {
        existingLootTables.Clear();
        string[] searchFolders = SA_AssetPathHelper.GetAssetSearchFolders("Game Assemblies/Databases/LootTables");
        string[] guids = AssetDatabase.FindAssets("t:LootTable", searchFolders);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            LootTable lootTable = AssetDatabase.LoadAssetAtPath<LootTable>(path);
            if (lootTable != null)
            {
                existingLootTables.Add(lootTable);
            }
        }
    }

    private void OnGUI()
    {
        // Display tutorial image if available
        //if (tutorialImage != null)
        //{
            // Adjust size as needed
            //EditorGUILayout.LabelField(new GUIContent(tutorialImage), GUILayout.Height(100));
        //}

        EditorGUILayout.Space(10);

        // Toggle between create new and edit existing
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Toggle(!showExistingLootTables, "Create New", EditorStyles.toolbarButton))
        {
            showExistingLootTables = false;
        }
        if (GUILayout.Toggle(showExistingLootTables, "Edit Existing", EditorStyles.toolbarButton))
        {
            showExistingLootTables = true;
            RefreshExistingLootTables();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        if (showExistingLootTables)
        {
            DrawExistingLootTablesPanel();
        } else
        {
            DrawCreateLootTablePanel();
        }
    }

    private void DrawExistingLootTablesPanel()
    {
        EditorGUILayout.LabelField("Edit Existing Loot Tables", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        if (existingLootTables.Count == 0)
        {
            EditorGUILayout.HelpBox("No loot tables found in Game Assemblies/Databases/LootTables (Assets or package).", MessageType.Info);
            return;
        }

        string[] lootTableNames = existingLootTables.Select(lt => lt.name).ToArray();
        int newSelectedIndex = EditorGUILayout.Popup("Select Loot Table", selectedLootTableIndex, lootTableNames);

        if (newSelectedIndex != selectedLootTableIndex)
        {
            selectedLootTableIndex = newSelectedIndex;
            if (selectedLootTableIndex >= 0 && selectedLootTableIndex < existingLootTables.Count)
            {
                LoadLootTable(existingLootTables[selectedLootTableIndex]);
            }
        }

        if (selectedLootTableIndex >= 0 && currentLootTable != null)
        {
            EditorGUILayout.Space(10);
            serializedLootTable = new SerializedObject(currentLootTable);
            serializedLootTable.Update();

            // Get the possibleLoot list
            SerializedProperty lootEntries = serializedLootTable.FindProperty("possibleLoot");

            // Manual drawing of loot entries with better control over UI
            EditorGUILayout.LabelField("Loot Entries", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            // Show add button for new entries
            if (GUILayout.Button("Add New Entry", GUILayout.Width(120)))
            {
                lootEntries.arraySize++;
                serializedLootTable.ApplyModifiedProperties();
            }

            EditorGUILayout.Space(5);

            // Draw each entry with proper controls
            for (int i = 0; i < lootEntries.arraySize; i++)
            {
                SerializedProperty entry = lootEntries.GetArrayElementAtIndex(i);
                SerializedProperty resourceProp = entry.FindPropertyRelative("resource");
                SerializedProperty percentageProp = entry.FindPropertyRelative("dropPercentage");
                SerializedProperty quantityProp = entry.FindPropertyRelative("quantity");

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(resourceProp, new GUIContent("Resource"));

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    lootEntries.DeleteArrayElementAtIndex(i);
                    serializedLootTable.ApplyModifiedProperties();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    i--; // Adjust index since we removed an element
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                // Explicitly draw the slider for drop percentage
                float currentPercentage = percentageProp.floatValue;
                EditorGUI.BeginChangeCheck();
                float newPercentage = EditorGUILayout.Slider("Drop %", currentPercentage, 0.01f, 100f);
                if (EditorGUI.EndChangeCheck())
                {
                    percentageProp.floatValue = newPercentage;
                    serializedLootTable.ApplyModifiedProperties();
                }

                EditorGUILayout.PropertyField(quantityProp, new GUIContent("Quantity", "How many of this resource to generate when this entry is selected."));

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            EditorGUI.indentLevel--;

            // Calculate and display total percentage
            float totalPercentage = 0f;
            for (int i = 0; i < lootEntries.arraySize; i++)
            {
                SerializedProperty entry = lootEntries.GetArrayElementAtIndex(i);
                SerializedProperty percentage = entry.FindPropertyRelative("dropPercentage");
                totalPercentage += percentage.floatValue;
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField($"Total Drop Percentage: {totalPercentage:F2}%",
                totalPercentage >= 99.9f && totalPercentage <= 100.1f
                    ? EditorStyles.label
                    : EditorStyles.boldLabel);

            if (totalPercentage < 99.9f || totalPercentage > 100.1f)
            {
                EditorGUILayout.HelpBox($"Total should be 100%. Current: {totalPercentage:F2}%", MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Normalize Percentages"))
            {
                // Directly normalize using SerializedProperty for immediate UI update
                if (totalPercentage > 0)
                {
                    float factor = 100f / totalPercentage;
                    for (int i = 0; i < lootEntries.arraySize; i++)
                    {
                        SerializedProperty entry = lootEntries.GetArrayElementAtIndex(i);
                        SerializedProperty percentageProp = entry.FindPropertyRelative("dropPercentage");
                        percentageProp.floatValue *= factor;
                    }
                    serializedLootTable.ApplyModifiedProperties();
                }
            }

            if (GUILayout.Button("Balance Percentages"))
            {
                // Directly balance using SerializedProperty for immediate UI update
                int validEntries = 0;
                for (int i = 0; i < lootEntries.arraySize; i++)
                {
                    SerializedProperty entry = lootEntries.GetArrayElementAtIndex(i);
                    SerializedProperty resourceProp = entry.FindPropertyRelative("resource");
                    if (resourceProp.objectReferenceValue != null)
                    {
                        validEntries++;
                    }
                }

                if (validEntries > 0)
                {
                    float equalPercentage = 100f / validEntries;
                    for (int i = 0; i < lootEntries.arraySize; i++)
                    {
                        SerializedProperty entry = lootEntries.GetArrayElementAtIndex(i);
                        SerializedProperty resourceProp = entry.FindPropertyRelative("resource");
                        SerializedProperty percentageProp = entry.FindPropertyRelative("dropPercentage");

                        if (resourceProp.objectReferenceValue != null)
                        {
                            percentageProp.floatValue = equalPercentage;
                        } else
                        {
                            percentageProp.floatValue = 0f;
                        }
                    }
                    serializedLootTable.ApplyModifiedProperties();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Save Changes", GUILayout.Height(30)))
            {
                // Make sure all modifications are applied
                serializedLootTable.ApplyModifiedProperties();
                EditorUtility.SetDirty(currentLootTable);
                AssetDatabase.SaveAssets();

                // Verify changes were saved
                float verifiedTotal = 0f;
                SerializedObject verifyObj = new SerializedObject(currentLootTable);
                SerializedProperty verifyEntries = verifyObj.FindProperty("possibleLoot");
                for (int i = 0; i < verifyEntries.arraySize; i++)
                {
                    SerializedProperty entry = verifyEntries.GetArrayElementAtIndex(i);
                    SerializedProperty percentage = entry.FindPropertyRelative("dropPercentage");
                    verifiedTotal += percentage.floatValue;
                }

                string message = $"Loot table '{currentLootTable.name}' has been updated successfully.\nTotal percentage: {verifiedTotal:F2}%";
                EditorUtility.DisplayDialog("Loot Table Updated", message, "OK");

                // Refresh to ensure we're seeing the latest data
                serializedLootTable.Update();
            }
        }
    }

    private void LoadLootTable(LootTable lootTable)
    {
        currentLootTable = lootTable;
    }

    private void DrawCreateLootTablePanel()
    {
        EditorGUILayout.LabelField("Create a New Loot Table", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        lootTableName = EditorGUILayout.TextField("Loot Table Name", lootTableName);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Resources and Drop Chances", EditorStyles.boldLabel);

        // Calculate and display total percentage
        float totalPercentage = resourceEntries.Sum(entry => entry.dropPercentage);
        EditorGUILayout.LabelField($"Total Drop Percentage: {totalPercentage:F2}%",
            totalPercentage >= 99.9f && totalPercentage <= 100.1f
                ? EditorStyles.label
                : EditorStyles.boldLabel);

        if (totalPercentage < 99.9f || totalPercentage > 100.1f)
        {
            EditorGUILayout.HelpBox($"Total should be 100%. Current: {totalPercentage:F2}%", MessageType.Warning);
        }

        EditorGUILayout.Space(5);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Draw entries list
        for (int i = 0; i < resourceEntries.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");

            ResourceEntryEditor entry = resourceEntries[i];

            EditorGUILayout.BeginHorizontal();
            entry.resource = (Resource)EditorGUILayout.ObjectField("Resource", entry.resource, typeof(Resource), false);

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                resourceEntries.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                continue;
            }
            EditorGUILayout.EndHorizontal();

            entry.dropPercentage = EditorGUILayout.Slider("Drop %", entry.dropPercentage, 0.01f, 100f);
            entry.quantity = Mathf.Max(1, EditorGUILayout.IntField(new GUIContent("Quantity", "How many of this resource to generate when this entry is selected."), entry.quantity));

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        // Button controls
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Resource"))
        {
            resourceEntries.Add(new ResourceEntryEditor());
        }

        if (GUILayout.Button("Normalize Percentages"))
        {
            NormalizePercentages();
        }

        if (GUILayout.Button("Balance Percentages"))
        {
            BalancePercentages();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(20);

        EditorGUILayout.HelpBox(
            "This will create a new LootTable scriptable object at: Assets/Game Assemblies/Databases/LootTables/",
            MessageType.Info);

        if (GUILayout.Button("Create Loot Table", GUILayout.Height(30)))
        {
            CreateLootTable();
        }
    }

    private void NormalizePercentages()
    {
        float total = resourceEntries.Sum(entry => entry.dropPercentage);
        if (total <= 0) return;

        float factor = 100f / total;
        foreach (var entry in resourceEntries)
        {
            entry.dropPercentage *= factor;
        }
    }

    private void BalancePercentages()
    {
        int validEntries = resourceEntries.Count(entry => entry.resource != null);
        if (validEntries == 0) return;

        float equalPercentage = 100f / validEntries;
        foreach (var entry in resourceEntries)
        {
            if (entry.resource != null)
            {
                entry.dropPercentage = equalPercentage;
            } else
            {
                entry.dropPercentage = 0f;
            }
        }
    }

    private void CreateLootTable()
    {
        // Validation checks
        if (string.IsNullOrEmpty(lootTableName))
        {
            EditorUtility.DisplayDialog("Error", "Please enter a name for the loot table.", "OK");
            return;
        }

        if (resourceEntries.Count(entry => entry.resource != null) == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please add at least one resource to the loot table.", "OK");
            return;
        }

        SA_AssetPathHelper.EnsureAssetPathDirectories("Game Assemblies/Databases/LootTables");
        string folderPath = "Assets/Game Assemblies/Databases/LootTables";

        // Create the loot table asset
        string assetPath = $"{folderPath}/{lootTableName}.asset";

        // Check if asset already exists
        if (AssetDatabase.LoadAssetAtPath<LootTable>(assetPath) != null)
        {
            if (!EditorUtility.DisplayDialog("Overwrite?",
                $"A loot table named '{lootTableName}' already exists. Do you want to overwrite it?",
                "Yes", "No"))
            {
                return;
            }
        }

        // Create the loot table instance
        LootTable newLootTable = ScriptableObject.CreateInstance<LootTable>();

        // Create or overwrite the asset first, so we can modify it through SerializedObject
        AssetDatabase.CreateAsset(newLootTable, assetPath);

        // Use SerializedObject to ensure proper value setting
        SerializedObject serializedLootTable = new SerializedObject(newLootTable);
        SerializedProperty lootEntries = serializedLootTable.FindProperty("possibleLoot");

        // Clear existing entries
        lootEntries.ClearArray();

        // Add each resource with its percentage
        foreach (var entry in resourceEntries.Where(e => e.resource != null))
        {
            lootEntries.arraySize++;
            SerializedProperty newEntry = lootEntries.GetArrayElementAtIndex(lootEntries.arraySize - 1);
            SerializedProperty resourceProp = newEntry.FindPropertyRelative("resource");
            SerializedProperty percentageProp = newEntry.FindPropertyRelative("dropPercentage");
            SerializedProperty quantityProp = newEntry.FindPropertyRelative("quantity");

            resourceProp.objectReferenceValue = entry.resource;
            percentageProp.floatValue = entry.dropPercentage;
            quantityProp.intValue = Mathf.Max(1, entry.quantity);
        }

        // Apply the changes
        serializedLootTable.ApplyModifiedProperties();
        EditorUtility.SetDirty(newLootTable);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Ensure the created loot table has percentages that sum to 100%
        newLootTable.ValidatePercentages();
        EditorUtility.SetDirty(newLootTable);
        AssetDatabase.SaveAssets();

        // Select the created asset
        Selection.activeObject = newLootTable;

        // Verify the final values
        float totalPercentage = 0f;
        SerializedObject verifyObj = new SerializedObject(newLootTable);
        SerializedProperty verifyEntries = verifyObj.FindProperty("possibleLoot");
        for (int i = 0; i < verifyEntries.arraySize; i++)
        {
            SerializedProperty entry = verifyEntries.GetArrayElementAtIndex(i);
            SerializedProperty percentage = entry.FindPropertyRelative("dropPercentage");
            totalPercentage += percentage.floatValue;
        }

        string verificationMsg = totalPercentage >= 99.9f && totalPercentage <= 100.1f
            ? $"Loot table '{lootTableName}' created successfully with proper percentages (sum: {totalPercentage:F2}%)."
            : $"Loot table created, but percentages may need adjustment (sum: {totalPercentage:F2}%).";

        Debug.Log($"{verificationMsg} Path: {assetPath}");

        // Reset form for new entry
        lootTableName = "NewLootTable";
        resourceEntries.Clear();
        resourceEntries.Add(new ResourceEntryEditor());

        // Refresh existing loot tables
        RefreshExistingLootTables();
    }
}