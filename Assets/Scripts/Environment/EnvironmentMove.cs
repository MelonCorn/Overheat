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

    private void OnDisable()
    {
        // 비활성화 될때 리스트에서 지우기
        if (_spawner != null)
        {
            _spawner.RemoveFromList(_poolable);
        }
    }

    // 수치 설정
    public void Setup(EnvironmentSpawner spawner)
    {
        _spawner = spawner;
    }

    private void Update()
    {
        // 이동 (월드기준으로 해서 몸 돌아가도 뒤로가게)
        transform.Translate(Vector3.back * _spawner.Speed * Time.deltaTime, Space.World);

        // 디스폰 z 보다 뒤로 넘어가면 반납
        if (transform.position.z < _spawner.GetDespawnZ())
            _poolable.Release();
    }
}
