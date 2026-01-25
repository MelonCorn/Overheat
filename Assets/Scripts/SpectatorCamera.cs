using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

public class SpectatorCamera : MonoBehaviour
{
    [Header("관전 설정")]
    [SerializeField] float _distance = 6.0f;
    [SerializeField] float _height = 2.5f;
    [SerializeField] float _rotationSpeed = 2.0f;
    [SerializeField] float _smoothSpeed = 10f;

    // 타겟
    private PlayerHandler _targetPlayer;
    private int _targetIndex = -1;

    // 입력값 저장용
    private Vector2 _lookInput;

    // 카메라 회전각
    private float _yaw = 0f;
    private float _pitch = 0f;

    // 컴포넌트 캐싱
    private PlayerInput _playerInput;

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
    }


    // 입력
    private void Update()
    {
        // 카메라 회전 적용
        _yaw += _lookInput.x * _rotationSpeed * Time.deltaTime * 10f; // 감도 보정
        _pitch -= _lookInput.y * _rotationSpeed * Time.deltaTime * 10f;

        _pitch = Mathf.Clamp(_pitch, -45f, 60f);

        // 계속 타겟 상태 체크 관전 중 사망하면 넘어가야하니까
        if (_targetPlayer == null || IsDead(_targetPlayer))
        {
            FindNextTarget(1);
        }
    }

    private void LateUpdate()
    {
        // 카메라 위치 관리
    }

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
    #endregion


}
