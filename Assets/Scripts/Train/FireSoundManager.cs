using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FireSoundManager : MonoBehaviour
{
    public static FireSoundManager Instance;

    private AudioSource _fireAudioSource;

    [Header("설정")]
    [SerializeField] float _updateInterval = 0.1f; // 갱신 주기

    // 현재 켜져 있는 모든 불
    private List<Transform> _activeFires = new List<Transform>();

    private Vector3 _targetPosition;     // 목표 위치
    private float _lastUpdateTime = -1f; // 마지막 갱신 시간

    private void Awake()
    {
        Instance = this;

        _fireAudioSource = GetComponent<AudioSource>();

        // 까먹을까봐
        _fireAudioSource.loop = true;
        _fireAudioSource.spatialBlend = 1.0f;
        _fireAudioSource.Stop();
    }

    private void Update()
    {
        // 플레이어 있어야만 작동
        if (PlayerHandler.localPlayer == null) return;

        // 일정 시간마다 체크
        if (Time.time >= _lastUpdateTime + _updateInterval)
        {
            // 시간 갱신
            _lastUpdateTime = Time.time;

            // 소리 갱신
            UpdateFireSound();
        }

        // 목표 위치로 이동
        transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * 5f);
    }

    // 가장 가까운 불 찾아서 이동
    void UpdateFireSound()
    {
        // 불 없고
        if (_activeFires.Count == 0)
        {
            // 재생중이면 끔
            if (_fireAudioSource.isPlaying) _fireAudioSource.Stop();
            return;
        }

        // 불이 있으면 소리 킴
        if (!_fireAudioSource.isPlaying) _fireAudioSource.Play();

        // 가장 가까운 불
        Transform closestFire = null;
        float minDistance = float.MaxValue;

        // 플레이어 위치
        Vector3 playerPos = PlayerHandler.localPlayer.transform.position;

        // 가장 가까운 불 찾기
        foreach (var fire in _activeFires)
        {
            // 널 패스
            if (fire == null) continue;

            // 거리 체크
            float distance = Vector3.Distance(playerPos, fire.position);

            // 가까운 거리 갱신
            if (distance < minDistance)
            {
                minDistance = distance;
                closestFire = fire;
            }
        }

        if (closestFire != null)
        {
            // 목표 위치 설정
            _targetPosition = closestFire.position;
        }
    }

    // 불 생성될 때 등록
    public void RegisterFire(Transform fire)
    {
        if (_activeFires.Contains(fire) == false)
            _activeFires.Add(fire);
    }

    // 불 꺼질 때 해제
    public void UnregisterFire(Transform fire)
    {
        if (_activeFires.Contains(fire))
            _activeFires.Remove(fire);
    }
}
