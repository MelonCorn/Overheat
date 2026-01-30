using UnityEngine;

public class PlayerItemMoveHandler : MonoBehaviour
{
    private PlayerInputHandler _inputHandler;     // 입력 상태 확인용
    private PlayerMovementHandler _moveHandler;   // 이동 상태 확인용     

    [Header("타겟 트랜스폼")]
    [SerializeField] Transform _handTrans;        // 아이템 스웨이, 걷기, 충격
    [SerializeField] Transform _handHolderTrans;  // 아이템 장착     

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
    [SerializeField] float _shockSpeed = 15f;    // 충격 발생 시 내려가는 속도
    [SerializeField] float _shockSmooth = 5f;    // 충격 후 복구 속도

    [Header("걷기/뛰기 설정")]
    [SerializeField] float _walkSpeed = 7f;      // 걷는 속도
    [SerializeField] float _walkAmount = 0.05f;  // 걷는 흔들림
    [Space]
    [SerializeField] float _runSpeed = 10f;      // 뛰는 속도
    [SerializeField] float _runAmount = 0.1f;    // 뛰는 흔들림
    [Space]
    [SerializeField] float _walkOffset = -0.05f; // 아래로 살짝 내림

    // 이동 데이터
    private float _walkTimer = 0f;               // 누적 타이머
    private Vector3 _currentWalkPos;             // 현재 걸음 위치

    // 착지 데이터
    private float _targetShockY = 0f;            // 목표 충격값
    private float _currentShockY = 0f;           // 현재 충격값
    private bool _wasGrounded = true;            // 이전 프레임 땅 체크용


    // 목표 위치
    private Vector3 _handPos;        // 스웨이 위치
    private Vector3 _equipPos;       // 장착 위치
    private Vector3 _currentSwayPos; // 현재 스웨이 위치


    // 무기 비주얼 반동
    private Vector3 _targetWeaponRot;   // 목표 회전값
    private Vector3 _currentWeaponRot;  // 현재 회전값
    private float _visualSnappiness;    // 반동 적용 속도
    private float _visualReturnSpeed;   // 복귀 속도


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

        // 걷기
        WalkMotion();

        // 충격
        ShockMotion();

        // 반동
        RecoilMotion();

        // (HandSway) 최종 계산된 값 적용 : 기준점 + 스웨이 + 걷기 + 충격
        _handTrans.localPosition = _handPos + _currentSwayPos + _currentWalkPos + new Vector3(0, _currentShockY, 0);

        // (HandHolder) 최종 계산된 회전 값 적용 : 반동
        _handHolderTrans.localRotation = Quaternion.Euler(_currentWeaponRot);

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

        // 위치 초기화
        if (_handHolderTrans != null)
        {
            _handHolderTrans.localPosition = _equipPos + _startOffset;

            //  회전 초기화
            _handHolderTrans.localRotation = Quaternion.identity;
        }

        // 반동 변수 초기화
        _targetWeaponRot = Vector3.zero;
        _currentWeaponRot = Vector3.zero;
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


    // 걷기 모션
    private void WalkMotion()
    {
        // 움직임 핸들러, 입력 핸들러 필요
        if (_moveHandler == null || _inputHandler == null) return;

        // 땅에 있고
        // 이동 입력이 있을 때만
        if (_moveHandler.IsGrounded && _inputHandler.MoveInput.magnitude > 0.1f)
        {
            // 달리기 상태
            bool isRunning = _inputHandler.IsSprint;
            // 속도
            float speed = isRunning ? _runSpeed : _walkSpeed;
            // 흔들림
            float amount = isRunning ? _runAmount : _walkAmount;

            // 누적해서 나중에 이어가게
            _walkTimer += Time.deltaTime * speed;

            // 8자 궤적
            // X(좌우)는 Cos 사용해서 둥글게 왔다갔다
            // Y(위아래)는 Abs Sin 사용해서 좌우 한번 갈때마다 위아래는 두번
            // _walkOffset을 더해서 아래로 좀 내림
            float walkX = Mathf.Cos(_walkTimer) * amount;
            float walkY = Mathf.Abs(Mathf.Sin(_walkTimer)) * amount + _walkOffset;

            // 목표 위치 설정
            Vector3 targetPos = new Vector3(walkX, walkY, 0);

            // 부드럽게 이동
            _currentWalkPos = Vector3.Lerp(_currentWalkPos, targetPos, Time.deltaTime * 10f);
        }
        // 멈추거나 점프 상태면
        else
        {
            // 일단 부드럽게 원점 돌아감
            // 대신 나중에 이어하기 가능
            _currentWalkPos = Vector3.Lerp(_currentWalkPos, Vector3.zero, Time.deltaTime * 10f);
        }
    }


    // 충격 모션
    private void ShockMotion()
    {
        if (_moveHandler == null) return;

        // 착지 상태
        bool isGrounded = _moveHandler.IsGrounded;

        // 최종 목표 Y
        float finalY = isGrounded ? 0f : _fallAmount;

        // 점프 감지
        if (_wasGrounded && isGrounded == false)
        {
            // 점프 충격 아래로
            _targetShockY = -_jumpShock;
        }
        // 착지 감지
        else if (_wasGrounded == false && isGrounded)
        {
            // 착지 충격 아래로
            _targetShockY = -_landShock;
        }

        // 부드럽게 이동
        _targetShockY = Mathf.Lerp(_targetShockY, finalY, Time.deltaTime * _shockSmooth);           // 목표 지점
        _currentShockY = Mathf.Lerp(_currentShockY, _targetShockY, Time.deltaTime * _shockSpeed);   // 현재 지점

        // 지난 상태 갱신
        _wasGrounded = isGrounded;
    }


    // 반동 모션
    private void RecoilMotion()
    {
        // 목표 회전은 항상 원점 돌아가게
        _targetWeaponRot = Vector3.Lerp(_targetWeaponRot, Vector3.zero, _visualReturnSpeed * Time.deltaTime);

        // 현재 회전은 항상 목표 회전 따라가게
        _currentWeaponRot = Vector3.Slerp(_currentWeaponRot, _targetWeaponRot, _visualSnappiness * Time.deltaTime);
    }

    // 발사 반동 (PlayerItemHandler)
    public void AddWeaponRecoil(WeaponData data)
    {
        if (data == null) return;

        // 데이터 갱신
        _visualSnappiness = data.visualRecoilSnappiness;    // 반동 적용 속도
        _visualReturnSpeed = data.visualRecoilReturnSpeed;  // 복귀 속도

        // 목표 각도
        _targetWeaponRot = data.visualRecoilAngle;

        // 현재 각도 초기화
        _currentWeaponRot = Vector3.zero;

        // 약간의 z 랜덤
        float randomZ = Random.Range(-2f, 2f);
        _targetWeaponRot += new Vector3(0f, 0f, randomZ);
    }
}
