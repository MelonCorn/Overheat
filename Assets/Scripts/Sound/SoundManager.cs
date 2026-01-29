using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.Audio;
using UnityEngine;

public enum BGMType     // 배경음 타입
{
    None,       // 없음
    Lobby,      // 로비 (타이틀)
    Waiting,    // 마을
    Battle,     // 전투
    GameOver,   // 게임오버
    GameClear,  // 클리어
    Shop,       // 상점
}

public enum SFXType     // 효과음 타입 (공용만 - 버튼, 팝업)
{
    Click,      // 클릭
    PopupOpen,  // 팝업 켬
    PopupClose, // 팝업 끔
}


[System.Serializable]
public class BGMData
{
    public string name;         // 구분용 이름
    public BGMType type;        // 타입
    public AudioClip clip;      // 클립
}

[System.Serializable]
public class SFXData
{
    public string name;         // 구분용 이름
    public SFXType type;        // 타입
    public AudioClip clip;      // 클립
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private SettingUI _settingUI;

    [Header("오디오 믹서")]
    [SerializeField] AudioMixer _mixer;      // 믹서
    [Header("오디오 소스")]
    [SerializeField] AudioSource _bgmSource; // 배경음 소스
    [SerializeField] AudioSource _sfxSource; // 효과음 소스

    [Header("BGM 설정")]
    [SerializeField] float _fadeTime = 1.0f;
    [SerializeField] float _volume = 0.5f;
    [SerializeField] List<BGMData> _bgmList;

    [Header("공용 SFX 설정")]
    [SerializeField] List<SFXData> _sfxList;

    [Header("3D SFX 쿨타임")]
    [SerializeField] float _sfxCoolDown = 0.05f;

    // 현재 볼륨
    public float MasterVolume { get; private set; } = 1f;
    public float BGMVolume { get; private set; } = 1f;
    public float SFXVolume { get; private set; } = 1f;

    // 볼륨 상태
    public bool IsMasterOn { get; private set; } = true;
    public bool IsBGMOn { get; private set; } = true;
    public bool IsSFXOn { get; private set; } = true;


    // 클립 간단하게 가져올 수 있도록 딕셔너리 생성
    private Dictionary<BGMType, AudioClip> _bgmTable = new Dictionary<BGMType, AudioClip>();
    private Dictionary<SFXType, AudioClip> _sfxTable = new Dictionary<SFXType, AudioClip>();

    // 클립별 쿨타임용
    private Dictionary<AudioClip, float> _sfxCooldowns = new Dictionary<AudioClip, float>(); 

    // 배경음 코루틴
    private Coroutine _bgmCoroutine;

    // 세팅 액션
    private InputAction _settingAction;

