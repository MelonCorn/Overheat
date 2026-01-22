using Photon.Pun;
using UnityEngine;
using System.Collections;

public class EnemyBase : MonoBehaviourPun, IDamageable
{
    [Header("적 스탯")]
    [SerializeField] protected int _maxHp = 100;        // 최대 체력
    protected int _currentHp;                           // 현재 체력

    [Header("공격 설정")]
    [SerializeField] protected int _damage = 10;        // 공격력
    [SerializeField] protected float _attackRange = 2f; // 범위
    [SerializeField] protected float _attackRate = 1f;  // 간격

    protected Collider _collider;       // 콜라이더
    
    protected Transform _target;        // 타겟 (플레이어나 열차임)
    protected float _lastAttackTime;    // 공격 쿨타임

    protected virtual void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    protected virtual void OnEnable()
    {
        _currentHp = _maxHp;
        if(_collider != null) _collider.enabled = true;
    }
    protected virtual void OnDisable()
    {
    }
    protected virtual void Start()
    {
    }
    protected virtual void Update()
    {
        // AI 로직
    }

    protected virtual void Think() { }


    // 적 피격
    public void TakeDamage(int dmg)
    {
        // 체력없으면 중단
        if (_currentHp <= 0) return;

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
        // 사망 애니메이션 재생
        // 

        // 다른 공격 판정 받지 않도록 콜라이더 끄기
        if (_collider != null) _collider.enabled = false;

        // 방장이 반납 시작
        if (PhotonNetwork.IsMasterClient == true)
        {
            StartCoroutine(Despawn());
        }
    }


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
}
