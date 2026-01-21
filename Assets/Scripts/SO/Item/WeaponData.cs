using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Shop/Weapon Item")]
public class WeaponData : MonoBehaviour
{
    [Header("무기 설정")]
    public int damage = 10;        // 공격력
    public float range = 50f;      // 사거리 (근접 0 <--> n 원거리)
    public float fireRate = 0.5f;  // 공격 속도 (초당 발사 간격)
    public bool isAuto = false;    // 연사 가능 여부
}
