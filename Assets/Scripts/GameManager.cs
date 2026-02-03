using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

[RequireComponent(typeof(NetworkPool))]
public class GameManager : MonoBehaviourPunCallbacks, IPunObservable
{
    public static GameManager Instance;
    
    public List<PlayerHandler> ActivePlayers = new List<PlayerHandler>();

    [Header("바닥 스타터 아이템 (소화기 등)")]
    [SerializeField] GameObject _starterItem;

    [Header("씬 체크")]
    [SerializeField] bool _isShop;
    [SerializeField] bool _isWaitingRoom;

    [Header("대기UI (대기실)")]
    [SerializeField] GameObject _waitingCanvas;

    [Header("대기 패널 (인게임/상점)")]
    [SerializeField] GameObject _waitingPanel;

    [Header("엔진 UI")]
    [SerializeField] GameObject _engineUI;

    [Header("관전 카메라 (인게임)")]
    [SerializeField] SpectatorCamera _spectatorCamera;

    [Header("적 스포너 (인게임)")]
    [SerializeField] EnemySpawner _enemySpawner;

    [Header("환경 스포너 (인게임)")]
    [SerializeField] EnvironmentSpawner _environmentSpawner;

    [Header("유실물 생성 포인트 (인게임/상점)")]
    [SerializeField] Transform _lostItemSpawnPoint;

    [Header("레일 오디오 소스")]
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _steamClip;

    [Header("텍스트")]
    [SerializeField] TextMeshProUGUI _goldText;            // 골드
    [SerializeField] TextMeshProUGUI _SurviveDayText;      // 생존일

    [Header("이동할 씬")]
    [SerializeField] string _loadSceneName = "Game";



    private bool _isLocalLoad = false;      // 로컬 로딩 체크용
    public bool IsShop => _isShop;
    public bool IsWaitingRoom => _isWaitingRoom;
    public bool IsGameStart { get; private set; }      // 게임 시작 확인용
    public bool IsGameOver { get; private set; }

    public event Action<int> OnGoldChanged; // 골드 변화 알림

    // 무기 파티클 재생 전용
    public readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();

    private PlayerSpawner _spawner;         // 플레이어 스포너
    private TimelineManager _timelineManager;// 타임라인 매니저

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

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
     
    private IEnumerator Start()
    {
        yield return null;

        // 네트워크 메세지 허용
        PhotonNetwork.IsMessageQueueRunning = true;

        // 일단 스포너 찾기
        _spawner = FindAnyObjectByType<PlayerSpawner>();
        // 타임라인 매니저도
        _timelineManager = FindAnyObjectByType<TimelineManager>();

        // 대기실일 경우
        if (_isWaitingRoom)
        {
            // 바로 플레이어 생성
            if (_spawner != null) _spawner.PlayerSpawn();

            yield return new WaitForSeconds(0.3f);

            // 로딩 매니저에게 페이드인 요청
            if (LoadingManager.Instance != null)
                LoadingManager.Instance.RequestFadeIn();

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
        UpdateGold();
        UpdateDay();

        // 유실물 복구 시작
        if (PhotonNetwork.IsMasterClient == true)
        {
            StartCoroutine(SpawnLostItems());

            // 방장이 지급 받지 못했다면
            if(GameData.HasMasterStarterItem == false)
            {
                // 지급 확인
                GameData.HasMasterStarterItem = true;

                // 스타터 아이템 생성
                SpawnStarterItem();
            }
        }
        // 방장 이전 시 못 받게
        else
        {
            GameData.HasMasterStarterItem = true;
        }
    }

    // 스타터 아이템 바닥에 떨구기
    private void SpawnStarterItem()
    {
        if (_starterItem == null) return;

        // 위치 결정 (지정된 위치 없으면 0,0,0)
        Vector3 spawnPos = _lostItemSpawnPoint != null ? _lostItemSpawnPoint.position : Vector3.zero;
        // 좀 퍼지게 랜덤
        Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f));

        // 아이템 이름 보내기
        object[] initData = new object[] { _starterItem.name };

        // 네트워크 생성
        PhotonNetwork.Instantiate(_starterItem.name, spawnPos + randomOffset, Quaternion.identity, 0, initData);

        Debug.Log($"[GameManager] 바닥 스타터 아이템 생성 : {_starterItem.name}");
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
        if (IsGameStart == true) return;

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
        if (IsGameStart == true) return;

        // 게임 시작 상태로 변경
        IsGameStart = true;

