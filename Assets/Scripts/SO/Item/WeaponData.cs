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

    [Header("반동 설정")]
    public float recoilX = 2f;           // 위로 튀는 힘 (X)
    public float recoilY = 1f;           // 좌우 랜덤 힘 (Y)
    public float maxRecoilX = 10f;       // 반동 최대치
    public float snappiness = 6f;        // 반동이 적용되는 속도
    public float returnSpeed = 2f;       // 원점으로 돌아오는 속도

    [Header("산탄 설정")]
    public int pelletCount = 1;      // 발사체 개수
    [Range(0f, 10f)]
    public float spreadAngle = 0f;   // 탄퍼짐 정도

    [Header("도구 설정")]
    public bool isRepairTool = false; // 체크하면 수리 도구로 작동
}
