using UnityEngine;

public class PlayerMovementHandler : MonoBehaviour
{
    private CharacterController _cc;
    private PlayerInputHandler _inputHandler;
    private PlayerStatHandler _statHandler;
    private PlayerSoundHandler _soundHandler;

    [Header("이동 설정")]
    [SerializeField] float _walkSpeed = 5f;
    [SerializeField] float _runSpeed = 8f;
    [SerializeField] float _jumpPower = 2f;
    [SerializeField] float _gravity = -20f;
    
    [Header("기력 소모 설정")]
    [SerializeField] float _jumpStamina = 10f;
    [SerializeField] float _runStamina = 15f;

    [Header("관성 설정")]
    [SerializeField] float _airSmoothTime = 0.3f;

    [Header("지면 체크 설정")]
    [SerializeField] LayerMask _groundLayer;
    [SerializeField] float _groundCheckRadius = 0.4f;
    [SerializeField] float _groundCheckOffset = 0.5f;
    [SerializeField] float _groundCheckDistance = 0.2f;
    [SerializeField] private bool _isGrounded; // 디버그용

    public bool IsJump { get; private set; }    // 점프 상태 애니메이션용

    public bool IsGrounded => _isGrounded;

    public LayerMask GroundLayer => _groundLayer;

    // 속도 데이터
    private Vector3 _verticalVelocity;   // Y축 속도 (점프/중력)
    private Vector3 _horizontalVelocity; // X, Z축 속도 (이동)
    private Vector3 _smoothDampVelocity;

    private RaycastHit _groundHit;

    // 생성 직후 소리 방지용
    private float _enableTime;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        _statHandler = GetComponent<PlayerStatHandler>();
        _soundHandler = GetComponent<PlayerSoundHandler>();
    }

    private void OnEnable()
    {
        _inputHandler.OnJumpEvent += PerformJump;

        // 활성화 시간 기록 (착지 소리 방지)
        _enableTime = Time.time;
    }

    private void OnDisable()
    {
        _inputHandler.OnJumpEvent -= PerformJump;
    }
    private void Update()
    {
        CheckGround();  // 지면 체크
        Move();         // 이동
        ApplyGravity(); // 중력

        // 최종 이동    수평 - 이동 / 수직 - 중력
        Vector3 finalVelocity = _horizontalVelocity + _verticalVelocity;

        // Move에 따른 상태 반환
        CollisionFlags flags = _cc.Move(finalVelocity * Time.deltaTime);

        // 머리위에 뭔가 부딪혔고
        // 올라가는 속도가 남아있다면
        if ((flags & CollisionFlags.Above) != 0 && _verticalVelocity.y > 0)
        {
            // 수직 속도 0
            _verticalVelocity.y = 0;
        }
    }
    private void Move()
    {
        // 입력 핸들러에서 방향 가져옴
        Vector2 input = _inputHandler.MoveInput;
        
        // 기본 속도
        float currentSpeed = _walkSpeed;

        // 입력 있고, 지면에 붙어있으면서, 달리기 상태일 때
        if (input != Vector2.zero && _isGrounded && _inputHandler.IsSprint)
        {
            // 스태미너 지속 사용
            // 근데 스태미너 부족하면 통과 못함
            if (_statHandler.UseStaminaContinuous(_runStamina))
            {
                currentSpeed = _runSpeed;
            }
        }

        // 속도 계산 (입력 방향 * 속도)
        Vector3 targetVelocity = (transform.right * input.x + transform.forward * input.y) * currentSpeed;

        // 바닥
        if (_isGrounded)
        {
            // 즉시 설정
            _horizontalVelocity = targetVelocity;

            // 관성 초기화
            _smoothDampVelocity = Vector3.zero;
        }
        // 공중
        else
        {
            // 공중에선 점프 상태
            if(IsJump == false) IsJump = true;

            // 관성
            _horizontalVelocity = Vector3.SmoothDamp(
                _horizontalVelocity,
                targetVelocity,
                ref _smoothDampVelocity,
                _airSmoothTime
            );
        }
    }

    // 점프 이벤트로 호출
    private void PerformJump()
    {
        if (_isGrounded)
        {
            // 기력 있으면 기력 소모 후
            if (_statHandler.TryUseStamina(_jumpStamina))
            {
                // 점프
                _verticalVelocity.y = Mathf.Sqrt(_jumpPower * -2f * _gravity);

                // 점프 소리
                if (_soundHandler != null)
                    _soundHandler.PlayJump();
            }
        }
    }

    private void ApplyGravity()
    {
        if (_isGrounded && _verticalVelocity.y < 0)
        {
            _verticalVelocity.y = -2f; // 바닥 밀착용
        }

        _verticalVelocity.y += _gravity * Time.deltaTime;
    }


    // 땅 체크
    private void CheckGround()
    {
        // 갱신 전 상태
        bool wasGrounded = _isGrounded;

        Vector3 sphereOrigin = transform.position + Vector3.up * _groundCheckOffset;
        _isGrounded = Physics.SphereCast(
            sphereOrigin,
            _groundCheckRadius,
            Vector3.down,
            out _groundHit,
            _groundCheckDistance,
            _groundLayer
        );

        // 착지 순간
        if (!wasGrounded && _isGrounded)
        {
            // 점프 상태는 항상 갱신
            IsJump = false;

            // 생성된지 0.6초 지난 후 속도 빠를 때만
            if (Time.time > _enableTime + 0.6f && _verticalVelocity.y < -3.0f)
            {
                if (_soundHandler != null)
                    _soundHandler.PlayLand();
            }
        }
    }
}
