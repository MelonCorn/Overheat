using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    // Key : 프리팹, Value : 프리팹 풀
    private Dictionary<PoolableObject, ObjectPool<PoolableObject>> _pools = new Dictionary<PoolableObject, ObjectPool<PoolableObject>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // 풀 생성
    private void CreatePool(PoolableObject prefab)
    {
        // 유니티 내장 풀 생성
        ObjectPool<PoolableObject> newPool = new ObjectPool<PoolableObject>(
            createFunc: () => // 풀이 비었을 때 생성
            {
                // 생성
                PoolableObject obj = Instantiate(prefab);
                // 풀매니저 자식으로
                obj.transform.SetParent(transform);

                return obj;
            },
            actionOnGet: (obj) => obj.gameObject.SetActive(true),       // 꺼낼 때 활성화
            actionOnRelease: (obj) => obj.gameObject.SetActive(false),  // 반납할 때 비활성화
            actionOnDestroy: (obj) => Destroy(obj.gameObject),          // 풀이 꽉 찼거나 터질 때 진짜 파괴
                                                              
            defaultCapacity: 10, // 기본 개수
            maxSize: 30          // 최대 개수
        );

        // 딕셔너리에 추가
        _pools.Add(prefab, newPool);
    }


    // 활성화 후 회전, 위치 수정
    public PoolableObject Spawn(PoolableObject prefab, Vector3 position, Quaternion rotation)
    {
        // 오브젝트 가져옴
        PoolableObject obj = GetObject(prefab);

        // 위치 세팅
        obj.transform.position = position;
        obj.transform.rotation = rotation;

        return obj;
    }

    // 그냥 프리팹만 활성화
    public PoolableObject Spawn(PoolableObject prefab)
    {
        return GetObject(prefab);
    }

    // 활성화 후 부모 지정
    public PoolableObject Spawn(PoolableObject prefab, Transform parent)
    {
        // 오브젝트 가져옴
        PoolableObject obj = GetObject(prefab);
        // 부모 지정
        obj.transform.SetParent(parent);

        obj.transform.localPosition = Vector3.zero;

        return obj;
    }



    // 풀에서 오브젝트 가져오기
    private PoolableObject GetObject(PoolableObject prefab)
    {
        // 풀에 키 프리팹 없으면 새로운 풀 생성
        if (_pools.ContainsKey(prefab) == false)
            CreatePool(prefab);

        // prefab 풀
        var pool = _pools[prefab];
        // 오브젝트 꺼내오기
        var obj = pool.Get();
        // 오브젝트 소속 지정
        obj.SetPool(pool);
        return obj;
    }
}
