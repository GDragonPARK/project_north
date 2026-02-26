using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Phase 3 Building Setup Tool.
/// Adds "Building" layer, SnapPoints to Real prefabs, and updates BuildingManager.
/// Menu: Tools > Project North > Setup Basic Building
/// </summary>
public class BuildingSetupTool : Editor
{
    private const string PREFAB_FOLDER = "Assets/Prefabs/Building";
    private const string GROUND_LAYER   = "Ground";
    private const string BUILDING_LAYER = "Building";

    private struct PieceDef
    {
        public string    name;
        public Vector3   scale;
        public Vector3   rotation;
        public Color     color;
        public SnapType  snapType;
    }

    private static readonly PieceDef[] Pieces = new PieceDef[]
    {
        new PieceDef { name="WoodFloor",   scale=new Vector3(3f,0.25f,3f), rotation=Vector3.zero,          color=new Color(0.55f,0.35f,0.15f,1f), snapType=SnapType.Floor },
        new PieceDef { name="WoodWall",    scale=new Vector3(3f,3f,0.25f), rotation=Vector3.zero,          color=new Color(0.5f, 0.30f,0.12f,1f), snapType=SnapType.Wall  },
        new PieceDef { name="WoodRoof_45", scale=new Vector3(3f,0.25f,3f), rotation=new Vector3(-45f,0,0), color=new Color(0.45f,0.25f,0.10f,1f), snapType=SnapType.Roof  },
        new PieceDef { name="WoodRoof_30", scale=new Vector3(3f,0.25f,3f), rotation=new Vector3(-30f,0,0), color=new Color(0.40f,0.22f,0.10f,1f), snapType=SnapType.Roof  },
    };

