using UnityEngine;

[CreateAssetMenu(fileName = "NewObjectAudioData", menuName = "AudioData/Object Audio")]
public class ObjectAudioData : ScriptableObject
{
    [Header("아무 사운드")]
    public AudioClip[] objectClips;

    // 랜덤 클립 반환
    public AudioClip GetRandomClip()
    {
        if (objectClips != null && objectClips.Length > 0)
            return objectClips[Random.Range(0, objectClips.Length)];

        return null;
    }
}
