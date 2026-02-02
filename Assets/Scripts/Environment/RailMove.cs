using UnityEngine;

[RequireComponent(typeof(PoolableObject))]
public class RailMove : MonoBehaviour
{
    [Header("다음 레일이 붙을 위치")]
    [SerializeField] Transform _socket;

    private PoolableObject _poolable;
    private RailSpawner _spawner;

    public Transform Socket => _socket;
    public PoolableObject Poolable => _poolable;

    private void Awake()
    {
        _poolable = GetComponent<PoolableObject>();
    }

    public void Setup(RailSpawner spawner)
    {
        _spawner = spawner;
    }

    private void Update()
    {
        if (_spawner == null) return;

        // 속도 맞춰서 뒤로 이동
        transform.Translate(Vector3.back * _spawner.Speed * Time.deltaTime, Space.World);
    }
}
