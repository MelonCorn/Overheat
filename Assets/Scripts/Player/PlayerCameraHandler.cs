using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraHandler : MonoBehaviour
{
    private PlayerInputHandler _inputHandler;

    [Header("카메라 홀더")]
    [SerializeField] Transform _cameraHolder;
    
    [Header("회전 세팅")]
    [SerializeField] float _mouseSensitivity = 15f; // 마우스 감도
    [SerializeField] float _topClamp = -90f;        // 위 제한
    [SerializeField] float _bottomClamp = 90f;      // 아래 제한

    private float _xRotation = 0f; // 현재 수직 회전

    private bool _isTab;

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
        if (_isTab == false)
            Rotate();   // 카메라 회전

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


    }


    private void Rotate()
    {
        // 마우스 회전 입력값
        Vector2 mouseInput = _inputHandler.LookInput;

        // 마우스 감도에 따라 수평(좌우) 회전 (루트)
        float mouseX = mouseInput.x * _mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        // 마우스 감도에 따라 수직(상하) 회전 (카메라 홀더)
        float mouseY = mouseInput.y * _mouseSensitivity * Time.deltaTime;
        _xRotation -= mouseY;
        // 위아래 제한
        _xRotation = Mathf.Clamp(_xRotation, _topClamp, _bottomClamp);

        // 카메라 수직 회전 적용
        _cameraHolder.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
    }
}
