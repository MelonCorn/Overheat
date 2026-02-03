using System.Collections;
using UnityEngine;
using static UnityEngine.ParticleSystem;

[RequireComponent(typeof(PoolableObject))]
public class ContinuousParticle : MonoBehaviour
{
    private Transform _targetMuzzle;
    private ParticleSystem _particle;
    private PoolableObject _poolObj;

    private void Awake()
    {
        _particle = GetComponent<ParticleSystem>();
        _poolObj = GetComponent<PoolableObject>();
    }
    public void Play(Transform muzzle)
    {
        _targetMuzzle = muzzle;

        // 켜자마자 총구 위치로
        UpdatePosition();

        // 파티클 재생
        _particle.Play();
    }
    private void LateUpdate()
    {
        if (_targetMuzzle == null) return;

        // LateUpdate 애니메이션 재생 이후 총구 위치로 업데이트
        UpdatePosition();
    }

    // 총구 위치로 항상 고정
    private void UpdatePosition()
    {
        transform.position = _targetMuzzle.position;
        transform.rotation = _targetMuzzle.rotation;
    }

    // 스탑 호출
    public void StopAndRelease()
    {
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

        // 다 끝나고 추적 중단
        _targetMuzzle = null;

        // 풀 반납
        if (_poolObj != null) _poolObj.Release();
        else Destroy(gameObject);
    }
}
