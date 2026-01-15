using UnityEngine;

public class EngineNode : TrainNode
{
    [Header("엔진 스탯")]
    protected float _maxSpeep;         // 최대 속도
    protected float _currentSpeed;     // 현재 속도
    protected float fuelConsumption;   // 연료 효율


    public override void Init(TrainDataSO data, int level)
    {
        base.Init(data, level);

        SetData(level);
    }


    public override void Upgrade(int level)
    {
        base.Upgrade(level);

        SetData(level);
    }


    // 레벨 데이터 설정
    private void SetData(int level)
    {
        if (Data is TrainEngineDataSO engineData)
        {
            // 레벨의 스탯
            var engineStat = engineData.GetEngineStat(level);

            // 속도
            _maxSpeep = engineStat.maxSpeed;
            // 효율
            fuelConsumption = engineStat.fuelConsumption;
        }
    }
}
