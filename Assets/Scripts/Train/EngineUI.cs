using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EngineUI : MonoBehaviour
{
    [Header("엔진 계기판 UI")]
    [SerializeField] private TextMeshProUGUI _speedText; // 속도 텍스트
    [SerializeField] private Slider _fuelSlider;         // 연료 게이지
    //[SerializeField] private Image _fuelGaugeImage;   // 연료 바늘?

    private int _lastChangedSpeed = -1;

    private EngineNode _targetEngine;

    // 엔진 연결
    public void ConnectEngine(EngineNode engine)
    {
        _targetEngine = engine;

        // UI 갱신 이벤트 구독
        _targetEngine.OnEngineStatChanged += UpdateDashboard;

        // 초기값 한 번 세팅 (현재 상태 바로 보여주기 위해)
        UpdateDashboard(engine.CurrentSpeed, engine.CurrentFuel, engine.MaxFuel);

        // (선택) 엔진 UI 켜기
        gameObject.SetActive(true);
    }

    // 엔진에서 신호가 오면 UI 갱신
    private void UpdateDashboard(float curSpeed, float curFuel, float maxFuel)
    {
        // 연료 슬라이더
        if (_fuelSlider != null)
        {
            _fuelSlider.value = (maxFuel > 0) ? (curFuel / maxFuel) : 0;
        }

        // 속도 텍스트
        if (_speedText != null)
        {
            // 소수점을 버린 정수값
            int intSpeed = Mathf.RoundToInt(curSpeed);

            // 숫자 바뀔 때만 텍스트 갱신
            if (_lastChangedSpeed != intSpeed)
            {
                _lastChangedSpeed = intSpeed;

                // SetText는 내부적으로 미리 할당된 char[] 배열을 덮어쓰기라고 함
                _speedText.SetText($"{curSpeed:0} km/h");
            }
        }

        //// 연료용량 바늘 느낌
        //if (_fuelGaugeImage != null)
        //{
        //    _fuelGaugeImage.fillAmount = (maxFuel > 0) ? (curFuel / maxFuel) : 0;
        //}
    }

    public void ClickAddFuel()
    {
        _targetEngine.AddFuel(10);
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
