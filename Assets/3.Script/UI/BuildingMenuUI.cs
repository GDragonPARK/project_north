using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class BuildingMenuUI : MonoBehaviour
{
    [Header("References")]
    public BuildManager buildManager;
    public GameObject menuPanel;
    public Transform contentParent;
    public GameObject buttonPrefab;

    private bool m_isMenuOpen = false;

    private void Start()
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        PopulateMenu();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }

public void ToggleMenu()
    {
        m_isMenuOpen = !m_isMenuOpen;
        menuPanel.SetActive(m_isMenuOpen);

        if (m_isMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // [Phase 10.2] 메뉴가 열릴 때마다 모든 버튼 자원 상태 갱신
            var allBuildBtns = contentParent.GetComponentsInChildren<BuildUIButton>();
            foreach (var btn in allBuildBtns)
            {
                btn.UpdateUIState();
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

private void PopulateMenu()
    {
        if (buildManager == null || contentParent == null) return;

        // [Phase 10.2-1] 동적 버튼 생성 로직 완전 삭제 및 씬에 배치된 정적 버튼 검색
        var allBuildBtns = contentParent.GetComponentsInChildren<BuildUIButton>();

        for (int i = 0; i < allBuildBtns.Length; i++)
        {
            int index = i; // 이벤트 리스너 클로저용 인덱스 캡처
            var buildBtn = allBuildBtns[i];

            // ItemData에서 비용 가져와서 연동 (인덱스 매칭)
            if (i < buildManager.buildablePieces.Count)
            {
                ItemData data = buildManager.buildablePieces[i];
                buildBtn.requiredItemName = "Wood"; // 기본값
                buildBtn.requiredAmount = data.woodCost;
            }

            if (buildBtn.button != null)
            {
                // 중복 등록 방지를 위해 기존 리스너 초기화
                buildBtn.button.onClick.RemoveAllListeners();
                buildBtn.button.onClick.AddListener(() => {
                    buildManager.SelectPiece(index);
                    ToggleMenu();
                });
            }
        }
    }
}
