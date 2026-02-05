# Development Log

## 1. Project Vision
**'Ghibli-style Valheim'**
- **Aesthetics**: Ghibli-inspired cel-shaded graphics (Vibrant green grass, fluffy clouds).
- **Gameplay**: Valheim's core survival loop (Gather -> Craft -> Build -> Survive).

## 2. Current Status
### Folder Structure
- **Assets/3.Script**: Core game logic (MVC pattern enforced for Inventory/UI).
- **Assets/Editor/Antigravity_Tools**: All editor utilities (Separated).
- **Assets/3.Script/Inventory**: New MVC Inventory System (`InventoryManager`).

### Refactoring Status
- **Inventory**: Migrated to MVC. `InventoryItem` (ScriptableObject) is the Safe source of truth.
- **Axe Logic**: `AxeAdjuster` script DISABLED. Now relies on **Hierarchy Logic** (`wrist.l` parent).
- **Resource Safety**: All critical scripts (`AxeAdjuster`, `InventoryManager`, `ResourceObject`) now feature robust null-checking and auto-recovery.

## 3. Critical Technical Settings
| System | Parameter | Value / Logic |
| :--- | :--- | :--- |
| **Player** | Grounded Offset | **0.1** (Raycast starts 0.1 unadjusted) |
| **Player** | Ground Check Dist | **0.2f** (Code value) |
| **Player** | Ground Layer | **Default, Terrain** (Mixed mask) |
| **Terrain** | Amplitude | **8.0** |
| **Terrain** | Frequency | **0.012** (Smooth hills) |
| **Bones** | Axe Socket | **LeftHand** (`wrist.l`) (Manual Reparent Required) |
| **Axe** | Best Transform | **Pos(0,0,0), Rot(0,30,90)** (User Approved) |
| **Axe** | Forward Logic | **Pure Hierarchy** (Script Override Disabled) |
| **Build** | Ghost Snap | Raycast to `Default/Terrain`, tag `SnapPoint` |

## 4. Troubleshooting Log
- **T-Pose Fix**: Resolved by forcing `Avatar` re-configuration and strictly using `Animator.GetBoneTransform`.
- **Rigidbody Issues**: Removed non-kinematic Rigidbody usage from `MyPlayerController` to prevent fighting with `CharacterController` / `ThirdPersonController`.
- **Duplicate Scripts**: `InventoryManager.cs` duplicate found in `System/` folder -> **Deleted**. Only `Assets/3.Script/Inventory/` remains.
- **Axe Visibility**: Script override disabled. `axe_1handed` MUST be a child of `wrist.l`.
- **Axe Alignment**: Validated "Natural" values: **Pos `0, 0, 0` / Rot `0, 30, 90`**.

## 5. Next Steps (TODO)
- **Animation**: Fix "Arm Spread" T-pose residue during idle.
- **Combat**: Implement Left Hand Wave / Attack animation logic.
- **Loot**: Implement physical Item Drop system.
- **Build**: Add physical `SnapPoint` GameObjects to building prefabs.

---
**Last Updated**: 2026-02-05
**Status**: Ready for Next Session.
