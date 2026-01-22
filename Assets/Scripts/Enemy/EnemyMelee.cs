using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using System.Collections;


[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMelee : EnemyBase
{
    private enum State // 근접 적 상태
    { 
        Approach,   // 열차 접근
        Climb,      // 열차 등반
        Chase       // 플레이어 추적
    }   

    [Header("침투 행동 설정")]
    [SerializeField] float _runSpeed = 8f;          // 열차 접근 속도
    [SerializeField] float _climbDuration = 1.5f;   // 창문 넘는 시간
    [SerializeField] float _vaultDuration = 0.5f;   // 내부로 착지하는 시간
    [SerializeField] float _chaseRadius = 15f;      // 플레이어 탐지 범위
    [SerializeField] float _retargetInterval = 0.5f;// 타겟 재탐색 간격

    private NavMeshAgent _agent;

    private State _state = State.Approach;  // 현재 상태
    private Transform _targetWindow;        // 침투할 창문

    private float _lastRetargetTime;        // 마지막 리타겟 시간

    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponent<NavMeshAgent>();
    }
    protected override void OnEnable()
    {
        base.OnEnable();

        // 다시 활성화 되었을 때

        // 일단 agent 끄기 
        if (_agent != null) _agent.enabled = false; 

        // 접근 상태
        _state = State.Approach;

        // 가까운 창문 찾기
        FindTargetWindow();
    }

    // 가까운 창문 찾기
    private void FindTargetWindow()
    {
        // 방어
        if (TrainManager.Instance == null) return;

        // 열차 노드 껍데기
        TrainNode targetNode = null;

        // 전체 열차 리스트
        var trains = TrainManager.Instance.TrainNodes;

        // 최소 거리 (비교하면서 줄여야하니까 일단 최대로)
        float minDistance = float.MaxValue;

        // 열차 리스트 순회
        foreach (var train in trains)
        {
            // null, 파괴된 열차 패스
            if (train == null || train.CurrentHp <= 0) continue;

            // 열차와 적 거리 (x는 전부 똑같기 때문에 앞뒤만 비교하면됨)
            float distance = Mathf.Abs(train.transform.position.z - transform.position.z);

            // 최소 거리 갱신
            if (distance < minDistance)
            {
                minDistance = distance;
                targetNode = train;
            }
        }

        // 가장 가까운 창문 위치 할당
        if (targetNode != null)
        {
            _targetWindow = targetNode.GetClosestWindow(transform.position);
        }
    }


    // AI 행동
    protected override void Think()
    {
        switch (_state)
        {
            case State.Approach:
                UpdateApproach();
                break;
            case State.Climb:
                // 코루틴이 동작 중
                break;
            case State.Chase:
                UpdateChase();
                break;
        }
    }

    // 가까운 열차에 접근
    private void UpdateApproach()
    {
        // 타겟 창문 없으면 무시
        if (_targetWindow == null) return;

        // 목표 창문의 X, Z
        // 자신의 Y
        // 창문 바로 아래까지 이동
        Vector3 underWindowPos = new Vector3(_targetWindow.position.x, transform.position.y, _targetWindow.position.z);

        // 이동
        float step = _runSpeed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, underWindowPos, step);

        // 창문 아래 바라보기
        transform.LookAt(underWindowPos);

        // 도착 체크 (창문 아래 도착)
        if (Vector3.Distance(transform.position, underWindowPos) < 0.2f)
        {
            StartCoroutine(Climb());
        }
    }



    // 열차 등반 코루틴
    private IEnumerator Climb()
    {
        // 등반 상태
        _state = State.Climb;

        // 기어오르는 애니메이션 재생
        // anim.SetTrigger("Climb");

        
        // 바닥 -> 창문

        // 바닥
        Vector3 startPos = transform.position;
        // 시간
        float time = 0f;

        // 등반에 걸리는 시간
        while (time < _climbDuration)
        {
            time += Time.deltaTime;
            float t = time / _climbDuration;

            // 창문 위치로 부드럽게 이동
            transform.position = Vector3.Lerp(startPos, _targetWindow.position, t);

            yield return null;
        }

        // 확실히 위치 고정
        transform.position = _targetWindow.position;


        // 창문 -> 열차 내부 바닥

        // 안쪽으로 들어갈 X
        // 창문 기준 열차 방향으로 들이기
        float dirX = (_targetWindow.position.x > 0) ? -1.5f : 1.5f;
        Vector3 targetX = _targetWindow.position + new Vector3(dirX, 0, 0);

        // 열차 침투 목표 지점
        Vector3 endPos = targetX; // 기본값


        // NavMesh 바닥 높이 찾기
        // targetX 위치에서 반경 2f 가장 가까운 NavMesh 바닥 찾기
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetX, out hit, 4f, NavMesh.AllAreas))
        {
            endPos = hit.position; // NavMesh 상의 좌표
        }

        Vector3 ledgePos = transform.position; // 창문 위치
        time = 0f;

        // 열차 바닥 이동 시간
        while (time < _vaultDuration)
        {
            time += Time.deltaTime;
            float t = time / _vaultDuration;

            // 안쪽 바닥으로 부드럽게 이동 아치형
            // SmoothStep로 가속,감속 더 들어감
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            // 부드럽게 이동
            transform.position = Vector3.Lerp(ledgePos, endPos, smoothT);

            yield return null;
        }

        // 확실히 위치 고정
        transform.position = endPos;


        
        // 열차 침투 완료
        // 플레이어 추적 모드 (NavMesh 활성화)
        if (_agent != null)
        {
            _agent.enabled = true;  // 에이전드 이제 활성화
            _agent.Warp(endPos);    // 에이전트 위치 동기화
            _agent.isStopped = false; // 정지
        }
        _state = State.Chase;
    }


    // 추적
    private void UpdateChase()
    {
        // agent null이거
        // NavMesh 위 아니면 무시
        if (_agent == null || _agent.isOnNavMesh == false) return;

        // 공격 쿨타임 체크
        if (Time.time >= _lastAttackTime + _attackRate)
        {
            // 공격 범위 내 플레이어 찾아서 공격 시도
            if (TryAttackInRange())
            {
                // 공격했으면 이번 프레임 이동 스킵
                _agent.SetDestination(transform.position);
                return;
            }
        }

        // 가장 가까운 타겟 갱신
        if (Time.time >= _lastRetargetTime + _retargetInterval)
        {
            FindClosestPlayer();
            _lastRetargetTime = Time.time;
        }

        // 이동
        if (_targetPlayer != null)
        {
            // 타겟이 너무 멀어지면 포기 (재탐색)
            if (Vector3.Distance(transform.position, _targetPlayer.position) > _chaseRadius)
            {
                _targetPlayer = null;
                return;
            }

            _agent.SetDestination(_targetPlayer.position);
        }
    }



    // 가까운 플레이어 탐지
    private void FindClosestPlayer()
    {
        // 현재 존재하는 모든 플레이어
        var players = GameManager.Instance.ActivePlayers;

        // 제일 가까운 거리 (일단 비교를 위해 최대로)
        float minDistance = float.MaxValue;

        // 제일 가까운 타겟
        Transform bestTarget = null;

        // 플레이어마다 체크
        foreach (var player in players)
        {
            // 플레이어 null 이면 패스
            if (player == null) continue;
            // 나중에 사망 상태 생기면
            // if (player.IsDead) continue;

            // 플레이어와 자신의 거리
            float distance = Vector3.Distance(transform.position, player.transform.position);

            // 거리가 더 가깝고
            // 추적 범위 안이면
            if (distance < minDistance && distance <= _chaseRadius)
            {
                // 타겟 갱신
                minDistance = distance;
                bestTarget = player.transform;
            }
        }

        // 최종 타겟 설정
        _targetPlayer = bestTarget;
    }

    // 범위 내 공격 시도
    private bool TryAttackInRange()
    {
        // 모든 플레이어
        var players = GameManager.Instance.ActivePlayers;

        foreach (var player in players)
        {
            if (player == null) continue; // or player.IsDead check

            // 거리 체크
            float distance = Vector3.Distance(transform.position, player.transform.position);

            // 공격 사거리 안이라면
            if (distance <= _attackRange)
            {
                // 타겟 변경
                _targetPlayer = player.transform;

                // 공격 실행
                Attack(player);
                return true;
            }
        }
        return false;
    }

    // 공격
    private void Attack(IDamageable target)
    {
        // 마지막 공격 시간 기록
        _lastAttackTime = Time.time;

        // 공격 애니메이션 재생
        // anim.SetTrigger("Attack");

        // 타겟이 컴포넌트면
        if (target is Component comp)
        {
            // 바라보기
            Vector3 lookPos = new Vector3(comp.transform.position.x, transform.position.y, comp.transform.position.z);
            transform.LookAt(lookPos);
        }

        // 데미지
        target.TakeDamage(_damage);
    }
}
