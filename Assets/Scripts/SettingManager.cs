using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("버튼")]
    [SerializeField] Button _leaveButton;   // 방 나가기 버튼
    [SerializeField] Button _closeButton;   // 닫기(확인) 버튼

    [Header("퇴장 패널")]
    [SerializeField] GameObject _exitBlind; // 퇴장 가리개
    
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
    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // 환경설정 UI 초기화
        InitUI();

        if (_exitBlind != null) _exitBlind.SetActive(false);
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

    // 씬 로드 시 실행
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬이 바뀌면 무조건 패널 닫기
        _isOpen = false;
        if (_settingUI != null) _settingUI.SetActive(false);
        if(_exitBlind != null)  _exitBlind.SetActive(false);
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
        if (_screenModeDropdown != null)
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

        if (_sensitivitySlider != null)
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

        // 퇴장 버튼 연결
        if (_leaveButton != null) _leaveButton.onClick.AddListener(OnClickLeaveRoom);
        // 닫기 버튼에 세팅 패널 토글 연결
        if (_closeButton != null) _closeButton.onClick.AddListener(ToggleSettingPanel);
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
        // 세팅 패널 상태 변경
        ToggleSettingPanel();
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
            // UI 상태 변경
            UpdateUIState();

            // 커서 보이기
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 입력 끄기
            currentController?.SetInputActive(false);
        }
        // Off로 변경 시
        else
        {
            // 인터페이스를 이용한 체크이기 때문에 내부에 따로 널체크 함
            // null 아니고 활성화되어있을때
            if (currentController != null)
            {
                // 커서 잠금은 컨트롤러 있을 때만
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                // 입력 켜기 (인터페이스 사용)
                currentController.SetInputActive(true);
            }
        }
    }


    // UI 상태 변경
    private void UpdateUIState()
    {
        // 로비인지 방인지 체크
        bool isRoom = PhotonNetwork.InRoom;

        // 퇴장 버튼 상태 변경
        if (_leaveButton != null)
            _leaveButton.gameObject.SetActive(isRoom);
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

        // 네트워크 객체 파괴되는거 보이기 싫으니까 가림
        if (_exitBlind != null) _exitBlind.SetActive(true);

        // 바로 창 닫아버리기
        ToggleSettingPanel();

    }

    // 방 나가기 성공 콜백
    public override void OnLeftRoom()
    {
        // 커서 보이기
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 싱글톤 정리
        CleanupSingleTons();

        // 타이틀 씬으로 이동 (로비)
        LoadingManager.Instance?.RequestLoadScene(_titleSceneName);
    }

    // 인게임에서만 쓰는 싱글톤들 파괴
    private void CleanupSingleTons()
    {
        // 파괴 대상 매니저들
        DestroySingleton(ItemManager.Instance);
        DestroySingleton(QuickSlotManager.Instance);
    }

    // 제네릭 파괴 함수
    private void DestroySingleton<T>(T instance) where T : MonoBehaviour
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
        }
    }

    #endregion
}
