using UnityEngine;

public class PlayerItemMoveHandler : MonoBehaviour
{
    [SerializeField] Transform _weaponHolder;            // 아이템 변경 시 움직일 객체
    private PlayerMovementHandler _moveHandler;          // 이동 상태 확인용

    [Header("장착 설정")]
    [SerializeField] float _equipSpeed = 10f;                          // 아이템 장착 속도
    [SerializeField] Vector3 _startPos = new Vector3(0, -0.5f, 0);     // 시작 위치
    private Vector3 _equipPos = Vector3.zero;                          // 들고 있을 때 위치

    // 아이템 장착 상태
    private bool _isEquipped = false;


    private void Awake()
    {
        _moveHandler = GetComponentInParent<PlayerMovementHandler>();
    }

    private void Update()
    {
        // 장착 모션
        EquipMotion();
    }

    // 장착 상태 변경 (PlayerItemHandler에서 호출)
    public void SetEquipState(bool isEquipped)
    {
        _isEquipped = isEquipped;
    }

    // 아이템 변경 시 즉시 이동
    public void SnapToStartPos()
    {
        // 바로 장착 해제 상태
        _isEquipped = false;

        // 위치도 즉시 갱신
        _weaponHolder.localPosition = _startPos; 
    }

    // 장착 모션
    private void EquipMotion()
    {
        // 목표 위치
        Vector3 target = _isEquipped ? _equipPos : _startPos;

        // 부드럽게 이동
        _weaponHolder.localPosition = Vector3.Lerp(_weaponHolder.localPosition, target, Time.deltaTime * _equipSpeed);
    }
}