    private void Awake()
    {
        if (Instance == null)
        {
            // SettingManager와 붙어있어서 DontDestroyOnLoad는 하지 않음
            Instance = this;

            // 리스트를 딕셔너리로 전환
            foreach (var data in _bgmList) _bgmTable.TryAdd(data.type, data.clip);
            foreach (var data in _sfxList) _sfxTable.TryAdd(data.type, data.clip);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 씬 로드 구독
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 맨첨에 일단 실행
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        // 파괴될일 없을 것 같지만 그래도 구독 해지
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    // 전체
    public void SetVolume(string name, float value)
    {
        // 믹서 그룹 볼륨 설정
        _mixer.SetFloat(name, Mathf.Log10(value) * 20);
    }
    public void SetMute(string name, bool isMute)
    {
        // Mute면 -80db
        // 아니면 SettingManager로 다시 복구
        if (isMute) _mixer.SetFloat(name, -80f);
    }

    // 씬 로드시 실행
    // 씬 이름에 맞는 타입의 배경음 재생
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 BGM 재생
        PlaySceneBGM(scene.name);
    }

    // 현재 씬의 기본 BGM으로 되돌리기
    // 보스전 끝났거나, 특수 상황 끝났을 떄
    public void PlaySceneBGM(string sceneName)
    {
        // 씬이름에 따라 BGM 타입 가져오기
        BGMType defaultType = GetSceneBGMType(sceneName);

        // 재생
        PlayBGM(defaultType);
    }

    // 씬에 따라 BGM 타입 반환
    private BGMType GetSceneBGMType(string sceneName)
    {
        BGMType targetBGM;

        switch (sceneName)
        {
            case "Lobby":
                targetBGM = BGMType.Lobby;
                break;
            case "Room":
                targetBGM = BGMType.Waiting;
                break;
            case "Game":
                targetBGM = BGMType.Battle;
                break;
            case "Shop":
                targetBGM = BGMType.Shop;
                break;
            default:
                targetBGM = BGMType.None;
                break;
        }

        return targetBGM;
    }
    // 배경음 재생 (BGMType)
    public void PlayBGM(BGMType type)
    {
        // None으로 끄기 시도하거나 타입 찾지 못했으면 멈춤
        if (type == BGMType.None)
        {
            // 재생중일 때
            if (_bgmSource.isPlaying)
            {
                //이미 꺼져있으면 무시
                if (_bgmCoroutine != null) StopCoroutine(_bgmCoroutine);
                //아니면 페이드 아웃만 실행
                _bgmCoroutine = StartCoroutine(BGMOff());
            }
            return;
        }

        // 해당 타입의 클립 배열 가져오기
        if (_bgmTable.TryGetValue(type, out AudioClip clip))
        {
            // 이미 재생 중인 배경음이면 무시
            if (_bgmSource.isPlaying && _bgmSource.clip == clip) return;

            // 코루틴이 돌고 있으면 정지
            if (_bgmCoroutine != null) StopCoroutine(_bgmCoroutine);

            // 배경음 교체 코루틴 시작
            _bgmCoroutine = StartCoroutine(ChangeBGM(clip));
        }
    }

    // 배경음 변경
    IEnumerator ChangeBGM(AudioClip nextClip)
    {
        // 이미 재생 중일 때만 줄임
        if (_bgmSource.isPlaying)
        {
            while (_bgmSource.volume > 0)
            {
                // 볼륨 내리기       프레임 단위로
                _bgmSource.volume -= Time.deltaTime / _fadeTime;
                yield return null;
            }
        }

        // 다 줄이고 확실히 볼륨 0, 멈추기
        _bgmSource.volume = 0;
        _bgmSource.Stop();

        // Clip 교체, 재생
        _bgmSource.clip = nextClip;
        _bgmSource.Play();

        // 페이드인
        while (_bgmSource.volume < _volume)
        {
            _bgmSource.volume += Time.deltaTime / _fadeTime;
            yield return null;
        }

        // 끝나면 확실히 1로
        _bgmSource.volume = _volume;
    }

    // 배경음 끄기 (페이드 아웃)
    IEnumerator BGMOff()
    {
        while (_bgmSource.volume > 0)
        {
            _bgmSource.volume -= Time.deltaTime / _fadeTime;
            yield return null;
        }
        _bgmSource.volume = 0;
        _bgmSource.Stop();
        _bgmSource.clip = null;
    }

    // 3D 효과음용
    // 딕셔너리에 클립 찾아보고 쿨타임 됐으면 재생
    public void PlayOneShot3D(AudioSource source, AudioClip clip)
    {
        // 둘 중 하나라도 없으면 무시
        if (clip == null || source == null) return;

        // 쿨타임 체크
        if (_sfxCooldowns.TryGetValue(clip, out float lastTime))
        {
            if (Time.time < lastTime + _sfxCoolDown) return;
        }

        // 쿨타임 갱신
        _sfxCooldowns[clip] = Time.time;

        // 믹서 그룹 맞추기 (SFX 볼륨 적용되게)
        if (_sfxSource.outputAudioMixerGroup != null)
            source.outputAudioMixerGroup = _sfxSource.outputAudioMixerGroup;

        // 한 번 재생
        source.PlayOneShot(clip);
    }


    // 공용 효과음 재생 (SFXType)
    public void PlaySFX(SFXType type)
    {
        if (_sfxTable.TryGetValue(type, out AudioClip clip))
        {
            PlaySFX(clip); // 아래 함수 재활용
        }
    }

    // 일반 재생 (보통 뭐 구매, 음식 이런거)
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("AudioClip이 할당되어있지 않습니다.");
            return;
        }
        _sfxSource.PlayOneShot(clip);
    }
}
