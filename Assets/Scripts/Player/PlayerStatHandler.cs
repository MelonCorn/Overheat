using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerStatHandler : MonoBehaviour
{
    [Header("체력 설정")]
    [SerializeField] int _maxHp = 100;

    [Header("기력 설정")]
    [SerializeField] float _maxStamina = 100f;
    [SerializeField] float _staminaRegen = 15f; // 초당 회복량

    [Header("UI")]
    [SerializeField] Slider _hpSlider;      // 체력 바
    [SerializeField] Slider _staminaSlider; // 기력 바


    // 내부 변수
    private int _currentHp;
    private float _currentStamina;
    private float _lastStaminaUseTime; // 회복 딜레이용

    private void Start()
    {
        _currentHp = _maxHp;
        _currentStamina = _maxStamina;
        // UI 한 번 갱신
        UpdateUI();
    }
    private void Update()
    {
        // 기력 회복
        RegenStamina();

        // UI 갱신
        UpdateUI();

        // (테스트용) K키 자해
        if (Keyboard.current.kKey.wasPressedThisFrame) TakeDamage(10);
    }

    #region 체력
    public void TakeDamage(int damage)
    {
        if (_currentHp <= 0) return;

        _currentHp -= damage;
        Debug.Log($"체력 감소: {_currentHp}/{_maxHp}");

        // UIManager.Instance.UpdateHp(_currentHp, _maxHp);

        if (_currentHp <= 0)
        {
            _currentHp = 0;
            Die();
        }
    }

    // 체력 회복
    public void Heal(int amount)
    {
        if (_currentHp <= 0) return;

        _currentHp += amount;

        if (_currentHp >= _maxHp)
            _currentHp = _maxHp;

        Debug.Log($"체력 회복: {_currentHp}");
    }

    // 사망
    private void Die()
    {
        Debug.Log("으앙 죽음");

        // 게임 데이터에 일단 로컬 플레이어 사망 기록 (스테이지 클리어 후 상점에서 체력 낮은 상태로 부활)
        GameManager.Instance.LocalPlayerDie();
    }
    #endregion

    #region 기력

    // 즉시 소모 (점프)
    public bool TryUseStamina(float amount)
    {
        // 기력이 충분할 때
        if (_currentStamina >= amount)
        {
            // 기력 감소
            _currentStamina -= amount;
            // 마지막 기력 사용 시간 기록
            _lastStaminaUseTime = Time.time;
            return true;
        }
        return false;
    }

    // 지속 소모 (달리기)
    public bool UseStaminaContinuous(float amount)
    {
        // 기력 있을 때
        if (_currentStamina > 0)
        {
            // 기력 감소
            _currentStamina -= amount * Time.deltaTime;
            if (_currentStamina < 0) _currentStamina = 0;

            // 마지막 기력 사용 시간 기록
            _lastStaminaUseTime = Time.time;
            return true;
        }
        return false;
    }

    // 기력 회복
    private void RegenStamina()
    {
        // 기력 사용시간이 2초 지나면
        if (Time.time - _lastStaminaUseTime > 2.0f && _currentStamina < _maxStamina)
        {
            // 기력 증가
            _currentStamina += _staminaRegen * Time.deltaTime;
            if (_currentStamina > _maxStamina) _currentStamina = _maxStamina;
        }
    }
    #endregion

    private void UpdateUI()
    {
        // 체력
        if (_hpSlider != null)
        {
            // 비율 계산
            float hpRatio = (float)_currentHp / _maxHp;

            // 반영
            _hpSlider.value = Mathf.Lerp(0f, 1f, hpRatio);
        }

        // 기력
        if (_staminaSlider != null)
        {
            // 비율 계산
            float staminaRatio = _currentStamina / _maxStamina;

            // 반영
            _staminaSlider.value = Mathf.Lerp(0f, 1f, staminaRatio);
        }
    }
}
