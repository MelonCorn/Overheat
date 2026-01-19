using UnityEngine;


[CreateAssetMenu(menuName = "Shop/Fuel Item")]
public class FuelData : PlayerItemData
{
    [Header("연료 충전량 설정")]
    public float _fuelAddAmount = 15f; // 연료 충전량
}
