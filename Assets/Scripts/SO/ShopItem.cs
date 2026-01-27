using UnityEngine;

public class ShopItem : ScriptableObject
{
    [Header("표시 이름")]
    public string displayName;
    [Header("스크립트용 이름")]
    public string itemName;
    [Header("설명")]
    [TextArea]
    public string desc;
    [Header("가격")]
    public int price;
    [Header("아이콘")]
    public Sprite icon;
}
