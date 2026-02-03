using UnityEngine;

[CreateAssetMenu(fileName = "NewTrainAudioData", menuName = "AudioData/Train Audio")]
public class TrainAudioData : ScriptableObject
{
    [Header("Æø¹ß")]
    public AudioClip[] explodeClips;

    // ·£´ý Æø¹ß
    public AudioClip GetRandomExplodeClip()
    {
        if (explodeClips == null || explodeClips.Length == 0) return null;
        return explodeClips[Random.Range(0, explodeClips.Length)];
    }
}
