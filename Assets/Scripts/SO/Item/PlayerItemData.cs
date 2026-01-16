using UnityEngine;

public enum ItemType
{
    Potion, // 회복 아이템
    Weapon, // 무기
}


[CreateAssetMenu(menuName = "Shop/Player Item")]
public class PlayerItemData : ShopItem
{
    [Header("아이템 타입")]
    public ItemType itemType;

    public override void Purchase()
    {
    }
}