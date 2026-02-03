using Photon.Pun;
using System.Collections;
using UnityEngine;

public class EnemyRange : EnemyBase
{
    [Header("비행 행동 설정")]
    [SerializeField] float _flySpeed = 20f;       // 비행 속도
    [SerializeField] float _shootRange = 15f;     // 사거리 (멈출 거리)
    [SerializeField] float _rotSpeed = 5f;        // 회전 속도
    [SerializeField] float _smoothTime = 0.5f;    // 목표 도달 시간

    [Header("비행 높이 제한")]
    [SerializeField] float _minHeight = 0f;
    [SerializeField] float _maxHeight = 10f;

    [Header("무빙 설정")]
    [SerializeField] int _minShots = 3;            // 최소 발사 수
    [SerializeField] int _maxShots = 6;            // 최대 발사 수
    [SerializeField] float _moveRadius = 3f;       // 사격 중 움직임 반경
    [SerializeField] float _movingSpeed = 1f;      // 사격 중 움직임 속도
    [SerializeField] float _movingChance = 0.3f;   // 사격 후 무빙 확률

    [Header("공격 설정")]
    [SerializeField] PoolableObject _projectilePrefab; // 투사체 프리팹
    [SerializeField] Transform _firePoint;             // 발사 위치
    [SerializeField] float _attackDelay = 0.08f;       // 공격 딜레이 (모션 대기용)

    [Header("오디오 데이터")]
    [SerializeField] EnemyAudioData _audioData;

    public EnemyAudioData AudioData => _audioData;

    // 타겟
    private TrainNode _targetTrain;                // 목표 열차
    private Vector3 _targetCenter;                 // 열차의 중앙
    private Vector3 _randomPoint;                  // 열차의 랜덤 포인트

    // 상태
    private Vector3 _currentVelocity;              // SmoothDamp 계산용
    private int _currentShotsLeft;                 // 남은 발사 수
    private bool _isAnchored = false;              // 사격 자리 잡았는지 체크
    private Vector3 _anchorPosition;               // 사격 개시 기준 위치
    private Vector3 _movingTargetPos;             // 현재 무빙 목표 지점

    private Vector3 _prevPos;   // 애니메이션 속도용

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

            // 재장전
            Reload();

            // 사격 기준점 해제
            _isAnchored = false;

