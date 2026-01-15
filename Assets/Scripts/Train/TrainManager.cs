using Photon.Pun;
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
    private const string KEY_TRAIN_TYPES = "TrainTypes";
    private const string KEY_TRAIN_LEVELS = "TrainLevels";

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

    [Header("상점 체크")]
    [SerializeField] private bool _isShop = false;

    [Header("시작 열차 설정")]
    [SerializeField] List<TrainType> _trainInitList;

    [Header("열차 목록")]
    [SerializeField] List<TrainDataSO> trains;

    // 검색용 열차 목록
    private Dictionary<TrainType, TrainDataSO> _trainDict;

    // 현재 열차 배치 (타입, 레벨)
    private List<Train> _currentTrains = new List<Train>();

    // 현재 열차 배치 (스크립트)
    private List<TrainNode> _currentTrainNodes = new List<TrainNode>();

    //private List<>


    // 열차 준비 상태
    public bool IsTrainReady { get; private set; }


    private void Awake()
    {
        if (_instance == null) _instance = this;
        else if (_instance != this) Destroy(gameObject);

        // 리스트를 딕셔너리로 변환
        _trainDict = new Dictionary<TrainType, TrainDataSO>();

        foreach (var train in trains)
        {
            // Key : TrainType(Enum),     Value : TrainDataSO(ScriptableObject)
            if (!_trainDict.ContainsKey(train.type))
                _trainDict.Add(train.type, train);
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

            if (PhotonNetwork.IsMasterClient == false)
                Debug.Log("열차 동기화 완료");

            IsTrainReady = true;
            Debug.Log("모든 열차 로딩 완료. 플레이어 스폰 승인");
        }
    }

    // 열차 생성 시도
    private IEnumerator SpawnTrainCoroutine()
    {
        // 룸 프로퍼티 데이터 담을 공간
        int[] types = null;
        int[] levels = null;
        // 현재 방의 커스텀 프로퍼티 키값 KEY_TRAIN_TYPES이 있을 때
        if (PhotonNetwork.CurrentRoom != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(KEY_TRAIN_TYPES))
        {
            types = (int[])PhotonNetwork.CurrentRoom.CustomProperties[KEY_TRAIN_TYPES];
            levels = (int[])PhotonNetwork.CurrentRoom.CustomProperties[KEY_TRAIN_LEVELS];
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

        // 방장
        if (PhotonNetwork.IsMasterClient == true)
        {
            // 목표 생성 수만큼 생성
            for (int i = 0; i < targetCount; i++)
            {
                SpawnTrain((TrainType)types[i], levels[i]);
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

        // 초기 열차 리스트
        foreach (var type in _trainInitList)
        {
            initTypes.Add(type);
            initLevels.Add(1); // 기본 1레벨
        }

        // 룸 프로퍼티 저장
        SaveRoomTrainData(initTypes, initLevels);

        foreach (TrainType type in _trainInitList)
        {
            SpawnTrain(type, 1);
        }
    }

    // 열차 생성
    private void SpawnTrain(TrainType type, int level)
    {
        // 열차 딕셔너리에 타입 있는지 확인
        if (_trainDict.TryGetValue(type, out TrainDataSO trainData))
        {
            // 열차 노드 하나 생성
            TrainNode newTrainNode = null;

            // 상점이면
            if (_isShop == true)
            {
                // 로컬 생성
                //newTrainNode = Instantiate(trainData.prefab);
                //
                //// 상점에서는 PhotonView 필요 없어서 지움
                //var pv = newTrainNode.GetComponent<PhotonView>();
                //if(pv) Destroy(pv); 
                //Train newTrain = new Train { type = type, level = level };
                //_currentTrains.Add(newTrain);
                //
                //// 노드 초기화
                //newTrainNode.Init(trainData, level);
                //
                //// 연결 (Linked List)
                //if (_currentTrainNodes.Count > 0)
                //{
                //    int prevIndex = _currentTrainNodes.Count - 1;
                //    _currentTrainNodes[prevIndex].ConnectNextTrain(newTrainNode);
                //}
                //
                //// 노드 데이터 등록
                //_currentTrainNodes.Add(newTrainNode);
                //
                //// 위치 정렬
                //AlignLast();
            }
            else
            {
                // 방장만
                if (PhotonNetwork.IsMasterClient == true)
                {
                    // 네트워크 객체 생성 시 넘길 데이터
                    object[] initData = new object[3];
                    initData[0] = _currentTrainNodes.Count;
                    initData[1] = level;
                    initData[2] = (int)type; // Enum -> int로

                    // 네트워크 객체 생성
                    GameObject newTrainObj = PhotonNetwork.InstantiateRoomObject(trainData.prefab.name, Vector3.zero, Quaternion.identity, 0, initData);
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
    public void RegisterNetworkTrain(TrainNode node, int index, TrainType type, int level)
    {
        // 리스트 공간 확보 (2번보다 3번이 먼저 왔을 경우)
        while (_currentTrainNodes.Count <= index)
        {
            _currentTrainNodes.Add(null);
        }

        // 데이터 리스트 공간 확보
        while (_currentTrains.Count <= index)
        {
            _currentTrains.Add(new Train()); // 빈 껍데기
        }

        // 현재 열차 리스트에 등록
        _currentTrainNodes[index] = node;

        // 현재 데이터 리스트에 등록
        Train trainInfo = new Train { type = type, level = level };
        _currentTrains[index] = trainInfo;

        // 데이터(SO) 찾아서 초기화
        if (_trainDict.TryGetValue(type, out TrainDataSO data))
        {
            node.Init(data, level);
        }


        Debug.Log($"현재 열차 노드 수 : " + _currentTrainNodes.Count);

        // 전체 재정렬
        AlignAll();
    }


    // 꼬리 자르기
    public void CutTail(TrainNode targetNode)
    {
        // 터진 열차의 번호
        int index = _currentTrainNodes.IndexOf(targetNode);

        // 없으면 무시 (이미 터진 경우)
        if (index == -1) return;

        // 잘릴 꼬리 수
        int count = _currentTrainNodes.Count - index;

        // 데이터 리스트에서 즉시 삭제해서 잘라버림
        _currentTrainNodes.RemoveRange(index, count);
        _currentTrains.RemoveRange(index, count);

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

    #region 룸프로퍼티
    // 현재 리스트 상태를 룸 프로퍼티에 덮어쓰기
    private void UpdateRoomProperties()
    {
        List<TrainType> types = new List<TrainType>();
        List<int> levels = new List<int>();

        // 기존 열차 리스트
        foreach (var train in _currentTrains)
        {
            types.Add(train.type);
            levels.Add(train.level);
        }

        // 룸 프로퍼티 저장
        SaveRoomTrainData(types, levels);
    }

    // 룸 프로퍼티 저장
    private void SaveRoomTrainData(List<TrainType> types, List<int> levels)
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
            { KEY_TRAIN_LEVELS, levels.ToArray() }
        };

        // 실제 룸 프로퍼티 갱신
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    // 룸 프로퍼티 갱신 (열차 배치 순서, 추가)
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        // 상점 모드이고, 기차 데이터가 변경되었다면
        if (_isShop && propertiesThatChanged.ContainsKey(KEY_TRAIN_TYPES))
        {
            // 열차 새로고침
            //RefreshTrains();
        }
    }
    #endregion


    #region 테스트 버튼
    // 화물칸 추가 버튼 (테스트용)
    public void OnClickAddCargo()
    {
        if (_currentTrainNodes.Count == 0 || _currentTrains[0].type != TrainType.Engine)
        {
            Debug.LogWarning("엔진이 없어서 화물칸을 붙일 수 없습니다!");
            return;
        }
        SpawnTrain(TrainType.Cargo, 1);

        // 룸 프로퍼티 갱신
        UpdateRoomProperties();
    }

    // 포탑칸 추가 버튼 (테스트용)
    public void OnClickAddTurret()
    {
        if (_currentTrainNodes.Count == 0 || _currentTrains[0].type != TrainType.Engine)
        {
            Debug.LogWarning("엔진이 없어서 화물칸을 붙일 수 없습니다!");
            return;
        }
        SpawnTrain(TrainType.Turret, 1);

        // 룸 프로퍼티 갱신
        UpdateRoomProperties();
    }
    #endregion


    #region 상점용
    // Type 열차 추가 (상점용)
    public void RequestAddTrain(TrainType type)
    {
        // 방장만 변경 가능
        if (PhotonNetwork.IsMasterClient == false) return;
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
    #endregion
}
