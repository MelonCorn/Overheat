using UnityEngine;

[CreateAssetMenu(fileName = "NewTurretAudioData", menuName = "AudioData/Turret Audio")]
public class TurretAudioData : TrainAudioData
{
    [Header("발사")]
    public AudioClip[] FireClips;

    // 랜덤 발사
    public AudioClip GetRandomFireClip()
    {
        if (FireClips == null || FireClips.Length == 0) return null;
        return FireClips[Random.Range(0, FireClips.Length)];
    }
}
