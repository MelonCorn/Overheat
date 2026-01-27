using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TrainType       //  열차 타입
{
    None,
    Engine,
    Cargo,
    Turret,
}

public struct Train         // 소유 열차 정보
{
    public TrainType type;
    public int level;
}

public class TrainManager : MonoBehaviourPunCallbacks
{
    // 룸 프로퍼티 키값
    public const string KEY_TRAIN_TYPES = "TrainTypes";        // 타입
    public const string KEY_TRAIN_LEVELS = "TrainLevels";      // 레벨
    public const string KEY_TRAIN_CONTENTS = "TrainContents";  // 화물 정보

    private static TrainManager _instance;
    public static TrainManager Instance
    {
        get
        {
            // 호출 했는데 아직 연결이 안 되어 있으면
            if (_instance == null)
            {
                // 씬에 있는 TrainManager를 찾아옴 (Awake보다 먼저 실행 가능)
                _instance = FindAnyObjectByType<TrainManager>();

                // 그래도 없다면 내 실수
                if (_instance == null)
                {
                    Debug.LogError("씬에 TrainManager가 없습니다");
                }
            }
            return _instance;
        }
    }

    [Header("시작 열차 설정")]
    [SerializeField] List<TrainType> _trainInitList;

    [Header("열차 목록")]
    [SerializeField] List<TrainData> trains;

    [Header("열차 부모")]
    [SerializeField] Transform _trainGroup;


    public event Action OnTrainListUpdated; // 열차 새로고침 알림

    // 검색용 열차 목록
    public Dictionary<TrainType, TrainData> TrainDict { get; private set; }

    // 현재 열차 배치 (타입, 레벨)
    private List<Train> _currentTrains = new List<Train>();

    // 현재 열차 배치 (스크립트)
    private List<TrainNode> _currentTrainNodes = new List<TrainNode>();

    // 현재 화물 배치 (화물칸 아이템명 묶음)
    private List<string> _currentContents = new List<string>();

    // 엔진 노드
    public EngineNode MainEngine
    {
        get
        {
            // 리스트 비어있으면 null
            if (_currentTrainNodes.Count == 0) return null;
            // 0번이 없거나(파괴) null이면 null
            if (_currentTrainNodes[0] == null) return null;
            // 0번 EngineNode로 형변환해서 리턴
            return _currentTrainNodes[0] as EngineNode;
        }
    }

    public List<TrainNode> TrainNodes => _currentTrainNodes;    // 외부 참조용 열차 노드들
    public List<Train> CurrentTrains => _currentTrains;         // 외부 참조용 열차 타입,레벨


    // 열차 준비 상태
    public bool IsTrainReady { get; private set; }

    private bool _isShop => GameManager.Instance.IsShop;


    private void Awake()
    {
        if (_instance == null) _instance = this;
        else Destroy(gameObject);

        // 리스트를 딕셔너리로 변환
        TrainDict = new Dictionary<TrainType, TrainData>();

        foreach (var train in trains)
        {
            // Key : TrainType(Enum),     Value : TrainDataSO(ScriptableObject)
            if (!TrainDict.ContainsKey(train.type))
                TrainDict.Add(train.type, train);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            _instance = null;
        }
    }

    private IEnumerator Start()
    {
        // 상점
        if (_isShop == true)
        {
            // 상점에서는 로컬 객체로 바로 생성
            if (_currentTrainNodes.Count == 0)
            {
                RefreshTrain();
            }
        }
        // 인게임
        else
        {
            // 혹시 남아있는 데이터 초기화
            _currentTrains.Clear();
            _currentTrainNodes.Clear();

            // 포톤 연결 대기
            yield return new WaitUntil(() => PhotonNetwork.IsConnectedAndReady);

            // 룸 프로퍼티 로드될 때까지 혹은 방장
            // 방에 들어왔어도 커스텀 프로퍼티 동기화에 틱이 걸릴 수 있다고 함
            if (PhotonNetwork.CurrentRoom != null)
            {
                yield return new WaitUntil(() =>
                PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(KEY_TRAIN_TYPES) ||
                PhotonNetwork.IsMasterClient);
            }

            // 열차 생성 및 동기화 대기 (코루틴 실행)
            yield return StartCoroutine(SpawnTrainCoroutine());
        }

        IsTrainReady = true;
        Debug.Log("모든 열차 로딩 완료. 플레이어 스폰 승인");
    }

