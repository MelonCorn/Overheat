using Photon.Pun;
using UnityEngine;
using UnityEngine.Pool;

public class PoolableObject : MonoBehaviour
{
    // 소속 풀
    private IObjectPool<PoolableObject> _pool;

    // 소속 풀 지정
    public void SetPool(IObjectPool<PoolableObject> pool)
    {
        _pool = pool;
    }

    // 사용이 끝나면 반환
    public void Release()
    {
        if (_pool != null)
        {
            _pool.Release(this);
        }
        else
        {
            // 풀 없으면 그냥 파괴
            Destroy(gameObject);
        }
    }
}
