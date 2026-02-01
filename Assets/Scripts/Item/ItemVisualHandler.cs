using Photon.Pun;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(NetworkItem))]
[RequireComponent(typeof(PoolableObject))]
[RequireComponent(typeof(PhotonView))]
public class ItemVisualHandler : MonoBehaviour
{
    [Header("발사 위치")]
    [SerializeField] Transform _muzzlePoint;

    [Header("이펙트 리소스")]
    [SerializeField] PoolableObject _tracerPrefab;      // 총알 궤적 프리팹
    [SerializeField] ParticleSystem _muzzleEffect;      // 화염 연기 등 총구 이펙트
    [SerializeField] PoolableObject _continuousEffect;  // 지속형 이펙트 (사실 소화기)

    [Header("지속용 설정")]
    [SerializeField] AudioSource _muzzleSource;         // 지속용 오디오 소스

    [Header("설정")]
    [SerializeField] bool _isContinuous = false;    // 지속형인지

    private ContinuousParticle _currentEffect;  // 현재 지속 파티클

    // 초기화
    public void Init( bool isLocal)
    {
        // 지속형 설정
        if (_isContinuous)
        {
            // 오디오 소스 할당안되어있으믄
            if (_muzzleSource == null) _muzzleSource.GetComponent<AudioSource>();

            if (isLocal)
            {
                // 로컬은 2d
                _muzzleSource.spatialBlend = 0f;
            }
            else
            {
                // 리모트는 3D
                _muzzleSource.spatialBlend = 1f;
                _muzzleSource.dopplerLevel = 0f;                           // 도플러효과 끄기
            }
        }
    }

   
    // 발사 연출
    public void FireImpact(Vector3 hitPoint, Vector3 hitNormal, PoolableObject impactPrefab)
    { 
        // 단발 총구 이펙트
        if (_muzzleEffect != null)
        {
            _muzzleEffect.transform.position = _muzzlePoint.position;
            _muzzleEffect.Play();
        }

        if (_tracerPrefab != null && PoolManager.Instance != null)
        {
            // 궤적 생성
            var tracerObj = PoolManager.Instance.Spawn(_tracerPrefab, _muzzlePoint.position, Quaternion.identity);
            var tracerScript = tracerObj.GetComponent<BulletTracer>();

            if (tracerScript != null)
            {
                // 궤적 쏘고 도착하면 임팩트 터트림
                // 못맞췄으면 임팩트 null보내서 허공에서 안터트림
                tracerScript.InitAndShoot(_muzzlePoint.position, hitPoint, hitNormal, impactPrefab);
            }
        }
        //StartCoroutine(Fire(hitPoint, hitNormal, impactPrefab));
    }

    private IEnumerator Fire(Vector3 hitPoint, Vector3 hitNormal, PoolableObject impactPrefab)
    {
        // 이번 프레임 끝까지 대기
        // 애니메이션 위치 보정을 LateUpdate에서 하기 떄문에
        // 파티클의 위치가 이상할 수 있음
        yield return GameManager.Instance.EndOfFrame;

       
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

            // 소리 재생
            if (_muzzleSource != null) _muzzleSource.Play();

            if (PoolManager.Instance != null)
            {
                // 풀
                PoolableObject obj = PoolManager.Instance.Spawn(_continuousEffect, null);

                // 지속형 이펙트의 하위 모든 오브젝트 레이어를
                // 무기의 레이어로 변경 (로컬, 리모트 구분) 
                SetLayer(obj.gameObject, gameObject.layer);

                // 로컬 위치 0으로 초기화
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;

                // 스크립트 가져오기
                if(obj.TryGetComponent<ContinuousParticle>(out ContinuousParticle continuousParticle))
                {
                    _currentEffect = continuousParticle;
                    continuousParticle.Play(_muzzlePoint);

                }
            }
        }
    }

    // 지속 이펙트용 레이어 변경 재귀 함수
    private void SetLayer(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayer(child.gameObject, newLayer);
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
                // 소리 중단
                if (_muzzleSource != null) _muzzleSource.Stop();

                // 부모 없애고 반납
                _currentEffect.StopAndRelease();
                _currentEffect = null;
            }
        }
    }

    // 무기 집어넣을 때
    private void OnDisable()
    {
        // 소화기 뿌리는 중에 넣으면
        StopLoop();
    }
}
