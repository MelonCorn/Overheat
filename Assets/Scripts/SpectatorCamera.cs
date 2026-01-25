using UnityEngine;
using UnityEngine.InputSystem;

public class SpectatorCamera : MonoBehaviour
{
    [Header("관전 설정")]   
    [SerializeField] float _height = 1.5f;          // 관전 높이
    [SerializeField] float _rotationSpeed = 2.0f;   // 회전 속도 (나중에 감도로 변경)

    [Header("회전 제한 설정")]
    [SerializeField] float _maxPitch = 80f;         // 내려다볼 각도 제한
    [SerializeField] float _minPitch = -60f;        // 올려다볼 각도 제한

    [Header("Zoom 설정")]
    [SerializeField] float _defaultDistance = 6.0f; // 기본 거리
    [SerializeField] float _minDistance = 2.0f;     // 최소 거리
    [SerializeField] float _maxDistance = 12.0f;    // 최대 거리
    [SerializeField] float _zoomSpeed = 0.5f;       // 줌 속도

    [Header("장애물 설정")]
    [SerializeField] LayerMask _obstacleLayer;      // 벽/바닥 레이어
    [SerializeField] float _cameraRadius = 0.2f;    // 카메라 충돌 크기 (SphereCast)

    // 타겟
    private PlayerHandler _targetPlayer;
    private int _targetIndex = -1;

    // 입력값 저장용
    private Vector2 _lookInput;
    private float _zoomInput;

    // 카메라 회전각
    private float _yaw = 0f;
    private float _pitch = 0f;

    // 줌 현재 거리
    private float _currentDistance;     

    // 컴포넌트
    private PlayerInput _playerInput;

    // 레이캐스트
    RaycastHit _hit;

    private void Awake()
    {
        // PlayerInput 컴포넌트 가져오기
        _playerInput = GetComponent<PlayerInput>();

        // 시작 시 비활성화
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        // 인스펙터에서 디폴트맵 바꿔놓긴 했는데 확실하게
        _playerInput.SwitchCurrentActionMap("Spectator");

        // 이벤트 연결
        _playerInput.actions["Look"].performed += OnLookPerformed;
        _playerInput.actions["Look"].canceled += OnLookCanceled;
        _playerInput.actions["NextTarget"].performed += OnNextTarget;
        _playerInput.actions["PrevTarget"].performed += OnPrevTarget;
        _playerInput.actions["Zoom"].performed += OnZoomPerformed;
        _playerInput.actions["Zoom"].canceled += OnZoomCanceled;

        // 줌 거리 초기화
        _currentDistance = _defaultDistance;

        // 첫 타겟 찾기
        FindNextTarget(1); // 1: 다음, -1: 이전

        // 마우스 잠그기
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        // 이벤트 연결 해제
        _playerInput.actions["Look"].performed -= OnLookPerformed;
        _playerInput.actions["Look"].canceled -= OnLookCanceled;
        _playerInput.actions["NextTarget"].performed -= OnNextTarget;
        _playerInput.actions["PrevTarget"].performed -= OnPrevTarget;
        _playerInput.actions["Zoom"].performed -= OnZoomPerformed;
        _playerInput.actions["Zoom"].canceled -= OnZoomCanceled;
    }


    // 입력
    private void Update()
    {
        // 카메라 회전
        _yaw += _lookInput.x * _rotationSpeed * Time.deltaTime * 10f; // 감도 보정
        _pitch -= _lookInput.y * _rotationSpeed * Time.deltaTime * 10f;

        // 각도 제한
        _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);

        // 줌
        if (_zoomInput != 0)
        {
            // _zoomInput이 0 보다 크면 줌인
            _currentDistance -= _zoomInput * _zoomSpeed * Time.deltaTime;
            // 거리제한
            _currentDistance = Mathf.Clamp(_currentDistance, _minDistance, _maxDistance);
        }

        // 계속 타겟 상태 체크 관전 중 사망하면 넘어가야하니까
        if (_targetPlayer == null || IsDead(_targetPlayer))
        {
            FindNextTarget(1);
        }
    }

    private void LateUpdate()
    {
        if (_targetPlayer == null) return;

        //  회전 계산
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        // 기준 위치 설정
        Vector3 pivotPos = _targetPlayer.transform.position + Vector3.up * _height;

        // 방향 벡터 (타겟 -> 카메라)
        Vector3 direction = rotation * Vector3.back;

        // 장애물 감지
        // 타겟에서 카메라 쪽으로 레이를 쏴서 벽이 있는지 확인
        float finalDistance = _currentDistance;

        // SphereCast로 카메라 벽에 묻히는거 방지
        if (Physics.SphereCast(pivotPos, _cameraRadius, direction, out _hit, _currentDistance, _obstacleLayer))
        {
            // 카메라를 충돌 지점까지 살짝 당김
            finalDistance = _hit.distance;
        }

        // 최종 위치
        Vector3 targetPos = pivotPos + (direction * finalDistance);

        // 부드럽게 이동
        transform.position = targetPos;

        // 타겟 바라보기
        transform.rotation = rotation;
    }



    #region 타겟 변경


    // 타겟 찾기
    private void FindNextTarget(int direction)
    {
        // 모든 플레이어
        var allPlayers = GameManager.Instance.ActivePlayers;
        if (allPlayers.Count == 0) return;

        // 현재 인덱스
        int startIndex = _targetIndex;
        int loopCount = 0;

        // 플레이어 수 만큼 루프
        while (loopCount < allPlayers.Count)
        {
            // 방향에 따라 인덱스 순환
            // 0미만이거나 플레이어 수 초과하면 반대로 넘어감 
            _targetIndex = (_targetIndex + direction + allPlayers.Count) % allPlayers.Count;
            loopCount++;

            // 타겟 플레이어
            PlayerHandler player = allPlayers[_targetIndex];

            // 로컬 제외
            // 산 플레이어만
            if (player != PlayerHandler.localPlayer && player.IsDead == false)
            {
                _targetPlayer = player;
                return;
            }
        }

        // 모든 플레이어 찾아봤는데 타겟 지정 못했으면 그냥 null
        _targetPlayer = null;
    }


    // 사망 상태 체크
    // 그리고 혹시 몰라 활성화 상태도 체크
    private bool IsDead(PlayerHandler targetPlayer)
    {
        if (targetPlayer == null) return true;
        return targetPlayer.IsDead || targetPlayer.gameObject.activeInHierarchy == false;
    }
    #endregion

    #region 인풋 이벤트
    private void OnNextTarget(InputAction.CallbackContext ctx)
    {
        FindNextTarget(1);
    }
    private void OnPrevTarget(InputAction.CallbackContext ctx)
    {
        FindNextTarget(-1);
    }
    private void OnLookPerformed(InputAction.CallbackContext ctx)
    {
        _lookInput = ctx.ReadValue<Vector2>();
    }
    private void OnLookCanceled(InputAction.CallbackContext ctx)
    {
        _lookInput = Vector2.zero;
    }
    private void OnZoomPerformed(InputAction.CallbackContext ctx)
    {
        float value = ctx.ReadValue<float>();
        // 보정으로 플마 제한
        _zoomInput = Mathf.Clamp(value, -1f, 1f) * 5.0f;
    }
    private void OnZoomCanceled(InputAction.CallbackContext ctx)
    {
        _zoomInput = 0f;
    }
    #endregion


}