    [MenuItem("Tools/Project North/Setup Basic Building")]
    public static void SetupBasicBuilding()
    {
        Debug.Log("[BuildingSetup] Phase 3 setup starting...");

        EnsureLayer(GROUND_LAYER);
        EnsureLayer(BUILDING_LAYER);
        EnsureLayer("SnapPoint"); // ★ 추가: SnapPoint 레이어
        int groundLayerIdx   = LayerMask.NameToLayer(GROUND_LAYER);
        int buildingLayerIdx = LayerMask.NameToLayer(BUILDING_LAYER);
        EnsureFolder(PREFAB_FOLDER);

        // ── 1. Create prefabs with SnapPoints ────────────────────────────────
        var realPrefabs  = new GameObject[Pieces.Length];
        var ghostPrefabs = new GameObject[Pieces.Length];

        for (int i = 0; i < Pieces.Length; i++)
        {
            var def = Pieces[i];

            realPrefabs[i]  = CreateOrLoadRealPrefab(
                def.name + "_Real", def.scale, def.rotation,
                CreateSolidMaterial(def.name + "_Real", def.color),
                buildingLayerIdx, def.snapType);

            // ★ Ghost는 Real을 복제하여 생성 (소켓 유실 원천 차단)
            ghostPrefabs[i] = CreateGhostFromReal(
                realPrefabs[i], def.name + "_Ghost",
                CreateGhostMaterial(def.name + "_Ghost"));
        }

        // ── 2. Ensure BuildingManager ─────────────────────────────────────────
        BuildingManager bm = Object.FindFirstObjectByType<BuildingManager>();
        if (bm == null)
        {
            var go = new GameObject("BuildingManager_System");
            bm = go.AddComponent<BuildingManager>();
            var audioSrc = go.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
            Undo.RegisterCreatedObjectUndo(go, "Create BuildingManager");
            Debug.Log("[BuildingSetup] Created BuildingManager_System.");
        }
        else
        {
            bm.gameObject.name = "BuildingManager_System";
            var audioSrc = bm.GetComponent<AudioSource>();
            if (audioSrc == null)
            {
                audioSrc = bm.gameObject.AddComponent<AudioSource>();
                audioSrc.playOnAwake = false;
            }
        }

        // ── 2.5 Clean up existing _Instance objects ──────────────────────────
        // ★ 4.15-4: 쓰레기 오브젝트 딥클린 (클론 증식 원천 차단)
        var existingGhosts = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var eg in existingGhosts)
        {
            // EndsWith는 "(Clone)" 접미사나 공백 때문에 매칭 실패 가능성이 높으므로 Contains 사용
            // Ghost 인스턴스로 추정되는, 씬에 남은 찌꺼기들 전부 파괴
            if (eg != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(eg)) && 
                (eg.name.Contains("_Ghost") || eg.name.Contains("_Instance")))
            {
                Undo.DestroyObjectImmediate(eg);
            }
        }

        // 추가 안전장치: BuildingManager의 기존 자식들도 모두 정리
        if (bm != null && bm.transform.childCount > 0)
        {
            for (int i = bm.transform.childCount - 1; i >= 0; i--)
            {
                var child = bm.transform.GetChild(i).gameObject;
                if (child.name.Contains("_Ghost") || child.name.Contains("_Instance"))
                {
                    Undo.DestroyObjectImmediate(child);
                }
            }
        }

        // ── 3. Instantiate ghost instances & assign piece list ─────────────────
        Undo.RecordObject(bm, "Assign BuildingManager Fields");
        bm.availablePieces.Clear();

        for (int i = 0; i < Pieces.Length; i++)
        {
            string ghostName = Pieces[i].name + "_Ghost_Instance";
            GameObject ghostInst = PrefabUtility.InstantiatePrefab(ghostPrefabs[i]) as GameObject;
            if (ghostInst != null)
            {
                ghostInst.name = ghostName;
                ghostInst.transform.SetParent(bm.transform); // Make it a child of BuildingManager_System
                ghostInst.SetActive(false);
                // Ghost should not block placement raycast
                SetLayerRecursive(ghostInst, LayerMask.NameToLayer("Ignore Raycast"));
                Undo.RegisterCreatedObjectUndo(ghostInst, "Create Ghost Instance");
            }

            bm.availablePieces.Add(new BuildingPieceEntry
            {
                pieceName   = Pieces[i].name,
                realPrefab  = realPrefabs[i],
                ghostPrefab = ghostInst
            });
        }

        bm.buildableLayer = LayerMask.GetMask(GROUND_LAYER, BUILDING_LAYER, "Terrain", "Default");
        EditorUtility.SetDirty(bm);

        // ── 4. Tag Terrain/ground objects ──────────────────────────────────────
        foreach (var t in Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None))
        {
            Undo.RecordObject(t.gameObject, "Set Terrain Layer");
            t.gameObject.layer = groundLayerIdx;
            EditorUtility.SetDirty(t.gameObject);
        }
        foreach (string gName in new[] { "Ground", "Floor", "Plane" })
        {
            var g = GameObject.Find(gName);
            if (g != null) { Undo.RecordObject(g, "Set Ground Layer"); g.layer = groundLayerIdx; }
        }

        // ── 5. Build UI ────────────────────────────────────────────────────────
        CreateBuildingUI(bm);

        // ── 6. ★ 자동 검증: Real vs Ghost 소켓 개수 비교 ──────────────────────
        bool allMatch = true;
        for (int i = 0; i < Pieces.Length; i++)
        {
            string pieceName = Pieces[i].name;
            int realCount  = realPrefabs[i] != null
                ? realPrefabs[i].GetComponentsInChildren<SnapPoint>(true).Length : 0;
            int ghostCount = ghostPrefabs[i] != null
                ? ghostPrefabs[i].GetComponentsInChildren<SnapPoint>(true).Length : 0;

            if (realCount != ghostCount)
            {
                Debug.LogError($"[치명적 오류] {pieceName}의 Ghost 소켓 개수가 Real과 다릅니다! (Real: {realCount}, Ghost: {ghostCount})");
                allMatch = false;
            }
            else
            {
                Debug.Log($"[BuildingSetup] ✅ {pieceName} 소켓 검증 통과 (Real: {realCount}, Ghost: {ghostCount})");
            }
        }

        if (allMatch)
            Debug.Log("[BuildingSetup] ✅ 모든 프리팹의 소켓 개수가 일치합니다!");

        Selection.activeGameObject = bm.gameObject;
        Debug.Log("[BuildingSetup] ✅ Phase 3 setup complete!");
    }

    // ── Prefab creation ───────────────────────────────────────────────────────

    private static GameObject CreateOrLoadRealPrefab(string prefabName, Vector3 scale,
        Vector3 rotation, Material mat, int layerIdx, SnapType snapType)
    {
        string path = $"{PREFAB_FOLDER}/{prefabName}.prefab";
        // Always recreate to ensure snappoints are fresh
        // (delete old if exists to avoid duplication)
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            AssetDatabase.DeleteAsset(path);

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = prefabName;
        go.transform.localScale    = scale;
        go.transform.localRotation = Quaternion.Euler(rotation);
        go.layer = layerIdx;

        var r = go.GetComponent<Renderer>();
        if (r != null && mat != null) r.sharedMaterial = mat;

        // Add SnapPoints as children
        AddSnapPoints(go, scale, snapType);

        // ★ PlacementAnchor 생성 (다중 메시 Bounds 합산 → 최하단 중앙)
        AddPlacementAnchor(go);

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        Debug.Log($"[BuildingSetup] Created Real prefab: {path}");
        return prefab;
    }

    /// <summary>
    /// ★ Ghost 프리팹을 Real 프리팹으로부터 복제하여 생성.
    /// SnapPoint 자식 오브젝트가 100% 보존됨.
    /// </summary>
    private static GameObject CreateGhostFromReal(GameObject realPrefab, string ghostName, Material ghostMat)
    {
        string path = $"{PREFAB_FOLDER}/{ghostName}.prefab";

        // 기존 Ghost가 있으면 삭제 후 재생성 (Real과 항상 동기화)
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            AssetDatabase.DeleteAsset(path);

        // ★ 핵심: Real 프리팹을 통째로 복제
        var go = Object.Instantiate(realPrefab);
        go.name = ghostName;
        go.SetActive(true); // ★ 4.15-1: 고스트 활성화 보장

        // Ghost 후처리 1: 머티리얼을 반투명 Ghost 머티리얼로 교체
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r != null && ghostMat != null)
                r.sharedMaterial = ghostMat;
            if (r != null) r.enabled = true; // ★ 4.15-1: 렌더러 강제 활성화
        }

        // Ghost 후처리 2: 콜라이더 제거 (Ghost는 레이캐스트를 방해하면 안 됨)
        var colliders = go.GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            Object.DestroyImmediate(col);
        }

        // ★ 절대 자식 오브젝트(SnapPoint 등)를 삭제하지 않는다!

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);

        int socketCount = prefab.GetComponentsInChildren<SnapPoint>(true).Length;
        Debug.Log($"[BuildingSetup] Created Ghost prefab: {path} (Sockets: {socketCount})");
        return prefab;
    }

    // ── PlacementAnchor generator ─────────────────────────────────────────────

    /// <summary>
    /// 다중 MeshFilter의 Bounds를 루트 로컬 기준으로 합산하여
    /// 최하단 중앙에 PlacementAnchor 자식 오브젝트를 생성 (Bake).
    /// </summary>
    private static void AddPlacementAnchor(GameObject root)
    {
        // 기존 Anchor가 있으면 삭제 후 재생성
        var existing = root.transform.Find("PlacementAnchor");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
        if (meshFilters.Length == 0)
        {
            Debug.LogWarning($"[BuildingSetup] {root.name}: MeshFilter가 없어 PlacementAnchor를 생성할 수 없습니다.");
            return;
        }

        // 통합 Bounds를 루트 로컬 공간에서 계산
        bool initialized = false;
        Bounds combinedBounds = new Bounds();

        foreach (var mf in meshFilters)
        {
            if (mf.sharedMesh == null) continue;
            Bounds meshBounds = mf.sharedMesh.bounds;

            // 메시 Bounds의 8개 코너를 루트 로컬 공간으로 변환
            Vector3 center = meshBounds.center;
            Vector3 ext = meshBounds.extents;
            Vector3[] corners = new Vector3[8]
            {
                center + new Vector3( ext.x,  ext.y,  ext.z),
                center + new Vector3( ext.x,  ext.y, -ext.z),
                center + new Vector3( ext.x, -ext.y,  ext.z),
                center + new Vector3( ext.x, -ext.y, -ext.z),
                center + new Vector3(-ext.x,  ext.y,  ext.z),
                center + new Vector3(-ext.x,  ext.y, -ext.z),
                center + new Vector3(-ext.x, -ext.y,  ext.z),
                center + new Vector3(-ext.x, -ext.y, -ext.z),
            };

            foreach (var corner in corners)
            {
                // 메시 로컬 → 월드 → 루트 로컬
                Vector3 worldPos = mf.transform.TransformPoint(corner);
                Vector3 rootLocal = root.transform.InverseTransformPoint(worldPos);

                if (!initialized)
                {
                    combinedBounds = new Bounds(rootLocal, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    combinedBounds.Encapsulate(rootLocal);
                }
            }
        }

        if (!initialized) return;

        // ★ 4.15-1: NaN/Infinity 방어
        Vector3 bc = combinedBounds.center;
        Vector3 bm = combinedBounds.min;
        if (float.IsNaN(bc.x) || float.IsNaN(bc.y) || float.IsNaN(bc.z) ||
            float.IsNaN(bm.x) || float.IsNaN(bm.y) || float.IsNaN(bm.z) ||
            float.IsInfinity(bc.x) || float.IsInfinity(bc.y) || float.IsInfinity(bc.z) ||
            float.IsInfinity(bm.x) || float.IsInfinity(bm.y) || float.IsInfinity(bm.z))
        {
            Debug.LogWarning($"[BuildingSetup] {root.name}: Bounds에 NaN/Infinity 감지. Anchor를 (0,0,0)으로 초기화합니다.");
            var safeAnchor = new GameObject("PlacementAnchor");
            safeAnchor.transform.SetParent(root.transform, false);
            safeAnchor.transform.localPosition = Vector3.zero;
            return;
        }

        // 최하단 중앙 포인트
        Vector3 bottomCenter = new Vector3(
            combinedBounds.center.x,
            combinedBounds.min.y,
            combinedBounds.center.z);

        var anchor = new GameObject("PlacementAnchor");
        anchor.transform.SetParent(root.transform, false);
        anchor.transform.localPosition = bottomCenter;

        Debug.Log($"[BuildingSetup] PlacementAnchor for {root.name}: localPos={bottomCenter}");
    }

    // ── SnapPoint generator ───────────────────────────────────────────────────

    /// <summary>
    /// Adds SnapPoint child GameObjects to a prefab root.
    /// Positions are in LOCAL space relative to the pivot (center).
    /// </summary>
    private static void AddSnapPoints(GameObject root, Vector3 scale, SnapType snapType)
    {
        float hx = scale.x * 0.5f; // half extent X
        float hy = scale.y * 0.5f; // half extent Y
        float hz = scale.z * 0.5f; // half extent Z

        List<(string id, Vector3 localPos, Vector3 forwardVec, SnapType type, SnapType[] compat)> points
            = new List<(string, Vector3, Vector3, SnapType, SnapType[])>();

        switch (snapType)
        {
            case SnapType.Floor:
                // Floor (3x0.2x3) -> Edge positions exactly at ±1.5f, facing outward
                points.Add(("Floor_N",  new Vector3(  0,  0,  hz), Vector3.forward, SnapType.Floor, new[]{ SnapType.Wall, SnapType.Roof, SnapType.Floor }));
                points.Add(("Floor_S",  new Vector3(  0,  0, -hz), Vector3.back,    SnapType.Floor, new[]{ SnapType.Wall, SnapType.Roof, SnapType.Floor }));
                points.Add(("Floor_E",  new Vector3( hx,  0,   0), Vector3.right,   SnapType.Floor, new[]{ SnapType.Wall, SnapType.Roof, SnapType.Floor }));
                points.Add(("Floor_W",  new Vector3(-hx,  0,   0), Vector3.left,    SnapType.Floor, new[]{ SnapType.Wall, SnapType.Roof, SnapType.Floor }));
                break;

            case SnapType.Wall:
                // Wall (3x3x0.2) -> Edge positions exactly at ±1.5f, facing outward
                points.Add(("Wall_Top", new Vector3(  0,  hy, 0), Vector3.up,    SnapType.Wall, new[]{ SnapType.Wall, SnapType.Roof }));
                points.Add(("Wall_Bot", new Vector3(  0, -hy, 0), Vector3.down,  SnapType.Wall, new[]{ SnapType.Floor, SnapType.Wall }));
                points.Add(("Wall_L",   new Vector3(-hx,   0, 0), Vector3.left,  SnapType.Wall, new[]{ SnapType.Wall }));
                points.Add(("Wall_R",   new Vector3( hx,   0, 0), Vector3.right, SnapType.Wall, new[]{ SnapType.Wall }));
                break;

            case SnapType.Roof:
                // Roof (3x0.2x3) but slanted. Local edges same as floor, facing outward.
                points.Add(("Roof_Top", new Vector3(  0,  0,  hz), Vector3.forward, SnapType.Roof, new[]{ SnapType.Wall, SnapType.Floor, SnapType.Roof }));
                points.Add(("Roof_Bot", new Vector3(  0,  0, -hz), Vector3.back,    SnapType.Roof, new[]{ SnapType.Wall, SnapType.Floor, SnapType.Roof }));
                points.Add(("Roof_L",   new Vector3(-hx,  0,   0), Vector3.left,    SnapType.Roof, new[]{ SnapType.Wall, SnapType.Floor, SnapType.Roof }));
                points.Add(("Roof_R",   new Vector3( hx,  0,   0), Vector3.right,   SnapType.Roof, new[]{ SnapType.Wall, SnapType.Floor, SnapType.Roof }));
                break;
        }

        foreach (var (id, localPos, fwd, sType, compat) in points)
        {
            var sp = new GameObject(id);
            sp.transform.SetParent(root.transform, false);
            sp.transform.localPosition = localPos;
            sp.transform.localRotation = Quaternion.LookRotation(fwd);
            var snapComp = sp.AddComponent<SnapPoint>();
            snapComp.socketId       = id;
            snapComp.snapType       = sType;
            snapComp.compatibleTypes = compat;

            // ★ 4.15-24A: 물리적 스냅 탐색을 위한 Collider 및 레이어 추가
            sp.layer = LayerMask.NameToLayer("SnapPoint");
            var col = sp.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.15f;

            // ★ Depth-1 검증: SnapPoint는 반드시 루트의 직계 자식이어야 함
            if (sp.transform.parent != root.transform)
            {
                Debug.LogError($"[BuildingSetup] SnapPoint '{id}' is NOT a direct child of root '{root.name}'! Depth violation.");
            }
        }
    }

    // ── UI creation ───────────────────────────────────────────────────────────

    private static void CreateBuildingUI(BuildingManager bm)
    {
        var existing = GameObject.Find("Canvas_Building");
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        // ── Canvas ───────────────────────────────────────────────────────────
        var canvasGO = new GameObject("Canvas_Building");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas_Building");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        if (canvasGO.GetComponent<GraphicRaycaster>() == null)
            canvasGO.AddComponent<GraphicRaycaster>();

        // ── EventSystem 검사 및 자동 생성 (4.15-5) ───────────────────────────
        var es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            // ★ 4.15-9 InputFix: 최신 Input System 패키지에 맞춘 모듈 부착
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
            Debug.Log("[BuildingSetup] Created EventSystem with InputSystemUIInputModule.");
        }

        // ── Panel (bottom-centre, compact) ───────────────────────────────────
        var panelGO   = new GameObject("Panel_BuildingBar");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin        = new Vector2(0.5f, 0f);
        panelRect.anchorMax        = new Vector2(0.5f, 0f);
        panelRect.pivot            = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 16f);
        panelRect.sizeDelta        = new Vector2(480f, 120f);

        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        // GridLayoutGroup — inventory-slot style
        var grid = panelGO.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(100f, 100f);
        grid.spacing         = new Vector2(10f, 0f);
        grid.padding         = new RectOffset(10, 10, 10, 10);
        grid.childAlignment  = TextAnchor.MiddleCenter;
        grid.constraint      = GridLayoutGroup.Constraint.FixedRowCount;
        grid.constraintCount = 1;

        // ── Slot buttons ─────────────────────────────────────────────────────
        bm.slotHighlights = new Image[Pieces.Length];
        string[] labels = { "1\nFloor", "2\nWall", "3\nRoof45", "4\nRoof30" };
        for (int i = 0; i < Pieces.Length; i++)
        {
            int capturedIdx = i;

            // Slot background
            var btnGO  = new GameObject($"Slot_{Pieces[i].name}");
            btnGO.transform.SetParent(panelGO.transform, false);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.18f, 0.15f, 0.12f, 0.95f);
            btnImg.raycastTarget = true; // ★ 4.15-5: 광선 충돌 허용 강제 지정
            bm.slotHighlights[i] = btnImg; // store reference for highlighting

            // ★ 4.15-6: 전용 BuildingUISlot 스크립트 기반 UI 이벤트 자동 바인딩
            var uiSlot = btnGO.AddComponent<BuildingUISlot>();
            uiSlot.pieceIndex = capturedIdx;

            // Button component (RequireComponent에 의해 생성됨)
            var btn = btnGO.GetComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor      = new Color(0.18f, 0.15f, 0.12f, 0.95f);
            cb.highlightedColor = new Color(0.45f, 0.70f, 0.25f, 1f);
            cb.pressedColor     = new Color(0.30f, 0.55f, 0.15f, 1f);
            cb.selectedColor    = new Color(0.35f, 0.60f, 0.20f, 1f);
            btn.colors = cb;

            // Label (centred, two-line, bold)
            var labelGO  = new GameObject("Label");
            labelGO.transform.SetParent(btnGO.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text      = labels[i];
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize  = 16f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.color     = new Color(0.92f, 0.88f, 0.78f, 1f); // warm off-white
            }
        }

        // Assign panel to BuildingManager so it toggles with build mode
        bm.buildingUIPanel = panelGO;
        panelGO.SetActive(false); // Start hidden
        EditorUtility.SetDirty(bm);

        Debug.Log("[BuildingSetup] Canvas_Building created (Grid slot UI, panel auto-assigned).");
    }

    // ── Utilities ─────────────────────────────────────────────────────────────

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }

    private static void EnsureLayer(string layerName)
    {
        if (LayerMask.NameToLayer(layerName) != -1) return;
        var tm  = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var arr = tm.FindProperty("layers");
        for (int i = 8; i < arr.arraySize; i++)
        {
            var slot = arr.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tm.ApplyModifiedProperties();
                Debug.Log($"[BuildingSetup] Added layer '{layerName}' at slot {i}.");
                return;
            }
        }
        Debug.LogWarning($"[BuildingSetup] No free layer slot for '{layerName}'—add manually.");
    }

    private static Material CreateSolidMaterial(string name, Color color)
    {
        string path = $"{PREFAB_FOLDER}/Mat_{name}.mat";
        var ex = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (ex != null) return ex;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = color;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Material CreateGhostMaterial(string name)
    {
        string path = $"{PREFAB_FOLDER}/Mat_{name}.mat";
        var ex = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (ex != null) return ex;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = new Color(0.1f, 0.9f, 0.3f, 0.4f);
        mat.SetFloat("_Surface", 1f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetFloat("_Mode", 3f);
        mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
