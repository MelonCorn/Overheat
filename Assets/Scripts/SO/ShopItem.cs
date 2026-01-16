using UnityEngine;

public abstract class ShopItem : ScriptableObject
{
    [Header("이름")]
    public string itemName;
    [Header("설명")]
    public string desc;
    [Header("가격")]
    public int price;
    [Header("아이콘")]
    public Sprite icon;

    // 결제 시 실행
    public abstract void Purchase(ShopItem item);
}
