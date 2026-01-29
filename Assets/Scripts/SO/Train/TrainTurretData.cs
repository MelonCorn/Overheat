using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TurretLevelData
{
    public int damage;           // 공격력
    public float fireRate;       // 공격속도
    public float range;          // 사거리
    public float rotationSpeed;  // 회전 속도
}

[CreateAssetMenu(fileName = "TurretData", menuName = "Train/Turret Data")]
public class TrainTurretData : TrainData
{
    [Header("터렛 레벨 정보")]
    public List<TurretLevelData> turretLevelDatas;


    // 레벨에 맞는 기본 스탯 반환
    public TurretLevelData GetTurretStat(int level)
    {
        // 인덱스 : 인풋 (0 ~ 최대 데이터 수)
        int index = Mathf.Clamp(level - 1, 0, turretLevelDatas.Count - 1);
        return turretLevelDatas[index];
    }


    public override List<(string name, string value)> GetUpgradeInfos(int level)
    {
        var stat =  base.GetUpgradeInfos(level);

        return stat;
    }
}
