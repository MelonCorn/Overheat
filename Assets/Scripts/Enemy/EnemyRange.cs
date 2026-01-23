using Photon.Pun;
using System.Collections;
using UnityEngine;

public class EnemyRange : EnemyBase
{
    [Header("비행 행동 설정")]
    [SerializeField] float _flySpeed = 20f;        // 비행 속도
    [SerializeField] float _shootRange = 15f;     // 사거리 (멈출 거리)
    [SerializeField] float _rotSpeed = 5f;        // 회전 속도
    [SerializeField] float _smoothTime = 0.5f;    // 목표 도달 시간

    [Header("공격 설정")]
    [SerializeField] PoolableObject _projectilePrefab; // 투사체 프리팹
    [SerializeField] Transform _firePoint;             // 발사 위치

    private TrainNode _targetTrain;                // 목표 기차
    private Vector3 _currentVelocity;              // SmoothDamp 계산용

    protected override void OnEnable()
    {
        base.OnEnable();

        if (PhotonNetwork.IsMasterClient == false) return;


        // 가장 가까운 기차 찾기
        FindClosestTrain();

    }


    // 생각
    protected override void Think()
    {
        // 타겟 체크
        // 타겟이 null이거나 체력이 없으면
        if (_targetTrain == null || _targetTrain.CurrentHp <= 0)
        {
            // 다른 열차 찾기
            FindClosestTrain();

            // 열차 없으면 계속 대기
            if (_targetTrain == null) return;
        }

        // 거리 계산 (자신, 적)
        float distance = Vector3.Distance(transform.position, _targetTrain.transform.position);

        // 행동
        if (distance > _shootRange)
        {
            // 사거리보다 멀면 타겟에 접근
            MoveToTarget();
        }
        else
        {
            // 사거리 안이면 멈추고 발사
            LookAtTarget(); // 바라보기는 계속

            // 공격 쿨타임 체크 후
            if (Time.time >= _lastAttackTime + _attackRate)
            { 
                // 발사
                Fire();
            }
        }
    }


    // 타겟으로 이동
    private void MoveToTarget()
    {
        // 목표 지점 (타겟 열차)
        Vector3 targetPos = _targetTrain.transform.position;

        // 정지 위치를 사거리 살짝 안으로 지정해서 SmoothDamp 잘 작동되게
        // 그냥 쌩으로 열차 위치 쓰면 감속 전에 멈춤
        Vector3 dir = (transform.position - targetPos).normalized;
        Vector3 stopPoint = targetPos + (dir * (_shootRange - 0.3f));

        // 높이 고정
        stopPoint.y = transform.position.y;

        // 부드럽게 이동
        transform.position = Vector3.SmoothDamp(transform.position, stopPoint, ref _currentVelocity, _smoothTime, _flySpeed);

        // 타겟으로 회전
        LookAtTarget();
    }



    // 타겟으로 회전
    private void LookAtTarget()
    {
        // 방향 (적 - 자신)
        Vector3 dir = (_targetTrain.transform.position - transform.position).normalized;

        // 위아래로는 안움직이고 몸통만
        dir.y = 0; 

        if (dir != Vector3.zero)
        {
            // 부드럽게 회전
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * _rotSpeed);
        }
    }

    // 가장 가까운 열차 찾기
    private void FindClosestTrain()
    {
        if (TrainManager.Instance == null) return;

        // 열차 노드 리스트 가져오기
        var trains = TrainManager.Instance.TrainNodes;

        // 최소 거리 (비교)
        float minDistance = float.MaxValue;

        // 제일 가까운 열차 노드
        TrainNode bestTarget = null;

        // 열차 노드 순회
        foreach (var train in trains)
        {
            // 열차가 null이거나 체력 없으면 패스
            if (train == null || train.CurrentHp <= 0) continue;

            // 거리 체크
            float distance = Vector3.Distance(transform.position, train.transform.position);
            if (distance < minDistance)
            {
                // 최소 거리 갱신
                minDistance = distance;
                bestTarget = train;
            }
        }

        // 최종 타겟 설정
        _targetTrain = bestTarget;
    }


    // 공격, 투사체 발사
    private void Fire()
    {
        // 공격 쿨타임 갱신
        _lastAttackTime = Time.time;

        // 모든 클라이언트에게 투사체 만들라고 요청
        photonView.RPC(nameof(RPC_Fire), RpcTarget.All, _firePoint.position, transform.rotation);
    }


    // 투사체 발사 RPC
    [PunRPC]
    private void RPC_Fire(Vector3 pos, Quaternion rot)
    {
        // 로컬로 투사체 생성
        PoolableObject projectile = PoolManager.Instance.Spawn(_projectilePrefab, pos, rot);
    }
}
