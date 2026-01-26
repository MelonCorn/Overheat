using UnityEngine;
using UnityEngine.Playables;


public enum TimelineType // 타임라인 종류
{
    None,
    Start,              // 대기실, 상점 -> 인게임 (출발)
    GameClear,          // 인게임 -> 상점        (생존)
    ShopArrival,        // 상점 도착             (도착)
    GameOver            // 엔진 파괴             (게임오버)
}

public class TimelineManager : MonoBehaviour
{
    [Header("타임라인")]
    [SerializeField] PlayableDirector _startDirector;       // 출발     (대기실/상점)
    [SerializeField] PlayableDirector _clearDirector;       // 클리어   (인게임)
    [SerializeField] PlayableDirector _gameOverDirector;    // 게임오버 (인게임)
    [SerializeField] PlayableDirector _arrivalDirector;     // 도착     (상점)


    // 타임라인 타입에 맞는 디렉터 반환
    public PlayableDirector GetDirector(TimelineType type)
    {
        switch (type)
        {
            case TimelineType.Start:
                return _startDirector;

            case TimelineType.GameClear:
                return _clearDirector;

            case TimelineType.ShopArrival:
                return _arrivalDirector;

            case TimelineType.GameOver:
                return _gameOverDirector;
        }
        return null;
    }
}