        // 씬 시작
        StartCoroutine(SceneStart());
    }


    // 씬 시작
    private IEnumerator SceneStart()
    {
        // 열차 생성 대기
        if (TrainManager.Instance != null)
        {
            yield return new WaitUntil(() => TrainManager.Instance.IsTrainReady);
        }

        // 열차 생성되고 대기 UI 끄기
        if (_waitingPanel != null) _waitingPanel.SetActive(false);

        // 상점일 때
        if (IsShop == true)
        {
            if (_timelineManager != null)
            {
                // 상점 도착 타임라인 재생
                // 페이드 인 -> 도착 타임라인 -> 페이드 아웃
                yield return StartCoroutine(PlayTimeline(TimelineType.ShopArrival));
            }
        }
        // 상점도 아닌데 대기실도 아닐 때 (인게임)
        else if (IsWaitingRoom == false)
        {
            PlayTrainWheel();   // 바퀴 회전
        }

        // 플레이어 스폰
        if (_spawner != null) _spawner.PlayerSpawn();

        // 스폰 잠깐 대기
        yield return new WaitForSeconds(0.3f);

        if (LoadingManager.Instance != null)
        {
            // 페이드 인
            LoadingManager.Instance.RequestFadeIn();
        }

        // 유실물 생성 (방장만)
        if (PhotonNetwork.IsMasterClient == true) StartCoroutine(SpawnLostItems());

        // 열차 순회하면서 네비링크 새로고침
        if (TrainManager.Instance != null)
        {
            TrainManager.Instance.RefreshAllTrainLinks();
        }
    }



    // 씬 전환 요청
    public void RequestChangeScene()
    {
        if (PhotonNetwork.IsMasterClient == true)
            RPC_StartChangeScene();
        else
            photonView.RPC(nameof(RPC_StartChangeScene), RpcTarget.MasterClient);
    }

    // 출발 요청 (방장이 수신)
    [PunRPC]
    private void RPC_StartChangeScene()
    {
        if (PhotonNetwork.IsMasterClient == false) return;

        Debug.Log("씬 전환 코루틴 뿌리기");
        // 씬 전환 코루틴 뿌리기
        photonView.RPC(nameof(RPC_ChangeScene), RpcTarget.All);
    }


    [PunRPC] // 씬 전환 코루틴 뿌리기 (모두 수신)
    private void RPC_ChangeScene()
    {
        StartCoroutine(ChangeSceneCoroutine());
    }

    // 씬 전환 코루틴
    private IEnumerator ChangeSceneCoroutine()
    {
        // 게임오버 상태 변경 (클리어)
        IsGameOver = true;

        if (LoadingManager.Instance != null)
        {
            // 페이드 아웃
            LoadingManager.Instance.RequestFadeOut();
            // 페이드 시간 대기
            yield return new WaitForSeconds(LoadingManager.Instance.FadeDuration);
        }

        // 대기실 아닐 때
        if(_isWaitingRoom == false)
        {
            // 적 청소
            CleanupEnemy();

            // 유실물 청소
            CleanupLostItems();

            // 죽은 열차 청소
            if (TrainManager.Instance != null)
            {
                TrainManager.Instance.CleanupDeadTrain();
            }

            // 레이더 초기화
            RadarNode.ResetRadarCount();

            // 엔진 ui 비활성화
            if (_engineUI != null) _engineUI.SetActive(false);

            // 상점도 아닐 때 (인게임)
            if(_isShop == false)
            {
                // 환경 오브젝트 끌어당기기
                if (_environmentSpawner) _environmentSpawner.Repositon();

                // 브금 끄기
                if (SoundManager.Instance) SoundManager.Instance.PlayBGM(BGMType.None);
            }
        }
        else
        {
            _waitingCanvas?.SetActive(false);
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

        // 타임라인 재생
        if (_timelineManager != null)
        {
            // 타임라인 타입
            TimelineType targetType = TimelineType.None;
        
            // 현재 씬에 따라 재생할 타임라인 결정
            if (_isWaitingRoom || _isShop)
                targetType = TimelineType.Start;        // 대기실, 상점 -> 출발
            else
                targetType = TimelineType.GameClear;    // 인게임 -> 상점 (클리어)
        
            // 타임라인 재생 대기 (페이드인 -> 타임라인 재생 -> 페이드 아웃)
            yield return StartCoroutine(PlayTimeline(targetType));
        }

        // 방장만 씬 전환 명령
        if (PhotonNetwork.IsMasterClient)
        {
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

        // 이제 비동기로딩하면서 씬 전환
        photonView.RPC(nameof(RPC_LoadSceneAsync), RpcTarget.All, _loadSceneName);
    }

    // 비동기 씬 전환 (모두 수신)
    [PunRPC]
    private void RPC_LoadSceneAsync(string sceneName)
    {
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
        UpdateGold();
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
            UpdateGold();

            return true;
        }

        return false;
    }


    // 골드 갱신
    private void UpdateGold()
    {
        _goldText?.SetText($"{GameData.Gold:N0}");

        // 골드 변화 뿌림
        OnGoldChanged?.Invoke(GameData.Gold);
    }

    // 생존일 갱신
    private void UpdateDay()
    {
        _SurviveDayText?.SetText($"{GameData.SurviveDay} 일차");

        // 환경 시간대 설정
        SceneEnvironmentManager.Instance.SetEnvironmentTime();
    }

    #endregion


    #region 유실물

    // 바닥 유실물 저장, 비활성화
    private void CleanupLostItems()
    {
        // 씬의 모든 아이템 검색
        NetworkItem[] lostItems = FindObjectsByType<NetworkItem>(FindObjectsSortMode.None);

        // 방장은 유실물 데이터 리스트 초기화
        if (PhotonNetwork.IsMasterClient == true)
        {
            GameData.LostItems.Clear();
            Debug.Log("[유실물] 저장 및 청소 시작");
        }

        // 아이템 순회
        foreach (var item in lostItems)
        {
            // 혹시 null이면 패스
            if (item == null) continue;

            // 방장인데 상점 아닐때만 저장
            if (PhotonNetwork.IsMasterClient == true && _isShop == false)
            {
                // 저장 조건 체크 (활성화, 이름 존재, 포톤뷰 켜짐)
                if (item.gameObject.activeInHierarchy &&
                    string.IsNullOrEmpty(item.ItemName) == false &&
                    item.photonView.enabled)
                {
                    // 데이터 확인
                    ShopItem data = FindItemData(item.ItemName);

                    // 연료 아니면 저장
                    if (data == null || (data is FuelData) == false)
                    {
                        GameData.LostItems.Add(item.ItemName);
                    }
                }
            }

            // 아이템 비활성화
            item.gameObject.SetActive(false);
        }

        if (PhotonNetwork.IsMasterClient == true)
        {
            Debug.Log($"[유실물] {GameData.LostItems.Count}개 저장하고 바닥 청소 완료");
        }
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
            Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f));

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

    // 관전모드로 전환
    public void SpectatorMode()
    {
        if (_spectatorCamera != null)
        {
            _spectatorCamera.gameObject.SetActive(true);
            _spectatorCamera.transform.position = PlayerHandler.localPlayer.CameraHolderTrans.position;
            _spectatorCamera.transform.rotation = PlayerHandler.localPlayer.CameraHolderTrans.rotation;
        }

        // 레이더 초기화
        RadarNode.ResetRadarCount();
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
        Debug.Log("게임오버");
        // 방장만
        if (PhotonNetwork.IsMasterClient == false) return;

        // 모두에게 게임오버 알림
        photonView.RPC(nameof(RPC_GameOver), RpcTarget.All);
    }

    // 게임오버 알림
    [PunRPC]
    private void RPC_GameOver()
    {
        StartCoroutine(GameOverCoroutine());
    }

    private IEnumerator GameOverCoroutine()
    {
        // 게임오버 상태 변경
        IsGameOver = true;

        if (LoadingManager.Instance != null)
        {
            // 페이드 아웃
            LoadingManager.Instance.RequestFadeOut();
            // 페이드 시간 대기
            yield return new WaitForSeconds(LoadingManager.Instance.FadeDuration);
        }

        // 적 청소
        CleanupEnemy();

        // 아이템 청소
        CleanupLostItems();

        // 엔진 ui 비활성화
        if(_engineUI) _engineUI.SetActive(false);

        // 환경 오브젝트 끌어당기기
        if (_environmentSpawner) _environmentSpawner.Repositon();

        // 브금 끄기
        if (SoundManager.Instance) SoundManager.Instance.PlayBGM(BGMType.None);

        // 레이더 초기화
        RadarNode.ResetRadarCount();

        // 플레이어 전부 치우고
        foreach (var player in ActivePlayers.ToArray())
        {
            if (player != null) player.gameObject.SetActive(false);
        }

        // 게임오버 타임라인 재생
        if (_timelineManager != null)
        {
            // 타임라인 재생 완료 대기 (페이드 인 -> 파괴 타임라인 재생 -> 페이드 아웃)
            yield return StartCoroutine(PlayTimeline(TimelineType.GameOver));
        }

        // 데이터 초기화
        GameData.Reset();

        // 파괴되지 않는 싱글톤 정리
        if (QuickSlotManager.Instance != null) Destroy(QuickSlotManager.Instance.gameObject);
        if (ItemManager.Instance != null) Destroy(ItemManager.Instance.gameObject);

        // 씬 이동 (방장)
        if (PhotonNetwork.IsMasterClient == true)
        {
            // 이제 다 같이 대기실로 이동
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


    // 적 청소
    private void CleanupEnemy()
    {
        // 적 스포너 비활성화
        if (_enemySpawner != null) _enemySpawner.enabled = false;

        // 적 스크립트 가진 모든 객체 수집
        var enemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);

        // 적 순회
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                // 비활성화
                enemy.gameObject.SetActive(false);
            }
        }

        Debug.Log($"[적 청소] {enemies.Length} 마리의 적을 화면에서 치웠습니다.");
    }

    // 타임라인 재생 코루틴
    private IEnumerator PlayTimeline(TimelineType type)
    {
        Debug.Log($"타임라인 재생 : {type}");
        if (QuickSlotManager.Instance != null)
        {
            // 퀵슬롯 UI 비활성화
            QuickSlotManager.Instance.SetUIActive(false);
        }

        // 관전 카메라 비활성화
        if (_spectatorCamera != null && _spectatorCamera.gameObject.activeSelf)
        {
            _spectatorCamera.gameObject.SetActive(false);
        }

        // 해당 타입 디렉터 가져오기
        PlayableDirector director = _timelineManager.GetDirector(type);

        // 없으면 패스
        if (director == null) yield break;

        // 타임라인 재생될 카메라나 오브젝트 미리 켜기 (필요하다면)
        director.gameObject.SetActive(true);

        // 페이드 인
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.RequestFadeIn();
        }

        // 타임라인 재생
        director.Play();

        // 타임라인 재생시간에서 페이드 아웃 빼서
        // 페이드 아웃 일찍 실행
        float timelineDuration = (float)director.duration - LoadingManager.Instance.FadeDuration;
        // 페이드 시간보다 타임라인 재생 시간이 짧으면 즉시
        float waitBeforeFade = Mathf.Max(0f, timelineDuration);

        // 타임라인 재생시간 동안 끝날 때까지 대기
        yield return new WaitForSeconds(waitBeforeFade);

        // 다시 페이드 아웃
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.RequestFadeOut();
            yield return new WaitForSeconds(LoadingManager.Instance.FadeDuration);
        }
    }


    // 타임 라인 재생 중 열차 폭파 시그널
    public void ExplodeTrain()
    {
        // 폭발
        if (TrainManager.Instance != null && TrainManager.Instance.TrainNodes.Count > 0)
        {
            TrainNode engine = TrainManager.Instance.TrainNodes[0];

            // 엔진 폭발
            engine.ExplodeRPC();
        }

    }


    // 증기 파티클 재생 시그널
    public void PlayEngineSteam()
    {
        if (TrainManager.Instance != null && TrainManager.Instance.TrainNodes.Count > 0)
        {
            TrainNode engine = TrainManager.Instance.TrainNodes[0];

            if (engine is EngineNode engineNode)
            {
                // 증기 파티클 재생
                engineNode.PlaySteam();
            }
        }
    }
    // 증기 파티클 중지 시그널
    public void StopEngineSteam()
    {
        if (TrainManager.Instance != null && TrainManager.Instance.TrainNodes.Count > 0)
        {
            TrainNode engine = TrainManager.Instance.TrainNodes[0];

            if (engine is EngineNode engineNode)
            {
                // 증기 파티클 중지
                engineNode.StopSteam();
            }
        }
    }

    // 바퀴 재생 시그널
    public void PlayTrainWheel()
    {
        if (TrainManager.Instance != null && TrainManager.Instance.TrainNodes.Count > 0)
        {
            // 모든 열차 순회
            foreach(var node in TrainManager.Instance.TrainNodes)
            {
                if(node != null)
                {
                    // 바퀴 애니메이션 재생
                    node.StartWheel();
                }
            }
        }
    }

    // 바퀴 브레이크 시그널
    public void BreakTrainWheel()
    {
        if (TrainManager.Instance != null && TrainManager.Instance.TrainNodes.Count > 0)
        {
            // 모든 열차 순회
            foreach (var node in TrainManager.Instance.TrainNodes)
            {
                if (node != null)
                {
                    // 바퀴 중지 애니메이션 재생
                    node.BreakWheel();
                }
            }
        }
    }

    // 정지 소리
    public void PlayBreakSteam()
    {
        if (_audioSource == null || _steamClip == null || SoundManager.Instance == null) return;

        SoundManager.Instance.PlayOneShot3D(_audioSource, _steamClip);
    }

    // 롤링 루프 중지 시그널
    public void StopRolling()
    {
        // 열차 롤링 끄기
        if (_audioSource != null) _audioSource.Stop();
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

                UpdateGold();
            }

            // 생존일
            if (GameData.SurviveDay != receiveDay)
            {
                GameData.SurviveDay = receiveDay;

                UpdateDay();
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
        if (_isLocalLoad == true && IsGameStart == false)
        {
            // 새로운 방장에게 준비 됐다고 다시 보내기
            photonView.RPC(nameof(RPC_SceneLoaded), RpcTarget.MasterClient);
        }
    }
    #endregion
}
