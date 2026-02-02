using Photon.Pun;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PoolableObject))]
public class EnemyBase : MonoBehaviourPun, IPunObservable, IDamageable
{
    [Header("적 스탯")]
    [SerializeField] protected int _maxHp = 100;        // 최대 체력
    protected int _currentHp;                           // 현재 체력

    [Header("공격 설정")]
    [SerializeField] protected int _damage = 10;        // 공격력
    [SerializeField] protected float _attackRange = 2f; // 범위
    [SerializeField] protected float _attackRate = 1f;  // 간격

    [Header("드랍 골드량")]
    [SerializeField] protected int _minGold = 5;        // 최소 골드
    [SerializeField] protected int _maxGold = 20;       // 최대 골드

    [Header("애니메이터")]
    [SerializeField] protected Animator _animator;      // 애니메이터

    [Header("네트워크 동기화 설정")]
    [SerializeField] float _moveSmoothSpeed = 10f;  // 이동 
    [SerializeField] float _rotSmoothSpeed = 10f;   // 회전
    [SerializeField] float _teleportDistance = 5f;  // 텔포

    public bool IsDead { get; protected set; }  // 사망 상태
    protected Collider _collider;       // 콜라이더
    
    protected Transform _target;        // 타겟 (플레이어나 열차임)
    protected float _lastAttackTime;    // 공격 쿨타임
        
    // 네트워크 트랜스폼
    private Vector3 _networkPos;
    private Quaternion _networkRot;

    protected virtual void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    protected virtual void OnEnable()
    {
        _currentHp = _maxHp;
        IsDead = false;
        if (_collider != null) _collider.enabled = true;

        // 개체 수 증가
        EnemySpawner.ActiveCount++;

        // 미니맵에 등록
        if (MiniMapHandler.Instance != null)
        {
            MiniMapHandler.Instance.RegisterEnemy(transform);
        }

        _networkPos = transform.position;
        _networkRot = transform.rotation;
    }
    protected virtual void OnDisable()
    {
        // 개체 수 감소
        EnemySpawner.ActiveCount--;
        
        // 음수 방지
        if (EnemySpawner.ActiveCount < 0) EnemySpawner.ActiveCount = 0;
    }
    protected virtual void Start()
    {
    }
    protected virtual void Update()
    {
        if (PhotonNetwork.IsMasterClient == true)
        {
            if (_currentHp <= 0) return;
            if (IsDead) return;

            // AI 로직
            Think();
        }
        else
        {
            // 네트워크 트랜스폼 동기화
            UpdateNetworkTransform();
        }

    }

    // 자식에서 구현할 AI 로직
    protected virtual void Think() { }


    // 클라이언트용 네트워크 동기화
    protected void UpdateNetworkTransform()
    {
        // 위치 보간
        // 거리 차이가 너무 벌어지면 텔포
        if (Vector3.Distance(transform.position, _networkPos) > _teleportDistance)
            transform.position = _networkPos;
        else
            transform.position = Vector3.Lerp(transform.position, _networkPos, Time.deltaTime * _moveSmoothSpeed);

        // 회전 보간
        transform.rotation = Quaternion.Lerp(transform.rotation, _networkRot, Time.deltaTime * _rotSmoothSpeed);
    }

    // 적 피격
    public void TakeDamage(int dmg)
    {
        // 체력없으면 중단
        if (_currentHp <= 0) return;

        Debug.Log($"[적] {dmg}의 고통을 맛봤습니다.");

        // 이펙트, 사운드 로컬 즉시 실행 (예측)
        PlayHitEffect();

        // 방장은
        if (PhotonNetwork.IsMasterClient)
        {
            // 실제 처리
            ApplyDamage(dmg);
        }
        // 나머지는
        else
        {
            // 요청
            photonView.RPC(nameof(RPC_TakeDamage), RpcTarget.MasterClient, dmg);
        }
    }

    // 타격 이펙트
    private void PlayHitEffect()
    {
        // 이펙트, 사운드 재생
        Debug.Log($"{name} 피격!");
    }


    // 방장에게 피해 요청
    [PunRPC]
    protected void RPC_TakeDamage(int dmg)
    {
        if (PhotonNetwork.IsMasterClient == false) return;
        ApplyDamage(dmg);
    }

    // 실제 데미지 적용 (방장)
    protected void ApplyDamage(int dmg)
    {
        // 체력 감소
        _currentHp -= dmg;

        // 0 이하면
        if (_currentHp <= 0)
        {
            // 체력 0 고정
            _currentHp = 0;

            // 사망 확정 뿌리기
            photonView.RPC(nameof(RPC_Die), RpcTarget.All);
        }
    }


    // 사망 처리
    [PunRPC]
    protected void RPC_Die()
    {
        IsDead = true;

        // 자식 클래스 사망
        OnDeath();

        // 사망 파티클

        // 다른 공격 판정 받지 않도록 콜라이더 끄기
        if (_collider != null) _collider.enabled = false;

        // 방장이 반납 시작
        if (PhotonNetwork.IsMasterClient == true)
        {
            if (GameManager.Instance != null)
            {
                // 골드 랜덤 보상
                int reward = Random.Range(_minGold, _maxGold + 1);
                // 골드 추가
                GameManager.Instance.AddGold(reward);

                Debug.Log($"[적 처치] 골드 획득 : {reward}");
            }

            StartCoroutine(Despawn());
        }
    }

    // 사망
    protected virtual void OnDeath() { }

    // 디스폰 코루티 
    IEnumerator Despawn()
    {
        // 사망 애니메이션 시간 대기
        // yield return new WaitForSeconds(1.0f);

        // 일단 테스트용으로 바로 사라지게
        yield return null;

        // 풀 반납
        PhotonNetwork.Destroy(gameObject);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if(stream.IsWriting)
        {
            // 트랜스폼
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            // 트랜스폼
            _networkPos = (Vector3)stream.ReceiveNext();
            _networkRot = (Quaternion)stream.ReceiveNext();
        }
    }
}
