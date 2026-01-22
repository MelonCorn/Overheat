using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;


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

    private NavMeshAgent _agent;


    private State _state = State.Approach;  // 현재 상태
    private Transform _targetWindow;        // 침투할 창문

    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponent<NavMeshAgent>();
    }
    protected override void OnEnable()
    {
        base.OnEnable();

        // 다시 활성화 되었을 때
        if (_agent != null)
        {
            _agent.enabled = true;
            _agent.isStopped = false;
        }

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


    // 행동
    protected override void Think()
    {

    }
}
