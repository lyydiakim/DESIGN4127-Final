# Tutorial 07: Procedural Level Builder

The **Procedural Level Builder** is an editor tool for generating level content procedurally—placing prefabs, tiles, and environmental objects across your scene with configurable randomness and patterns. This tutorial explains what the tool can do today and what is planned for future releases.

## Development Status

> **Note:** The Procedural Level Builder is a feature **still in development**. The three tools described below are fully functional. Additional tools are planned and will be added over time. Your feedback and use cases help shape the roadmap.

---

## Opening the Tool

1. **Open the window**: `Game Assemblies → Environment → Procedural Level Builder`
2. The window shows a **Tools** dropdown. Select the tool you want to use.
3. Configure the parameters and click the execute button to generate content in the scene.

All generated objects are placed in the scene Hierarchy under a parent GameObject (e.g., "Scattered Prefabs", "Grid Prefabs"). You can move, scale, or delete the parent to adjust the result.

---

## Available Tools (Current)

### 1. Scatter Prefab

**Purpose:** Place prefabs randomly across a rectangular area with probability-based selection.

| Setting | Description |
|--------|-------------|
| **Prefabs to Scatter** | Add one or more prefabs. Each has a **%** (percent) that controls how often it is chosen relative to others. |
| **Total Instances** | How many prefabs to place (e.g., 50 rocks, 100 bushes). |
| **Axis Plane** | XY (2D top-down), XZ (3D ground), or YZ (vertical). |
| **Bounds** | Width (±) and Height (±) define the area. Positions are random within this range. |
| **Center** | Center point of the scatter area. |

**Use cases:** Rocks, bushes, trees, debris, pickups, or any object you want distributed randomly across a play area.

**Example:** Add "Rock" (70%) and "Bush" (30%). Set Total Instances to 80. Prefabs are placed randomly; roughly 70% will be rocks and 30% bushes.

---

### 2. Grid of Prefabs

**Purpose:** Fill a grid with prefabs. Support gaps (empty cells) and random offset for a less uniform look.

| Setting | Description |
|--------|-------------|
| **Prefabs to Instantiate** | Add prefabs with **%** for probability. Each cell picks one prefab based on these weights. |
| **Columns / Rows** | Grid size (e.g., 5×5, 10×20). |
| **Spacing** | Distance between grid cells. |
| **Axis Plane** | XY, XZ, or YZ. |
| **Center** | Center of the grid. |
| **% of Gaps** | Percentage of cells left empty (0–100). |
| **% with Random Offset** | Percentage of cells that get a random position offset. |
| **Offset Range (±)** | X and Y range for the random offset when a cell gets one. |

**Use cases:** Terrain tiles, floor patterns, wall segments, or any grid-based layout where you want some variation (gaps, slight misalignment).

**Example:** Add "GrassTile" (100%). Set 5×5 grid, 20% gaps, 30% random offset. You get a mostly filled grid with holes and slight position variation.

---

### 3. Perlin Noise Scatter

**Purpose:** Grid placement with Perlin noise as a mask. Prefabs are placed only where the noise value falls within a specified range, creating organic clusters and patterns.

| Setting | Description |
|--------|-------------|
| **Prefabs to Scatter** | Add prefabs with **%** for probability. |
| **Columns / Rows** | Grid size. |
| **Spacing** | Distance between grid cells. |
| **Axis Plane** | XY, XZ, or YZ. |
| **Center** | Center of the grid. |
| **Noise Value Range (Min / Max)** | Prefabs are placed only where noise (0–1) falls within this range. Narrow range = tighter clusters; wider range = more coverage. |
| **Noise Scale** | Size of noise features. Larger = bigger blobs; smaller = finer detail. |
| **Noise Offset (X / Y)** | Moves the noise pattern to get different placements. |
| **Random Position Offset** | Optional (±) offset per element for a less rigid grid. |
| **Random Scale per Object** | Apply random scale (min/max) to each placed prefab. |

**Use cases:** Terrain tiles, vegetation patches, clustered debris, or any placement where you want organic, non-uniform distribution.

**Example:** Add "Tree" (60%) and "Rock" (40%). Set noise range 0.6–1.0 so only "white" areas get objects. You get clusters of trees and rocks following the noise pattern.

---

## Roadmap (Planned)

The following tools are planned for future implementation. They appear in the Tools dropdown but show a placeholder message when selected:

| Planned Tool | Description |
|--------------|-------------|
| **Room / Wave** | Define rooms or wave-based layouts for structured level generation. |
| **Tilemap from Noise** | Generate Unity Tilemap layers from noise patterns. |
| **Path / Walkway** | Create paths or walkways between points. |
| **Spawn Points** | Place player or objective spawn points procedurally. |
| **Boundary / Fence** | Generate boundaries, fences, or walls around areas. |
| **Layered Placement** | Place multiple layers of prefabs (e.g., ground + decorations) with different rules. |

These tools are not yet implemented. Check the Changelog and package updates for release notes when they become available.

---

## Workflow Tips

1. **Create prefabs first.** Use **Create Environment Object** (Tutorial 06) or the Station Builder to create obstacles, tiles, and foliage. Assign those prefabs in the Procedural Level Builder.
2. **Use a parent.** Generated objects are grouped under a parent. Move the parent to reposition everything, or delete it to start over.
3. **Combine tools.** Run Scatter, then Grid, then Perlin Noise in different areas to build layered environments.
4. **Undo.** All placements are registered with Undo. Use Ctrl+Z (Cmd+Z) to revert.
5. **Iterate.** Adjust parameters and run again. Each run creates a new parent; delete the previous one if you don't want duplicates.

---

## Summary

| Tool | Status | Use For |
|------|--------|---------|
| Scatter Prefab | Available | Random placement across an area |
| Grid of Prefabs | Available | Grid layout with gaps and offset |
| Perlin Noise Scatter | Available | Organic, noise-masked placement |
| Room / Wave | Planned | Structured room layouts |
| Tilemap from Noise | Planned | Tilemap generation from noise |
| Path / Walkway | Planned | Paths and walkways |
| Spawn Points | Planned | Procedural spawn placement |
| Boundary / Fence | Planned | Boundaries and walls |
| Layered Placement | Planned | Multi-layer prefab placement |
