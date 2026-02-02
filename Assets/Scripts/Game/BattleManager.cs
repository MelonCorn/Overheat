using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviourPun, IPunObservable
{
    public static BattleManager Instance;

    [Header("스테이지 길이 설정")]
    [SerializeField] float _baseDistance = 1000f;    // 기본 거리
    [SerializeField] float _distancePerLevel = 200f; // 레벨당 추가 거리

    [Header("스테이지 진행률 슬라이더")]
    [SerializeField] Slider _progressSlider;

    private float _totalDistance;      // 목표 거리         (클리어 목표)
    private float _currentDistance;    // 현재 이동 거리

    private bool _isStageClear = false; // 스테이지 클리어 여부

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        // 스테이지 목표 설정 (테스트용 1레벨)
        int stageLevel = 1;
        _totalDistance = _baseDistance + ((stageLevel - 1) * _distancePerLevel);

        // 이동 거리 초기화
        _currentDistance = 0f;

        // UI 초기화
        if (_progressSlider) _progressSlider.value = 0;
    }

    private void Update()
    {
        // 클리어 시 무시
        if (_isStageClear == true) return;

        // 게임 매니저 없거나, 시작 안했거나, 게임오버 상태면 무시
        if (GameManager.Instance == null || GameManager.Instance.IsGameStart == false || GameManager.Instance.IsGameOver == true) return;

        // 열차 매니저 없거나, 준비가 덜되었다면 무시
        if (TrainManager.Instance == null || TrainManager.Instance.IsTrainReady == false) return;
        // 거리 계산
        if (PhotonNetwork.IsMasterClient)
        {
            CalculateProgress();
        }

        // 진행 UI 갱신
        UpdateProgressUI();
    }

    // 거리 계산
    private void CalculateProgress()
    {
        // 엔진 가져와서
        EngineNode engine = TrainManager.Instance.MainEngine;

        //엔진 있으면
        if (engine != null)
        {
            // 거리 = 속도 * 시간
            float speed = engine.CurrentSpeed;
            _currentDistance += speed * Time.deltaTime;

            // 현재 이동 거리가 누적 이동거리 이상이면
            if (_currentDistance >= _totalDistance)
            {
                // 스테이지 클리어
                ClearStage();
            }
        }
    }

    // 진행도 갱신
    private void UpdateProgressUI()
    {
        // 거리 비율 계산
        float ratio = _currentDistance / _totalDistance;

        if (_progressSlider != null)
        {
            _progressSlider.value = Mathf.Lerp(0, 1, ratio);
        }
    }

    // 스테이지 클리어 -> 상점으로 이동
    private void ClearStage()
    {
        if (_isStageClear) return;
        _isStageClear = true;

        Debug.Log("오늘 하루도 생존!");

        // 씬 변경 요청
        if (GameManager.Instance != null) GameManager.Instance.RequestChangeScene();
    }

    // 데이터 동기화
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(_currentDistance);
        }
        else
        {
            _currentDistance = (float)stream.ReceiveNext();
        }
    }
}
