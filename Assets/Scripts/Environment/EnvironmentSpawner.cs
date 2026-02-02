using System.Collections.Generic;
using UnityEngine;

public class EnvironmentSpawner : MonoBehaviour
{
    [Header("환경 프리팹")]
    [SerializeField] PoolableObject[] _prefabs; // 프리팹

    [Header("환경 수치 설정")]
    [SerializeField] float _spawnZ = 80f;                // 생성 Z 차이
    [SerializeField] float _despawnZ = -80f;             // 반납 Z 차이
    [SerializeField] float _spawnDistanceInterval = 10f; // 거리 단위 젠

    [Header("스폰 범위 설정")]
    [SerializeField] float _spawnWidth = 15f;    // 전체 스폰 너비
    [SerializeField] float _trackRadius = 4f;    // 철로 안전 반경


    // 활성화된 환경 오브젝트
    private List<PoolableObject> _activeEnvironments = new List<PoolableObject>();

    public float Speed { get; private set; }    // 속도

    private float _totalDistance;               // 누적 이동 거리
    private Transform _target;                  // 타겟


    private void Start()
    {
        // 초반에 좀 심어두기
        Repositon();
    }

    private void Update()
    {
        // 타겟 없으면 실행 안함
        if (_target == null)
        {
            // 근데 로컬플레이어 가져오려고 시도해봄
            if (PlayerHandler.localPlayer != null)
            {
                _target = PlayerHandler.localPlayer.transform;
            }
            return;
        }

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


    // 오브젝트 재위치
    public void Repositon()
    {
        // 타겟 재설정 (카메라가 0,0,0으로 갔으면 거기가 기준)
        if (Camera.main != null) _target = Camera.main.transform;
        else _target = transform;

        // 역순 반납
        for (int i = _activeEnvironments.Count - 1; i >= 0; i--)
        {
            if (_activeEnvironments[i] != null && _activeEnvironments[i].gameObject.activeSelf)
            {
                _activeEnvironments[i].Release();
            }
        }

        // 깔끔하게 정리
        _activeEnvironments.Clear();

        // 뒤쪽부터 앞쪽
        float startZ = _target.position.z - _despawnZ;
        float endZ = _target.position.z + _spawnZ;

        // 간격마다 심기
        for (float z = startZ; z <= endZ; z += _spawnDistanceInterval)
        {
            SpawnObjectWithZ(z);
        }

        // 이동 거리 초기화
        _totalDistance = 0f;

        Debug.Log("[배경] 리셋 완료");
    }

    // 특정 Z 위치에 생성
    void SpawnObjectWithZ(float zPos)
    {
        // 랜덤 환경오브젝트
        int index = Random.Range(0, _prefabs.Length);

        // 선택된 오브젝트
        PoolableObject selectedPrefab = _prefabs[index];

        // 랜덤 포지션 X 가운데 비움
        float randomX = 0f;

        // 회전 각도
        float baseYRot = 0f;


        // 반반으로 왼쪽 오른쪽
        if (Random.value > 0.5f)
        {
            randomX = Random.Range(_trackRadius, _spawnWidth);

            // 안쪽 보도록
            baseYRot = -90f;
        }
        else
        {
            randomX = Random.Range(-_spawnWidth, -_trackRadius);

            baseYRot = 90f;
        }

        // 스폰 위치
        Vector3 spawnPos = new Vector3(randomX, transform.position.y, zPos);

        // 랜덤 Y인데 은근 안쪽이어야 함
        float randomOffset = Random.Range(-45f, 45f);
        Quaternion spawnRot = Quaternion.Euler(0f, baseYRot + randomOffset, 0f);

        // 생성
        if (PoolManager.Instance != null)
        {
            PoolableObject spawnedObj = PoolManager.Instance.Spawn(selectedPrefab, spawnPos, spawnRot);
            var mover = spawnedObj.GetComponent<EnvironmentMove>();
            if (mover != null)
            {
                // 초기화
                mover.Setup(this);

                // 리스트에 등록
                _activeEnvironments.Add(spawnedObj);
            }
        }
    }

    // 기존 SpawnObject는 이제 SpawnObjectAtZ를 호출하는 껍데기 역할
    void SpawnObject()
    {
        float targetZ = (_target == null || _target.gameObject.activeInHierarchy == false) ? 0f : _target.position.z;

        // 앞쪽에 생성
        float spawnZ = targetZ + _spawnZ;
        SpawnObjectWithZ(spawnZ);
    }

    // 리스트에서 제거
    public void RemoveFromList(PoolableObject item)
    {
        if (_activeEnvironments.Contains(item))
        {
            _activeEnvironments.Remove(item);
        }
    }

    public float GetDespawnZ()
    {   
        // 로컬 플레이어 업승면 좀 많이 뒤를 잡아두기
        if (_target == null || _target.gameObject.activeInHierarchy == false) return -999f;
        // 로컬 플레이어 있으면 위치 - 디스폰z
        return _target.position.z - _despawnZ;
    }
}
