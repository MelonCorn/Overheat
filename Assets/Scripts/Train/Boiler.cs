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
        // 손 아이템 확인
        string handItem = QuickSlotManager.Instance.CurrentSlotItemName;

        // 아이템 데이터 찾아오기
        if (ItemManager.Instance.ItemDict.TryGetValue(handItem, out ShopItem data))
        {
            // 연료 타입 아이템인지 확인
            if (data is PlayerItemData itemData && itemData.itemType == ItemType.Fuel)
            {
                return $"{itemData.displayName} 태우기";
            }
        }

        // 평소엔 상태 표시
        return $"연료 : {(int)_currentFuel} / {(int)_maxFuel}";
    }

    public void OnInteract()
    {
        // 손 아이템
        string handItem = QuickSlotManager.Instance.CurrentSlotItemName;
        // 퀵슬롯 번호
        int slotIndex = QuickSlotManager.Instance.CurrentSlotIndex;

        // 손에 든 아이템 데이터 찾아와서
        if (ItemManager.Instance.ItemDict.TryGetValue(handItem, out ShopItem data))
        {
            // 연료 타입이면
            if (data is PlayerItemData itemData && itemData.itemType == ItemType.Fuel)
            {
                // 엔진에 연료 충전 요청
                if (_engineNode != null)
                {
                    _engineNode.AddFuelRequest(_fuelAddAmount);
                }

                // 아이템 소모
                QuickSlotManager.Instance.RemoveItem(slotIndex, handItem);
            }
        }
    }
}
