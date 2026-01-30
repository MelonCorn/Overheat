using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PoolableObject))]
public class ContinuousParticle : MonoBehaviour
{
    private ParticleSystem _particle;
    private PoolableObject _poolObj;

    private void Awake()
    {
        _particle = GetComponent<ParticleSystem>();
        _poolObj = GetComponent<PoolableObject>();
    }

    private void OnEnable()
    {
        // 켜질 때 파티클 재생
        _particle.Play();
    }

    // 스탑 호출
    public void StopAndRelease()
    {
        // 부모 없음
        transform.SetParent(null);

        // 발사 중지
        _particle.Stop();

        // 남은 파티클다 사라질때까지 대기 후 반납
        StartCoroutine(ReleaseDelay());
    }

    private IEnumerator ReleaseDelay()
    {
        // 파티클 사라질때까지 계속 체크
        while (_particle.IsAlive(true))
        {
            yield return new WaitForSeconds(0.5f);
        }

        // 풀 반납
        if (_poolObj != null) _poolObj.Release();
        else Destroy(gameObject);
    }
}
