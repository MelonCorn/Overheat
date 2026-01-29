using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct RadarLevelData
{
    public float range;          // 사거리
    public float rate;           // 주기
}
[CreateAssetMenu(fileName = "RadarData", menuName = "Train/Radar Data")]
public class TrainRadarData : TrainData
{
    [Header("레이더 레벨 정보")]
    public List<RadarLevelData> radarLevelDatas;


    // 레벨에 맞는 기본 스탯 반환
    public RadarLevelData GetRadarStat(int level)
    {
        // 인덱스 : 인풋 (0 ~ 최대 데이터 수)
        int index = Mathf.Clamp(level - 1, 0, radarLevelDatas.Count - 1);
        return radarLevelDatas[index];
    }

    public override List<(string name, string value)> GetUpgradeInfos(int level)
    {
        var stat = base.GetUpgradeInfos(level);

        return stat;
    }
}
