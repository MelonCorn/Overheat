using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(NetworkPool))]
public class GameManager : MonoBehaviourPun, IPunObservable
{
    public static GameManager Instance;
    
    public List<PlayerHandler> ActivePlayers = new List<PlayerHandler>();

    [Header("상점 체크")]
    [SerializeField] bool _isShop;

    [Header("유실물 생성 포인트")]
    [SerializeField] Transform _lostItemSpawnPoint;

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

        // 유실물 복구 시작
        if (PhotonNetwork.IsMasterClient == true)
        {
            StartCoroutine(SpawnLostItems());
        }
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

    // 씬 이동 (방장만)
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
        // 룸 닫혀잇으면 게임중이니까
        else
        {
            // 유실물 저장
            SaveLostItems();
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



    // 바닥 유실물 저장
    private void SaveLostItems()
    {
        // 일단 싹 비우고
        GameData.LostItems.Clear();

        // 씬에 있는 모든 NetworkItem 찾기
        NetworkItem[] floorItems = FindObjectsByType<NetworkItem>(FindObjectsSortMode.None);

        // 아이템마다 체크
        foreach (var item in floorItems)
        {
            // 활성화 상태고
            // 이름 유효하고
            // PhotonView 켜져 있는 아이템만
            if (item.gameObject.activeInHierarchy &&
                string.IsNullOrEmpty(item.ItemName) == false &&
                item.photonView.enabled)
            {
                GameData.LostItems.Add(item.ItemName);
            }
        }

        Debug.Log($"[유실물 저장] 바닥에서 {GameData.LostItems.Count}개의 유실물 챙김");
    }

    // 유실물 생성 코루틴
    private IEnumerator SpawnLostItems()
    {
        // 유실물 없으면 패스
        if (GameData.LostItems.Count == 0) yield break;

        // 인게임이라면 기차가 준비될 때까지 대기
        if (_isShop == false)
        {
            // TrainManager가 있고 준비가 끝날 때까지 대기
            yield return new WaitUntil(() => TrainManager.Instance != null && TrainManager.Instance.IsTrainReady);
        }

        Debug.Log($"[유실물 복구] {GameData.LostItems.Count}개의 아이템을 복구합니다.");

        // 생성 위치 결정
        Vector3 spawnCenterPos = Vector3.zero;
        spawnCenterPos = _lostItemSpawnPoint != null ? _lostItemSpawnPoint.position : Vector3.zero;

        // 아이템 생성
        foreach (string itemName in GameData.LostItems)
        {
            // 겹치지 않게 약간의 랜덤
            Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));

            object[] initData = new object[] { itemName };

            // 네트워크 객체로 생성 (방장 소유)
            PhotonNetwork.Instantiate(itemName, spawnCenterPos + randomOffset, Quaternion.identity, 0 , initData);
        }

        // 복구 완료 후 비우기
        GameData.LostItems.Clear();
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
