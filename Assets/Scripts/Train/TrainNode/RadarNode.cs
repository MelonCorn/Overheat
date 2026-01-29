using System;
using UnityEngine;

public class RadarNode : TrainNode
{
    public static event Action<bool> OnRadarStateChanged;

    // 현재 활성화된 레이더 개수
    // 1 개라도 있으면 레이더 활성화
    // 0 개면 레이더 비활성화
    private static int _activeRadarCount = 0;

    public override void Init(TrainData data, int level)
    {
        base.Init(data, level);

        // 생성되면 등록
        _activeRadarCount++;

        // 레이더 체크
        UpdateRadarUI();
    }

    private void OnDisable()
    {
        // 감소
        if (_activeRadarCount > 0)
        {
            _activeRadarCount--;

            // 레이더 체크
            UpdateRadarUI();
        }
    }

    private void UpdateRadarUI()
    {
        // 레이더 한개 이상 있는지
        bool isRadarActive = _activeRadarCount > 0;

        Debug.Log($"[RadarNode] 활성 레이더 수: {_activeRadarCount}, 미니맵 상태: {isRadarActive}");

        // 상태 전환 실행
        OnRadarStateChanged?.Invoke(isRadarActive);
    }
}
