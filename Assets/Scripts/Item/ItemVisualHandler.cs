using System.Collections;
using UnityEngine;

public class ItemVisualHandler : MonoBehaviour
{
    [Header("발사 위치")]
    [SerializeField] Transform _muzzlePoint;

    [Header("이펙트 리소스")]
    [SerializeField] PoolableObject _tracerPrefab;      // 총알 궤적 프리팹
    [SerializeField] ParticleSystem _muzzleEffect;      // 화염 연기 등 총구 이펙트
    [SerializeField] PoolableObject _continuousEffect;  // 지속형 이펙트 (사실 소화기)
    [SerializeField] AudioClip _muzzleClip;             // 화염 연기 소리 

    [Header("피격 이펙트")]
    [SerializeField] PoolableObject _impactWallEffect;  // 벽 파티클
    [SerializeField] PoolableObject _impactEnemyEffect; // 피격 파티클

    [Header("설정")]
    [SerializeField] bool _isContinuous = false;    // 지속형인지

    private float _fireInterval = 0.1f;         // 소총 연사 속도

    private ContinuousParticle _currentEffect;  // 현재 지속 파티클
    private Coroutine _fireCoroutine;

    // 발사 속도 설정
    public void SetFireRate(float rate) => _fireInterval = rate;

   
    // 발사 연출
    public void FireImpact(Vector3 hitPoint, bool isEnemy, Vector3 hitNormal, bool isHit)
    {
        // 단발 총구 이펙트
        if (_muzzleEffect != null) _muzzleEffect.Play();
        //if (_muzzleClip != null) PlayerHandler.LocalPlayer.PlayAudio(_muzzleClip);

        // 피격 이펙트 결정 (맞췄는지 적인지 아닌지)
        PoolableObject targetEffect = null;
        if (isHit == true)
        {
            targetEffect = (isEnemy == true) ? _impactEnemyEffect : _impactWallEffect;
        }

        if (_tracerPrefab != null && PoolManager.Instance != null)
        {
            // 궤적 생성
            var tracerObj = PoolManager.Instance.Spawn(_tracerPrefab, _muzzlePoint.position, Quaternion.identity);
            var tracerScript = tracerObj.GetComponent<BulletTracer>();

            if (tracerScript != null)
            {
                // 궤적 쏘고 도착하면 타겟 이펙트 터트림
                // 못맞췄으면 임팩트 null보내서 허공에서 안터트림
                tracerScript.InitAndShoot(_muzzlePoint.position, hitPoint, hitNormal, targetEffect);
            }
        }
    }

    // 연사 루프 연출
    public void PlayLoop()
    {
        // 지속형인지 (소화기, 용접기)
        if (_isContinuous)
        {
            // 프리팹 없으면 패스
            if (_continuousEffect == null) return;

            // 이미 쏘고 있으면 패스
            if (_currentEffect != null) return;

            if (PoolManager.Instance != null)
            {
                // 풀에서 총구 하위로
                PoolableObject obj = PoolManager.Instance.Spawn(_continuousEffect, _muzzlePoint);

                // 로컬 위치 0으로 초기화
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;

                // 스크립트 가져오기
                _currentEffect = obj.GetComponent<ContinuousParticle>();
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
            // 뿜고 있던 게 있으면
            if (_currentEffect != null)
            {
                // 부모 없애고 반납
                _currentEffect.StopAndRelease();
                _currentEffect = null;
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
        StopLoop();

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
            //// 불꽃 이펙트 활성화하고
            //if (_muzzleEffectPrefab != null)
            //{
            //    PoolManager.Instance.Spawn(_muzzleEffectPrefab, _muzzlePoint.position, _muzzlePoint.rotation);
            //}

            // 발사 속도만큼 대기
            yield return new WaitForSeconds(_fireInterval);
        }
    }
}
