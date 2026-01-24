using UnityEngine;

public class PlayerItemMoveHandler : MonoBehaviour
{
    [SerializeField] Transform _weaponHolder;            // 아이템 변경 시 움직일 객체
    [SerializeField] PlayerMovementHandler _moveHandler; // 이동 상태 확인용
}
