using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PlayerItemMoveHandler : MonoBehaviour
{
    [Header("타겟 트랜스폼")]
    [SerializeField] Transform _handTrans;        // 아이템 스웨이, 충격 객체
    [SerializeField] Transform _handHolderTrans;  // 아이템 변경 시 움직일 객체

    private PlayerInputHandler _inputHandler;     // 입력 상태 확인용
    private PlayerMovementHandler _moveHandler;   // 이동 상태 확인용          

    [Header("장착 설정")]
    [SerializeField] float _equipSpeed = 10f;                          // 아이템 장착 속도
    [SerializeField] Vector3 _startOffset = new Vector3(0, -0.5f, 0);  // 내릴 간격

    [Header("스웨이 설정")]
    [SerializeField] float _swayAmount = 0.02f;   // 강도
    [SerializeField] float _maxSway = 0.06f;      // 최대
    [SerializeField] float _swaySmooth = 4f;      // 부드러움

    [Header("충격 설정")]
    [SerializeField] float _jumpShock = 0.05f;   // 점프할 때 내려가는 충격
    [SerializeField] float _landShock = 0.15f;   // 착지할 때 내려가는 충격
    [SerializeField] float _fallAmount = 0.08f;  // 공중에 있을 때 올라가는 정도
    [SerializeField] float _shockSmooth = 5f;    // 충격 후 복구 속도

    private float _currentShockY = 0f;           // 현재 충격값
    private bool _wasGrounded = true;            // 이전 프레임 땅 체크용

    private Vector3 _handPos;   // 스웨이 위치

    private Vector3 _equipPos;  // 장착 위치

    private Vector3 _currentSwayPos;    // 현재 스웨이 위치


    // 아이템 장착 상태
    private bool _isEquipped = false;


    private void Awake()
    {
        _inputHandler = GetComponentInParent<PlayerInputHandler>();
        _moveHandler = GetComponentInParent<PlayerMovementHandler>();


        // 시작하면

        // 장착 기준점
        if (_handHolderTrans != null)
            _equipPos = _handHolderTrans.localPosition;

        // 스웨이 기준점
        if (_handTrans != null)
            _handPos = _handTrans.localPosition;
    }

    private void Update()
    {
        // 둘 중 하나라도 없으면 무시
        if (_handTrans == null || _handHolderTrans == null) return;

        // 장착
        EquipMotion();

        // 스웨이
        SwayMotion();

        // 좌우

        // 충격
        ShockMotion();

        // 최종 계산된 값 적용 : 기준점 + 스웨이 + 충격
        _handTrans.localPosition = _handPos + _currentSwayPos + new Vector3(0, _currentShockY, 0); ;
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
        if (_handHolderTrans != null)
        {
            // 원래 위치에서 StartPos만큼 내림
            _handHolderTrans.localPosition = _equipPos + _startOffset;
        }
    }

    // 장착 모션
    private void EquipMotion()
    {
        // 목표 위치
        Vector3 target = _isEquipped ? _equipPos : (_equipPos + _startOffset);

        // 부드럽게 이동
        _handHolderTrans.localPosition = Vector3.Lerp(_handHolderTrans.localPosition, target, Time.deltaTime * _equipSpeed);
    }

    // 스웨이 모션
    private void SwayMotion()
    {
        // 입력 핸들러 필요
        if (_inputHandler == null) return;

        // 마우스 X,Y
        float mouseX = _inputHandler.LookInput.x;
        float mouseY = _inputHandler.LookInput.y;

        // 입력 반대 방향으로
        float moveX = Mathf.Clamp(mouseX * -_swayAmount, -_maxSway, _maxSway);
        float moveY = Mathf.Clamp(mouseY * -_swayAmount, -_maxSway, _maxSway);

        // 목표 지점
        Vector3 targetPos = new Vector3(moveX, moveY, 0);

        // 부드럽게 적용      
        _currentSwayPos = Vector3.Lerp(_currentSwayPos, targetPos, Time.deltaTime * _swaySmooth);
    }


    // 충격 모션
    private void ShockMotion()
    {
        if (_moveHandler == null) return;

        // 착지 상태
        bool isGrounded = _moveHandler.IsGrounded;

        float targetY = 0f; // 기본 목표는 원점

        // 점프 감지
        if (_wasGrounded && isGrounded == false)
        {
            // 점프 충격 아래로
            _currentShockY = -_jumpShock;
        }
        // 착지 감지
        else if (_wasGrounded == false && isGrounded)
        {
            // 착지 충격 아래로
            _currentShockY = -_landShock;
        }

        // 낙하 중
        if (!isGrounded)
        {
            // 땅이 아니면 점점 위로 올라감
            targetY = _fallAmount;
        }

        // 지난 상태 갱신
        _wasGrounded = isGrounded;

        // 부드럽게 이동
        _currentShockY = Mathf.Lerp(_currentShockY, targetY, Time.deltaTime * _shockSmooth);
    }
}
