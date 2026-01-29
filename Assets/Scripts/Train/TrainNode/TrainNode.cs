using Photon.Pun;
using System;
using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;

public class TrainNode : MonoBehaviourPun, IPunObservable, IPunInstantiateMagicCallback, // 네트워크 객체 생성 후 데이터 콜백
                                            IDamageable, IRepairable        // 피해, 수리
{
    protected Collider _collider;
    protected Rigidbody _rigidbody;

    protected int _maxHp;         // 최대 체력
    protected int _currentHp;     // 현재 체력

    protected TrainNode _prevTrain;   // 앞차
    protected TrainNode _nextTrain;   // 뒷차

    [Header("침투 창문 포인트")]
    [SerializeField] Transform[] _windowPoints;

    [Header("후방 연결부")]
    [SerializeField] Transform _rearSocket;


    [Header("화재 설정")]
    [SerializeField] GameObject _firePrefab; // Resource 폴더의 불 프리팹
    [SerializeField] Transform[] _firePoints; // 불이 생성될 위치들 (열차 바닥 등)
    [SerializeField] float _fireProbability = 20f; // 피격 시 화재 발생 확률

    [Header("폭발 연출 설정")]
    [SerializeField] float _explosionForce = 10f; // 날아가는 힘
    [SerializeField] float _torqueForce = 5f;     // 회전하는 힘 (굴리기)

    // 파괴 타겟 레이어
    protected LayerMask _localPlayerLayer;   // 로컬플레이어
    protected LayerMask _itemLayer;         // 아이템
    protected LayerMask _enemyLayer;        // 적

    private LayerMask _explosionMask;       // 폭발 체크용

    private NavMeshLink _navMeshLink;       // 링크 새로고침

    // 기차 번호
    public int TrainIndex { get; private set; }
    // 참조 데이터
    public TrainData Data { get; private set; }

    public event Action<int, int> OnHpChanged;
    public event Action OnExplode;
    public Transform RearSocket => _rearSocket;
    public int CurrentHp => _currentHp;
    public int MaxHp => _maxHp;

    private bool _isExploding = false;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _rigidbody = GetComponent<Rigidbody>();
        _navMeshLink = GetComponentInChildren<NavMeshLink>();
    }

    public virtual void Init(TrainData data, int level)
    {
        // 데이터
        Data = data;

        // 레벨의 스탯
        var stat = Data.GetBasicStat(level);

        // 체력
        _maxHp = stat.maxHP;
        _currentHp = _maxHp;

        // 이름
        gameObject.name = Data.itemName;

        // UI 생성
        if (TestTrainUIManager.Instance != null)
        {
            TestTrainUIManager.Instance.CreateUI(this);
        }


        // LocalPlayer 레이어 설정
        int layerIndex = LayerMask.NameToLayer("LocalPlayer");
        if (layerIndex != -1) _localPlayerLayer = 1 << layerIndex;

        //  Item 레이어 설정
        int itemIndex = LayerMask.NameToLayer("Item");
        if (itemIndex != -1) _itemLayer = 1 << itemIndex;

        // Enemy 레이어 설정
        int enemyIndex = LayerMask.NameToLayer("Enemy");
        if (enemyIndex != -1) _enemyLayer = 1 << enemyIndex;

        // 비트 OR 연산으로 동시에 감지
        _explosionMask = _localPlayerLayer | _itemLayer | _enemyLayer;
    }

    // 앞차의 후방 연결부에 붙임
    public void Attach(TrainNode prevTrain)
    {
        // 앞차 저장
        _prevTrain = prevTrain;

        //  앞차가 없다면 (아마 엔진)
        if (prevTrain == null)
        {
            // 0,0,0
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            return;
        }

        // 앞차 소켓 확인
        if (prevTrain.RearSocket != null)
        {
            // 소켓의 위치와 회전 그대로 적용
            transform.position = prevTrain.RearSocket.position;
            transform.rotation = prevTrain.RearSocket.rotation;
        }
        else
        {
            Debug.LogError($"{prevTrain.name}에 RearSocket이 연결되지 않았습니다!");
        }
    }

    // 다음 칸 생기면 정보 받음
    public void ConnectNextTrain(TrainNode nextTrain)
    {
        // 뒷차 저장
        _nextTrain = nextTrain;
    }


    #region 외부 호출용

    // 가장 가까운 창문 위치 반환
    public Transform GetClosestWindow(Vector3 enemyPos)
    {
        // 등록된 창문 없으면 그냥 내 위치 (예외)
        if (_windowPoints == null || _windowPoints.Length == 0)
            return transform;

        // 가까운 창문 트랜스폼 껍데기
        Transform closestPoint = null;
        // 제일 가까운 거리 (비교하면서 줄여야하기 때문에 일단 최대로)
        float minDistance = float.MaxValue;

        // 창문 포인트마다
        foreach (Transform point in _windowPoints)
        {
            // 적 위치와 창문 위치 거리
            float distance = Vector3.Distance(enemyPos, point.position);

            // 가까운 거리 갱신
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint = point;
            }
        }

        // 제일 가까운 창문 트랜스폼 반환
        return closestPoint;
    }

    // 콜라이더 중앙 반환 (원거리 적 이동용)
    public Vector3 GetCenter()
    {
        if (_collider != null)
        {
            // 콜라이더의 중앙
            return _collider.bounds.center;
        }

        // 콜라이더 없으면 그냥 원래 위치
        return transform.position;
    }

    // 콜라이더 랜덤 위치 반환 (사격 타겟용)
    public Vector3 GetRandomPoint()
    {
        if (_collider != null)
        {
            // 콜라이더 영역 박스
            Bounds bounds = _collider.bounds;

            // 바운드 박스 안에서 랜덤 좌표 추출
            float randX = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
            float randY = UnityEngine.Random.Range(bounds.min.y, bounds.max.y);
            float randZ = UnityEngine.Random.Range(bounds.min.z, bounds.max.z);

            return new Vector3(randX, randY, randZ);
        }
        // 콜라이더 없으면 그냥 원래 위치
        return transform.position;
    }
    #endregion


    // 피해
    public void TakeDamage(int amount)
    {
        // 체력 없거나
        // 게임오버 상태면 무시 (연출 중 무적)
        if (_currentHp <= 0) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        _currentHp -= amount;

        // 화재 발생 체크
        if (PhotonNetwork.IsMasterClient && _currentHp > 0)
        {
            CheckFireSpawn();
        }

        // 사망 판정
        if (_currentHp <= 0)
        {
            // 깔끔하게 0
            _currentHp = 0;

            // 폭발 RPC 발송
            photonView.RPC(nameof(ExplodeRPC), RpcTarget.All);
        }

        // 본인 권한의 SerializeView는 읽기가 안됨
        // 그래서 직접 호출
        OnHpChanged?.Invoke(_currentHp, _maxHp);
    }

    // 수리
    public void TakeRepair(int amount)
    {
        if (_currentHp >= _maxHp) return;

        _currentHp += amount;

        if (_currentHp > _maxHp)
            _currentHp = _maxHp;

        OnHpChanged?.Invoke(_currentHp, _maxHp);
    }


    // 열차 화재 체크
    public void CheckFireSpawn()
    {
        // 확률 체크
        float random = UnityEngine.Random.Range(0f, 100f);
        if (random > _fireProbability) return;

        // 생성 위치 랜덤 선정
        if (_firePoints == null || _firePoints.Length == 0) return;
        int randIndex = UnityEngine.Random.Range(0, _firePoints.Length);
        Vector3 spawnPos = _firePoints[randIndex].position;

        // 이미 그 자리에 불이 있는지 체크
        // 작은 구 쏴서 TrainFire 컴포넌트 있는지 확인
        Collider[] hits = Physics.OverlapSphere(spawnPos, 0.5f);
        foreach (var hit in hits)
        {
            if (hit.GetComponent<TrainFire>()) return; // 이미 불타고 있으면 패스
        }

        // 불 생성
        // ViewID 데이터로 넘겨서 불 부모 설정
        object[] data = new object[] { photonView.ViewID };
        PhotonNetwork.Instantiate(_firePrefab.name, spawnPos, Quaternion.identity, 0, data);

        Debug.Log($"{gameObject.name}에 화재 발생!");
    }



    #region 파괴
    // 열차 파괴 
    [PunRPC]
    public virtual void ExplodeRPC()
    {
        // 엔진 파괴 시
        if (TrainIndex == 0)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver == false)
            {
                // 방장만
                if (PhotonNetwork.IsMasterClient)
                {
                    Debug.Log("[엔진 파괴] 동력 상실. 게임 오버 진입.");
                    // 게임오버 호출
                    GameManager.Instance.GameOver();
                }

                // 엔진 터지면 타임라인으로 연쇄 폭발 연출할거기 때문에
                // 그리고 CutTail하면 애초에 룸 프로퍼티 갱신되어서
                // 게임오버 시 룸 프로퍼티 초기화와 꼬일 가능성 있음

                // 패스
                return;
            }

        }

        // 앞차랑 연결 끊기
        if (_prevTrain != null)
        {
            _prevTrain.ConnectNextTrain(null);
            _prevTrain = null;
        }

        // 꼬리자르기 (리스트 정리)
        if (TrainManager.Instance != null)
        {
            TrainManager.Instance.CutTail(this);
        }

        // 폭발
        Explode();
    }

    // 호출용 폭발
    public void Explode()
    {
        // 이미 폭발 중이면 무시
        if (_isExploding) return;

        _isExploding = true;

        // 체력 0
        if (_currentHp > 0)
        {
            _currentHp = 0;
            OnHpChanged?.Invoke(0, _maxHp);
        }

        // 폭발 코루틴 시작
        StartCoroutine(ExplodeCoroutine());
    }

    IEnumerator ExplodeCoroutine()
    {
        Debug.Log($"{name} 쾅!");

        // 날려버리기
        BlowAway();

        // 연출 딜레이
        yield return new WaitForSeconds(0.15f);

        // 뒷차 연쇄 작용
        if (_nextTrain != null) _nextTrain.Explode();

        // 폭발 범위 내 오브젝트 처리
        ExplosionHit();

        // 만약 이 열차가 화물 열차라면
        // 선반 비우기 
        if (this is CargoNode cargo) cargo.ClearAllSockets();

        // UI 파괴
        OnExplode?.Invoke();

        // 삭제 딜레이
        yield return new WaitForSeconds(5f);

        // 진짜 사망
        if (PhotonNetwork.IsMasterClient) PhotonNetwork.Destroy(gameObject);
        else gameObject.SetActive(false);
    }


    // 폭발 시 날려버리기
    private void BlowAway()
    {
        if (_rigidbody == null || _collider == null) return;

        // 부모 연결 해제
        transform.SetParent(null);

        // 물리 켜기
        _rigidbody.isKinematic = false;
        _rigidbody.useGravity = true;
        _rigidbody.maxAngularVelocity = 100f; // 회전 제한 해제

        // 콜라이더 설정
        Collider[] allColliders = GetComponentsInChildren<Collider>();

        // 일단 전부 끄기
        foreach (var col in allColliders)
        {
            col.enabled = false;
        }

        // 루트에 있는 박스 콜라이더 가져오기
        BoxCollider rootCollider = GetComponent<BoxCollider>();
        if(rootCollider != null)
        {
            // 활성화, 트리거 끄기
            rootCollider.enabled = true;
            rootCollider.isTrigger = false;
        }

        // 폭발점
        // 앞쪽으로 좀 많이 이동
        Vector3 forwardOffset = transform.forward * 2.0f;

        // 아래쪽으로 이동
        Vector3 downOffset = Vector3.down * 1.5f;

        // 랜덤 양 사이드
        float randomSide = UnityEngine.Random.Range(-1.5f, 1.5f);
        Vector3 sideOffset = transform.right * randomSide;

        // 최종 폭발 위치 연결부 + 앞 + 아래 + 랜덤옆
        Vector3 explosionOrigin = transform.position + forwardOffset + downOffset + sideOffset;

        // 폭발력 적용 
        _rigidbody.AddExplosionForce(_explosionForce, explosionOrigin, 10f, 0.5f, ForceMode.Impulse);

        // 회전력 적용
        _rigidbody.AddTorque(UnityEngine.Random.insideUnitSphere * _torqueForce, ForceMode.Impulse);
    }

    // 숨기기
    public void Hide()
    {
        // 모든 코루틴 중단
        StopAllCoroutines();

        // 걍 꺼버림
        gameObject.SetActive(false);
    }

    // 폭발 범위 내 오브젝트 처리
    private void ExplosionHit()
    {
        // 열차의 루트 콜라이더
        BoxCollider boxCol = GetComponent<BoxCollider>();

        if (boxCol == null) return;

        Debug.Log("박스 콜라이더 유, 폭발 처리 시작");

        // 범위 설정
        Vector3 worldCenter = transform.TransformPoint(boxCol.center);
        Vector3 halfSize = boxCol.size * 0.5f;
        halfSize.y += 5.0f;

        // 박스 범위 내 잡힌 모든 _explosionMask 콜라이더 저장
        Collider[] hits = Physics.OverlapBox(worldCenter, halfSize, transform.rotation, _explosionMask);

        Debug.Log($"박스 범위 내 모든 explosion 레이어 오브젝트 수 {hits.Length}");

        // 모든 콜라이더 순회
        foreach (var hit in hits)
        {
            // 충돌한 물체의 레이어 (비트 값으로 변환)
            int hitLayerMask = 1 << hit.gameObject.layer;

            // IDamageable 가져와보기
            IDamageable target = hit.GetComponentInParent<IDamageable>();

            // IDamageable 객체
            if (target != null)
            {
                // 로컬 플레이어
                if ((hitLayerMask & _localPlayerLayer) != 0)
                {
                    // 확실하게 플레이어고, 로컬 플레이어면
                    if (target is PlayerHandler player && player == PlayerHandler.localPlayer)
                    {
                        Debug.Log($"[사망] 열차 폭발에 휘말림");
                        target.TakeDamage(999999); // 즉사
                    }
                }
                // 적 (방장이)
                else if ((hitLayerMask & _enemyLayer) != 0)
                {
                    if (PhotonNetwork.IsMasterClient == true)
                    {
                        Debug.Log($"[파괴] 적({hit.name}) 폭사 처리");
                        target.TakeDamage(999999); // 즉사
                    }
                }
            }
            // 아이템
            else if ((hitLayerMask & _itemLayer) != 0)
            {
                // 아이템의 PhotonView 확인
                PhotonView itemPhotonview = hit.GetComponentInParent<PhotonView>();

                // 내가 생성한 아이템인지 확인
                if (itemPhotonview != null && itemPhotonview.IsMine)
                {
                    Debug.Log($"[파괴] 내 아이템 {itemPhotonview.gameObject.name} 파괴");
                    PhotonNetwork.Destroy(itemPhotonview.gameObject);
                }
            }
        }
    }

    #endregion


    // 네비 링크 새로고침 (타임라인 때문에 꼬인거 푸는용)
    public void RefreshNavMeshLink()
    {
        _navMeshLink.enabled = false;
        _navMeshLink.enabled = true;
    }


    // 네트워크로 생성되면 호출
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // 보낸 데이터 꺼내기
        object[] data = info.photonView.InstantiationData;

        // 데이터 없으면 무시 (상점) 
        if (data == null || data.Length < 3) return;

        // 데이터 언박싱
        int index = (int)data[0];            // 순서
        int level = (int)data[1];            // 레벨
        TrainType type = (TrainType)data[2]; // 타입
        string content = (string)data[3];    // 화물내용

        // 열차 번호
        TrainIndex = index;

        // 매니저에 등록, Init 요청
        if (TrainManager.Instance != null)
        {
            TrainManager.Instance.RegisterNetworkTrain(this, index, type, level, content);
        }
    }

    public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // 룸 소유라 방장이 실행
        if (stream.IsWriting)
        {
            stream.SendNext(_currentHp);
        }
        else
        {
            _currentHp = (int)stream.ReceiveNext();

            // UI 갱신 알림
            OnHpChanged?.Invoke(_currentHp, _maxHp);
        }
    }
}
