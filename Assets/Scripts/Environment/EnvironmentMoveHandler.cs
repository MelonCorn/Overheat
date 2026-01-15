using UnityEngine;

// 환경 오브젝트는 반드시 PoolableObject
[RequireComponent(typeof(PoolableObject))]
public class EnvironmentMoveHandler : MonoBehaviour
{
    private PoolableObject _poolable;   // 자신의 풀 관리자
    private float _speed;               // 이동 속도
    private float _despawnZ;            // 반납 Z 값

    private void Awake()
    {
        _poolable = GetComponent<PoolableObject>();
    }

    // 수치 설정
    public void Setup(float speed, float despawnZ)
    {
        _speed = speed;
        _despawnZ = despawnZ;
    }

    private void Update()
    {
        // 이동
        transform.Translate(Vector3.back * _speed * Time.deltaTime);

        // 뒤로 넘어가면 반납
        if (transform.position.z < _despawnZ)
            _poolable.Release();
    }
}
