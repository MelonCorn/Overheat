using System.Collections.Generic;
using UnityEngine;

public class MiniMapHandler : MonoBehaviour
{
    public static MiniMapHandler Instance;

    [Header("UI 연결")]
    [SerializeField] GameObject _minimapUI;        // 미니맵 UI
    [SerializeField] RectTransform _dotsContainer; // 점 찍힐 패널

    [Header("프리팹")]
    [SerializeField] PoolableObject _dotPrefab;  // 점 프리팹

    [Header("설정")]
    [SerializeField] float _mapScale = 2.0f;  // 월드 1m가 UI에서 몇 픽셀인지
    [SerializeField] float _maxRadius = 50f;  // 레이더 반지름

    // 관리 리스트
    private List<MinimapDot> _activeDots = new List<MinimapDot>();

    private void Awake()
    {
        Instance = this;

        // 시작할 땐 끔
        _minimapUI.SetActive(false);
    }

    private void OnEnable()
    {
        // 스태틱 구독
        RadarNode.OnRadarStateChanged += SetActive;
    }

    private void OnDisable()
    {
        RadarNode.OnRadarStateChanged -= SetActive;
    }

    private void Update()
    {
        // 미니맵 꺼져있거나 로컬 플레이어 없으면 무시
        if (_minimapUI.activeSelf == false || PlayerHandler.localPlayer == null) return;

        // 레이더 상 중앙은 플레이어 위치
        Vector3 centerPos = PlayerHandler.localPlayer.transform.position;

        // 내 플레이어 Y 회전값
        float playerRotY = PlayerHandler.localPlayer.transform.eulerAngles.y;

        // 모든 점 위치 갱신 (역순)
        for (int i = _activeDots.Count - 1; i >= 0; i--)
        {
            if (_activeDots[i] == null || _activeDots[i].Target == null)
            {
                // 타겟 죽었으면 반납
                if (_activeDots[i] != null) _activeDots[i].PoolObj.Release();

                // 리스트에서 제거
                _activeDots.RemoveAt(i);

                continue;
            }

            // 위치 업데이트
            _activeDots[i].UpdatePosition(centerPos, playerRotY, _mapScale, _maxRadius);
        }
    }

    // 레이더 켜기/끄기 (RadarNode에서 호출)
    public void SetActive(bool isOn)
    {
        _minimapUI.SetActive(isOn);
    }

    // 적 등록 (EnemyBase 에서 호출)
    public void RegisterEnemy(Transform enemy)
    {
        CreateDot(enemy, Color.red);
    }

    // 다른 플레이어 등록 (PlayerHandler 에서 호출)
    public void RegisterPlayer(Transform player)
    {
        CreateDot(player, Color.green);
    }

    // 점 생성
    private void CreateDot(Transform target, Color color)
    {
        if(PoolManager.Instance != null)
        {
            // 풀 꺼내서
            var poolObj = PoolManager.Instance.Spawn(_dotPrefab, _dotsContainer);
            MinimapDot dot = poolObj.GetComponent<MinimapDot>();
            // 초기화
            dot.Init(target, color);
            // 등록
            _activeDots.Add(dot);
        }
    }
}
