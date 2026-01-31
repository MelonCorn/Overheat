using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PoolableObject))]
[RequireComponent(typeof(AudioSource))]
public class ImpactEffect : MonoBehaviour
{
    private PoolableObject _poolObj;
    private AudioSource _audioSource;
    private ParticleSystem[] _particles;        // 자식의모든 파티클

    [Header("오디오 클립")]
    [SerializeField] private ObjectAudioData _audioData;

    private void Awake()
    {
        _poolObj = GetComponent<PoolableObject>();
        _particles = GetComponentsInChildren<ParticleSystem>();
        _audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        // 파티클 재생
        foreach(var particle in _particles)
            if (particle != null) particle.Play();

        // 사운드 매니저 재생 요청 (3D + 쿨타임 적용)
        if (_audioSource != null && _audioData != null && SoundManager.Instance != null)
        {
            Debug.Log("임팩트 소리 재생");
            SoundManager.Instance.PlayOneShot3D(_audioSource, _audioData.GetRandomClip());
        }

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

            // 파티클이 하나라도 살아있는지 확인
            foreach (var particle in _particles)
                if (particle != null && particle.isPlaying) isPlaying = true;

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
