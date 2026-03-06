using UnityEngine;
using UnityEngine.UI;

public class HotfixSetupRunner : MonoBehaviour
{
    void Awake()
    {
        bool changed = false;

        // 1. Player_New 스탯 연결
        GameObject player = GameObject.Find("Player_New");
        if (player != null)
        {
            CharacterStats stats = player.GetComponent<CharacterStats>();
            if (stats == null)
            {
                stats = player.AddComponent<CharacterStats>();
                changed = true;
                Debug.Log($"[Hotfix] Attached CharacterStats to {player.name}");
            }

            // UI 찾기
            if (stats.staminaBar == null)
            {
                GameObject stamObj = GameObject.Find("StaminaBar_Fill");
                if (stamObj != null)
                {
                    stats.staminaBar = stamObj.GetComponent<Image>();
                    changed = true;
                }
            }
            if (stats.healthBar == null)
            {
                GameObject hpObj = GameObject.Find("HealthBar_Fill");
                if (hpObj != null)
                {
                    stats.healthBar = hpObj.GetComponent<Image>();
                    changed = true;
                }
            }
        }
        else
        {
            Debug.LogWarning("[Hotfix] Could not find 'Player_New' in the scene.");
        }


        // 3. 퀵슬롯 아이콘 색상 임시 세팅 (1:도끼(회색), 2:망치(주황), 3:횃불(빨강))
        Color[] fallbackColors = { Color.gray, new Color(0.8f, 0.5f, 0.3f), new Color(1f, 0.4f, 0.1f) };

        for (int i = 1; i <= 3; i++)
        {
            GameObject slotObj = GameObject.Find($"QuickSlot_{i}");
            if (slotObj == null) continue;

            Transform iconTr = slotObj.transform.Find("Icon");
            Image iconImg = null;

            if (iconTr == null)
            {
                GameObject newIcon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                newIcon.transform.SetParent(slotObj.transform, false);
                iconImg = newIcon.GetComponent<Image>();
                
                RectTransform rt = newIcon.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(10, 10);
                rt.offsetMax = new Vector2(-10, -10);
                
                changed = true;
            }
            else
            {
                iconImg = iconTr.GetComponent<Image>();
            }

            if (iconImg != null)
            {
                int index = i - 1;
                if (iconImg.sprite == null) {
                    iconImg.color = fallbackColors[index];
                }
                changed = true;
            }
        }
        
        Debug.Log("<color=cyan>[Hotfix Complete]</color> CharacterStats linked and QuickSlot colors setup!");
        
        Destroy(this); // 실행 후 제거
    }
}
