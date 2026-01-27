using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct EngineLevelData
{
    public float maxSpeed;        // 최고 속도
    public float minSpeed;        // 최저 속도 (기본 속도)
    public float maxFuel;         // 최대 연료
    public float burnRate;        // 초당 연료 소모
    public float accel;           // 가속도
}

[CreateAssetMenu(fileName = "EngineData", menuName = "Train/Engine Data")]
public class TrainEngineData : TrainData
{
    [Header("엔진 레벨 정보")]
    public List<EngineLevelData> engineLevelData;

    // 레벨에 맞는 기본 스탯 반환
    public EngineLevelData GetEngineStat(int level)
    {
        // 인덱스 : 인풋 (0 ~ 최대 데이터 수)
        int index = Mathf.Clamp(level - 1, 0, engineLevelData.Count - 1);
        return engineLevelData[index];
    }


    // 기본 열차 업그레이드 정보 가지고 추가로
    public override List<(string name, string value)> GetUpgradeInfos(int level)
    {
        var stats = base.GetUpgradeInfos(level);

        EngineLevelData current = GetEngineStat(level);
        bool isMax = IsMaxLevel(level);

        if (isMax)
        {
            stats.Add(("최고 속도", $"{current.maxSpeed}"));
            stats.Add(("최저 속도", $"{current.minSpeed}"));
            stats.Add(("가속도", $"{current.accel}"));
            stats.Add(("보일러 용량", $"{current.maxFuel}"));
            stats.Add(("초당 연료 소모", $"{current.burnRate}"));
        }
        else
        {
            EngineLevelData next = GetEngineStat(level + 1);

            // 속도
            stats.Add(("최고 속도", GetNestStatString(next.maxSpeed, next.maxSpeed - current.maxSpeed)));
            stats.Add(("최저 속도", GetNestStatString(next.minSpeed, next.minSpeed - current.minSpeed)));
            stats.Add(("가속도", GetNestStatString(next.accel, next.accel - current.accel)));
            stats.Add(("보일러 용량", GetNestStatString(next.maxFuel, next.maxFuel - current.maxFuel)));
            stats.Add(("초당 연료 소모", GetNestStatString(next.burnRate, next.burnRate - current.burnRate)));
        }

        return stats;
    }
}