    // 열차 생성 시도
    private IEnumerator SpawnTrainCoroutine()
    {
        // 룸 프로퍼티 데이터 담을 공간
        int[] types = null;
        int[] levels = null;
        string[] contents = null;

        // 현재 방의 커스텀 프로퍼티 키값 KEY_TRAIN_TYPES이 있을 때
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(KEY_TRAIN_TYPES))
        {
            types = (int[])PhotonNetwork.CurrentRoom.CustomProperties[KEY_TRAIN_TYPES];
            levels = (int[])PhotonNetwork.CurrentRoom.CustomProperties[KEY_TRAIN_LEVELS];
            contents = (string[])PhotonNetwork.CurrentRoom.CustomProperties[KEY_TRAIN_CONTENTS];
        }

        // 룸 데이터 없으면
        if (types == null)
        {
            // 방장이 기본 열차 생성
            if (PhotonNetwork.IsMasterClient == true)
            {
                GenerateInitTrain();
            }

            // 클라이언트는 프로퍼티 있을 때 까지 대기하기 때문에 여기 올 일이 없음
            yield break;
        }

        int targetCount = types.Length; // 타입 길이 만큼 생성

        Debug.Log($"타겟 카운트 : " + targetCount);

        // 상점에선 누구나 로컬 객체로 스폰
        if (_isShop == true)
        {
            for (int i = 0; i < targetCount; i++)
            {
                SpawnTrain((TrainType)types[i], levels[i], contents[i]);
            }

            // 구독자들에게 새로고침 알림
            OnTrainListUpdated?.Invoke();

            // 상점 로직으로 끝
            yield break;
        }

