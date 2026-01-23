using UnityEngine;

// 환경 오브젝트는 반드시 PoolableObject
[RequireComponent(typeof(PoolableObject))]
public class EnvironmentMove : MonoBehaviour
{
    private PoolableObject _poolable;   // 자신의 풀 관리자

    private EnvironmentSpawner _spawner;// 스포너

    private void Awake()
    {
        _poolable = GetComponent<PoolableObject>();
    }

    // 수치 설정
    public void Setup(EnvironmentSpawner spawner)
    {
        _spawner = spawner;
    }

    private void Update()
    {
        // 이동
        transform.Translate(Vector3.back * _spawner.Speed * Time.deltaTime);

        // 뒤로 넘어가면 반납
        if (transform.position.z < _spawner.DespawnZ)
            _poolable.Release();
    }
}
