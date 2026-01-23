using Photon.Pun;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [Header("투사체 설정")]
    [SerializeField] float _speed = 20f;        // 속도
    [SerializeField] int _damage = 10;          // 공격력
    [SerializeField] float _lifeTime = 3f;      // 지속 시간

    private bool _isReleased = false;   // 중복 반납 방지
    private PoolableObject _poolable;   // 자신의 풀 관리자

    private void Awake()
    {
        _poolable = GetComponent<PoolableObject>();
    }


    private void OnEnable()
    {
        // 반납 상태 풀어주기
        _isReleased = false;

        // 활성화되면 바로 디스폰 예약걸기
        Invoke(nameof(Despawn), _lifeTime);
    }
    private void OnDisable()
    {
        // 혹시라도 나중에 걸린예약 때문에 다음 발사 때 문제 있을 수 있으니까
        CancelInvoke();
    }


    private void Update()
    {
        // 반납 상태면 추가 이동 금지
        if (_isReleased == true) return;

        // 앞으로 이동
        // 동기화 필요 없음
        transform.Translate(Vector3.forward * _speed * Time.deltaTime);
    }


    // 충돌 판정
    private void OnTriggerEnter(Collider other)
    {
        // 반납 상태면 추가 충돌 금지
        if (_isReleased == true) return;

        // 데미지 줄 열차
        // 기본적으로 열차 타겟인데 플레이어도 혹시나 뭐 맞을 수 있음
        if (other.CompareTag("Train") || other.CompareTag("Player"))
        {
            IDamageable target = other.GetComponent<IDamageable>();

            // 데미지줄 수 있으면
            if (target != null)
            {
                // 방장이 아니면 판정 못함
                // 데미지 적용
                if (PhotonNetwork.IsMasterClient == true)
                    target.TakeDamage(_damage);
            }

            Despawn();
        }
        else if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            Despawn();
        }
    }

    // 풀 반납
    private void Despawn()
    {
        // 반납 상태면 추가 반납 금지
        if (_isReleased) return;

        // 반납 상태 돌입
        _isReleased = true;

        _poolable.Release();
    }
}
