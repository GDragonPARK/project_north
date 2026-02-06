# Development Log & Handover - Project North

**Last Updated:** 2026-02-06
**Status:** In Progress (Session Ended)

## 1. Project Vision (프로젝트 비전)
- **Concept**: **"Ghibli-style Valheim" (지브리풍 발헤임)**
- **Art Style**: Low Poly, Vibrant Colors (Ghibli Trees/Grass), Soft Lighting.
- **Core Loop**: Survival -> Gathering (Wood/Stone) -> Crafting (Workbench) -> Building -> Exploration.
- **Perspective**: Third Person (Over-the-shoulder / Free Look).

## 2. Current State (현재 상태)
### Folder Structure (Refactored)
The project follows a numbered structure for clarity:
- `Assets/0.Scene`: Main Scenes (SampleScene, etc).
- `Assets/1.Setting`: Input Actions, Render Pipelines, Physics Materials.
- `Assets/2.Model`: 3D Assets (KayKit Knight, Nature assets).
- `Assets/3.Script`: Core Codebase (`Player/`, `Build/`, `System/`, `Terrain/`, `Inventory/`).
- `Assets/98.Doc`: Planning & Documentation.
- `Assets/99.ThirdParty`: External Assets (StarterAssets, Cinemachine, etc).

### Refactoring Status
- **Architecture**: Separated Logic (`PlayerController`) from Data (`CharacterStats`).
- **Input System**: Migrated to **Unity New Input System**.
- **Terrain**: Moved from static fixed size to dynamic `TerrainGenerator` (Currently Scaled to 2049x2049).

## 3. Core Technical Settings (핵심 기술 설정)
**Player Controller (ThirdPersonController)**:
- **Grounded Offset**: `0.1` (Adjusted to prevent floating/sinking).
- **Grounded Radius**: `0.28`.
- **Ground Layers**: `Default`, `Terrain`.

**Weapon & Equipment**:
- **Socket**: Attached to `wrist.l` bone (Left Hand).
- **Axe Transform**:
  - Position: `(0.6, 0.3, -0.5)`
  - Rotation: `(-150, 30, 250)`
- **Input Keys**:
  - `1`: Axe (Equip/Toggle)
  - `2`: Pickaxe
  - `3-6`: Build Blueprints
  - `B`: Build Menu (Toggle)
  - `Mouse Scroll`: Camera Zoom

**Camera (Cinemachine)**:
- **Inverted Y-Axis**: Mouse Up -> Look Up.
- **Inverted X-Axis**: Mouse Right -> Look Right.
- **Zoom**: Controlled by Scroll Wheel (Radius modification).

## 4. Troubleshooting Log (트러블슈팅 기록)
- **T-Pose Issue**: Resolved by forcing Humanoid Avatar configuration and extracting the Avatar from the FBX properly.
- **Rigidbody Conflict**: Removed `Rigidbody` from the Model child object; ensured only Root object has `Rigidbody` to prevent physics explosions.
- **Stamina UI Sync**: Fixed `CharacterStats.cs` to auto-find UI elements (`StaminaBar`) in `Awake()` and force update, resolving the "UI shows full but log says empty" bug.
- **Input Freeze**: Created `CameraInputBridge.cs` to manually feed `StarterAssets` input into `Cinemachine` when the Input Provider failed.
- **Duplicate Managers**: Merged `Environment` and `Environment_Manager`.

## 5. Pending Issues (해결 필요 항목) - **CRITICAL**
- **Magenta (Pink) Terrain Error**: 
  - **Symptom**: Terrain appears pink/magenta due to missing Material or Texture references on the `TerrainLayer`.
  - **Attempted Fix**: Created `TextureEmergencyGen` tool to generate 2x2 textures.
  - **Next Step**: **MUST EXECUTE** `Antigravity > Emergency Fix: Generate & Apply Terrain Textures` immediately upon next startup. Verify Shader compatibility (`Nature/Terrain/Standard`).

## 6. Next TODO (다음 할 일)
1.  **Fix Magenta Terrain**: (See above). Priority #1.
2.  **Animation Polish**:
    - "Left Hand Swing": Currently uses generic Attack. Needs a specific Axe Swing animation.
    - "Arm Spread": Fix any residual T-Pose or stiffness during idle/run.
3.  **Building Mode UI**: Implement the actual Visual UI for the 'B' key (currently just logs to console).
4.  **Item Drop System**: Implement `Pickup` items spawning when Trees/Rocks are destroyed.
5.  **Infinite Terrain**: Transition from "Huge Static Terrain" to "Chunk-based Infinite Terrain" if the player reaches the edge.

---
*Created by Antigravity AI Assistant*
