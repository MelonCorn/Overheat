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
public class TrainEngineDataSO : TrainDataSO
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
}
