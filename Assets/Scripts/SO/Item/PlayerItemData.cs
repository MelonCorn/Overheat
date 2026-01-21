using UnityEngine;

public abstract class PlayerItemData : ShopItem
{
    [Header("프리팹")]
    public GameObject prefab;    // name 붙여서 네트워크 생성용
}