using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingManager : MonoBehaviourPunCallbacks
{
    public static SettingManager Instance;

    [Header("UI 참조")]
    [SerializeField] SettingUI _settingUi;

    [Header("씬 이동 설정")]
    [SerializeField] string _titleSceneName = "Lobby";

    // 저장 키
    private const string KEY_SENSITIVITY = "MouseSensitivity";
    private const string KEY_SCREEN_MODE = "ScreenMode";
    private const string KEY_VOL_MASTER = "MasterVolume";
    private const string KEY_VOL_BGM = "BGMVolume";
    private const string KEY_VOL_SFX = "SFXVolume";
    private const string KEY_MUTE_MASTER = "MasterMute";
    private const string KEY_MUTE_BGM = "BGMMute";
    private const string KEY_MUTE_SFX = "SFXMute";

    private float _sensitivity;
    private float _masterVol;
    private float _bgmVol;
    private float _sfxVol;

    private bool _isMasterOn;
    private bool _isBgmOn;
    private bool _isSfxOn;

    // 데이터
    public float Sensitivity => _sensitivity;    // 감도
    public float MasterVol => _masterVol;        // 전체 볼륨
    public float BgmVol => _bgmVol;              // 브금 볼륨
    public float SfxVol => _sfxVol;              // 효과음 볼륨
    public bool IsMasterOn => _isMasterOn;       // 마스터 토글
    public bool IsBgmOn => _isBgmOn;             // 브금 토글
    public bool IsSfxOn => _isSfxOn;             // 효과음 토글

    private bool _isChangingMode = false;        // 화면 전환 상태

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
    }

    private void Update()
    {
        // esc 입력으로 패널 상태 변경
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 세팅 패널 상태 변경
            EscInput();
        }


        // 화면 변화 감지 (AltEnter)
        CheckScreenChange();
    }

    // 씬 로드 시 실행
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SettingManager] 씬 로드 감지: {scene.name}"); // 로그
        // 씬이 바뀌면 무조건 패널 닫기
        if (_settingUi != null) _settingUi.SetPanelActive(false);
    }

    // 시작 세팅
    private void LoadSettings()
    {
        // 마우스 감도 (기본 15)
        _sensitivity = PlayerPrefs.GetFloat(KEY_SENSITIVITY, 15f);

        // 볼륨 (기본 1)
        _masterVol = PlayerPrefs.GetFloat(KEY_VOL_MASTER, 1f);
        _bgmVol = PlayerPrefs.GetFloat(KEY_VOL_BGM, 1f);
        _sfxVol = PlayerPrefs.GetFloat(KEY_VOL_SFX, 1f);

        // 활성화 (기본값 True)
        _isMasterOn = PlayerPrefs.GetInt(KEY_MUTE_MASTER, 1) == 1;
        _isBgmOn = PlayerPrefs.GetInt(KEY_MUTE_BGM, 1) == 1;
        _isSfxOn = PlayerPrefs.GetInt(KEY_MUTE_SFX, 1) == 1;

        // 화면 모드 적용 (기본 창전체)
        SetScreenMode(PlayerPrefs.GetInt(KEY_SCREEN_MODE, 1));

        // 사운드 적용
        ApplySoundSettings();
    }

    // 사운드 설정 확인
    private void ApplySoundSettings()
    {
        if (SoundManager.Instance == null) return;

        if (IsMasterOn) SoundManager.Instance.SetVolume("MasterVolume", MasterVol);
        else SoundManager.Instance.SetMute("MasterVolume", true);

        if (IsBgmOn) SoundManager.Instance.SetVolume("BGMVolume", BgmVol);
        else SoundManager.Instance.SetMute("BGMVolume", true);

        if (IsSfxOn) SoundManager.Instance.SetVolume("SFXVolume", SfxVol);
        else SoundManager.Instance.SetMute("SFXVolume", true);
    }


    // 마우스 감도 설정
    public void SetSensitivity(float value)
    {
        _sensitivity = value;
        PlayerPrefs.SetFloat(KEY_SENSITIVITY, value);
        _settingUi.UpdateSensitivityText(value);
    }

    // -----------------------------------------------

    #region 초기화
    private void InitUI()
    {
        if (_settingUi == null) return;

        // 감도
        _settingUi.sensitivitySlider.value = Sensitivity;                                  // 감도 갱신
        _settingUi.UpdateSensitivityText(Sensitivity);                                     // 텍스트 갱신
        _settingUi.sensitivitySlider.onValueChanged.AddListener(SetSensitivity);           // 이벤트 연결

        // 화면 모드
        int mode = PlayerPrefs.GetInt(KEY_SCREEN_MODE, 1);                          // 모드 불러오기
        _settingUi.screenModeDropdown.value = mode;                                 // 값 갱신
        _settingUi.screenModeDropdown.RefreshShownValue();                          // 새로고침     // 이벤트 연결
        _settingUi.screenModeDropdown.onValueChanged.AddListener((index) => StartCoroutine(SetScreenModeCoroutine(index)));

        // 사운드 슬라이더 값 로드
        _settingUi.masterSlider.value = MasterVol;
        _settingUi.bgmSlider.value = BgmVol;
        _settingUi.sfxSlider.value = SfxVol;

        // 슬라이더 이벤트 연결
        _settingUi.masterSlider.onValueChanged.AddListener((value) => SetVolume("MasterVolume", value, ref _masterVol, KEY_VOL_MASTER, _settingUi.masterText));
        _settingUi.bgmSlider.onValueChanged.AddListener((value) => SetVolume("BGMVolume", value, ref _bgmVol, KEY_VOL_BGM, _settingUi.bgmText));
        _settingUi.sfxSlider.onValueChanged.AddListener((value) => SetVolume("SFXVolume", value, ref _sfxVol, KEY_VOL_SFX, _settingUi.sfxText));

        // 볼륨 텍스트 갱신
        _settingUi.UpdateVolumeText(_settingUi.masterText, MasterVol);
        _settingUi.UpdateVolumeText(_settingUi.bgmText, BgmVol);
        _settingUi.UpdateVolumeText(_settingUi.sfxText, SfxVol);

        // 사운드 토글 설정
        // 토글 이벤트 연결
        InitToggle(_settingUi.masterToggle, _settingUi.masterIcon, IsMasterOn, (isOn) => SetMute("MasterVolume", isOn, ref _isMasterOn, KEY_MUTE_MASTER));
        InitToggle(_settingUi.bgmToggle, _settingUi.bgmIcon, IsBgmOn, (isOn) => SetMute("BGMVolume", isOn, ref _isBgmOn, KEY_MUTE_BGM));
        InitToggle(_settingUi.sfxToggle, _settingUi.sfxIcon, IsSfxOn, (isOn) => SetMute("SFXVolume", isOn, ref _isSfxOn, KEY_MUTE_SFX));

        // 버튼
        _settingUi.leaveButton.onClick.AddListener(OnClickLeaveRoom);
        _settingUi.closeButton.onClick.AddListener(ToggleSettingPanel);

        RefreshSoundUI();
    }

    // 토글 초기화 (토글, 아이콘, 활성화, 액션)
    private void InitToggle(Toggle toggle, Image icon, bool isOn, UnityAction<bool> action)
    {
        toggle.isOn = isOn;
        _settingUi.UpdateToggleIcon(icon, isOn);
        toggle.onValueChanged.AddListener(action);
    }
    #endregion

    // -----------------------------------------------

    #region 세팅 패널 On/Off

    // ESC 입력
    private void EscInput()
    {
        // 상점 이용 중이면 끄기만 가능
        if (ShopTerminal.IsUsing == true)
        {
            if (_settingUi.IsActive == true) ToggleSettingPanel();
            return;
        }

        // 세팅 패널 토글
        ToggleSettingPanel();
    }

    // 닫기 버튼용
    public void CloseSetting()
    {
        // 세팅 패널 상태 변경
        ToggleSettingPanel();
    }

    // 세팅 패널 상태 변경
    public void ToggleSettingPanel()
    {
        if (_settingUi == null) return;

        // 상태 반전
        bool isOpen = !_settingUi.IsActive;
        _settingUi.SetPanelActive(isOpen);

        // 퇴장 버튼은 방에 있을 때만
        _settingUi.leaveButton.gameObject.SetActive(PhotonNetwork.InRoom);

        // 현재 입력 제어권 객체 찾기
        IInputControllable currentController = GetCurrentController();

        if (currentController != null)
        {
            // 상점 이용 중이 아닐 때만 입력 제어
            if (ShopTerminal.IsUsing == false)
                currentController.SetInputActive(!isOpen);
        }

        // 커서 처리
        // 열려있으면 보이게
        if (isOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        // 닫았는데 단말기 사용 안하면
        else if (ShopTerminal.IsUsing == false)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
    #endregion

    // -----------------------------------------------

    #region 사운드


    // 볼륨 설정 (볼륨 파라미터 이름, 슬라이더 입력값, 현재 값, 저장 키, 볼륨 텍스트)
    private void SetVolume(string name, float inputValue, ref float currentValue, string saveKey, TextMeshProUGUI textUI)
    {
        // 현재 값 변경
        currentValue = inputValue;
        // 키에 저장
        PlayerPrefs.SetFloat(saveKey, inputValue);

        // 사운드 매니저에 볼륨 설정
        SoundManager.Instance.SetVolume(name, inputValue);

        // 텍스트 갱신
        _settingUi.UpdateVolumeText(textUI, inputValue);
    }

    // 뮤트 설정 (볼륨 파라미터 이름, 토글 값, 현재 값, 저장 키)
    private void SetMute(string name, bool isOn, ref bool currentBool, string saveKey)
    {
        // 현재 값 변경
        currentBool = isOn;
        // 저장 (확실하게 보이도록)
        PlayerPrefs.SetInt(saveKey, isOn ? 1 : 0);

        // 켜질 땐 현재 볼륨값
        // 꺼질 땐 Mute 
        if (isOn)
            SoundManager.Instance.SetVolume(name, (name == "MasterVolume") ? MasterVol : (name == "BGMVolume" ? BgmVol : SfxVol));
        else
            SoundManager.Instance.SetMute(name, true);

        // UI 새로고침
        RefreshSoundUI();
    }

    // 사운드 UI 새로고침
    private void RefreshSoundUI()
    {
        if (_settingUi == null) return;
        _settingUi.masterSlider.interactable = IsMasterOn;
        _settingUi.bgmSlider.interactable = IsBgmOn;
        _settingUi.sfxSlider.interactable = IsSfxOn;

        _settingUi.UpdateToggleIcon(_settingUi.masterIcon, IsMasterOn);
        _settingUi.UpdateToggleIcon(_settingUi.bgmIcon, IsBgmOn);
        _settingUi.UpdateToggleIcon(_settingUi.sfxIcon, IsSfxOn);
    }

    #endregion

    // -----------------------------------------------

    #region 화면 모드

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

    // 화면 설정 코루틴
    private IEnumerator SetScreenModeCoroutine(int index)
    {
        // 모드 전환 중 중단
        if (_isChangingMode) yield break;

        // 모드 전환 중
        _isChangingMode = true;

        // UI 강제 갱신
        Canvas.ForceUpdateCanvases();

        // 드롭다운 닫힐 시간
        // 전체화면 거의 바로
        // 나머지 1프레임
        if (index == 0) yield return new WaitForEndOfFrame();
        else yield return null;

        // 전체화면에서 창전체나 창모드갈 때 OS 독점모드 풀어서 버벅인다고함
        if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen && index != 0)
        {
            // 강제로 창모드로 먼저 변경
            Screen.fullScreenMode = FullScreenMode.Windowed;

            // OS가 창 다시 그릴 때까지 대기
            yield return null;
            yield return null;
        }

        // 진짜 모드 설정
        SetScreenMode(index);

        // 설정 적용 후 안정화
        yield return null;

        // 모드 전환 풀기
        _isChangingMode = false;
    }

    // 알트 엔터로 화면 강제 변환 시 계속 체크
    private void CheckScreenChange()
    {
        // 기본 창모드
        int current = 2;

        // 0 전체
        if (Screen.fullScreenMode == FullScreenMode.ExclusiveFullScreen) current = 0;
        // 1 창전체
        else if (Screen.fullScreenMode == FullScreenMode.FullScreenWindow) current = 1;

        // 드롭다운 값이 다를 때
        if (_settingUi.screenModeDropdown.value != current)
        {
            // 설정
            _settingUi.screenModeDropdown.SetValueWithoutNotify(current);
            // 저장
            PlayerPrefs.SetInt(KEY_SCREEN_MODE, current);
        }
    }
    #endregion

    // -----------------------------------------------

    #region 퇴장

    // 방 나가기 버튼
    public void OnClickLeaveRoom()
    {
        // 방 연결 해제
        PhotonNetwork.LeaveRoom();

        // 네트워크 객체 파괴되는거 보이기 싫으니까 가림
        _settingUi.SetExitBlind(true);

        // 바로 창 닫아버리기
        ToggleSettingPanel();

    }

    // 방 나가기 성공 콜백
    public override void OnLeftRoom()
    {
        // 커서 보이기
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 인게임에서만 쓰는 싱글톤 정리
        if (ItemManager.Instance != null) Destroy(ItemManager.Instance.gameObject);
        if (QuickSlotManager.Instance != null) Destroy(QuickSlotManager.Instance.gameObject);

        // 타이틀 씬으로 이동 (로비)
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.RequestLoadScene(_titleSceneName);
        }
    }

    #endregion
}
