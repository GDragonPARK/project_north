using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class BuildingUISlot : MonoBehaviour
{
    public int pieceIndex;

    private void Start()
    {
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(() => 
        {
            if (BuildingManager.Instance != null)
            {
                Debug.Log($"[BuildingUISlot] 클릭됨 - {pieceIndex}번 슬롯 선택 요청");
                BuildingManager.Instance.SelectPiece(pieceIndex);
            }
            else
            {
                Debug.LogWarning("[BuildingUISlot] BuildingManager.Instance가 없습니다!");
            }
        });
    }
}
