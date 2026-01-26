using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(NetworkPool))]
public class GameManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public static GameManager Instance;
    
    public List<PlayerHandler> ActivePlayers = new List<PlayerHandler>();

    [Header("씬 체크")]
    [SerializeField] bool _isShop;
    [SerializeField] bool _isWaitingRoom;

    [Header("대기 패널 (인게임/상점)")]
    [SerializeField] GameObject _waitingPanel;

    [Header("관전 카메라 (인게임)")]
    [SerializeField] GameObject _spectatorCamera;

    [Header("유실물 생성 포인트 (상점)")]
    [SerializeField] Transform _lostItemSpawnPoint;

    [Header("텍스트")]
    [SerializeField] TextMeshProUGUI _goldText;            // 골드
    [SerializeField] TextMeshProUGUI _SurviveDayText;      // 생존일

    [Header("이동할 씬")]
    [SerializeField] string _loadSceneName = "Game";



    private bool _isLocalLoad = false;      // 로컬 로딩 체크용
    private bool _isGameStart = false;      // 게임 시작 확인용
    public bool IsShop => _isShop;
    public bool IsWaitingRoom => _isWaitingRoom;
    public bool IsGameOver { get; private set; }

    private PlayerSpawner _spawner;         // 플레이어 스포너

    private HashSet<int> _loadedActorNumbers = new HashSet<int>();  // 준비 완료된 플레이어

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

    private IEnumerator Start()
    {
        yield return null;

        // 네트워크 메세지 허용
        PhotonNetwork.IsMessageQueueRunning = true;

        // 일단 스포너 찾기
        _spawner = FindAnyObjectByType<PlayerSpawner>();

        // 대기실일 경우
        if (_isWaitingRoom)
        {
            // 로딩 매니저에게 페이드인 요청
            if (LoadingManager.Instance != null)
                LoadingManager.Instance.RequestFadeIn();

            // 바로 플레이어 생성
            if (_spawner != null) _spawner.PlayerSpawn();

            // 대기실은 여기까지
            yield break;
        }

        // 인게임/상점은 대기 패널 온
        if (_waitingPanel != null) _waitingPanel.SetActive(true);

        // 방장에게 로딩 다끝났다고 알림
        photonView.RPC(nameof(RPC_SceneLoaded), RpcTarget.MasterClient);

        // 로컬 로딩 체크
        _isLocalLoad = true;

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


    // 씬 로딩 완료 (방장이 수신)
    [PunRPC]
    private void RPC_SceneLoaded(PhotonMessageInfo info)
    {
        // 방장만
        if (PhotonNetwork.IsMasterClient == false) return;

        // 준비완료 플레이어 등록
        _loadedActorNumbers.Add(info.Sender.ActorNumber);

        // 모든 플레이어 준비 상태 확인
        CheckAllPlayersReady();
    }

    // 모든 플레이어 준비 상태 확인
    private void CheckAllPlayersReady()
    {
        // 게임 시작했으면 체크 패스
        if (_isGameStart == true) return;

        Debug.Log($"[로딩 체크] {_loadedActorNumbers.Count} / {PhotonNetwork.CurrentRoom.PlayerCount} 명 완료");

        // 현재 방 인원수만큼 로딩이 완료되면
        if (_loadedActorNumbers.Count >= PhotonNetwork.CurrentRoom.PlayerCount)
        {
            // 씬 준비 완료 알림
            photonView.RPC(nameof(RPC_AllPlayersReady), RpcTarget.All);
        }
    }
    
    // 씬 준비완료 모든 사람들에게 뿌림
    [PunRPC]
    private void RPC_AllPlayersReady()
    {
        // 중복 콜 무시
        if (_isGameStart == true) return;

        // 게임 시작 상태로 변경
        _isGameStart = true;

        // 대기 UI 끄기
        if (_waitingPanel != null) _waitingPanel.SetActive(false);

        // 씬 시작
        StartCoroutine(SceneStart());
    }


    // 씬 시작
    // 열차 생성 대기 -> 페이드 인 -> 타임라인 재생 -> 페이드 아웃 -> 플레이어 스폰 -> 페이드 인 -> 게임 시작
    private IEnumerator SceneStart()
    {
        // 열차 생성 대기
        if (TrainManager.Instance != null)
        {
            yield return new WaitUntil(() => TrainManager.Instance.IsTrainReady);
        }

        // 페이드 인
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.RequestFadeIn();
        }

        // 타임라인 재생

        // 페이드 아웃
        //if (LoadingManager.Instance != null)
        //{
        //    LoadingManager.Instance.RequestFadeOut();
        //}

        // 플레이어 스폰
        if (_spawner != null) _spawner.PlayerSpawn();

        // 페이드 인
        //if (LoadingManager.Instance != null)
        //{
        //    LoadingManager.Instance.RequestFadeIn();
        //}

        // 유실물 생성 (방장만)
        if (PhotonNetwork.IsMasterClient == true) StartCoroutine(SpawnLostItems());
    }



    // 씬 전환 요청
    public void RequestChangeScene()
    {
        Debug.Log("씬 전환 요청");
        if (PhotonNetwork.IsMasterClient == true)
            RPC_StartChangeScene();
        else
            photonView.RPC(nameof(RPC_StartChangeScene), RpcTarget.MasterClient);
    }

    // 출발 요청
    [PunRPC]
    private void RPC_StartChangeScene()
    {
        if (PhotonNetwork.IsMasterClient == false) return;

        Debug.Log("씬 전환 코루틴 뿌리기");
        // 씬 전환 코루틴 뿌리기
        photonView.RPC(nameof(RPC_ChangeScene), RpcTarget.All);
    }


    [PunRPC] // 씬 전환 코루틴 뿌리기 (모두)
    private void RPC_ChangeScene()
    {
        Debug.Log("씬 전환 코루틴 실행 (모두)");
        StartCoroutine(ChangeSceneCoroutine());
    }

    // 씬 전환 코루틴 (모두)
    private IEnumerator ChangeSceneCoroutine()
    {
        Debug.Log("씬 전환 코루틴 시작 (모두)");
        // 페이드 아웃
        if (LoadingManager.Instance != null)
        {
            Debug.Log("씬 전환 페이드 아웃 시작(모두)");
            LoadingManager.Instance.RequestFadeOut();
            // 페이드 시간 대기
            yield return new WaitForSeconds(LoadingManager.Instance.FadeDuration);

            Debug.Log("씬 전환 페이드 아웃 끝(모두)");
        }

        // 모든 플레이어 비활성화
        // 플레이어 리스트 복사해서 사용
        // Disable될 때 빠지기 때문에 터짐
        foreach (var player in ActivePlayers.ToArray())
        {
            if (player != null)
            {
                // 파괴는 네트워크 에러 확률 존재
                player.gameObject.SetActive(false);
            }
        }

        // 방장만 씬 전환 명령
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("씬 전환 (방장)");
            ChangeScene();
        }
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

        Debug.Log("비동기로딩 뿌리기 (방장)");
        // 이제 비동기로딩하면서 씬 전환
        photonView.RPC(nameof(RPC_LoadSceneAsync), RpcTarget.All, _loadSceneName);
    }

    // 모두가 받을 비동기 씬 전환
    [PunRPC]
    private void RPC_LoadSceneAsync(string sceneName)
    {
        Debug.Log("비동기로딩 시작 (모두)");
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.RequestLoadScene(sceneName);
        }
    }
    #endregion


    #region 골드, 생존일

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

    #endregion


    #region 유실물

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

    #endregion


    #region 게임오버 관련

    // 로컬 플레이어 사망
    public void LocalPlayerDead(bool active)
    {
        Debug.Log($"로컬 사망 상태 전환 {active}");
        GameData.LocalDead = active;
    }

    public void SpectatorMode()
    {
        if (_spectatorCamera != null) _spectatorCamera.SetActive(true);
    }


    // 전멸 체크
    public void CheckAllPlayersDead()
    {
        // 플레이어 1명 이상일 때 체크
        if (ActivePlayers.Count <= 0) return;

        // 활성화된 플레이어 순회하면서 생존자 체크
        // 한 명이라도 생존 시 무시
        foreach (var player in ActivePlayers)
            if (player.IsDead == false) return;

        // 전부 다 사망해서 통과하면
        GameOver(); // 게임오버
    }

    // 게임오버 발동 (외부용)
    public void GameOver()
    {
        // 방장만
        if (PhotonNetwork.IsMasterClient == false) return;

        Debug.Log("게임 오버! 대기실로 이동합니다.");

        // 모두에게 게임오버 알림
        photonView.RPC(nameof(RPC_GameOver), RpcTarget.All);
    }


    // 게임오버 알림
    [PunRPC]
    private void RPC_GameOver()
    {
        // 게임오버
        IsGameOver = true;


        // 나중에 코루틴으로 만들어서
        // 게임오버 상태 먼저 하고나서 타임라인 재생
        // 재생 끝나고 나서 룸프로퍼티, 데이터 초기화 등 과정 실행

        // 게임 데이터 리셋
        GameData.Reset();

        // 파괴되지 않는 싱글톤 정리
        if (QuickSlotManager.Instance != null) Destroy(QuickSlotManager.Instance.gameObject);
        if(ItemManager.Instance != null) Destroy(ItemManager.Instance.gameObject);

        // 씬 이동
        if (PhotonNetwork.IsMasterClient)
        {
            // 대기실로 비동기 씬 로드
            photonView.RPC(nameof(RPC_LoadSceneAsync), RpcTarget.All, "Room");
        }


    }

    // 룸 프로퍼티 초기화
    public void ResetRoomProperties()
    {
        ExitGames.Client.Photon.Hashtable resetProps = new ExitGames.Client.Photon.Hashtable
        {
            { TrainManager.KEY_TRAIN_TYPES, null },
            { TrainManager.KEY_TRAIN_LEVELS, null },
            { TrainManager.KEY_TRAIN_CONTENTS, null }
        };

        PhotonNetwork.CurrentRoom.SetCustomProperties(resetProps);
    }
    #endregion


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


    #region Pun 콜백

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


    // 플레이어가 방에서 나갔을 때
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // 방장만
        if (PhotonNetwork.IsMasterClient == false) return;

        // 나간 사람이 로딩 완료 명단에 있었다면
        if (_loadedActorNumbers.Contains(otherPlayer.ActorNumber))
        {
            //  제거
            _loadedActorNumbers.Remove(otherPlayer.ActorNumber);
        }

        // 나간 사람 때문에 인원수가 줄어서 다시 체크
        CheckAllPlayersReady();
    }

    // 방장이 바꼈을 때
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        // 로컬 로딩이 끝났고, 게임 시작 안했을 때
        if (_isLocalLoad == true && _isGameStart == false)
        {
            // 새로운 방장에게 준비 됐다고 다시 보내기
            photonView.RPC(nameof(RPC_SceneLoaded), RpcTarget.MasterClient);
        }
    }
    #endregion
}
