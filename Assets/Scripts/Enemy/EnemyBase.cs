using Photon.Pun;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PoolableObject))]
public class EnemyBase : MonoBehaviourPun, IPunObservable, IDamageable
{
    [Header("적 스탯")]
    [SerializeField] protected int _maxHp = 100;        // 최대 체력
    protected int _currentHp;                           // 현재 체력 (동기화)

    [Header("공격 설정")]
    [SerializeField] protected int _damage = 10;        // 공격력
    [SerializeField] protected float _attackRange = 2f; // 범위
    [SerializeField] protected float _attackRate = 1f;  // 간격

    [Header("드랍 골드량")]
    [SerializeField] protected int _minGold = 5;        // 최소 골드
    [SerializeField] protected int _maxGold = 20;       // 최대 골드

    [Header("애니메이터")]
    [SerializeField] protected Animator _animator;      // 애니메이터

    [Header("사망 파티클")]
    [SerializeField] protected Transform _dieParticlePoint;
    [SerializeField] protected PoolableObject _dieParticle;

    [Header("네트워크 동기화 설정")]
    [SerializeField] float _moveSmoothSpeed = 10f;  // 이동 
    [SerializeField] float _rotSmoothSpeed = 10f;   // 회전
    [SerializeField] float _teleportDistance = 5f;  // 텔포

    public AudioSource GetAudioSource => _audioSource; // 오디오 소스
    public bool IsDead { get; protected set; }  // 사망 상태
    protected Collider _collider;       // 콜라이더
    protected AudioSource _audioSource; // 오디오 소스
    
    protected Transform _target;        // 타겟 (플레이어나 열차임)
    protected float _lastAttackTime;    // 공격 쿨타임
    protected PoolableObject _currentDeadParticle;  // 사망 파티클
    protected bool _isVisualDead = false;         // 사망 비주얼 처리 플래그

    // 네트워크 트랜스폼
    private Vector3 _networkPos;
    private Quaternion _networkRot;

    protected virtual void Awake()
    {
        _collider = GetComponent<Collider>();
        _audioSource = GetComponent<AudioSource>();
    }

    protected virtual void OnEnable()
    {
        _currentHp = _maxHp;
        IsDead = false;
        _isVisualDead = false;
        if (_collider != null) _collider.enabled = true;

        // 비주얼 활성화
        if (_animator != null) _animator.gameObject.SetActive(true);

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

        // 이펙트, 사운드 로컬 즉시 실행 (예측)
        PlayHitEffect();

        // 그냥 예측으로 사망처리
        if (_currentHp - dmg <= 0)
        {
            VisualDeath();
        }

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

    // 비주얼 사망 처리
    private void VisualDeath()
    {
        // 비주얼 사망처리 되어있으면 무시
        if (_isVisualDead == true) return;

        // 비주얼 사망처리
        _isVisualDead = true;

        // 미니맵에서 즉시 제거 요청
        if (MiniMapHandler.Instance != null)
        {
            MiniMapHandler.Instance.Unregister(transform);
        }

        // 사망 로직
        OnDeath();

        // 모델 끄기
        if (_animator != null) _animator.gameObject.SetActive(false);

        // 콜라이더 끄기
        if (_collider != null) _collider.enabled = false;
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
    protected virtual void RPC_Die()
    {
        // 진짜 사망
        IsDead = true;

        // 한 번 더 호출
        VisualDeath();

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
        // 사망 시간 대기 (사운드, 파티클)
        yield return new WaitForSeconds(1.0f);

        // 사망 파티클 반납
        _currentDeadParticle.Release();

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

            // 체력
            stream.SendNext(_currentHp);
        }
        else
        {
            // 트랜스폼
            _networkPos = (Vector3)stream.ReceiveNext();
            _networkRot = (Quaternion)stream.ReceiveNext();

            // 체력
            _currentHp = (int)stream.ReceiveNext();
        }
    }
}
