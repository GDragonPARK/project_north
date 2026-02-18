using UnityEngine;

public class PickupItem : MonoBehaviour
{
    [Header("Debug View")]
    public ItemData itemData;
    public Transform playerTarget; // Inspector에서 확인 가능하게 Public 변경
    public bool isFlying = false;  // 상태 확인용 Public 변경
    
    private float spawnTime; 

    void Start()
    {
        spawnTime = Time.time;

        // 1. 데이터 안전장치
        if(itemData == null) itemData = Resources.Load<ItemData>("Items/Wood");

        // 2. 시작하자마자 플레이어 찾기 시도
        FindTarget();
    }

    void Update()
    {
        // 0.5초 대기
        if (Time.time < spawnTime + 0.5f) return;

        // 타겟이 없으면 매 프레임 찾기 (놓치지 않기 위해)
        if (playerTarget == null)
        {
            FindTarget();
            if (playerTarget == null) return; // 찾을 때까지 대기
        }

        // 거리 계산
        float dist = Vector3.Distance(transform.position, playerTarget.position);

        // 3m 안으로 들어오면 비행 모드 ON
        if (dist < 3.0f) isFlying = true;

        if (isFlying)
        {
            // 물리 끄기 (충돌 방지)
            var rb = GetComponent<Rigidbody>();
            var col = GetComponent<Collider>();
            
            if(rb != null && !rb.isKinematic) 
            {
                rb.isKinematic = true; // Set kinematic first
                rb.useGravity = false;
            }
            
            // Disable collider to prevent physics interactions completely
            if(col != null && col.enabled)
            {
                col.enabled = false;
            }

            // 이동 (가슴 높이로) - Use transform only, no velocity
            transform.position = Vector3.MoveTowards(transform.position, playerTarget.position + Vector3.up, 15f * Time.deltaTime);

            // 도착 판정
            if (dist < 2.0f)
            {
                // Add to BuildingManager (which now handles InventorySystem integration and overflow)
                if (BuildingManager.Instance != null && itemData != null)
                {
                    BuildingManager.Instance.AddResource(itemData, 1);
                }
                else
                {
                    // Fallback to InventorySystem if BuildingManager is missing
                    if (InventorySystem.Instance != null)
                    {
                        InventorySystem.Instance.AddItem(itemData, 1);
                        InventorySystem.Instance.ForceUIUpdate();
                    }
                }

                Destroy(gameObject);
            }
        }
    }

    void FindTarget()
    {
        // 전략 1: 수집기 마커 컴포넌트로 찾기
        var collector = Object.FindAnyObjectByType<AutoPickupCollector>();
        if (collector != null) 
        {
            playerTarget = collector.transform;
            return;
        }

        // 전략 2: 태그로 찾기
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) 
        {
            playerTarget = p.transform;
        }
    }
}
