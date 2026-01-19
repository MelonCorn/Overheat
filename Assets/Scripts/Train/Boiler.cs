using UnityEngine;

public class Boiler : MonoBehaviour, IInteractable
{
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

    // 연료 연소 (방장이 태움)
    public void BurnFuel()
    {
        if (_currentFuel > 0)
        {
            _currentFuel -= _burnRate * Time.deltaTime;
            if (_currentFuel < 0) _currentFuel = 0;
        }
    }

    // 연료 추가 (방장이 뿌린 RPC)
    public void AddFuel(float amount)
    {
        _currentFuel += amount;
        if (_currentFuel > _maxFuel) _currentFuel = _maxFuel;
    }

    // 강제 설정 (동기화용)
    public void SetFuel(float amount)
    {
        _currentFuel = amount;
    }



    public string GetInteractText()
    {
        // 손 아이템 확인
        string handItem = QuickSlotManager.Instance.CurrentSlotItemName;

        // 손에 아이템 있을 때
        if (string.IsNullOrEmpty(handItem) == false)
        {
            // 아이템 데이터 찾아오기
            if (ItemManager.Instance.ItemDict.TryGetValue(handItem, out ShopItem data))
            {
                // 연료 타입 아이템인지 확인
                if (data is FuelData fuelData && fuelData.itemType == ItemType.Fuel)
                {
                    return $"{fuelData.displayName} 태우기\n연료 : {(int)_currentFuel} / {(int)_maxFuel}";
                }
            }
        }

        // 평소엔 상태 표시
        return $"연료 : {(int)_currentFuel} / {(int)_maxFuel}";
    }

    public void OnInteract()
    {
        // 상점이면 일단 임시로 중단 나중에 레디로 변경
        if (GameManager.Instance.IsShop) return;


        // 손 아이템
        string handItem = QuickSlotManager.Instance.CurrentSlotItemName;
        // 퀵슬롯 번호
        int slotIndex = QuickSlotManager.Instance.CurrentSlotIndex;

        // 손에 든 아이템 데이터 찾아와서
        if (ItemManager.Instance.ItemDict.TryGetValue(handItem, out ShopItem data))
        {
            // 연료 타입이면
            if (data is FuelData fuelData && fuelData.itemType == ItemType.Fuel)
            {
                float amonut = fuelData._fuelAddAmount;

                // 엔진에 연료 충전 요청
                if (_engineNode != null)
                {
                    _engineNode.AddFuelRequest(amonut);
                }

                // 아이템 소모
                QuickSlotManager.Instance.RemoveItem(slotIndex, handItem);
            }
        }
    }
}
