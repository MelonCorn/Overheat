using Photon.Pun;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(NetworkPool))]
public class GameManager : MonoBehaviourPun, IPunObservable
{
    public static GameManager Instance;

    [Header("상점 체크")]
    [SerializeField] bool _isShop;

    [Header("텍스트")]
    [SerializeField] TextMeshProUGUI _goldText;            // 골드
    [SerializeField] TextMeshProUGUI _SurviveDayText;      // 생존일

    [Header("이동할 씬")]
    [SerializeField] string _loadSceneName = "GameScene";

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

        // 네트워크 풀 사용
        NetworkPool customPool = GetComponent<NetworkPool>();

        PhotonNetwork.PrefabPool = customPool;
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

    #region 씬 변경
    // 열차 출발 요청
    public void RequestChangeScene()
    {
        // 방장이면 바로 출발
        if (PhotonNetwork.IsMasterClient)
        {
            ChangeScene();
        }
        // 방장이 아니면 출발 요청
        else
        {
            photonView.RPC(nameof(RPC_RequestChangeScene), RpcTarget.MasterClient);
        }
    }

    // 출발 요청
    [PunRPC]
    private void RPC_RequestChangeScene()
    {
        if (PhotonNetwork.IsMasterClient == false) return;

        ChangeScene();
    }

    // 씬 이동
    private void ChangeScene()
    {
        Debug.Log("씬 이동");

        // 룸이 열려있으면 닫기
        if (PhotonNetwork.CurrentRoom.IsOpen)
        {
            Debug.Log("방을 잠궜습니다.");
            PhotonNetwork.CurrentRoom.IsOpen = false;       // 참가 불가
            PhotonNetwork.CurrentRoom.IsVisible = false;    // 목록에서도 안보이게
        }

        // 모두 다 같이 이동
        PhotonNetwork.LoadLevel(_loadSceneName);
    }
    #endregion


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
        _goldText?.SetText($"{GameData.Gold:N0}");
    }

    // 생존일 텍스트 갱신
    private void UpdateDayText()
    {
        _SurviveDayText?.SetText($"{GameData.SurviveDay} 일차");
    }


    // 로컬 플레이어 사망
    public void LocalPlayerDead(bool active)
    {
        GameData.LocalDead = active;
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
