using UnityEngine;

public enum WeaponType
{
    None,
    Extinguisher,
}


[CreateAssetMenu(fileName = "New Weapon", menuName = "Shop/Weapon Item")]
public class WeaponData : PlayerItemData
{
    [Header("무기 설정")]
    public int damage = 10;        // 공격력 (수리 도구면 수리량)
    public float range = 50f;      // 사거리 (근접 0 <--> n 원거리)
    public float fireRate = 0.5f;  // 공격 속도 (초당 발사 간격)
    public bool isAuto = false;    // 연사 가능 여부
    public LayerMask hitLayer;     // 타겟 설정
    public WeaponType type;        // 타입

    [Header("도구 설정")]
    public bool isRepairTool = false; // 체크하면 수리 도구로 작동
}
