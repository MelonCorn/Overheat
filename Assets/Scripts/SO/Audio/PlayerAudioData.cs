using UnityEngine;

[CreateAssetMenu(fileName = "NewPlayerAudioData", menuName = "AudioData/Player Audio")]
public class PlayerAudioData : ScriptableObject
{
    [Header("발자국 사운드")]
    public AudioClip[] walkGroundClips;
    public AudioClip[] walkTrainClips;

    [Header("점프 사운드")]
    public AudioClip[] jumpGroundClips;
    public AudioClip[] jumpTrainClips;

    [Header("착지 사운드")]
    public AudioClip[] landGroundClips;
    public AudioClip[] landTrainClips;

    [Header("스왑 사운드")]
    public AudioClip[] itemSwapClips;

    [Header("전투 사운드")]
    public AudioClip[] hitClips;
    public AudioClip dieClip;


    // 타입에 맞는 발자국 소리 반환
    public AudioClip GetClip(FootStepSoundType type, bool isTrain)
    {
        AudioClip[] targetArray = null;

        switch (type)
        {
            case FootStepSoundType.Walk:
                targetArray = isTrain ? walkTrainClips : walkGroundClips;
                break;
            case FootStepSoundType.Jump:
                targetArray = isTrain ? jumpTrainClips : jumpGroundClips;
                break;
            case FootStepSoundType.Land:
                targetArray = isTrain ? landTrainClips : landGroundClips;
                break;
        }

        // 배열에서 랜덤
        if (targetArray != null && targetArray.Length > 0)
        {
            return targetArray[Random.Range(0, targetArray.Length)];
        }

        return null;
    }

    // 피격용
    public AudioClip GetHitClip()
    {
        if (hitClips != null && hitClips.Length > 0)
            return hitClips[Random.Range(0, hitClips.Length)];
        return null;
    }
    
    // 스왑
    public AudioClip GetSwapClip()
    {
        if (itemSwapClips != null && itemSwapClips.Length > 0)
            return itemSwapClips[Random.Range(0, itemSwapClips.Length)];
        return null;
    }
}
