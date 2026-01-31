using UnityEngine;

public abstract class PlayerItemData : ShopItem
{
    [Header("프리팹")]
    public GameObject prefab;    // name 붙여서 네트워크 생성용

    [Header("1인칭 장착 보정")]
    public Vector3 handPosOffset; // 위치 오프셋 (0, -0.2, 0)
    public Vector3 handRotOffset; // 회전 오프셋

    [Header("3인칭 장착 보정")]
    public Vector3 tpsHandPosOffset;
    public Vector3 tpsHandRotOffset;
}