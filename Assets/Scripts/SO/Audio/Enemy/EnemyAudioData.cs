using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyAudioData", menuName = "AudioData/Enemy Audio")]
public class EnemyAudioData : ScriptableObject
{
    [Header("°ø°Ý")]
    public AudioClip[] attackClips;

    [Header("»ç¸Á")]
    public AudioClip[] dieClips;


    // °ø°Ý
    public AudioClip GetAttackClip()
    {
        if (attackClips != null && attackClips.Length > 0)
            return attackClips[Random.Range(0, attackClips.Length)];
        return null;
    }

    // »ç¸Á
    public AudioClip GetDieClip()
    {
        if (dieClips != null && dieClips.Length > 0)
            return dieClips[Random.Range(0, dieClips.Length)];
        return null;
    }
}
