using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

/// <summary>
/// [Phase 8.1] 숫자 키(1/2/3)로 무기를 실시간 스왑하는 퀵슬롯 장비 관리자.
/// Player_New 루트에 부착. axeObject / hammerObject를 Inspector에서 할당.
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    [Header("Weapon Objects")]
    public GameObject axeObject;
    public GameObject hammerObject;

    [Header("State")]
    public ToolType currentTool = ToolType.None;

    // 캐시
    private ThirdPersonController _controller;

    private void Awake()
    {
        _controller = GetComponent<ThirdPersonController>();
    }

    private void Start()
    {
        // 시작 시 모든 무기 비활성화(맨손)
        SetTool(ToolType.None);
    }

    private void Update()
    {
        // 키보드 입력 (New Input System 사용 중이므로 Keyboard 직접 읽기)
        var kb = Keyboard.current;
        if (kb == null) return;

        // 1번 키 입력 -> 0번 인덱스 (도끼)
        if (kb.digit1Key.wasPressedThisFrame) SetTool(ToolType.Axe);
        // 2번 키 입력 -> 1번 인덱스 (망치)
        else if (kb.digit2Key.wasPressedThisFrame) SetTool(ToolType.Hammer);
        // 3번 키 입력 -> 2번 인덱스 (횃불)
        else if (kb.digit3Key.wasPressedThisFrame) SetTool(ToolType.Torch);
        // X키 등 -> 맨손
        else if (kb.xKey.wasPressedThisFrame) SetTool(ToolType.None);
    }

    public void SetTool(ToolType tool)
    {
        currentTool = tool;

        // 무기 오브젝트 활성/비활성
        if (axeObject)    axeObject.SetActive(tool == ToolType.Axe);
        if (hammerObject) hammerObject.SetActive(tool == ToolType.Hammer);

        // UI 하이라이트 동기화 (Single Source of Truth)
        if (QuickSlotUI.Instance != null)
        {
            int quickSlotIndex = -1;
            if (tool == ToolType.Axe) quickSlotIndex = 0;
            else if (tool == ToolType.Hammer) quickSlotIndex = 1;
            else if (tool == ToolType.Torch) quickSlotIndex = 2; // Torch 등 추후 2번 인덱스로 확장 가능
            
            QuickSlotUI.Instance.HighlightSlot(quickSlotIndex);
        }

        Debug.Log($"[EquipmentManager] 장비 스왑: {tool}");
    }
}
