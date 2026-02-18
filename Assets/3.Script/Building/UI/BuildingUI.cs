using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class BuildingUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject uiPanel;
    public Transform gridParent;
    public GameObject iconPrefab; // 버튼 프리팹
    public Text categoryNameText; // 카테고리 이름 표시
    public List<BuildingDataSO> testPieces = new List<BuildingDataSO>(); // 테스트용 데이터 리스트

    [Header("Input System")]
    private PlayerInput playerInput;
    private InputAction prevTabAction;
    private InputAction nextTabAction;

    private int currentCategoryIndex = 0;

    void Start()
    {
        // Auto-wire references if not assigned in Inspector
        if (uiPanel == null) uiPanel = gameObject;
        if (gridParent == null)
        {
            Transform grid = transform.Find("BuildingGrid");
            if (grid != null) gridParent = grid;
        }
        if (categoryNameText == null)
        {
            Transform catText = transform.Find("CategoryTab_Text");
            if (catText != null) categoryNameText = catText.GetComponent<Text>();
        }
        if (iconPrefab == null)
        {
            iconPrefab = Resources.Load<GameObject>("UI/Slot_Prefab");
        }

        // 시작 시 꺼둠
        if(uiPanel) uiPanel.SetActive(false);

        // Setup Input System
        SetupInputActions();
    }

    void OnEnable()
    {
        if (prevTabAction != null) prevTabAction.Enable();
        if (nextTabAction != null) nextTabAction.Enable();
    }

    void OnDisable()
    {
        if (prevTabAction != null) prevTabAction.Disable();
        if (nextTabAction != null) nextTabAction.Disable();
    }

    private void SetupInputActions()
    {
        playerInput = Object.FindAnyObjectByType<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogWarning("[BuildingUI] PlayerInput not found in scene!");
            return;
        }

        // Get tab navigation actions
        prevTabAction = playerInput.actions["PrevTab"];
        nextTabAction = playerInput.actions["NextTab"];

        // Subscribe to events
        if (prevTabAction != null)
        {
            prevTabAction.performed += OnPrevTab;
        }

        if (nextTabAction != null)
        {
            nextTabAction.performed += OnNextTab;
        }
    }

    private void OnPrevTab(InputAction.CallbackContext context)
    {
        if (!uiPanel.activeSelf) return; // Only work when UI is open
        SwitchCategory(-1);
    }

    private void OnNextTab(InputAction.CallbackContext context)
    {
        if (!uiPanel.activeSelf) return; // Only work when UI is open
        SwitchCategory(1);
    }

    private void SwitchCategory(int direction)
    {
        if (BuildingManager.Instance == null || BuildingManager.Instance.categories == null) return;
        if (BuildingManager.Instance.categories.Count == 0) return;

        currentCategoryIndex += direction;

        // Wrap around
        if (currentCategoryIndex < 0)
            currentCategoryIndex = BuildingManager.Instance.categories.Count - 1;
        else if (currentCategoryIndex >= BuildingManager.Instance.categories.Count)
            currentCategoryIndex = 0;

        RefreshGrid();
        Debug.Log($"[BuildingUI] Switched to category: {BuildingManager.Instance.categories[currentCategoryIndex].categoryName}");
    }

    public void ToggleUI(bool isOn)
    {
        if (uiPanel) uiPanel.SetActive(isOn);
        if (isOn) RefreshGrid();
    }

    void RefreshGrid()
    {
        // 기존 아이콘 청소
        foreach (Transform child in gridParent) Destroy(child.gameObject);

        List<BuildingDataSO> currentPieces = null;

        // Get pieces from category system or fallback to testPieces
        if (BuildingManager.Instance != null && BuildingManager.Instance.categories != null && BuildingManager.Instance.categories.Count > 0)
        {
            BuildingCategorySO currentCategory = BuildingManager.Instance.categories[currentCategoryIndex];
            currentPieces = currentCategory.pieces;

            // Update category name display
            if (categoryNameText != null)
            {
                categoryNameText.text = currentCategory.categoryName;
            }
        }
        else
        {
            // Fallback to testPieces for backward compatibility
            currentPieces = testPieces;
            if (categoryNameText != null)
            {
                categoryNameText.text = "Test Items";
            }
        }

        // 아이콘 생성
        if (currentPieces != null)
        {
            foreach (var piece in currentPieces)
            {
                if(piece == null) continue;

                GameObject newBtn = Instantiate(iconPrefab, gridParent);
                // 텍스트 표시 (임시)
                var text = newBtn.GetComponentInChildren<Text>();
                if (text) text.text = piece.displayName;

                // 클릭 이벤트
                newBtn.GetComponent<Button>().onClick.AddListener(() => 
                {
                    BuildingManager.Instance.SelectPiece(piece);
                });
            }
        }
    }
}
