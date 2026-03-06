using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// [Phase 10.2] 각 건축 버튼을 제어하며, 인벤토리 자원 보유량에 따라 스스로 색상/활성 상태를 업데이트.
/// </summary>
public class BuildUIButton : MonoBehaviour
{
    [Header("Resource Requirement")]
    public string requiredItemName = "Wood";
    public int requiredAmount = 2;

    [Header("UI References")]
    public Image iconImage;
    public Button button;

    public void UpdateUIState()
    {
        if (InventorySystem.Instance == null) return;

        bool hasResource = InventorySystem.Instance.HasItem(requiredItemName, requiredAmount);

        if (button != null)
            button.interactable = hasResource;

        if (iconImage != null)
        {
            if (hasResource)
            {
                // [조건 만족 시] 컬러 복구
                iconImage.color = Color.white;
            }
            else
            {
                // [조건 불만족 시] 어두운 회색/비활성화 톤
                iconImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            }
        }
    }
}
