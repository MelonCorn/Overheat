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

    private TrainNode _targetTrain;                // 목표 열차

    private Vector3 _targetCenter;                 // 열차의 중앙
    private Vector3 _randomPoint;                  // 열차의 랜덤 포인트

    private Vector3 _currentVelocity;              // SmoothDamp 계산용

    protected override void OnEnable()
    {
        base.OnEnable();

        if (PhotonNetwork.IsMasterClient == false) return;

        // 랜덤 열차 가져오기
        GetRandomTrain();

    }


    // 생각
    protected override void Think()
    {
        // 타겟 체크
        // 타겟이 null이거나 체력이 없으면
        if (_targetTrain == null || _targetTrain.CurrentHp <= 0)
        {
            // 다른 랜덤 열차 가져오기
            GetRandomTrain();

            // 열차 없으면 계속 대기
            if (_targetTrain == null) return;
        }

        // 거리 계산 (자신, 적)
        float distance = Vector3.Distance(transform.position, _targetCenter);

        // 행동
        if (distance > _shootRange)
        {
            // 사거리보다 멀면 타겟에 접근
            MoveToTarget();
        }
        else
        {
            // 사거리 안이면 멈추고 발사
            // 랜덤 포인트 바라보기는 계속
            LookAtTarget(_randomPoint);

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
        // 정지 위치를 사거리 살짝 안으로 지정해서 SmoothDamp 잘 작동되게
        // 그냥 쌩으로 열차 위치 쓰면 감속 전에 멈춤
        Vector3 dir = (transform.position - _targetCenter).normalized;
        Vector3 stopPoint = _targetCenter + (dir * (_shootRange - 0.3f));

        // 높이 고정
        stopPoint.y = transform.position.y;

        // 부드럽게 이동
        transform.position = Vector3.SmoothDamp(transform.position, stopPoint, ref _currentVelocity, _smoothTime, _flySpeed);

        // 타겟으로 회전
        LookAtTarget(_targetCenter);
    }



    // 타겟으로 회전
    private void LookAtTarget(Vector3 targetPos)
    {
        // 방향 (타겟 포인트 - 자신)
        Vector3 dir = (targetPos - transform.position).normalized;

        // 위아래로는 안움직이고 몸통만
        dir.y = 0; 

        if (dir != Vector3.zero)
        {
            // 부드럽게 회전
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * _rotSpeed);
        }
    }

    //// 가장 가까운 열차 찾기
    //private void FindClosestTrain()
    //{
    //    if (TrainManager.Instance == null) return;

    //    // 열차 노드 리스트 가져오기
    //    var trains = TrainManager.Instance.TrainNodes;

    //    // 최소 거리 (비교)
    //    float minDistance = float.MaxValue;

    //    // 제일 가까운 열차 노드
    //    TrainNode bestTarget = null;

    //    // 열차 노드 순회
    //    foreach (var train in trains)
    //    {
    //        // 열차가 null이거나 체력 없으면 패스
    //        if (train == null || train.CurrentHp <= 0) continue;

    //        // 거리 체크
    //        float distance = Vector3.Distance(transform.position, train.transform.position);
    //        if (distance < minDistance)
    //        {
    //            // 최소 거리 갱신
    //            minDistance = distance;
    //            bestTarget = train;
    //        }
    //    }

    //    // 최종 타겟 설정
    //    _targetTrain = bestTarget;

    //    // 열차의 랜덤 위치
    //    if (_targetTrain != null)
    //    {
    //        _targetCenter = _targetTrain.GetCenter();
    //        _randomPoint = _targetTrain.GetRandomPoint();
    //    }
    //}


    // 랜덤 열차 반환
    private void GetRandomTrain()
    {
        if (TrainManager.Instance == null) return;

        // 열차 노드 리스트 가져오기
        var trains = TrainManager.Instance.TrainNodes;

        // 리스트 비었으면 중단 (방어)
        if (trains == null || trains.Count == 0) return;

        // 랜덤 열차 가져오기 10번 시도
        for (int i = 0; i < 10; i++)
        {
            int randIndex = Random.Range(0, trains.Count);
            TrainNode train = trains[randIndex];

            // 살아있는지 체크
            if (train != null && train.CurrentHp > 0)
            {
                // 살아있으면 타겟 지정 후 중단
                SetTarget(train);
                return;
            }
        }


        // 세상에 이런일이
        // 어쩌다보니 열차 null이나 체력 없는게 너무 많아서 여기로 넘어옴
        // 그냥 앞에서부터 검색해서 살아있는 열차 가져옴
        foreach (var train in trains)
        {
            if (train != null && train.CurrentHp > 0)
            {
                SetTarget(train);
                return;
            }
        }

        // 여기까지 왔으면 진짜 살아있는 객체가 하나도 없는 거임
        _targetTrain = null;
    }

    // 타겟 열차 설정
    private void SetTarget(TrainNode train)
    {
        _targetTrain = train;
        _targetCenter = _targetTrain.GetCenter();
        _randomPoint = _targetTrain.GetRandomPoint();
    }

    // 공격, 투사체 발사
    private void Fire()
    {
        // 공격 쿨타임 갱신
        _lastAttackTime = Time.time;

        // 몸은 다 안돌아갔을 수 있으니까 투사체 방향 바로 해주기
        Vector3 fireDir = (_randomPoint - _firePoint.position).normalized;
        Quaternion fireRot = Quaternion.LookRotation(fireDir);

        // 모든 클라이언트에게 투사체 만들라고 요청
        photonView.RPC(nameof(RPC_Fire), RpcTarget.All, _firePoint.position, fireRot);

        // 공격 후 열차의 랜덤 위치 리타겟
        _randomPoint = _targetTrain.GetRandomPoint();
    }


    // 투사체 발사 RPC
    [PunRPC]
    private void RPC_Fire(Vector3 pos, Quaternion rot)
    {
        // 로컬로 투사체 생성
        PoolableObject projectile = PoolManager.Instance.Spawn(_projectilePrefab, pos, rot);
    }
}
