using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BasicLevelData
{
    public int maxHP;          // 체력
    public int upgradePrice;   // 업그레이드 비용
}

[CreateAssetMenu(fileName = "TrainData", menuName = "Train/Basic Data")]
public class TrainData : ShopItem
{
    [Header("열차 공통")]
    public TrainType type;      // 타입
    public TrainNode prefab;    // 프리팹 상점용, name 붙이면 네트워크

    [Header("공통 레벨 정보")]
    public List<BasicLevelData> levelDatas;    // 업그레이드 정보

    // 레벨에 맞는 기본 스탯 반환
    public BasicLevelData GetBasicStat(int level)
    {
        // 인덱스 : 인풋 (0 ~ 최대 데이터 수)
        int index = Mathf.Clamp(level - 1, 0, levelDatas.Count - 1);
        return levelDatas[index];
    }

    // 최대 레벨 반환
    public int GetMaxLevel() => levelDatas.Count;

    // 최대 레벨 체크
    public bool IsMaxLevel(int level)
    {
        if (levelDatas == null) return true;
        return level >= levelDatas.Count;
    }


    // 업그레이드 정보 수집용
    public virtual List<(string name, string value)> GetUpgradeInfos(int level)
    {
        // 스탯 정보 튜플
        List<(string statName, string statValue)> stats = new List<(string name, string value)>();

        // 현재 레벨 데이터
        BasicLevelData current = GetBasicStat(level);

        // 최대 레벨 체크
        bool isMax = IsMaxLevel(level);

        if(isMax == true)
        {
            stats.Add(("최대 내구도", $"{current.maxHP}"));
        }
        else
        {
            BasicLevelData next = GetBasicStat(level + 1);

            // 차이 계산
            int diff = next.maxHP - current.maxHP;
                                    // 다음 스탯 문자열
            stats.Add(("최대 내구도", GetNestStatString(next.maxHP, diff)));
        }

        return stats;
    }


    // 다음 스탯 문자열
    protected string GetNestStatString(float nextValue, float diff)
    {
        // 소수점이 있으면 보여주고 없으면 정수
        // 업그레이드 스탯
        string next = nextValue.ToString("0.##");
        // 다음 - 현재 스탯
        string diffStr = diff.ToString("0.##");

        // 변화 없으면 수치만
        if (diff == 0) return next;

        // -는 알아서 가짐
        // 녹색으로
        string sign = diff > 0 ? "+" : "";
        return $"<color=#00FF00>{next} ({sign}{diffStr})</color>";
    }
}
