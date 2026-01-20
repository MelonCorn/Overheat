using Photon.Pun;
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

    // 손에 석탄 들고있는지 확인
    private bool IsCoalInHand(out ShopItem data)
    {
        // out이라 일단
        data = null;

        // 손 아이템 확인
        string handItem = QuickSlotManager.Instance.CurrentSlotItemName;

        // 손에 아이템 있을 때
        if (string.IsNullOrEmpty(handItem) == false)
        {
            // 아이템 데이터 찾아오기
            if (ItemManager.Instance.ItemDict.TryGetValue(handItem, out ShopItem item))
            {
                // 아이템이 연료 데이터고, 연료 타입이 맞다면
                if (item is FuelData fuelData && fuelData.itemType == ItemType.Fuel)
                {
                    data = item;
                    return true;
                }
            }
        }
        return false;
    }


    public string GetInteractText()
    {
        // 아이템이 석탄일 때 텍스트 분기
        if (IsCoalInHand(out ShopItem data))
        {
            // 상점이나 대기실이면
            // EngineNode가 없거나 상점이면 출발
            if (GameManager.Instance.IsShop || _engineNode == null)
            {
                // 대기실인데 방장 아니면
                if (GameManager.Instance.IsShop == false && PhotonNetwork.IsMasterClient == true)
                    return "방장만 조작할 수 있습니다.";

                // 상점이면 클라이언트도 가능
                // 방장이면 항상
                return "열차 출발 !";
            }

            // 인게임이면 상호작용 + 연료
            return $"{((FuelData)data).displayName} 태우기\n연료 : {(int)_currentFuel} / {(int)_maxFuel}";
        }

        // 석탄 없을 때 상태 표시
        return $"연료 : {(int)_currentFuel} / {(int)_maxFuel}";
    }

    public void OnInteract()
    {
        // 상점이면 일단 임시로 중단 나중에 레디로 변경
        if (GameManager.Instance.IsShop) return;

        // 손에 든 게 석탄인지 확인
        string handItem = QuickSlotManager.Instance.CurrentSlotItemName;
        int slotIndex = QuickSlotManager.Instance.CurrentSlotIndex;

        if (ItemManager.Instance.ItemDict.TryGetValue(handItem, out ShopItem data))
        {
            if (data is FuelData fuelData && fuelData.itemType == ItemType.Fuel)
            {
                // 인게임 (엔진 연결돼 있고 상점 아님)
                if (_engineNode != null && GameManager.Instance.IsShop == false)
                {
                    // 연료 채우고 속도 올림
                    float amount = fuelData._fuelAddAmount;
                    // 엔진에 요청
                    _engineNode.AddFuelRequest(amount);
                    // 아이템 삭제
                    QuickSlotManager.Instance.RemoveItem(slotIndex, handItem);
                }
                // 대기실이거나 상점
                else
                {
                    // 게임 시작 요청
                    TryStartGame(slotIndex, handItem);
                }
            }
        }
    }

    // 대기실, 상점
    // 게임 시작 시도
    private void TryStartGame(int slotIndex, string handItem)
    {
        // 대기실
        if (GameManager.Instance.IsShop == false)
        {
            // 방장만 가능
            if (PhotonNetwork.IsMasterClient == true)
            {
                // 아이템 소모
                QuickSlotManager.Instance.RemoveItem(slotIndex, handItem);
                // 게임 매니저에게 출발 요청
                GameManager.Instance.RequestChangeScene();
            }
            else
            {
                Debug.Log("대기실에서는 방장만 출발시킬 수 있습니다.");
            }
        }
        // 상점
        else
        {
            // 누구나 가능
            // 아이템 소모
            QuickSlotManager.Instance.RemoveItem(slotIndex, handItem);
            // 게임 매니저에 출발 요청 (RPC)
            GameManager.Instance.RequestChangeScene();
        }
    }
}
