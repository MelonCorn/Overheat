using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraHandler : MonoBehaviour
{
    private PlayerInputHandler _inputHandler;

    [Header("진짜 카메라")]
    [SerializeField] Camera _camera;

    [Header("카메라 홀더")]
    [SerializeField] Transform _cameraHolder;

    [Header("아이템 홀더")]
    [SerializeField] GameObject _handHolder;
    
    [Header("회전 세팅")]
    [SerializeField] float _topClamp = -90f;        // 위 제한
    [SerializeField] float _bottomClamp = 90f;      // 아래 제한

    public Camera LocalCamera => _camera;

    private float _xRotation = 0f; // 현재 수직 회전

    private bool _isTab;

    // 단말기 이용 상태
    private bool _isTerminalControl;

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Start()
    {
        // 로컬 객체일 때
        // 마우스 커서 숨기고 고정
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            _isTab = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Keyboard.current.tabKey.wasReleasedThisFrame)
        {
            _isTab = false;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // 탭, 단말기 사용금지
        if (_isTab || _isTerminalControl) return;

        Rotate();   // 카메라 회전
    }


    private void Rotate()
    {
        // 마우스 회전 입력값
        Vector2 mouseInput = _inputHandler.LookInput;

        // 마우스 감도에 따라 수평(좌우) 회전 (루트)
        float mouseX = mouseInput.x * SettingManager.Instance.Sensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        // 마우스 감도에 따라 수직(상하) 회전 (카메라 홀더)
        float mouseY = mouseInput.y * SettingManager.Instance.Sensitivity * Time.deltaTime;
        _xRotation -= mouseY;
        // 위아래 제한
        _xRotation = Mathf.Clamp(_xRotation, _topClamp, _bottomClamp);

        // 카메라 수직 회전 적용
        _cameraHolder.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
    }


    #region 단말기 카메라

    // 카메라를 타겟으로 이동
    public void MoveCameraToTarget(Transform target, float duration)
    {
        if (_camera == null) return;

        // 단말기 사용 중
        _isTerminalControl = true;

        // 아이템 안보이게
        _handHolder.SetActive(false);

        // 실행중인 코루틴 모두 정지
        StopAllCoroutines();

        // 이동 코루틴 실행
        StartCoroutine(MoveToTarget(target, duration));
    }

    // 원래 자리로 복귀
    public void ReturnCameraToPlayer(float duration)
    {
        if (_camera == null) return;

        // 실행중인 코루틴 모두 정지
        StopAllCoroutines();
        
        // 복귀 코루틴 실행
        StartCoroutine(ReturnCamera(duration));
    }


    // 단말기로 이동
    private IEnumerator MoveToTarget(Transform target, float duration)
    {
        float timer = 0f;

        // 이동 시작 전 데이터
        Vector3 startPos = _camera.transform.position;      // 위치
        Quaternion startRot = _camera.transform.rotation;   // 회전

        // 시간동안
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            // 부드러운 곡선
            t = Mathf.SmoothStep(0f, 1f, t);

            // 월드 좌표 기준으로 부드럽게 전환
            _camera.transform.position = Vector3.Lerp(startPos, target.position, t);
            _camera.transform.rotation = Quaternion.Slerp(startRot, target.rotation, t);

            // 마지막 프레임에 계속 갱신
            yield return GameManager.Instance.EndOfFrame;
        }

        // 최종 보정
        _camera.transform.position = target.position;
        _camera.transform.rotation = target.rotation;
    }

    // 플레이어 몸으로 복귀 
    private IEnumerator ReturnCamera(float duration)
    {
        float timer = 0f;

        // 현재 위치 (단말기)
        Vector3 startPos = _camera.transform.position;      // 위치
        Quaternion startRot = _camera.transform.rotation;   // 회전

        // 시간동안
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            // 부드러운 곡선
            t = Mathf.SmoothStep(0f, 1f, t);

            // 돌아가는거 홀더로 이동
            Vector3 targetPos = _cameraHolder.position;
            Quaternion targetRot = _cameraHolder.rotation;

            // 부드럽게 전환
            _camera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            _camera.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return GameManager.Instance.EndOfFrame;
        }

        // 복귀 완료 후
        // 0,0,0 으로 딱 보정
        _camera.transform.localPosition = Vector3.zero;
        _camera.transform.localRotation = Quaternion.identity;

        // 잠금 해제
        _isTerminalControl = false;

        // 아이템 다시 보이게
        _handHolder.SetActive(true);

        // 아이템 들리게 손 새로고침 
        QuickSlotManager.Instance.RefreshHand();
    }
    #endregion

}
