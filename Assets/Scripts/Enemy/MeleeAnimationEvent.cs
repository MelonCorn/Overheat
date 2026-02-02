using UnityEngine;

public class MeleeAnimationEvent : MonoBehaviour
{
    private EnemyMelee _enemy;

    private void Awake()
    {
        _enemy = GetComponentInParent<EnemyMelee>();
    }

    // 접근 발자국 애니메이션 이벤트
    public void OnApproachFootstep()
    {
        if (_enemy.GetAudioSource != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayOneShot3D(_enemy.GetAudioSource, _enemy.AudioData.GetApproachClip());
        }
    }
    
    // 추적 발자국 애니메이션 이벤트
    public void OnChaseFootstep()
    {
        if (_enemy.GetAudioSource != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayOneShot3D(_enemy.GetAudioSource, _enemy.AudioData.GetChaseClip());
        }
    }
    // 등반 애니메이션 이벤트
    public void OnClimb()
    {
        if (_enemy.GetAudioSource != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayOneShot3D(_enemy.GetAudioSource, _enemy.AudioData.GetClimbClip());
        }
    }

    // 착지 애니메이션 이벤트
    public void OnLand()
    {
        if (_enemy.GetAudioSource != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayOneShot3D(_enemy.GetAudioSource, _enemy.AudioData.GetLandClip());
        }
    }

    // 공격 애니메이션 이벤트
    public void OnAttack()
    {
        if (_enemy.GetAudioSource != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayOneShot3D(_enemy.GetAudioSource, _enemy.AudioData.GetAttackClip());
        }
    }
}
