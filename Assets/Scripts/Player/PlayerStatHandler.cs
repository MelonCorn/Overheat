using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStatHandler : MonoBehaviour
{
    [Header("체력 설정")]
    [SerializeField] int _maxHp = 100;

    [Header("기력 설정")]
    [SerializeField] float _maxStamina = 100f;
    [SerializeField] float _staminaRegen = 15f; // 초당 회복량

    // 내부 변수
    private int _currentHp;
    private float _currentStamina;
    private float _lastStaminaUseTime; // 회복 딜레이용

    private void Start()
    {
        _currentHp = _maxHp;
        _currentStamina = _maxStamina;
    }
    private void Update()
    {
        // 기력 회복
        RegenStamina();

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

        _currentHp = Mathf.Min(_currentHp + amount, _maxHp);

        Debug.Log($"체력 회복: {_currentHp}");
    }

    // 사망
    private void Die()
    {
        Debug.Log("으앙 죽음");

        GameManager.Instance.LocalPlayerDie();
    }
    #endregion

    #region 기력

    // 즉시 소모 (점프)
    public bool TryUseStamina(float amount)
    {
        if (_currentStamina >= amount)
        {
            _currentStamina -= amount;
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
            _currentStamina -= amount * Time.deltaTime;
            if (_currentStamina < 0) _currentStamina = 0;

            _lastStaminaUseTime = Time.time;
            return true;
        }
        return false;
    }

    // 기력 회복
    private void RegenStamina()
    {
        if (Time.time - _lastStaminaUseTime > 2.0f && _currentStamina < _maxStamina)
        {
            _currentStamina += _staminaRegen * Time.deltaTime;
            if (_currentStamina > _maxStamina) _currentStamina = _maxStamina;
        }
    }
    #endregion
}
