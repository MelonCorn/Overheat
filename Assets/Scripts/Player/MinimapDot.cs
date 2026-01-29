using UnityEngine;
using UnityEngine.UI;

public class MinimapDot : MonoBehaviour
{
    [SerializeField] Image _icon;   // 이미지

    // 추적 대상
    public Transform Target { get; private set; }

    private RectTransform _rect;
    public PoolableObject PoolObj { get; private set; }

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
        PoolObj = GetComponent<PoolableObject>();
    }

    // 초기화
    public void Init(Transform target, Color color)
    {
        Target = target;
        _icon.color = color;
    }

    // 매니저에서 호출할 위치 갱신
    public void UpdatePosition(Vector3 playerPos, float mapScale, float maxRadius)
    {
        // 타겟 없으면
        if (Target == null)
        {
            // 반납
            PoolObj.Release();
            return;
        }

        // 플레이어와 타겟 사이의 거리 계산
        Vector3 dir = Target.position - playerPos;
        Vector2 pos = new Vector2(dir.x, dir.z); // X, Z만 사용

        // 스케일 적용 (월드 거리를 UI 거리로)
        Vector2 uiPos = pos * mapScale;

        // 원형 레이더 밖으로 나가지 않게
        if (uiPos.magnitude > maxRadius)
        {
            uiPos = uiPos.normalized * maxRadius;
        }

        // UI 적용
        _rect.anchoredPosition = uiPos;
    }
}
