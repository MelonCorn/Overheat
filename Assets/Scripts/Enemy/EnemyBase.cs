using Photon.Pun;
using UnityEngine;

public class EnemyBase : MonoBehaviour, IPunObservable, IDamageable
{
    [Header("적 스탯")]
    [SerializeField] protected int _maxHp = 100;        // 최대 체력
    protected int _currentHp;                           // 현재 체력

    [Header("공격 설정")]
    [SerializeField] protected int _damage = 10;        // 공격력
    [SerializeField] protected float _attackRange = 2f; // 범위
    [SerializeField] protected float _attackRate = 1f;  // 간격

    protected Transform _target;        // 타겟 (플레이어나 열차임)
    protected float _lastAttackTime;    // 공격 쿨타임

    protected PoolableObject _poolable; // 풀 반납용

    protected virtual void Awake()
    {
        _poolable = GetComponent<PoolableObject>();
    }

    protected virtual void OnEnable()
    {
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


    public void TakeDamage(int dmg)
    {
        // 피격 시

        // 방장은 즉시

        // 클라이언트는 예측 후 요청
    }

    protected void Die()
    {
        // 사망 처리

        // 골드 보상

        // 반납
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {

        }
        else
        {

        }
    }

}
