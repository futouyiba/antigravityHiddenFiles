# Voxel Data Visualization and Analysis Plan

## Goal
Deepen the analysis of the voxel map `Fishing_1006001_Global.npy` by inspecting Y-axis distribution and handling multiple bitmasks.

## User Review Required
None.

## Proposed Changes
Modify `d:\fishinggame\precompute\PrecomputeVisualization.ipynb`:

1.  **Y-Axis Distribution**:
    - Iterate through all Y indices (0 to 7).
    - Count non-zero elements for each Y.
    - Plot a simple 2D bar chart (matplotlib) inside the notebook to visualize the "shape" of the lake's volume at each depth.

2.  **Multi-Bitmask Analysis**:
    - Iterate through all non-zero voxels.
    - Check if `popcount(value)` > 1 (i.e., has more than just `FLAG_WATER`).
    - Note: `FLAG_WATER` is bit 0. So strictly speaking, `value & (value - 1) != 0` means multiple properties.
    - Count how many voxels have overlapping features (e.g., `FLAG_STONE | FLAG_DEEP_PIT`).
    - Identify common combinations.

3.  **Color Strategy Refinement**:
    - Based on common combinations, decide if the current priority list is sufficient.
    - Current Priority: Features (Pier/Driftwood) > Vegetation > Bottom Type > Structure (Deep Pit/Ridge) > Water.
    - *Rationale*: "Pixelation" of color means we must pick one. Usually, physical objects (Pier) are most important for collisions. Vegetation is important for gameplay. Bottom type is the "base". Structure flags (Deep Pit) are logical attributes that might overlap with bottom types.
    - *Proposal*: If a voxel has both `FLAG_STONE` (Bit 2) and `FLAG_DEEP_PIT` (Bit 5), which is more important to *see*? Probably Stone (visual material) is what renders, while Deep Pit is a logical zone.
    - I will print the findings and keep the priority logic unless I see a specific ambiguous case.

## Verification Plan
- Run the analysis cells.
- View the bar chart.
- Review the text output regarding multi-flag counts.