        // -------------- 인게임용 네트워크 로직-----------------
        // 방장
        if (PhotonNetwork.IsMasterClient == true)
        {
            // 목표 생성 수만큼 생성
            for (int i = 0; i < targetCount; i++)
            {
                SpawnTrain((TrainType)types[i], levels[i], contents[i]);
            }
        }
        // 일반 클라이언트
        else
        {
            // OnPhotonInstantiate 콜백으로 목표 채울 때까지 대기
            yield return new WaitUntil(() => _currentTrainNodes.Count >= targetCount);

            // 다 도착했으면 정렬
            AlignAll();
        }
    }

    // 초기 열차 생성
    private void GenerateInitTrain()
    {
        List<TrainType> initTypes = new List<TrainType>();
        List<int> initLevels = new List<int>();
        List<string> initContents = new List<string>();

        // 초기 열차 리스트
        foreach (var type in _trainInitList)
        {
            initTypes.Add(type);
            initLevels.Add(1); // 기본 1레벨
            initContents.Add(""); // 화물 비어있음
        }

        // 룸 프로퍼티 저장
        SaveRoomTrainData(initTypes, initLevels, initContents);

        foreach (TrainType type in _trainInitList)
        {
            SpawnTrain(type, 1, "");
        }
    }

    // 열차 생성
    private void SpawnTrain(TrainType type, int level, string content)
    {
        // 열차 딕셔너리에 타입 있는지 확인
        if (TrainDict.TryGetValue(type, out TrainData trainData))
        {
            // 열차 노드 하나 생성
            TrainNode newTrainNode = null;

            // 상점이면
            if (_isShop == true)
            {
                // 로컬 생성
                if(_trainGroup != null)
                    newTrainNode = Instantiate(trainData.prefab, _trainGroup);
                else
                    newTrainNode = Instantiate(trainData.prefab);

                // 상점에서는 PhotonView 필요 없어서 지움
                var pv = newTrainNode.GetComponent<PhotonView>();
                if (pv) Destroy(pv);

                // 프로퍼티 기반 새로운 열차 데이터 생성
                Train newTrain = new Train { type = type, level = level };
                _currentTrains.Add(newTrain);

                // 노드 초기화
                newTrainNode.Init(trainData, level);

                // 화물칸 소켓 정보
                if (newTrainNode is CargoNode cargo)
                {
                    cargo.ImportData(content);
                }

                // 연결
                if (_currentTrainNodes.Count > 0)
                {
                    int prevIndex = _currentTrainNodes.Count - 1;
                    _currentTrainNodes[prevIndex].ConnectNextTrain(newTrainNode);
                }

                // 노드 데이터 등록
                _currentTrainNodes.Add(newTrainNode);

                // 위치 정렬
                AlignLast();
            }
            else
            {
                // 방장만
                if (PhotonNetwork.IsMasterClient == true)
                {
                    // 네트워크 객체 생성 시 넘길 데이터
                    object[] initData = new object[4];
                    initData[0] = _currentTrainNodes.Count;
                    initData[1] = level;
                    initData[2] = (int)type; // Enum -> int로
                    initData[3] = content; 


                    // 네트워크 객체 생성
                    GameObject newTrainObj = PhotonNetwork.InstantiateRoomObject(trainData.prefab.name, Vector3.zero, Quaternion.identity, 0, initData);

                    // 열차 그룹 하위 객체로
                    if (_trainGroup != null) newTrainObj.transform.SetParent(_trainGroup);

                    // 열차의 노드 스크립트
                    newTrainNode = newTrainObj.GetComponent<TrainNode>();
                }

                return;
            }
        }
        else
        {
            Debug.LogError($"{type} 타입의 데이터가 딕셔너리에 없습니다");
        }
    }

    // 생성된 네트워크 열차 객체 동기화용
    public void RegisterNetworkTrain(TrainNode node, int index, TrainType type, int level, string content)
    {
        // 클라이언트도 열차 생성 시 그룹의 하위 객체로
        if (_trainGroup != null) node.transform.SetParent(_trainGroup);

        // 리스트 공간 확보 (2번보다 3번이 먼저 왔을 경우)
        while (_currentTrainNodes.Count <= index)
        {
            _currentTrainNodes.Add(null);
        }

        // 데이터 리스트 공간 확보
        while (_currentTrains.Count <= index)
        {
            _currentTrains.Add(new Train());
        }

        // 화물 정보 공간
        while (_currentContents.Count <= index)
        {
            _currentContents.Add("");
        }

        // 현재 열차 리스트에 등록
        _currentTrainNodes[index] = node;

        // 현재 데이터 리스트에 등록
        Train trainInfo = new Train { type = type, level = level };
        _currentTrains[index] = trainInfo;

        // 현재 화물 정보 리스트에 등록
        _currentContents[index] = content;

        // 데이터(SO) 찾아서 초기화
        if (TrainDict.TryGetValue(type, out TrainData data))
        {
            node.Init(data, level);

            if (node is CargoNode cargo)
            {
                cargo.ImportData(content);
            }
        }

        Debug.Log($"현재 열차 노드 수 : " + _currentTrainNodes.Count);

        // 전체 재정렬
        AlignAll();
    }


    // 꼬리 자르기
    public void CutTail(TrainNode targetNode)
    {
        // 게임오버 상태에서 혹시나 들어왔을까봐 체크
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver == true) return;

        // 터진 열차의 번호
        int index = _currentTrainNodes.IndexOf(targetNode);

        // 없으면 무시 (이미 터진 경우)
        if (index == -1) return;

        // 잘릴 꼬리 수
        int count = _currentTrainNodes.Count - index;

        // 데이터 리스트에서 즉시 삭제해서 잘라버림
        _currentTrainNodes.RemoveRange(index, count);   // 열차 노드 리스트
        _currentTrains.RemoveRange(index, count);       // 열차 데이터 리스트 (Train)
        _currentContents.RemoveRange(index, count);     // 화물 데이터

        // 룸 프로퍼티 갱신 (열차 리스트)
        if (PhotonNetwork.IsMasterClient)
        {
            UpdateRoomProperties();
        }
    }

    // 전체 리스트 위치 정렬 (인게임용)
    private void AlignAll()
    {
        // 현재 열차 리스트 순회
        for (int i = 0; i < _currentTrainNodes.Count; i++)
        {
            // 열차 노드
            TrainNode currentNode = _currentTrainNodes[i];

            // 해당 칸이 아직 도착 안 했으면 null
            // 네트워크 지연으로 중간에 구멍 뚫려있을 수 있음
            if (currentNode == null) continue;

            // 엔진 처리
            if (i == 0)
            {
                currentNode.Attach(null);
            }
            // 뒤 칸들 처리
            else
            {
                // 앞차
                TrainNode prevNode = _currentTrainNodes[i - 1];

                // 앞차가 도착해 있을 때만 붙임
                // 앞차 아직 null 이면 (0,0,0)이나 엉뚱한 곳에 대기
                // 앞차 도착 후 AlignAll 또 호출될 때 다시 시도
                if (prevNode != null)
                {
                    prevNode.ConnectNextTrain(currentNode); // 앞차에 자신 할당
                    currentNode.Attach(prevNode);           // 자신은 앞차 연결부로 위치 이동
                }
            }
        }
    }

    // 열차 마지막 칸의 Z 좌표 반환 (적 스포너용)
    public float GetLastZ()
    {
        // 열차 없거나 초기화 안 됐으면 0
        if (_currentTrainNodes == null || _currentTrainNodes.Count == 0) return 0f;

        // 마지막 칸 가져오기
        TrainNode lastNode = _currentTrainNodes[_currentTrainNodes.Count - 1];

        // 마지막 칸이 비어있으면 0 (방어)
        if (lastNode == null) return 0f;

        // 마지막 칸의 z 반환
        return lastNode.transform.position.z;
    }


    #region 화물칸 아이템 변경
    public void RequestSocketInteract(CargoNode node, int socketIndex, string newItem, string oldItem, int slotIndex)
    {
        // 화물칸 번호 체크
        int trainIndex = _currentTrainNodes.IndexOf(node);
        if (trainIndex == -1) return;

        // 방장에게 확인 요청 (열차 번호, 소켓 번호, 아이템, 아이템, 퀵슬롯 번호)
        photonView.RPC(nameof(RPC_SocketInteract), RpcTarget.MasterClient, trainIndex, socketIndex, newItem, oldItem, slotIndex, PhotonNetwork.LocalPlayer);
    }


    // 방장 전용 소켓 상호작용 확인
    [PunRPC]
    private void RPC_SocketInteract(int trainIndex, int socketIndex, string newItem, string oldItem, int slotIndex, Player player)
    {
        // 현재 화물 룸 프로퍼티 데이터 가져오기
        string[] contents = new string[0];
        if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(KEY_TRAIN_CONTENTS))
            contents = (string[])PhotonNetwork.CurrentRoom.CustomProperties[KEY_TRAIN_CONTENTS];

        // 해당 화물칸의 아이템 문자열 분리
        string targetContent = contents[trainIndex];
        string[] items = targetContent.Split(',');

        // 현재 화물에 저장된 아이템
        string serverItem = (socketIndex < items.Length) ? items[socketIndex] : "";

        // 새 아이템이 비어있으면 픽업
        // 새 아이템이 존재하면 보관
        bool isStore = !string.IsNullOrEmpty(newItem);
        bool isSuccess = false;

        // 검증
        if (isStore) // 수납 시도
        {
            // 소켓이 비어있는지 확인
            if (string.IsNullOrEmpty(serverItem)) isSuccess = true;
        }
        else // 픽업 시도
        {
            // 소켓에 아이템이 있는지 확인
            if (serverItem == oldItem) isSuccess = true;
        }

        // 결과
        if (isSuccess)
        {
            // 임시 배열
            List<string> itemList = new List<string>(items);

            // 사이 빈칸 채우기
            while (itemList.Count <= socketIndex)
            {
                itemList.Add("");
            }

            // 데이터 갱신
            itemList[socketIndex] = newItem;

            // 다시 문자열 하나로 합치기
            contents[trainIndex] = string.Join(",", itemList);

            // 룸 프로퍼티 갱신
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { KEY_TRAIN_CONTENTS, contents }
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
        else
        {
            // 실패 시 롤백 아이템
            string rollbackItem = isStore ? newItem : oldItem;

            // 실패 알림을 상호작용 시도 요청자에게만
            photonView.RPC(nameof(RPC_SocketRollback), player, trainIndex, socketIndex, serverItem, rollbackItem, slotIndex, isStore);
        }
    }

    // 상호작용 실패로 롤백
    [PunRPC]
    private void RPC_SocketRollback(int trainIndex, int socketIndex, string serverItem, string rollbackItem, int slotIndex, bool isStore)
    {
        if (_currentTrainNodes[trainIndex] is CargoNode cargo)
            cargo.RollbackSocket(socketIndex, serverItem, rollbackItem, slotIndex, isStore);
        
    }
    #endregion


    #region 룸프로퍼티
    // 현재 리스트 상태를 룸 프로퍼티에 덮어쓰기
    private void UpdateRoomProperties()
    {
        List<TrainType> types = new List<TrainType>();
        List<int> levels = new List<int>();
        List<string> contents = new List<string>();

        // 기존 열차 리스트
        foreach (var trainNode in _currentTrainNodes)
        {
            // 혹시나 비어있으면 스킵
            if (trainNode == null) continue;

            // 타입, 레벨 저장
            types.Add(trainNode.Data.type);
            int index = _currentTrainNodes.IndexOf(trainNode);
            levels.Add(_currentTrains[index].level);

            // 화물칸이면 데이터 불러오기
            if (trainNode is CargoNode cargo)
            {
                // 화물칸이 가지고있는 데이터들 압축해서 하나의 문자열로 만듬
                string cargoString = cargo.ExportData();
                contents.Add(cargoString);
            }
            else
            {
                // 화물칸 아니면 내용물 없음
                contents.Add("");
            }
        }

        // 룸 프로퍼티 저장
        SaveRoomTrainData(types, levels, contents);
    }

    // 룸 프로퍼티 저장
    private void SaveRoomTrainData(List<TrainType> types, List<int> levels, List<string> contents)
    {
        if (PhotonNetwork.IsMasterClient == false) return;

        // Enum -> int 변환
        int[] typeArr = new int[types.Count];
        for (int i = 0; i < types.Count; i++)
        {
            typeArr[i] = (int)types[i];
        }

        // 새로운 프로퍼티
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
        {
            { KEY_TRAIN_TYPES, typeArr },
            { KEY_TRAIN_LEVELS, levels.ToArray() },
            { KEY_TRAIN_CONTENTS, contents.ToArray() },
        };

        // 실제 룸 프로퍼티 갱신
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    // 룸 프로퍼티 갱신 (열차 배치 순서, 추가)
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        // 게임오버 상태면 열차 갱신 중지
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver == true) return;

        // 상점 모드이고, 열차 데이터가 변경되었다면
        if (_isShop && propertiesThatChanged.ContainsKey(KEY_TRAIN_TYPES))
        {
            // 열차 새로고침
            RefreshTrain();

            // 내용은 알아서 채울테니 그냥 중단
            return;
        }

        // 열차 변경 없이
        // 화물 데이터만 변경 시
        if (propertiesThatChanged.ContainsKey(KEY_TRAIN_CONTENTS))
        {
            // 새 화물 정보들
            string[] newContents = (string[])propertiesThatChanged[KEY_TRAIN_CONTENTS];

            // 혹시라도 열차랑 데이터 수 안 맞으면 중단
            if (_currentTrainNodes.Count != newContents.Length) return;

            // 정보 순회
            for (int i = 0; i < newContents.Length; i++)
            {
                if (_currentTrainNodes[i] is CargoNode cargo)
                {
                    // 각 화물칸에 데이터 주입
                    cargo.ImportData(newContents[i]);
                }
            }
        }
    }
    #endregion


    #region 상점용

    // Type 열차 추가 (상점용)
    public void RequestAddTrain(TrainType type)
    {
        // 방장만 변경 가능
        if (PhotonNetwork.IsMasterClient == false) return;

        // 엔진 체크
        if (_currentTrainNodes.Count == 0 || _currentTrains[0].type != TrainType.Engine)
        {
            Debug.LogWarning("엔진이 없어서 화물칸을 붙일 수 없습니다!");
            return;
        }

        // Type 열차 1레벨로 생성
        SpawnTrain(type, 1, "");

        // 룸 프로퍼티 갱신
        UpdateRoomProperties();
    }
    // 마지막 추가 위치 정렬 (상점용)
    private void AlignLast()
    {
        // 방금 추가 열차 칸
        int index = _currentTrainNodes.Count - 1;
        TrainNode newTrain = _currentTrainNodes[index];

        // 혹시나 비어있으면 리턴
        if (newTrain == null) return;

        if (index == 0)
        {
            // 엔진은 앞차 없음
            newTrain.Attach(null);
        }
        else
        {
            // 앞차 꼬리에 붙이기
            TrainNode prevTrain = _currentTrainNodes[index - 1];
            newTrain.Attach(prevTrain);
        }
    }

    // 열차 갱신 (상점용)
    public void RefreshTrain()
    {
        // 기존 열차 파괴
        foreach (var node in _currentTrainNodes)
        {
            if (node != null) Destroy(node.gameObject);
        }
        _currentTrainNodes.Clear();
        _currentTrains.Clear();

        // 열차 생성
        StartCoroutine(SpawnTrainCoroutine());
    }

    // 열차 업그레이드 요청 (상점용)
    public void RequestUpgradeTrain(int index)
    {
        Debug.Log($"열차 업그레이드 요청 : {index} 번");
        // 범위 체크
        if (index < 0 || index >= _currentTrains.Count) return;

        // 방장이면 바로 실행
        if (PhotonNetwork.IsMasterClient == true)
        {
            Debug.Log($"열차 업그레이드 실행");
            TrainUpgrade(index);
        }
        // 아니면 방장에게 요청
        else
        {
            Debug.Log($"방장에게 열차 업그레이드 요청");
            photonView.RPC(nameof(RPC_UpgradeTrain), RpcTarget.MasterClient, index);
        }
    }


    // 방장에게 업그레이드 요청
    [PunRPC]
    private void RPC_UpgradeTrain(int index)
    {
        // 항상 이런건 방장 체크
        if (PhotonNetwork.IsMasterClient == false) return;

        TrainUpgrade(index);
    }

    // 실제 업그레이드 로직 (방장만)
    private void TrainUpgrade(int index)
    {
        // index 열차 {type, level}
        Train target = _currentTrains[index];

        // 타입에 맞는 데이터 가져오기
        if (TrainDict.TryGetValue(target.type, out TrainData data) == false) return;

        // 만렙 체크
        if (data.IsMaxLevel(target.level)) return;

        // 비용 체크
        int price = data.GetBasicStat(target.level).upgradePrice;

        // 골드 사용 시도
        if (GameManager.Instance.TryUseGold(price))
        {
            // 레벨 업
            target.level++;
            // 레벨 덮어씌우기
            _currentTrains[index] = target;

            // 룸 프로퍼티 갱신
            UpdateRoomProperties();

            // OnRoomPropertiesUpdate 호출 후 ShopManager 갱신
        }
    }
    #endregion
}
