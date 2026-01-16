using UnityEngine;

public class TurretNode : TrainNode
{
    [Header("포탑 스탯")]
    protected float _damage;       // 공격력
    protected float _fireRate;     // 발사 속도


    public override void Init(TrainData data, int level)
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
        if (Data is TrainTurretData turretData)
        {
            // 레벨의 스탯
            var turretStat = turretData.GetTurretStat(level);

            // 공격력
            _damage = turretStat.damage;
            // 발사 속도
            _fireRate = turretStat.fireRate;
        }
    }
}
