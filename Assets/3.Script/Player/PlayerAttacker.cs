using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttacker : MonoBehaviour
{
    // [Phase 8.3-3] 구형 공격 제어 로직 삭제
    // ThirdPersonController가 공격(AttackCheck)과 트리거를 모두 전담하므로 이 스크립트는 무효화함.
    // 기존에 Update에서 마우스 좌클릭 시 무조건 SetTrigger("Attack")을 쏴서 렌치 스윙 시 
    // 도끼 애니메이션이 섞여 나오는 치명적 난입 버그(Rogue Trigger)의 원인이었음.
}
