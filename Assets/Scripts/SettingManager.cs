using Photon.Pun;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEditor.IMGUI.Controls.PrimitiveBoundsHandle;

public class SettingManager : MonoBehaviourPunCallbacks
{
    public static SettingManager Instance;

    [Header("씬 이동")]
    [SerializeField] string _titleSceneName = "Lobby";

    [Header("UI 설정")]
    [SerializeField] GameObject _settingUI;             // 세팅 UI 패널
    [SerializeField] TMP_Dropdown _screenModeDropdown;  // 화면모드 드롭다운
    [SerializeField] Slider _sensitivitySlider;         // 마우스 감도 슬라이더
    [SerializeField] TextMeshProUGUI _sensitivityText;  // 현재 마우스 감도 텍스트

    // 설정값 저장용 키
    private const string KEY_SENSITIVITY = "MouseSensitivity";
    private const string KEY_SCREEN_MODE = "ScreenMode";

    // 현재 마우스 감도
    public float MouseSensitivity { get; private set; }

    // 패널 상태
    private bool _isOpen = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 저장된 설정 불러오기
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        // UI 초기화
        InitUI();
    }

    private void Update()
    {
        // esc 입력으로 패널 상태 변경
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 세팅 패널 상태 변경
            ToggleSettingPanel();
        }
    }


    #region 세팅

    private void LoadSettings()
    {
        // 마우스 감도
        // 기본값 15f
        MouseSensitivity = PlayerPrefs.GetFloat(KEY_SENSITIVITY, 15f);

        // 화면 모드
        // 기본 보더리스
        // 0: 전체화면 1: 경계없는 창모드 2: 전체창모드
        int modeIndex = PlayerPrefs.GetInt(KEY_SCREEN_MODE, 1);
        SetScreenMode(modeIndex);
    }


    // 마우스 감도 설정
    public void SetSensitivity(float value)
    {
        MouseSensitivity = value;
        PlayerPrefs.SetFloat(KEY_SENSITIVITY, value);
    }


    // 화면 모드 설정
    public void SetScreenMode(int index)
    {
        // 현재 모니터 해상도
        Resolution maxResolusion = Screen.currentResolution;

        switch (index)
        {
            case 0: // 전체화면
                Screen.SetResolution(maxResolusion.width, maxResolusion.height, FullScreenMode.ExclusiveFullScreen);
                break;

            case 1: // 보더리스
                // 해상도는 최대로, 모드는 FullScreenWindow
                Screen.SetResolution(maxResolusion.width, maxResolusion.height, FullScreenMode.FullScreenWindow);
                break;

            case 2: // 창모드
                // 16:9 비율 유지하면서 조금만 작게
                Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
                break;
        }

        PlayerPrefs.SetInt(KEY_SCREEN_MODE, index);
    }
    #endregion

    #region UI
    private void InitUI()
    {
        // 화면모드 드롭다운 초기화 (고정 드롭다운)
        // 화면모드 불러오기, 기본값 창전체
        int currentMode = PlayerPrefs.GetInt("ScreenMode", 1);
        if(_screenModeDropdown != null)
        {
            // 화면모드 드롭다운 설정
            _screenModeDropdown.value = currentMode;
            // 화면모드 드롭다운 새로고침
            _screenModeDropdown.RefreshShownValue();
            // 화면모드 드롭다운 값 변경 이벤트 연결
            _screenModeDropdown.onValueChanged.AddListener((index) => SetScreenMode(index));
        }

        // 감도 슬라이더 초기화
        // 현재 감도
        float currentSensitivity = MouseSensitivity;

        if(_sensitivitySlider != null)
        {
            _sensitivitySlider.minValue = 1f;    // 최소 감도
            _sensitivitySlider.maxValue = 100f;  // 최대 감도
            // 슬라이더 값 설정
            _sensitivitySlider.value = currentSensitivity;

            // 슬라이더 값 변경 이벤트
            _sensitivitySlider.onValueChanged.AddListener((value) =>
            {
                SetSensitivity(value);          // 감도 변경
                UpdateSensitivityText(value);   // 텍스트 변경
            });
        }

        // 한 번 갱신
        UpdateSensitivityText(currentSensitivity);
    }

    // 감도 텍스트 변경
    private void UpdateSensitivityText(float value)
    {
        // 점하나 내리고 소수점 한 자리로 반올림
        if (_sensitivityText != null)
            _sensitivityText.text = $"{(value * 0.1f):0.0}";
    }

    // 닫기 버튼용
    public void CloseSetting()
    {
        gameObject.SetActive(false);
    }
    #endregion


    // 세팅 패널 상태 변경
    public void ToggleSettingPanel()
    {
        // 상태 반전
        _isOpen = !_isOpen;
        _settingUI?.SetActive(_isOpen);

        // 현재 입력 제어권을 가진 객체 찾기
        IInputControllable currentController = GetCurrentController();

        // On으로 변경 시
        if (_isOpen == true)
        {
            // 커서 보이기
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 입력 끄기
            currentController?.SetInputActive(false);
        }
        // Off로 변경 시
        else
        {
            // 커서 잠금은 컨트롤러 있을 때만
            if (currentController != null)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                // 입력 켜기 (인터페이스 사용)
                currentController.SetInputActive(true);
            }
        }
    }
    // 현재 입력 객체 가져오기
    private IInputControllable GetCurrentController()
    {
        // 먼저 관전 카메라 상태 체크
        if (SpectatorCamera.Instance != null && SpectatorCamera.Instance.gameObject.activeInHierarchy)
        {
            return SpectatorCamera.Instance;
        }

        // 그다음 로컬 플레이어
        if (PlayerHandler.localPlayer != null)
        {
            return PlayerHandler.localPlayer.GetComponent<IInputControllable>();
        }

        return null;
    }




    #region 퇴장

    // 방 나가기 버튼
    public void OnClickLeaveRoom()
    {
        // 방 연결 해제
        PhotonNetwork.LeaveRoom();

        // 버튼 중복 클릭 방지로 버튼 UI 비활성화 해도될듯
    }

    // 게임 종료 버튼
    public void OnClickQuitGame()
    {
        Application.Quit();

        // 에디터에서는 종료 시 플레이 풀기
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // 방 나가기 성공 콜백
    public override void OnLeftRoom()
    {
        // 타이틀 씬으로 이동 (로비)
        LoadingManager.Instance?.RequestLoadScene(_titleSceneName);
    }

#endregion
}
