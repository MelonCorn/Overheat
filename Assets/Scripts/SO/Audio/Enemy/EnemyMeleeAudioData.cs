using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyMeleeAudioData", menuName = "AudioData/EnemyMelee Audio")]
public class EnemyMeleeAudioData : EnemyAudioData
{
    [Header("접근 발자국")]
    public AudioClip[] approachFootStepClips;

    [Header("추적 발자국")]
    public AudioClip[] chaseFootStepClips;

    [Header("등반")]
    public AudioClip[] climbClips;

    [Header("착지")]
    public AudioClip[] landClips;

    // 접근 발자국
    public AudioClip GetApproachClip()
    {
        if (approachFootStepClips != null && approachFootStepClips.Length > 0)
            return approachFootStepClips[Random.Range(0, approachFootStepClips.Length)];
        return null;
    }
    // 추적 발자국
    public AudioClip GetChaseClip()
    {
        if (chaseFootStepClips != null && chaseFootStepClips.Length > 0)
            return chaseFootStepClips[Random.Range(0, chaseFootStepClips.Length)];
        return null;
    }
    // 등반
    public AudioClip GetClimbClip()
    {
        if (climbClips != null && climbClips.Length > 0)
            return climbClips[Random.Range(0, climbClips.Length)];
        return null;
    }
    // 착지
    public AudioClip GetLandClip()
    {
        if (landClips != null && landClips.Length > 0)
            return landClips[Random.Range(0, landClips.Length)];
        return null;
    }
}
