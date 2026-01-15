using UnityEngine;

public class EnvironmentSpawner : MonoBehaviour
{
    [Header("환경 프리팹")]
    [SerializeField] PoolableObject[] _prefabs; // 프리팹

    [Header("환경 수치 설정")]
    [SerializeField] float _spawnZ = 65f;           // 생성 Z 위치
    [SerializeField] float _despawnZ = -65f;        // 반납 Z 위치
    [SerializeField] float _speed = 20f;            // 속도
    [SerializeField] float _spawnInterval = 0.5f;   // 젠 시간

    [Header("스폰 범위 설정")]
    [SerializeField] float _spawnWidth = 15f;    // 전체 스폰 너비
    [SerializeField] float _trackRadius = 4f;    // 철로 안전 반경

    private float _timer;
    
    private void Update()
    {
        _timer += Time.deltaTime;

        // 타이머가 젠 시간보다 커지면
        if (_timer >= _spawnInterval)
        {
            // 오브젝트 생성
            SpawnObject();
            _timer = 0f;
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
            mover.Setup(_speed, _despawnZ);
        }
    }
}
