using Photon.Pun;
using UnityEngine;

public class Boiler : MonoBehaviour, IInteractable
{

    [Header("보일러 오디오 데이터")]
    [SerializeField] ObjectAudioData _boilerAudioData;

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


    private bool _isStart;  // 대기실용 중복 시작 방지


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
                // 아이템이 연료 데이터라면
                if (item is FuelData fuelData)
                {
                    data = item;
                    return true;
                }
            }
        }
        return false;
    }


    public string GetInteractText(out bool canInteract)
    {
        // 기본적으로 상호작용 가능
        canInteract = true;

        // 상점 (아무나, 맨손)
        if (GameManager.Instance.IsShop == true)
        {
            return "다음 지역으로 출발";
        }

        // 대기실 (맨손)
        if (_engineNode == null)
        {
            // 시작 안했을 때만
            if (_isStart == false)
            {
                // 방장
                if (PhotonNetwork.IsMasterClient == true)
                {
                    return "열차 가동 !";
                }
                // 참가자
                else
                {
                    // 상호작용 불가능
                    canInteract = false;
                    return "출발 대기 중 ...";
                }
            }
            else
            {
                canInteract = false;
                return "";
            }
        }

        // 퀵슬롯 확인
        if (QuickSlotManager.Instance != null)
        {
            // 손에 석탄이 들려있을 때만
            if (IsCoalInHand(out ShopItem data))
            {
                return $"{((FuelData)data).displayName} 태우기\n연료 : {(int)_currentFuel} / {(int)_maxFuel}";
            }
        }

        // 손에 석탄 없을 때 상태 표시
        // 상호작용 불가능
        canInteract = false;
        return $"연료 : {(int)_currentFuel} / {(int)_maxFuel}";
    }

    public AudioClip OnInteract()
    {
        // 랜덤 클립 미리 가져오기
        AudioClip clip = _boilerAudioData.GetRandomClip();

        // 상점 (아무나, 맨손)
        if (GameManager.Instance.IsShop == true)
        {
            // 씬 전환 요청
            GameManager.Instance.RequestChangeScene();
            return clip;
        }

        // 대기실 (방장만, 맨손)
        if (_engineNode == null)
        {
            if (PhotonNetwork.IsMasterClient == true)
            {
                // 룸 프로퍼티 초기화
                GameManager.Instance.ResetRoomProperties();

                // 시작 안했을 때만
                if (_isStart == false)
                {
                    Debug.Log("대기실에서 게임 시작 요청");
                    _isStart = true;
                    GameManager.Instance.RequestChangeScene();
                }
            }
            return clip;
        }

        // null 방어
        if (QuickSlotManager.Instance == null) return null;

        string handItem = QuickSlotManager.Instance.CurrentSlotItemName;
        int slotIndex = QuickSlotManager.Instance.CurrentSlotIndex;

        // 석탄인지 확인
        if (IsCoalInHand(out ShopItem data))
        {
            if (data is FuelData fuelData)
            {
                // 인게임 로직: 연료 채우기 + 아이템 소모
                float amount = fuelData._fuelAddAmount;
                if (_engineNode != null)
                {
                    _engineNode.AddFuelRequest(amount);
                }

                QuickSlotManager.Instance.RemoveItem(slotIndex, handItem);
            }

            return clip;
        }
        // 석탄 아니면 소리 없음
        else
        {
            return null;
        }
    }
}
