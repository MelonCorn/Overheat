using Photon.Pun;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviourPun, IPunObservable
{
    public static GameManager Instance;

    [Header("상점 체크")]
    [SerializeField] bool _isShop;

    [Header("텍스트")]
    [SerializeField] TextMeshProUGUI _goldText;            // 골드
    [SerializeField] TextMeshProUGUI _SurviveDayText;      // 생존일

    public bool IsShop => _isShop;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 씬 변경 시 새로운 GameManager

        if (PhotonNetwork.IsMasterClient == true)
        {
            // 상점이면
            if (IsShop == true)
                // 생존일 추가
                GameData.SurviveDay++;
        }

        // 텍스트 갱신
        UpdateGoldText();
        UpdateDayText();
    }

    // 골드 추가
    public void AddGold(int amount)
    {
        // 방장만
        if (PhotonNetwork.IsMasterClient == false) return;

        // 데이터에 추가
        GameData.Gold += amount;

        // UI 갱신
        UpdateGoldText();
    }

    // 골드 사용 시도
    public bool TryUseGold(int amount)
    {
        if (PhotonNetwork.IsMasterClient == false) return false;

        // 보유 골드가 요구 골드 이상이면
        if (GameData.Gold >= amount)
        {
            // 골드 사용
            GameData.Gold -= amount;

            // UI 갱신
            UpdateGoldText();

            return true;
        }

        return false;
    }



    // 골드 텍스트 갱신
    private void UpdateGoldText()
    {
        _goldText.SetText($"{GameData.Gold:N0}");
    }

    // 생존일 텍스트 갱신
    private void UpdateDayText()
    {
        _SurviveDayText.SetText($"{GameData.SurviveDay} 일차");
    }


    // 로컬 플레이어 사망
    public void LocalPlayerDie()
    {
        GameData.LocalDead = true;
    }


    // 아이템 데이터 검색
    public ShopItem FindItemData(string itemName)
    {
        if (ItemManager.Instance.ItemDict.ContainsKey(itemName))
            return ItemManager.Instance.ItemDict[itemName];

        if (TrainManager.Instance != null)
        {
            foreach (var train in TrainManager.Instance.TrainDict.Values)
            {
                if (train.itemName == itemName) return train;
            }
        }
        return null;
    }


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(GameData.Gold);
            stream.SendNext(GameData.SurviveDay);
        }
        else
        {
            int receiveGold = (int)stream.ReceiveNext();
            int receiveDay = (int)stream.ReceiveNext();

            // 골드
            if (GameData.Gold != receiveGold)
            {
                GameData.Gold = receiveGold;

                UpdateGoldText();
            }

            // 생존일
            if (GameData.SurviveDay != receiveDay)
            {
                GameData.SurviveDay = receiveDay;

                UpdateDayText();
            }
        }
    }

}
