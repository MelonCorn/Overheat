using System.Collections.Generic;
using UnityEngine;

public static class GameData
{
    public static int Gold = 0;             // 골드
    public static int SurviveDay = 1;       // 생존일

    public static bool HasStarterItem = false;  // 스타터팩 지급 여부
    public static bool HasMasterStarterItem = false;  // 방장용 스타터팩 지급 여부
    public static bool LocalDead = false;       // 로컬 사망
    public static int LocalCurrentHp = 100;

    public static List<string> LostItems = new List<string>();  // 씬 전환 유실물


    // 게임 리셋 (타이틀로 돌아갈 때 호출용)
    public static void Reset()
    {
        Gold = 0;
        SurviveDay = 1;
        LocalDead = false;
        LocalCurrentHp = 100;
        HasStarterItem = false;
        HasMasterStarterItem = false;
        LostItems.Clear();
    }
}
