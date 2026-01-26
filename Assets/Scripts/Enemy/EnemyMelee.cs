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
        Chase,      // 플레이어 추적
        Cross,      // 열차 칸 이동
    }   

    [Header("침투 행동 설정")]
    [SerializeField] float _runSpeed = 8f;          // 열차 접근 속도
    [SerializeField] float _climbDuration = 1.5f;   // 창문 넘는 시간
    [SerializeField] float _vaultDuration = 1f;   // 내부로 착지하는 시간

    [Header("추적 행동 설정")]
    [SerializeField] float _chaseSpeed = 3f;        // 추적 속도
    [SerializeField] float _rotSpeed = 5f;          // 회전 속도
    [SerializeField] float _chaseRadius = 15f;      // 플레이어 탐지 범위
    [SerializeField] float _retargetInterval = 0.5f;// 타겟 재탐색 간격
    [SerializeField] float _wayWidth = 2f;          // 통로 너비

    private NavMeshAgent _agent;

    private State _state = State.Approach;  // 현재 상태
    private Transform _targetWindow;        // 침투할 창문
    private PlayerHandler _targetPlayerHandler; // 타겟 플레이어의 플레이어 핸들러

    private float _lastRetargetTime;        // 마지막 리타겟 시간

    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponent<NavMeshAgent>();
    }
    protected override void OnEnable()
    {
        base.OnEnable();

        if (PhotonNetwork.IsMasterClient == false) return;

        // 다시 활성화 되었을 때
        
        // 에이전트 리셋
        ResetAgent();

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
                // 코루틴 동작 중
                break;

            case State.Chase:
                UpdateChase();
                break;

            case State.Cross:
                // 코루틴 동작 중
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

        // 링크 만나면
        if (_agent.isOnOffMeshLink)
        { 
            // 커스텀 링크 타기
            StartCoroutine(CrossLink());
            // 링크 타는 동안 추적, 공격 중지
            return;
        }

        // 타겟 유효 검사
        if (_target != null)
        {
            // 타겟이 없거나
            // 죽었거나
            // 비활성화 상태라면
            if (_targetPlayerHandler == null ||
                _targetPlayerHandler.IsDead == true ||
                _targetPlayerHandler.gameObject.activeInHierarchy == false)
            {
                // 타겟 해제 후 재탐색
                _target = null; 
            }
        }

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

        // 거리 체크 후 이동
        if (_target != null)
        {
            float distance = Vector3.Distance(transform.position, _target.position);

            // 타겟이 너무 멀어지면 포기 (재탐색)
            if (distance > _chaseRadius)
            {
                _target = null;
                return;
            }

            // 에이전트의 정지 거리
            float stopDistance = _agent.stoppingDistance;

            // 정지 거리보다 안쪽으로 들어왔다면
            if (distance <= stopDistance)
            {
                // 멈춤
                if (_agent.isStopped == false)
                {
                    _agent.isStopped = true;
                }

                // 멈춰있을 때도 플레이어를 바라보게
                // y 제외 방향
                Vector3 dir = (_target.position - transform.position).normalized;
                dir.y = 0;
                // 회전
                if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * _rotSpeed);
            }
            // 정지거리보다 0.5더 멀어지면
            else if (distance > stopDistance + 0.5f)
            {
                // 이동
                if (_agent.isStopped) _agent.isStopped = false;
                _agent.SetDestination(_target.position);
            }
        }
    }


    // 열차 커스텀 링크 건너기 
    private IEnumerator CrossLink()
    {
        _state = State.Cross;

        // 링크 데이터 가져오기
        OffMeshLinkData data = _agent.currentOffMeshLinkData;

        // 시작 위치는 그냥 지금 위치
        Vector3 startPos = transform.position;
        // 링크의 정중앙 도착점을 사용
        Vector3 linkEndPos = data.endPos;

        // 벽 뚫기 방지용 X 제한
        float minX = linkEndPos.x - (_wayWidth * 0.5f);
        float maxX = linkEndPos.x + (_wayWidth * 0.5f);
        float clampedX = Mathf.Clamp(startPos.x, minX, maxX);

        // 도착점의 z사용
        Vector3 targetPos = new Vector3(clampedX, linkEndPos.y, linkEndPos.z);

        // 거리 체크
        while (transform.position != targetPos)
        {
            // 한 프레임 이동 거리
            float step = _chaseSpeed * Time.deltaTime;

            // 타겟으로 이동
            transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

            Vector3 dir = (targetPos - transform.position).normalized;
            if (dir != Vector3.zero)
            {
                //  몸 방향도 이동 방향으로 회전
                Quaternion lookRot = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);
            }

            yield return null;
        }

        // 끝나면 위치 확정
        transform.position = targetPos;
        // 에이전트 강제 동기화
        _agent.Warp(targetPos);

        Vector3 finalDir = (targetPos - startPos).normalized;
        if (finalDir == Vector3.zero) finalDir = transform.forward;

        _agent.velocity = finalDir * _chaseSpeed;

        if (_target != null)
        {
            _agent.SetDestination(_target.position);
        }

        // 이거 쓰면 링크 EndPos로 끌려감
        //_agent.CompleteOffMeshLink();


        // 다시 추적 상태로 복귀
        _state = State.Chase; 
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
            // 사망 상태 패스
            if (player.IsDead) continue;

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
        _target = bestTarget;
        if(_target != null) _targetPlayerHandler = _target.GetComponent<PlayerHandler>();
    }

    // 범위 내 공격 시도
    private bool TryAttackInRange()
    {
        // 모든 플레이어
        var players = GameManager.Instance.ActivePlayers;

        foreach (var player in players)
        {
            // 플레이어 상태 체크 후 패스
            if (player == null) continue;
            if (player.IsDead) continue;

            // 거리 체크
            float distance = Vector3.Distance(transform.position, player.transform.position);

            // 공격 사거리 안이라면
            if (distance <= _attackRange)
            {
                // 타겟 변경
                _target = player.transform;

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


    // 사망 시
    protected override void OnDeath()
    {
        base.OnDeath();

        // 진행 중인 코루틴 강제 종료
        // 창문 넘기, 링크 건너기
        StopAllCoroutines();

        // 에이전트 켜져있고
        // NavMesh 위에 있을 때
        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
        }

        // 에이전트 리셋
        ResetAgent();
    }


    // 에이전트 리셋
    private void ResetAgent()
    {
        if (_agent == null) return;

        // 일단 끄기            
        _agent.enabled = false;

        // 물리적 힘 제거
        _agent.velocity = Vector3.zero;

        // 스탯 초기화
        _agent.speed = _chaseSpeed;
        _agent.autoTraverseOffMeshLink = false;
        _agent.stoppingDistance = Mathf.Max(_attackRange - 0.3f, 0.5f);
    }
}
