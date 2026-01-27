using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CharacterController))]
public class PlayerInputHandler : MonoBehaviour, IInputControllable
{
    private PlayerInput _input;

    public event Action OnJumpEvent;     // 점프 이벤트
    public event Action OnInteractEvent; // 상호작용 이벤트
    public event Action OnDropEvent;     // 버리기 이벤트
    public event Action OnFireEvent;     // 좌클릭 이벤트
    public event Action<int> OnQuickSlotEvent;  // 퀵슬롯 이벤트

    public bool IsSprint { get; private set; }     // 달리기 상태
    public bool IsFiring { get; private set; }     // 마우스 누르고 있는 상태

    public Vector2 MoveInput { get; private set; } // 이동값 저장
    public Vector2 LookInput { get; private set; } // 회전값 저장

    private void Awake()
    {
        _input = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        _input.actions["Move"].performed += OnMovePerformed;
        _input.actions["Move"].canceled += OnMoveCanceled;

        _input.actions["Jump"].performed += OnJump;

        _input.actions["Sprint"].performed += OnSprint;
        _input.actions["Sprint"].canceled += OnSprint;

        _input.actions["Interact"].performed += OnInteract;
        _input.actions["DropItem"].performed += OnDropItem;

        _input.actions["Look"].performed += OnLookPerformed;
        _input.actions["Look"].canceled += OnLookCanceled;

        _input.actions["Fire"].performed += OnFirePerformed;
        _input.actions["Fire"].canceled += OnFireCanceled;

        _input.actions["Slot1"].performed += _ => OnQuickSlotEvent?.Invoke(0);
        _input.actions["Slot2"].performed += _ => OnQuickSlotEvent?.Invoke(1);
        _input.actions["Slot3"].performed += _ => OnQuickSlotEvent?.Invoke(2);

        if(QuickSlotManager.Instance != null)
        {
            OnQuickSlotEvent += QuickSlotManager.Instance.SelectSlot;
        }
    }

    private void OnDisable()
    {
        _input.actions["Move"].performed -= OnMovePerformed;
        _input.actions["Move"].canceled -= OnMoveCanceled;

        _input.actions["Jump"].performed -= OnJump;

        _input.actions["Sprint"].performed -= OnSprint;
        _input.actions["Sprint"].canceled -= OnSprint;

        _input.actions["Interact"].performed -= OnInteract;
        _input.actions["DropItem"].performed -= OnDropItem;

        _input.actions["Look"].performed -= OnLookPerformed;
        _input.actions["Look"].canceled -= OnLookCanceled;

        _input.actions["Fire"].performed -= OnFirePerformed;
        _input.actions["Fire"].canceled -= OnFireCanceled;

        if (QuickSlotManager.Instance != null)
        {
            OnQuickSlotEvent -= QuickSlotManager.Instance.SelectSlot;
        }
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        MoveInput = ctx.ReadValue<Vector2>();
    }
    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        MoveInput = Vector2.zero;
    }
    private void OnJump(InputAction.CallbackContext ctx)
    {
        OnJumpEvent?.Invoke();
    }
    private void OnSprint(InputAction.CallbackContext ctx)
    {
        IsSprint = ctx.ReadValueAsButton();
    }
    private void OnInteract(InputAction.CallbackContext ctx)
    {
        OnInteractEvent?.Invoke();
    }
    private void OnDropItem(InputAction.CallbackContext ctx)
    {
        OnDropEvent?.Invoke();
    }
    private void OnLookPerformed(InputAction.CallbackContext ctx)
    {
        LookInput = ctx.ReadValue<Vector2>();
    }
    private void OnLookCanceled(InputAction.CallbackContext ctx)
    {
        LookInput = Vector2.zero;
    }
    private void OnFirePerformed(InputAction.CallbackContext ctx)
    {
        IsFiring = true;
        OnFireEvent?.Invoke();
    }
    private void OnFireCanceled(InputAction.CallbackContext ctx)
    {
        IsFiring = false;
    }


    // 입력 모드 변경
    public void SetInputActive(bool active)
    {
        if (active)
        {
            // 플레이어 맵으로 변경
            _input.SwitchCurrentActionMap("Player");
        }
        else
        {
            // UI 모드로 변경
            _input.SwitchCurrentActionMap("UI");

            // 움직이다가 메뉴 켰을 때 멈추게
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
            IsFiring = false;
            IsSprint = false;
        }
    }
}
