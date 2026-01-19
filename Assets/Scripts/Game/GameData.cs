
public static class GameData
{
    public static int Gold = 0;
    public static int SurviveDay = 1;

    public static bool LocalDead = false;   // 로컬 사망

    // 게임 리셋 (타이틀로 돌아갈 때 호출용)
    public static void Reset()
    {
        Gold = 0;
        SurviveDay = 1;
        LocalDead = false;
    }
}
