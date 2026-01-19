using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EngineUI : MonoBehaviour
{
    [Header("엔진 계기판 UI")]
    [SerializeField] private TextMeshProUGUI _speedText; // 속도 텍스트
    [SerializeField] private Slider _fuelSlider;         // 연료 게이지
    //[SerializeField] private Image _fuelGaugeImage;   // 연료 바늘?

    [Header("연료 게이지 속도")]
    [SerializeField] private float _lerpSpeed = 10f;

    private float _currentDisplayedFuel = 0f; // 현재 연료 슬라이더 값
    private float _targetFuelRatio = 0f;      // 목표 연료 비율

    private int _lastChangedSpeed = -1;

    private EngineNode _targetEngine;
    
    
    private void Update()
    {
        if (_fuelSlider != null)
        {
            // 게이지 변화
            _currentDisplayedFuel = Mathf.Lerp(_currentDisplayedFuel,_targetFuelRatio,Time.deltaTime * _lerpSpeed);
            _fuelSlider.value = _currentDisplayedFuel;
        }
    }

    // 엔진 연결
    public void ConnectEngine(EngineNode engine)
    {
        _targetEngine = engine;

        // UI 갱신 이벤트 구독
        _targetEngine.OnEngineStatChanged += UpdateDashboard;

        // 속도 텍스트 갱신
        UpdateSpeedText(engine.CurrentSpeed);

        // 엔진 UI 켜기
        gameObject.SetActive(true);
    }

    // 엔진에서 신호가 오면 UI 갱신
    private void UpdateDashboard(float curSpeed, float curFuel, float maxFuel)
    {
        // 연료 목표치 설정
        UpdateFuelTarget(curFuel, maxFuel);

        // 텍스트 갱신
        UpdateSpeedText(curSpeed);
    }

    // 연료 목표 업데이트
    private void UpdateFuelTarget(float curFuel, float maxFuel)
    {
        if (maxFuel > 0)
            _targetFuelRatio = curFuel / maxFuel;
        else
            _targetFuelRatio = 0f;
    }

    // 속도 텍스트 업데이트
    private void UpdateSpeedText(float curSpeed)
    {
        if (_speedText == null) return;

        // 소수점 버린 정수값
        int intSpeed = Mathf.RoundToInt(curSpeed);

        // 숫자 바뀔 때만 텍스트 갱신
        if (_lastChangedSpeed != intSpeed)
        {
            _lastChangedSpeed = intSpeed;

            // SetText는 내부적으로 미리 할당된 char[] 배열을 덮어쓰기라고 함
            _speedText.SetText($"{curSpeed:0} km/h");
        }
    }

    private void OnDestroy()
    {
        // 파괴될 때 구독 해제 (중요)
        if (_targetEngine != null)
        {
            _targetEngine.OnEngineStatChanged -= UpdateDashboard;
        }
    }
}