            // 열차 없으면 계속 대기
            if (_targetTrain == null) return;
        }

        // 거리 계산 (자신, 적)
        float distance = Vector3.Distance(transform.position, _targetCenter);

        // 자리 잡았으면 사거리 조금 늘려서 추격모드로 잘 안빠지게
        float effectiveRange = (_isAnchored == true) ? _shootRange * 1.3f : _shootRange;


        // 사거리보다 멀면
        if (distance > effectiveRange)
        {
            // 기준점 해제
            _isAnchored = false;

            // 타겟에 접근
            MoveToTarget();
        }
        // 사거리 안이면
        else
        {
            // 그리고 사격 기준점이 안잡혔을 때
            if (_isAnchored == false)
            {
                // 현재 위치 기준점으로 설정
                _anchorPosition = transform.position;

                // 자리 잡음 체크
                _isAnchored = true;

                // 무빙 포인트 새로 잡기
                NewMovingPoint();
            }

            // 무빙
            Moving();

            // 공격 포인트 바라보기
            LookAtTarget(_randomPoint);

            // 공격 쿨타임 체크 후
            if (Time.time >= _lastAttackTime + _attackRate)
            { 
                // 발사
                Fire();
            }
        }

        // 애니메이터 이동 파라미터 값 갱신
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (_animator == null) return;

        // 좌표 차이로 속도 계산
        float currentSpeed = Vector3.Distance(transform.position, _prevPos) / Time.deltaTime;
        _prevPos = transform.position;

        _animator.SetFloat("Speed", currentSpeed);
    }


    // 타겟으로 이동
    private void MoveToTarget()
    {
        // 정지 위치를 사거리 살짝 안으로 지정해서 SmoothDamp 잘 작동되게
        // 그냥 쌩으로 열차 위치 쓰면 감속 전에 멈춤
        Vector3 dir = (transform.position - _targetCenter).normalized;
        Vector3 stopPoint = _targetCenter + (dir * (_shootRange - 0.3f));

        // 높이 고정
        stopPoint.y = Mathf.Clamp(transform.position.y, _minHeight, _maxHeight);

        // 부드럽게 이동
        transform.position = Vector3.SmoothDamp(transform.position, stopPoint, ref _currentVelocity, _smoothTime, _flySpeed);

        // 타겟으로 회전
        LookAtTarget(_targetCenter);
    }

    // 새로운 무빙 포인트 잡기
    private void NewMovingPoint()
    {
        // 자리 안 잡았으면 무시
        if (_isAnchored == false) return;

        // 랜덤 위치
        Vector3 randomOffset = Random.insideUnitSphere * _moveRadius;

        // 기준점에 더해서 최종 위치
        Vector3 finalPos = _anchorPosition + randomOffset;

        // Y 는 제한 걸어두고
        finalPos.y = Mathf.Clamp(finalPos.y, _minHeight, _maxHeight);

        // 목표 지점으로 설정
        _movingTargetPos = finalPos;
    }
    
    // 무빙
    private void Moving()
    {
        // 자리 안 잡았으면 무시
        if (_isAnchored == false) return;

        // 부드럽게 무빙포인트까지 이동
        transform.position = Vector3.Lerp(transform.position, _movingTargetPos, Time.deltaTime * _movingSpeed);
    }

    // 타겟으로 회전
    private void LookAtTarget(Vector3 targetPos)
    {
        // 방향 (타겟 포인트 - 자신)
        Vector3 dir = (targetPos - transform.position).normalized;

        if (dir != Vector3.zero)
        {
            // 부드럽게 회전
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * _rotSpeed);
        }
    }

   

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

    // 재장전
    private void Reload()
    {
        // 타겟 있을 때
        if (_targetTrain == null) return;

        // 장전 수 랜덤
        _currentShotsLeft = Random.Range(_minShots, _maxShots + 1);
    }


    // 공격, 투사체 발사
    private void Fire()
    {
        // 공격 쿨타임 갱신
        _lastAttackTime = Time.time;

        // 공격 애니메이션 선 재생
        _animator.SetTrigger("Attack");

        // 조금 딜레이 뒤에 발사
        StartCoroutine(DelayFire());
    }

    private IEnumerator DelayFire()
    {
        // 공격 모션 대기
        yield return new WaitForSeconds(_attackDelay);

        // 대기중 사망해버리면 취소
        if (IsDead == true || _targetTrain == null || _targetTrain.CurrentHp <= 0) yield break;

        // 몸은 다 안돌아갔을 수 있으니까 투사체 방향 바로 해주기
        Vector3 fireDir = (_randomPoint - _firePoint.position).normalized;
        Quaternion fireRot = Quaternion.LookRotation(fireDir);

        // 모든 클라이언트에게 투사체 만들라고 요청
        photonView.RPC(nameof(RPC_Fire), RpcTarget.All, _firePoint.position, fireRot);

        // 확률로 무빙
        if (Random.value > _movingChance)
            NewMovingPoint();

        // 공격 후 열차의 랜덤 위치 리타겟
        _randomPoint = _targetTrain.GetRandomPoint();

        // 탄 차감
        _currentShotsLeft--;

        // 근데 탄 다 씀
        if (_currentShotsLeft <= 0)
        {
            // 다른 랜덤 열차 가져오기
            GetRandomTrain();

            // 재장전
            Reload();

            // 사격 기준점 해제
            _isAnchored = false;
        }
    }


    // 투사체 발사 RPC
    [PunRPC]
    private void RPC_Fire(Vector3 pos, Quaternion rot)
    {
        // 로컬로 투사체 생성
        PoolableObject projectile = PoolManager.Instance.Spawn(_projectilePrefab, pos, rot);
    }

    protected override void OnDeath()
    {
        // 사망 파티클
        if (_dieParticle != null && PoolManager.Instance != null)
        {
            // 받아두었다가 비활성화될 때 릴리즈
            _currentDeadParticle = PoolManager.Instance.Spawn(_dieParticle, _dieParticlePoint.position, Quaternion.identity);
        }
        
        // 사운드
        if (_audioSource != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayOneShot3D(_audioSource, _audioData.GetDieClip());
        }
    }
}
