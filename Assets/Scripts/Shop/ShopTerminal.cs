using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShopTerminal : MonoBehaviour, IInteractable
{
    [Header("상점 매니저")]
    [SerializeField] ShopManager _shopManager;

    [Header("단말기 클립")]
    [SerializeField] AudioClip _terminalOnClip;
    [SerializeField] AudioClip _terminalOffClip;

    [Header("단말기 카메라 위치")]
    [SerializeField] Transform _viewPoint;

    [Header("연출 설정")]
    [SerializeField] float _moveDuration = 0.5f;    // 전환 시간

    [Header("닫기 버튼")]
    [SerializeField] Button _exitButton;

    [Header("단말기 UI")]
    [SerializeField] Canvas _terminalCanvas;                // 단말기 캔버스
    [SerializeField] CanvasGroup _terminalCanvasGroup;      // 사용 상태 아니면 레이캐스트 블록 상태 off

    public static bool IsUsing = false;             // 사용 상태
    public bool _isTransition = false;              // 전환 중 상태

    private PlayerInputHandler _localPlayerInputHandler;
    private PlayerMovementHandler _localPlayerMovementHandler;
    private PlayerCameraHandler _localPlayerCameraHandler;
    private PlayerRecoilHandler _localPlayerRecoilHandler;
    private PlayerSoundHandler _localPlayerSoundHandler;

    private void Awake()
    {
        // 일단 항상 초기화
        IsUsing = false;
    }

    private void Start()
    {
        _exitButton.onClick.AddListener(() => ExitTerminal());
    }

    private void Update()
    {
        // 사용 중일 때 입력 체크
        if (IsUsing)
        {
            // 우클릭
            if(Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                // 단말기 종료
                ExitTerminal();
            }
        }
    }

    private void OnDestroy()
    {
        // 사용 상태 해제
        IsUsing = false;
    }

    public AudioClip OnInteract()
    {
        if (IsUsing || _isTransition) return null;

        // 사용 상태로 전환
        IsUsing = true;
        // 단말기 UI 상호작용 가능
        _terminalCanvasGroup.blocksRaycasts = true;
        // 퀵슬롯 끄기
        QuickSlotManager.Instance.SetUIActive(false);

        if (PlayerHandler.localPlayer != null)
        {
            // 플레이어 입력
            if(_localPlayerInputHandler == null)
                _localPlayerInputHandler = PlayerHandler.localPlayer.GetComponent<PlayerInputHandler>();

            // 플레이어 움직임
            if (_localPlayerMovementHandler == null)
                _localPlayerMovementHandler = PlayerHandler.localPlayer.GetComponent<PlayerMovementHandler>();

            // 플레이어 카메라
            if (_localPlayerCameraHandler == null)
                _localPlayerCameraHandler = PlayerHandler.localPlayer.GetComponent<PlayerCameraHandler>();

            // 플레이어 반동
            if (_localPlayerRecoilHandler == null)
                _localPlayerRecoilHandler = PlayerHandler.localPlayer.GetComponentInChildren<PlayerRecoilHandler>();

            // 플레이어 사운드
            if (_localPlayerSoundHandler == null)
                _localPlayerSoundHandler = PlayerHandler.localPlayer.GetComponent<PlayerSoundHandler>();

            // 카메라 등록 안되어 있으면
            if (_terminalCanvas.worldCamera == null)
            {
                // 플레이어의 카메라 등록
                _terminalCanvas.worldCamera = _localPlayerCameraHandler.LocalCamera;
            }

            // 플레이어 입력 UI로 전환
            if (_localPlayerInputHandler) _localPlayerInputHandler.SetInputActive(false);
            // 플레이어 얼음
            if (_localPlayerMovementHandler) _localPlayerMovementHandler.enabled = false;
            // 카메라 납치
            if (_localPlayerCameraHandler) _localPlayerCameraHandler.MoveCameraToTarget(_viewPoint, _moveDuration);
            // 반동 Off
            if (_localPlayerRecoilHandler) _localPlayerRecoilHandler.enabled = false;
        }

        // 마우스 커서 풀기
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        return _terminalOnClip;
    }

    // 단말기 종료
    private void ExitTerminal()
    {
        // 사용 해제
        IsUsing = false;

        // 카메라 되돌아가는 전환 중 다 이동되면 상태 변환
        StartCoroutine(TransitionDelay(_moveDuration));

        // 단말기 UI 상호작용 불가능
        _terminalCanvasGroup.blocksRaycasts = false;

        if (PlayerHandler.localPlayer != null)
        {
            // 플레이어 입력 플레이어로 전환
            if (_localPlayerInputHandler) _localPlayerInputHandler.SetInputActive(true);

            // 카메라 돌려보내기
            if (_localPlayerCameraHandler) _localPlayerCameraHandler.ReturnCameraToPlayer(_moveDuration);

            // 플레이어 얼음땡
            if (_localPlayerMovementHandler) _localPlayerMovementHandler.enabled = true;

            // 닫기 사운드 재생
            if (_localPlayerSoundHandler) _localPlayerSoundHandler.PlayInteractSound(_terminalOffClip);

            // 얼음땡 기다리고 반동 허용
            StartCoroutine(EnableRecoilDelay(_moveDuration));
        }
        
        // 마우스 커서 잠그기
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 퀵슬롯 켜기
        QuickSlotManager.Instance.SetUIActive(true);

        // 버튼 선택되는거 바로 풀기
        EventSystem.current.SetSelectedGameObject(null);
    }

    private IEnumerator TransitionDelay(float delay)
    {   
        // 락 걸기
        _isTransition = true;

        // 카메라 이동 시간만큼 대기
        yield return new WaitForSeconds(delay);

        // 락 풀기
        _isTransition = false; 
    }

    // 단말기 나오는동안 반동 금지
    private IEnumerator EnableRecoilDelay(float delay)
    {
        // 카메라가 돌아오는 시간만큼 대기 (여유까지)
        yield return new WaitForSeconds(delay);
        yield return null;

        if (PlayerHandler.localPlayer != null)
        {
            var recoil = PlayerHandler.localPlayer.GetComponentInChildren<PlayerRecoilHandler>();
            if (recoil)
            {
                recoil.enabled = true;
            }
        }
    }


    public string GetInteractText(out bool canInteract)
    {
        // 사용 반대
        canInteract = !IsUsing && !_isTransition;

        if (canInteract)
            return $"상점 이용";
        else
            return "";
    }


    
}
