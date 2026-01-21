using UnityEngine;


[CreateAssetMenu(fileName = "New Potion", menuName = "Shop/Potion Item")]
public class PotionData : PlayerItemData
{
    [Header("회복량 설정")]
    public int healAmount = 30;
}
