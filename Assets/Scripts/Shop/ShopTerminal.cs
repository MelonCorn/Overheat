using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShopTerminal : MonoBehaviour, IInteractable
{
    [Header("상점 매니저")]
    [SerializeField] ShopManager _shopManager;

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

    public void OnInteract()
    {
        if (IsUsing) return;
        // 사용 상태로 전환
        IsUsing = true;
        // 단말기 UI 상호작용 가능
        _terminalCanvasGroup.blocksRaycasts = true;
        // 퀵슬롯 끄기
        QuickSlotManager.Instance.SetUIActive(false);

        if (PlayerHandler.localPlayer != null)
        {
            // 플레이어 입력
            var input = PlayerHandler.localPlayer.GetComponent<PlayerInputHandler>();
            // 플레이어 움직임
            var move = PlayerHandler.localPlayer.GetComponent<PlayerMovementHandler>();
            // 플레이어 카메라
            var cam = PlayerHandler.localPlayer.GetComponent<PlayerCameraHandler>();
            // 플레이어 아이템 움직임

            // 카메라 등록 안되어 있으면
            if (_terminalCanvas.worldCamera == null)
            {
                // 플레이어의 카메라 등록
                _terminalCanvas.worldCamera = cam.LocalCamera;
            }
            // 단말기 캔버스에 카메라 등록 되어있으면
            else
            {
                // 플레이어 입력 UI로 전환
                if (input) input.SetInputActive(false);
                // 플레이어 얼음
                if (move) move.enabled = false;
                // 카메라 납치
                if (cam) cam.MoveCameraToTarget(_viewPoint, _moveDuration);

            }
        }

        // 마우스 커서 풀기
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }


    // 단말기 종료
    private void ExitTerminal()
    {
        // 사용 해제
        IsUsing = false;

        // 단말기 UI 상호작용 불가능
        _terminalCanvasGroup.blocksRaycasts = false;

        if (PlayerHandler.localPlayer != null)
        {
            // 플레이어 입력 플레이어로 전환
            var input = PlayerHandler.localPlayer.GetComponent<PlayerInputHandler>();
            if (input) input.SetInputActive(true);

            // 카메라 돌려보내기
            var cam = PlayerHandler.localPlayer.GetComponent<PlayerCameraHandler>();
            if (cam) cam.ReturnCameraToPlayer(_moveDuration);

            // 플레이어 얼음땡
            var move = PlayerHandler.localPlayer.GetComponent<PlayerMovementHandler>();
            if (move) move.enabled = true;
        }
        
        // 마우스 커서 잠그기
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 퀵슬롯 켜기
        QuickSlotManager.Instance.SetUIActive(false);
    }

    public string GetInteractText(out bool canInteract)
    {
        // 사용 반대
        canInteract = !IsUsing;

        if (canInteract)
            return $"상점 이용";
        else
            return "";
    }
}
