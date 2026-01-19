using UnityEngine;

public class Boiler : MonoBehaviour, IInteractable
{
    [Header("연료 설정")]
    [SerializeField] float _fuelAddAmount = 15f; // 연료 충전량

    // 연결된 엔진
    private EngineNode _engineNode;

    // 내부 스탯
    private float _maxFuel;     // 최대 연료
    private float _burnRate;    // 초당 연료 소모

    // 실시간 변수
    private float _currentFuel; // 현재 연료량
    public float CurrentFuel => _currentFuel;
    public float MaxFuel => _maxFuel;
    public float FuelRatio => _maxFuel > 0 ? _currentFuel / _maxFuel : 0f; // 연료 비율


    // 초기화
    public void Init(EngineNode engine, float maxFuel, float burnRate)
    {
        _engineNode = engine;
        _maxFuel = maxFuel;
        _burnRate = burnRate;
    }

    public string GetInteractText()
    {
        throw new System.NotImplementedException();
    }

    public void OnInteract()
    {
        throw new System.NotImplementedException();
    }
}
