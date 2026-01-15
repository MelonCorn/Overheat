using UnityEngine;

public class EnvironmentSpawner : MonoBehaviour
{
    [Header("환경 프리팹")]
    [SerializeField] PoolableObject[] _prefabs; // 프리팹

    [Header("환경 수치 설정")]
    [SerializeField] float _spawnZ = 80f;                // 생성 Z 위치
    [SerializeField] float _despawnZ = -80f;             // 반납 Z 위치
    [SerializeField] float _spawnDistanceInterval = 10f; // 거리 단위 젠

    [Header("스폰 범위 설정")]
    [SerializeField] float _spawnWidth = 15f;    // 전체 스폰 너비
    [SerializeField] float _trackRadius = 4f;    // 철로 안전 반경

    public float Speed { get; private set; }    // 속도
    public float DespawnZ => _despawnZ;         // 반납 위치 Z

    private float _totalDistance;               // 누적 이동 거리

    private void Update()
    {
        // 배경 속도는 엔진의 속도
        Speed = (TrainManager.Instance != null && TrainManager.Instance.MainEngine != null) ?
            TrainManager.Instance.MainEngine.CurrentSpeed : 0f;

        // 속도가 있을 때
        if (Speed > 0)
        {
            // 이동거리 증가
            _totalDistance += Speed * Time.deltaTime;

            // 누적 이동 거리가 설정한 거리보다 커지면
            if (_totalDistance >= _spawnDistanceInterval)
            {
                // 생성
                SpawnObject();

                // 누적 거리 초기화
                _totalDistance -= _spawnDistanceInterval;
            }
        }
    }

    // 오브젝트 생성
    void SpawnObject()
    {
        // 일단 프리팹 수 만큼 랜덤
        int index = Random.Range(0, _prefabs.Length);

        // 랜덤 프리팹 선정
        PoolableObject selectedPrefab = _prefabs[index];

        // 랜덤 X
        float randomX = 0f;

        // 왼쪽 아니면 오른쪽 결정
        if (Random.value > 0.5f)
        {
            // 철로 반경 ~ 스폰 너비
            randomX = Random.Range(_trackRadius, _spawnWidth);
        }
        else
        {
            // -스폰 너비 ~ - 철로 반경
            randomX = Random.Range(-_spawnWidth, -_trackRadius);
        }

        // 스폰 위치 결정
        Vector3 spawnPos = new Vector3(randomX, 0, _spawnZ);

        // 풀에서 꺼냄
        PoolableObject spawnedObj = PoolManager.Instance.Spawn(selectedPrefab, spawnPos, Quaternion.identity);

        // 움직임 세팅
        var mover = spawnedObj.GetComponent<EnvironmentMoveHandler>();
        if (mover != null)
        {
            // 이동 속도, 반환 Z 설정
            mover.Setup(this);
        }
    }
}
