using UnityEngine;

public class PlayerMovementHandler : MonoBehaviour
{
    private CharacterController _cc;
    private PlayerInputHandler _inputHandler;

    [Header("이동 설정")]
    [SerializeField] float _moveSpeed = 5f;
    [SerializeField] float _jumpPower = 2f;
    [SerializeField] float _gravity = -20f;
    
    [Header("관성 설정")]
    [SerializeField] float _airSmoothTime = 0.3f;

    [Header("지면 체크 설정")]
    [SerializeField] LayerMask _groundLayer;
    [SerializeField] float _groundCheckRadius = 0.4f;
    [SerializeField] float _groundCheckOffset = 0.5f;
    [SerializeField] float _groundCheckDistance = 0.2f;
    [SerializeField] private bool _isGrounded; // 디버그용

    // 속도 데이터
    private Vector3 _verticalVelocity;   // Y축 속도 (점프/중력)
    private Vector3 _horizontalVelocity; // X, Z축 속도 (이동)
    private Vector3 _smoothDampVelocity;

    private RaycastHit _groundHit;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void OnEnable()
    {
        _inputHandler.OnJumpEvent += PerformJump;
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
        _cc.Move(finalVelocity * Time.deltaTime);
    }
    private void Move()
    {
        // 입력 핸들러에서 방향 가져옴
        Vector2 input = _inputHandler.MoveInput;

        // 속도 계산 (입력 방향 * 속도)
        Vector3 targetVelocity = (transform.right * input.x + transform.forward * input.y) * _moveSpeed;

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
            _verticalVelocity.y = Mathf.Sqrt(_jumpPower * -2f * _gravity);
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

    private void CheckGround()
    {
        Vector3 sphereOrigin = transform.position + Vector3.up * _groundCheckOffset;
        _isGrounded = Physics.SphereCast(
            sphereOrigin,
            _groundCheckRadius,
            Vector3.down,
            out _groundHit,
            _groundCheckDistance,
            _groundLayer
        );
    }

    // 기즈모
    private void OnDrawGizmos()
    {
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector3 sphereOrigin = transform.position + Vector3.up * _groundCheckOffset;
        Gizmos.DrawWireSphere(sphereOrigin + (Vector3.down * _groundCheckDistance), _groundCheckRadius);
    }
}
