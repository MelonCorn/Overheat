using UnityEngine;

public enum WeaponType
{
    None,
    Revolver,       // 리볼버
    SMG,            // 기관단총
    Shotgun,        // 샷건
    BoltAction,     // 볼트액션
    Welder,         // 용접기
    Extinguisher,   // 소화기
}
public enum ImpactType
{
    None,       
    Enemy,      // 적
    TrainFloor, // 열차 바닥
    Ground,     // 땅 (모래)
    Defualt,    // 나머지 기본
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

    [Header("카메라 반동 설정")]
    public float recoilX = 2f;           // 위로 튀는 힘 (X)
    public float recoilY = 1f;           // 좌우 랜덤 힘 (Y)
    public float maxRecoilX = 10f;       // 반동 최대치
    public float snappiness = 6f;        // 반동이 적용되는 속도
    public float returnSpeed = 2f;       // 원점으로 돌아오는 속도

    [Header("비주얼 반동")]
    public Vector3 visualRecoilAngle = new Vector3(-10f, 0f, 0f); // 반동 각도 (-X가 위쪽으로 회전임)
    public float visualRecoilSnappiness = 15f;                    // 반동 적용 속도
    public float visualRecoilReturnSpeed = 10f;                   // 복귀 속도

    [Header("산탄 설정")]
    public int pelletCount = 1;      // 발사체 개수
    [Range(0f, 20f)]
    public float spreadAngle = 0f;   // 탄퍼짐 정도

    [Header("발사 클립")]
    public AudioClip[] fireClips;

    [Header("이펙트 설정")]
    public PoolableObject impactEnemyEffect;        // 적        (피)
    public PoolableObject impactTrainFloorEffect;   // 열차 바닥 (나무)
    public PoolableObject impactGroundEffect;       // 땅        (모래)
    public PoolableObject impactDefaultEffect;      // 기본      (스파크)

    [Header("도구 설정")]
    public bool isRepairTool = false; // 체크하면 수리 도구로 작동


    // 발사 클립
    public AudioClip GetFireClip()
    {
        if (fireClips != null && fireClips.Length > 0)
            return fireClips[Random.Range(0, fireClips.Length)];
        return null;
    }
}
