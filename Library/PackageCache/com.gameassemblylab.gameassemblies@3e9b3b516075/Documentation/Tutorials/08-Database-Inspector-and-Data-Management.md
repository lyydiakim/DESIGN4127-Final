# Tutorial 08: Database Inspector and Data Management

Game Assemblies uses **ScriptableObjects as databases** to store all game data—resources, goals, levels, stations, loot tables, and more. This tutorial explains why this paradigm is used and how the **Database Inspector** helps you view, edit, and validate your project's data in one place.

## Prerequisites

- Unity 2021.3 LTS or later
- Game Assemblies library imported
- Some data assets created (e.g., resources, goals, levels—see Tutorials 02–05)

---

## Part 1: Why ScriptableObjects as Databases?

Game Assemblies organizes game content as **databases**—collections of ScriptableObject assets stored in project folders. This approach offers several benefits for building data-driven games.

### Data-Driven Design

Game content is **separated from code**:

- **Data** (resources, goals, levels) lives in ScriptableObject assets
- **Logic** (stations, managers, players) lives in MonoBehaviours

You can add new resources, goals, or levels by creating assets—no code changes required. Designers iterate on balance and content by editing asset values in the Inspector.

### Single Source of Truth

Each resource, goal, or level exists as **one asset** that can be referenced by many objects:

- 50 stations can all reference the same "Wood" Resource asset
- 10 levels can reference the same "Collect Bread" goal
- No duplication of data; changes to the asset propagate everywhere

### Memory Efficiency

ScriptableObjects are loaded once and shared. Hundreds of stations referencing the same Resource don't duplicate its data—they hold a reference.

### Version Control Friendly

Assets are files (`.asset`) stored in folders like `Game Assemblies/Databases/Resources/`. Git/SVN can track changes. Diffs show exactly what was modified.

### Reusable Across Scenes

ScriptableObjects live in the Project, not in scenes. Any scene can reference them. Level data, goals, and resources are shared across your project.

### Organized by Type

Each data type has its own folder:

| Database | Folder | Purpose |
|----------|--------|---------|
| Resources | `Game Assemblies/Databases/Resources/` | Resource types (wood, bread, etc.) |
| Loot Tables | `Game Assemblies/Databases/LootTables/` | Weighted random drop tables |
| Levels | `Game Assemblies/Databases/Levels/` | Level configuration and goal lists |
| Resource Goals | `Game Assemblies/Databases/Goals/` | Goal objectives (collect X of Y) |
| Station Data | `Game Assemblies/Databases/Stations/` | Station configuration (inputs, outputs) |
| Color Palettes | `Game Assemblies/Databases/Color Palettes/` | Color palettes for art |
| Resource Managers | `Game Assemblies/Databases/Resource Managers/` | ResourceManager configuration |
| Rules | `Game Assemblies/Databases/Rules/` | Rule definitions |
| Rules Sessions | `Game Assemblies/Databases/Rules Sessions/` | Rule session configuration |

---

## Part 2: Opening the Database Inspector

1. **Open the window**: `Game Assemblies → Databases → Database Inspector`
2. The window has two panels:
   - **Left**: List of all assets for the selected database type
   - **Right**: Inspector for the selected asset (view and edit)

---

## Part 3: Browsing and Searching

### Selecting a Database Type

Use the **Database Type** dropdown to switch between:

- **Resources** — All Resource assets
- **Loot Tables** — All LootTable assets
- **Levels** — All LevelDataSO assets
- **Resource Goals** — All ResourceGoalSO assets
- **Color Palettes** — All ColorPaletteSO assets
- **Resource Managers** — All ResourceManager_SO assets
- **Rules** — All RuleSO assets
- **Rules Sessions** — All RulesSessionSO assets
- **Station Data** — All StationDataSO assets

The list shows every asset of that type in your project (including package and Assets folders). Entries are sorted alphabetically by name.

### Search

Enter text in the **Search** field to filter the list. Only assets whose names contain the search text (case-insensitive) are shown.

### Selecting an Asset

Click an asset name in the list to:
- View and edit its properties in the right panel
- Select it in the Project window
- See a thumbnail (if the asset has a sprite or texture)

Click the **→** (Select in Project) button to focus the asset in the Project window.

---

## Part 4: Editing Data

The right panel shows the standard Unity Inspector for the selected asset. You can:

- Edit any serialized field (name, icon, prefab, time limit, etc.)
- Changes are saved when you save the project or switch selection
- The Database Inspector uses the same Inspector as the Project window—no special editing mode

This is useful when you have many assets: you can browse by type and refine without hunting through folders.

---

## Part 5: Validation

The Database Inspector can check all databases for **missing references**—object reference fields that are null when they probably shouldn't be.

### Running Validation

1. Click the **Validate** button in the toolbar
2. The tool scans all database types and checks each asset
3. A report is written to the **Console** (Window → General → Console)
4. Validation indicators appear next to each asset in the list:
   - **Green** — No missing references
   - **Red** — One or more missing references (needs fixing)
   - **Gray** — Not yet validated

### What Is Checked

The validator looks at object reference fields (e.g., `Resource`, `Sprite`, `GameObject`). If a field is null and it's not in the "optional" list (e.g., `previewImage`, `icon`), it's reported as missing.

**Example:** A LevelDataSO with a null entry in its `sequentialGoals` list would be flagged. A Resource with no `icon` is *not* flagged (icon is optional).

### Interpreting the Report

Open the Console after validating. The report lists each database, each asset, and any missing reference paths. Use this to fix broken references before runtime.

---

## Part 6: Resource-Specific Tools

When you select a **Resource** asset, the Database Inspector shows extra **Prefab Tools**:

| Tool | Purpose |
|------|---------|
| **Update Sprite** | Copies the Resource's icon to the SpriteRenderer on the linked prefab. Use when you change the resource icon and want the prefab to match. |
| **Update Collider** | Resizes the prefab's Collider2D (Box or Circle) to match the sprite bounds. Use when you change the sprite and want the collider to fit. |

**Requirements:** The Resource must have a `resourcePrefab` assigned. For Update Sprite, the Resource must have an `icon` assigned.

---

## Part 7: Refresh

If you create new assets (e.g., via Create Goal, Create Level) while the Database Inspector is open, the list may not update immediately. Click **Refresh** to reload the current database type.

---

## Part 8: Workflow Tips

1. **Central hub:** Use the Database Inspector as a central place to review and edit all game data. Switch database types to audit resources, goals, levels, or stations.
2. **Validate regularly:** Run Validate after creating or moving assets to catch missing references early.
3. **Search for duplicates:** Use the search to find assets with similar names and consolidate or rename as needed.
4. **Resource prefab sync:** When you update a Resource's icon or sprite, use Update Sprite and Update Collider so physical prefabs stay in sync.
5. **Bulk checks:** Validate scans all databases at once. Use the Console report to fix issues across the project.

---

## Summary

| Feature | Purpose |
|---------|---------|
| **Database Type dropdown** | Switch between Resources, Goals, Levels, Stations, etc. |
| **List panel** | Browse all assets of the selected type |
| **Search** | Filter the list by asset name |
| **Inspector panel** | View and edit the selected asset |
| **Validate** | Check all databases for missing references |
| **Refresh** | Reload the list after creating new assets |
| **Prefab Tools** (Resources) | Update Sprite and Update Collider on linked prefabs |

The **ScriptableObject-as-database** paradigm keeps your game data organized, reusable, and easy to iterate on. The Database Inspector is the tool to manage it all in one place.
