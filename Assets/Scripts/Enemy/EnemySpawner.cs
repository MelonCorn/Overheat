using Photon.Pun;
using UnityEngine;

public class EnemySpawner : MonoBehaviourPun
{
    public static int ActiveCount = 0;  // 활성화된 적 수

    [Header("적 프리팹 리소스")]
    [SerializeField] GameObject _meleeEnemy;      // 침투형 (근거리)
    [SerializeField] GameObject _rangeEnemy;      // 파괴형 (원거리)

    [Header("생성 설정")]
    [SerializeField] float _spawnInterval = 5f;  // 생성 주기
    [SerializeField] int _maxEnemyCount = 15;    // 최대 적 수

    [Header("범위 설정")]
    [SerializeField] float _spawnWidth = 20f;    // 적 생성 시 좌우 거리
    [SerializeField] float _flyMinHeight = 1f;   // 파괴형 적 최소 높이
    [SerializeField] float _flyMaxHeight = 5f;   // 파괴형 적 최대 높이

    private float _timer;

    private void Awake()
    {
        // 남아있을 카운트 초기화
        ActiveCount = 0;
    }

    private void Start()
    {
        // 테스트용 즉시 스폰
        Invoke(nameof(TrySpawnEnemy), 2.0f);
    }

    private void Update()
    {
        // 방장만 스폰 관리
        if (PhotonNetwork.IsMasterClient == false) return;

        // 기차 준비 전엔 스폰 안 함
        if (TrainManager.Instance == null || !TrainManager.Instance.IsTrainReady) return;

        // 스폰 타이머 증가
        _timer += Time.deltaTime;

        // 생성 주기보다 이상이면
        if (_timer >= _spawnInterval)
        {
            // 0 할당보다 빼주기
            _timer -= _spawnInterval;

            // 적 생성 시도
            TrySpawnEnemy();
        }
    }


    // 적 생성 시도
    private void TrySpawnEnemy()
    {
        // 현재 맵에 있는 적 숫자 제한
        if (ActiveCount >= _maxEnemyCount) return;

        // 랜덤하게 적 타입 결정 (50:50)
        // 나중에 스테이지 난이도에 따라 확률 조정 가능
        bool isRange = Random.value > 0.5f;

        // 스폰 포인트 선언
        Vector3 spawnPos = Vector3.zero;

        // Z축 위치
        // 엔진부터 꼬리칸 사이
        float zPos = TrainManager.Instance.GetLastZ();

        // 엔진이 (0,0,0)이라 음수
        // rearZ ~ 0 사이 랜덤인데 여유로 5정도 플마
        float randomZ = Random.Range(zPos - 5f, 5f);

        // X축 위치
        // 왼쪽(-) 아니면 오른쪽(+)
        float xPos = (Random.value > 0.5f) ? _spawnWidth : -_spawnWidth;

        float yPos = 0f;

        if (isRange) // 파괴형 (멀리서)
        {
            // 높이 랜덤
            yPos = Random.Range(_flyMinHeight, _flyMaxHeight);

            // 최종 생성 위치
            spawnPos = new Vector3(xPos, yPos, randomZ);

            // 룸 오브젝트로 생성 (방장이 나가도 유지됨)
            PhotonNetwork.InstantiateRoomObject(_rangeEnemy.name, spawnPos, Quaternion.identity);
        }
        else // 침투형 (가까이서)
        {
            // 최종 생성 위치
            spawnPos = new Vector3(xPos, yPos, randomZ);

            // 룸 오브젝트로 생성
            PhotonNetwork.InstantiateRoomObject(_meleeEnemy.name, spawnPos, Quaternion.identity);
        }
    }
}
