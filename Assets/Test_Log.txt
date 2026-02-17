# Development Log - Project North

## Session: 2026-02-13
### Goals
- Refine Inventory UI layout (Top-Left) and visibility toggle ('I' key).
- Implement interaction feedback (Glow Effect) for items.
- Ensure reliable item pickup from fallen logs.

### Completed Tasks
1.  **Inventory UI Overhaul**:
    -   Moved `Inventory_Panel` to the **Top-Left** anchor (0, 1).
    -   Separated `QuickSlotHUD` to the **Bottom-Center** (0.5, 0) to remain visible during gameplay.
    -   Implemented `InventoryUI.Awake()` to force `SetActive(false)` on start, ensuring the inventory is hidden by default.
    -   Implemented Cursor Logic: Locked/Hidden when inventory is closed; Unlocked/Visible when open.

2.  **Interaction System**:
    -   Increased interaction distance in `PlayerInteraction.cs` to **10f**.
    -   Added Debug Log: "Item Hovered: [Name]" for immediate feedback.
    -   Implemented **Glow Effect** using `ChocDino.UIFX.GlowFilter`:
        -   Items glow **Yellow** with **Strength 2.0f** when hovered.
        -   Logic handles adding/removing the component dynamically.

3.  **Loot Spawning (`FallenLog.cs`)**:
    -   Forced spawned loot to the **"Item" Layer** immediately upon instantiation.
    -   Pre-attached a disabled `GlowFilter` component to loot for interaction readiness.

### Current Status
-   **Game Loop**: Chop Tree -> Log Falls -> Log Breaks -> Loot Spawns (Correct Layer/Components) -> Player Interacts (Glow) -> Pickup -> Inventory Updates.
-   **UI**: Inventory toggle works correctly with cursor management. Quick slots remain visible.

### Next Steps
-   **Playtest**: Verify the entire resource gathering loop in a build or extended play session.
-   **Polish**: check for any edge cases (e.g., inventory full).
-   **Feature**: Proceed to Crafting or Building system implementation.
