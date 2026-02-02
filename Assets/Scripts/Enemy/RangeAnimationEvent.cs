using UnityEngine;

public class RangeAnimationEvent : MonoBehaviour
{
    private EnemyRange _enemy;

    private void Awake()
    {
        _enemy = GetComponentInParent<EnemyRange>();
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
