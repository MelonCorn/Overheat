using System.Collections;
using UnityEngine;

public class ItemVisualHandler : MonoBehaviour
{
    [Header("발사 위치")]
    [SerializeField] Transform _muzzlePoint;

    [Header("이펙트 리소스")]
    [SerializeField] ParticleSystem _muzzleFlash;       // 총구 화염
    [SerializeField] AudioSource _fireAudio;            // 발사 소리
    [SerializeField] LineRenderer _bulletTracer;        // 총알 궤적 (라인 렌더러)
    [SerializeField] PoolableObject _impactWallEffect;  // 벽 파티클
    [SerializeField] PoolableObject _impactEnemyEffect; // 피격 파티클

    [Header("설정")]
    [SerializeField] float _tracerDuration = 0.05f; // 궤적 표시 시간
    [SerializeField] float _fireInterval = 0.1f;    // 소총 연사 속도

    [Header("완전지속형")]
    [SerializeField] bool _isContinuous = false;

    // 단발 발사 (목표 지점)
    public void FireOneShot(Vector3 hitPoint)
    {
        // 소리
        if (_fireAudio != null) _fireAudio.PlayOneShot(_fireAudio.clip);
        // 화염
        if (_muzzleFlash != null) _muzzleFlash.Play();

        // 궤적 그리기 (총구부터 목표지점까지)
        if (_bulletTracer != null && _muzzlePoint != null)
        {
            StartCoroutine(ShowTracer(hitPoint));
        }

        // 피격 이펙트 생성
        if (_impactWallEffect != null && PoolManager.Instance != null)
        {
            // 피격 지점에서 살짝 띄워서 생성 (벽에 파묻힘 방지)
            PoolManager.Instance.Spawn(_impactWallEffect, hitPoint, Quaternion.identity);
        }
    }

    // 궤적을 잠깐 보여주고 끄기
    private IEnumerator ShowTracer(Vector3 hitPoint)
    {
        // 라인렌더러 On
        _bulletTracer.enabled = true;
        _bulletTracer.SetPosition(0, _muzzlePoint.position); // 시작 총구
        _bulletTracer.SetPosition(1, hitPoint);              // 끝 히트포인트

        // 표시 시간 동안 기다리기
        yield return new WaitForSeconds(_tracerDuration);

        // 라인렌더러 Off
        _bulletTracer.enabled = false;
    }

    // 연사 무기 (소총, 소화기)
    public void PlayLoop()
    {
        // 화염 루프
        // 소리
        if (_muzzleFlash != null && _muzzleFlash.isPlaying == false) _muzzleFlash.Play();
        if (_fireAudio != null && _fireAudio.isPlaying == false) _fireAudio.Play();
    }

    // 연사 무기 스탑
    public void StopLoop()
    {
        if (_muzzleFlash != null) _muzzleFlash.Stop();
        if (_fireAudio != null) _fireAudio.Stop();
    }
}
