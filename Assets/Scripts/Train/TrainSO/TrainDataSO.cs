using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct BasicLevelData
{
    public int maxHP;          // 체력
    public int upgradePrice;   // 업그레이드 비용
}

[CreateAssetMenu(fileName = "TrainData", menuName = "Train/Basic Data")]
public class TrainDataSO : ScriptableObject
{
    [Header("공통")]
    public TrainType type;      // 타입
    public string trainName;    // 이름
    public TrainNode prefab;    // 프리팹 상점용, name 붙이면 네트워크

    [Header("기본 레벨 정보")]
    public List<BasicLevelData> levelDatas;    // 업그레이드 정보


    // 레벨에 맞는 기본 스탯 반환
    public BasicLevelData GetBasicStat(int level)
    {
        // 인덱스 : 인풋 (0 ~ 최대 데이터 수)
        int index = Mathf.Clamp(level - 1, 0, levelDatas.Count - 1);
        return levelDatas[index];
    }
}
