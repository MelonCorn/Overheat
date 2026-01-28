using System.Collections;
using UnityEngine;

public class ItemVisualHandler : MonoBehaviour
{
    [Header("발사 위치")]
    [SerializeField] Transform _muzzlePoint;

    [Header("이펙트 리소스")]
    [SerializeField] PoolableObject _tracerPrefab;      // 총알 궤적 프리팹
    [SerializeField] PoolableObject _muzzleEffectPrefab;// 화염 연기 소리 등 총구 이펙트

    [Header("피격 이펙트")]
    [SerializeField] PoolableObject _impactWallEffect;  // 벽 파티클
    [SerializeField] PoolableObject _impactEnemyEffect; // 피격 파티클

    [Header("설정")]
    [SerializeField] float _tracerDuration = 0.05f; // 궤적 표시 시간
    [SerializeField] bool _isContinuous = false;    // 지속형인지

    private float _fireInterval = 0.1f;         // 소총 연사 속도

    // 지속전용
    private PoolableObject _currentLoopEffect;  // 현재 재생중인 이펙트
    private Coroutine _fireCoroutine;

    // 발사 속도 설정
    public void SetFireRate(float rate) => _fireInterval = rate;

   
    // 단발 연출
    public void FireImpact(Vector3 hitPoint, int hitType)
    {
        if (_tracerPrefab != null && PoolManager.Instance != null)
        {
            // 궤적 풀에서 꺼내 활서오하
            var tracerObj = PoolManager.Instance.Spawn(_tracerPrefab, _muzzlePoint.position, Quaternion.identity);

            // 궤적에 시작포인트랑 히트포인트 넣고 명령
            var tracerScript = tracerObj.GetComponent<BulletTracer>();
            if (tracerScript != null)
            {
                tracerScript.Show(_muzzlePoint.position, hitPoint);
            }
        }

        //// 피격 이펙트 (기존과 동일)
        //PoolableObject targetEffect = (hitType == 1) ? _bloodImpact : _wallImpact;
        //if (targetEffect != null && PoolManager.Instance != null)
        //{
        //    PoolManager.Instance.Spawn(targetEffect, hitPoint, Quaternion.identity);
        //}
    }

    // 연사 루프 연출
    public void PlayLoop()
    {
        // 지속형인지 (소화기, 용접기)
        if (_isContinuous)
        {
            // 이미 실행중인 이펙트 있으면 무시
            if (_currentLoopEffect != null) return;

            if (_muzzleEffectPrefab != null)
            {
                // 이펙트 꺼내서 총구에 붙임
                _currentLoopEffect = PoolManager.Instance.Spawn(_muzzleEffectPrefab, _muzzlePoint);

                // 위치 초기화
                _currentLoopEffect.transform.localPosition = Vector3.zero;
                _currentLoopEffect.transform.localRotation = Quaternion.identity;
            }
        }
        // 지속형 아니면
        else
        {
            if (_fireCoroutine == null)
            {
                // 발사 코루틴 시작
                _fireCoroutine = StartCoroutine(Fire());
            }
        }
    }

    // 연사 연출 중단
    public void StopLoop()
    {
        // 지속형이면
        if (_isContinuous)
        {
            // 재생중인 이펙트 있을 때
            if (_currentLoopEffect != null)
            {
                // 반납, 비우기
                _currentLoopEffect.Release();
                _currentLoopEffect = null;
            }
        }
        // 아니면
        else
        {
            // 발사 코루틴 중단
            if (_fireCoroutine != null)
            {
                // 중단, 비우기
                StopCoroutine(_fireCoroutine);
                _fireCoroutine = null;
            }
        }
    }

    // 무기 집어넣을 때
    private void OnDisable()
    {
        // 소화기 뿌리는 중에 넣으면
        if (_currentLoopEffect != null)
        {
            // 반납
            _currentLoopEffect.Release();
            _currentLoopEffect = null;
        }

        // 발사 코루틴 정지
        if (_fireCoroutine != null)
        {
            StopCoroutine(_fireCoroutine);
            _fireCoroutine = null;
        }
    }


    // 발사 코루틴
    private IEnumerator Fire()
    {
        // 중단 요청 올 때 까지 무한 반복
        while (true)
        {
            // 불꽃 이펙트 활성화하고
            if (_muzzleEffectPrefab != null)
            {
                PoolManager.Instance.Spawn(_muzzleEffectPrefab, _muzzlePoint.position, _muzzlePoint.rotation);
            }

            // 발사 속도만큼 대기
            yield return new WaitForSeconds(_fireInterval);
        }
    }
}
