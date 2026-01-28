using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PoolableObject))]
public class ImpactEffect : MonoBehaviour
{
    private PoolableObject _poolObj;
    private ParticleSystem _particle;
    private AudioSource _audioSource;

    [Header("오디오 클립")]
    [SerializeField] private AudioClip _audioClip;

    private void Awake()
    {
        _poolObj = GetComponent<PoolableObject>();
        _particle = GetComponent<ParticleSystem>();
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        // 파티클 재생
        if (_particle != null) _particle.Play();

        //// 사운드 매니저 재생 요청 (3D + 쿨타임 적용)
        //if (_audioSource != null && _audioClip != null && SoundManager.Instance != null)
        //{
        //    SoundManager.Instance.PlayOneShot3D(_audioSource, _audioClip);
        //}

        // 수명 체크
        StartCoroutine(CheckAlive());
    }

    // 수명 체크
    private IEnumerator CheckAlive()
    {
        yield return null; // 초기화 대기

        while (true)
        {
            bool isPlaying = false;

            // 파티클이 살아있는지 확인
            if (_particle != null && _particle.isPlaying) isPlaying = true;

            // 오디오가 재생 중인지 확인
            if (_audioSource != null && _audioSource.isPlaying) isPlaying = true;

            // 둘 다 끝났으면 종료
            if (isPlaying == false) break;

            // 조금 대기
            yield return new WaitForSeconds(0.5f);
        }

        // 반납
        if (_poolObj != null) _poolObj.Release();
    }
}
