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
        // 상점 && 로컬 사망 상태 시
        if (GameManager.Instance.IsShop && GameData.LocalDead)
        {
            // 체력 10%로
            GameData.LocalCurrentHp = (int)(_maxHp * 0.1f);

            // 부활했으니 사망 상태 Off
            GameManager.Instance.LocalPlayerDead(false);

            Debug.Log("부상 상태로 부활했습니다. (체력 10%)");
        }

        // 플레이어 생성 시 저장된 로컬 체력 데이터로 생성
        _currentHp = GameData.LocalCurrentHp;
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
        // 체력 없으면 무시
        if (_currentHp <= 0) return;

        // 대기실, 상점에선 무시
        if (GameManager.Instance.IsWaitingRoom == true) return;
        if (GameManager.Instance.IsShop == true) return;

        // 혹시 몰라서 로컬 사망상태 한 번 더 체크
        if (GameData.LocalDead == true) return;

        // 게임오버 상태면 무적 또 체크
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        _currentHp -= damage;
        Debug.Log($"체력 감소: {_currentHp}/{_maxHp}");

        if (_currentHp <= 0)
        {
            _currentHp = 0;
            Die();
        }

        // 로컬 체력 데이터 저장
        GameData.LocalCurrentHp = _currentHp;
    }

    // 체력 회복
    public void Heal(int amount)
    {
        if (_currentHp <= 0) return;

        _currentHp += amount;

        if (_currentHp >= _maxHp)
            _currentHp = _maxHp;

        Debug.Log($"체력 회복: {_currentHp}");

        // 로컬 체력 데이터 저장
        GameData.LocalCurrentHp = _currentHp;
    }

    // 사망
    private void Die()
    {
        PlayerHandler.localPlayer.Die();
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
