using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CharacterController))]
public class PlayerInputHandler : MonoBehaviour
{
    private PlayerInput _input;

    public event Action OnJumpEvent;     // 점프 이벤트
    public event Action OnInteractEvent; // 상호작용 이벤트
    public event Action OnDropEvent;     // 버리기 이벤트
    public bool IsSprint { get; private set; }     // 달리기 상태

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
}
