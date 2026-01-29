using Photon.Pun;
using UnityEngine;

public class TurretNode : TrainNode
{
    [Header("포탑 설정")]
    [SerializeField] Transform _turretHead;              // 회전할 머리 부분
    [SerializeField] Transform _muzzlePoint;             // 발사 위치
    [SerializeField] ParticleSystem _muzzleFlash;        // 발사 이펙트
    [SerializeField] PoolableObject _tracerPrefab;       // 총알 궤적 프리팹
    [SerializeField] PoolableObject _impactPrefab;       // 탄착 프리팹
    [SerializeField] LayerMask _targetLayer;             // 적 레이어
    [SerializeField] float _safeWidth = 4.0f;            // 사격 금지 너비 (열차 폭)


    // 포탑 스탯
    protected float _damage;        // 공격력
    protected float _fireRate;      // 발사 속도
    protected float _range;         // 사거리
    protected float _rotationSpeed; // 회전 속도

    private Transform _target;     // 현재 타겟
    private float _lastFireTime;   // 발사 쿨타임
    private RaycastHit _hit;       // 레이캐스트 캐싱

    // 네트워크 동기화용
    private Quaternion _targetRotation;

    public override void Init(TrainData data, int level)
    {
        base.Init(data, level);

        SetData(level);
    }

    // 레벨 데이터 설정
    private void SetData(int level)
    {
        if (Data is TrainTurretData turretData)
        {
            // 레벨의 스탯
            var turretStat = turretData.GetTurretStat(level);

            // 공격력
            _damage = turretStat.damage;
            // 발사 속도
            _fireRate = turretStat.fireRate;
            // 사거리
            _range = turretStat.range;
            // 회전 속도
            _rotationSpeed = turretStat.rotationSpeed;
        }
    }
    private void Update()
    {
        // 터졌으면 작동 중지
        if (_currentHp <= 0) return;

        // 방장
        if (PhotonNetwork.IsMasterClient)
        {
            // 타겟팅, 회전 계산, 발사
            TerretLogic();
        }
        // 클라이언트
        else
        {
            // 회전 동기화
            RotationSync();
        }
    }


    // 방장용
    // 터렛 로직
    private void TerretLogic()
    {
        // 타겟이 없거나, 죽었거나, 사거리 밖이면
        if (IsTargetInvalid())
        {
            // 다시 찾기
            FindTarget();
        }

        // 타겟이 있으면
        if (_target != null)
        {
            // 회전 계산
            Vector3 dir = (_target.position - _turretHead.position).normalized;
            dir.y = 0; 

            if (dir != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir);
                // 부드럽게 회전
                _turretHead.rotation = Quaternion.Slerp(_turretHead.rotation, lookRot, Time.deltaTime * _rotationSpeed);
            }

            // 발사 체크
            // 포탑이 얼추 적을 보고 있을 때 발사 (10도정도)
            if (Vector3.Angle(_turretHead.forward, dir) < 10f)
            {
                // 쿨타임 체크
                if (Time.time >= _lastFireTime + _fireRate)
                {
                    // 발사
                    Fire();
                    // 쿨타임 갱신
                    _lastFireTime = Time.time;
                }
            }
        }
    }


    // 클라이언트용
    // 회전 동기화
    private void RotationSync()
    {
        if (_turretHead != null)
        {
            // SerializeView에서 받은 값으로 부드럽게 회전
            _turretHead.rotation = Quaternion.Slerp(_turretHead.rotation, _targetRotation, Time.deltaTime * _rotationSpeed);
        }
    }

    // 타겟 유효 검사
    private bool IsTargetInvalid()
    {
        // 타겟이 null 유효하지 않음
        if (_target == null) return true;

        // 타겟이 비활성화 유효하지 않음 !
        if (_target.gameObject.activeInHierarchy == false) return true;

        // 타겟이 거리 밖이다. 유효하지 않음 ! !
        if (Vector3.Distance(transform.position, _target.position) > _range) return true;

        // x 거리가 세이프 너비 안쪽이면 유효하지 않음 ! ! !
        float xDistance = Mathf.Abs(transform.position.x - _target.position.x);
        if (xDistance < _safeWidth) return true;

        // 다 통과했음. 유효..
        return false;
    }

    // 가장 가까운 적 찾기
    private void FindTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, _range, _targetLayer);

        Transform closest = null;
        float minDistance = float.MaxValue;

        foreach (var hit in hits)
        {
            // 안쪽에 있는 적은 스킵
            float xDistance = Mathf.Abs(transform.position.x - hit.transform.position.x);
            if (xDistance < _safeWidth) continue;

            // 적 스크립트 확인 (IDamageable 등)
            // 예시로 EnemyBase 컴포넌트 체크
            if (hit.GetComponentInParent<IDamageable>() != null)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < minDistance)
                {
                    // 가까운 적 갱신
                    minDistance = distance;
                    closest = hit.transform;
                }
            }
        }

        // 타겟 확정
        _target = closest;
    }

    // 방장용
    // 발사
    private void Fire()
    {
        if (_target == null) return;

        // 레이캐스트 대미지 처리
        Vector3 fireDir = (_target.position - _muzzlePoint.position).normalized;
        Vector3 hitPoint = _target.position;
        Vector3 hitNormal = Vector3.up;

        if (Physics.Raycast(_muzzlePoint.position, fireDir, out _hit, _range, _targetLayer))
        {
            // 히트 포인트와 표면 방향
            hitPoint = _hit.point;
            hitNormal = _hit.normal;

            IDamageable damageable = _hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                // 피해
                damageable.TakeDamage((int)_damage);
            }
        }

        // 모두에게 발사 이펙트
        photonView.RPC(nameof(RPC_FireTurret), RpcTarget.All, hitPoint, hitNormal);
    }

    [PunRPC]
    private void RPC_FireTurret(Vector3 hitPoint, Vector3 hitNormal)
    {
        // 총구 화염
        if (_muzzleFlash != null) _muzzleFlash.Play();

        // 총알 궤적
        if (_tracerPrefab != null && PoolManager.Instance != null)
        {
            // 궤적 생성
            var tracerObj = PoolManager.Instance.Spawn(_tracerPrefab, _muzzlePoint.position, Quaternion.identity);
            var tracer = tracerObj.GetComponent<BulletTracer>();

            if (tracer != null)
            {
                // 궤적 초기화 하면서 발사
                tracer.InitAndShoot(_muzzlePoint.position, hitPoint, hitNormal, _impactPrefab);
            }
        }
    }

    public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        base.OnPhotonSerializeView(stream, info);

        if (stream.IsWriting)
        {
            stream.SendNext(_turretHead.rotation);
        }
        else
        {
            // 목표 회전값
            _targetRotation = (Quaternion)stream.ReceiveNext();
        }
    }
}
