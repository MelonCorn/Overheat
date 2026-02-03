using UnityEngine;
using UnityEngine.UI;

public class MinimapDot : MonoBehaviour
{
    // 추적 대상
    public Transform Target { get; private set; }

    private Image _icon;            // 이미지
    private RectTransform _rect;    // UI 트랜스폼
    public PoolableObject PoolObj { get; private set; }

    private void Awake()
    {
        _icon = GetComponent<Image>();
        _rect = GetComponent<RectTransform>();
        PoolObj = GetComponent<PoolableObject>();
    }

    // 초기화
    public void Init(Transform target, Sprite sprite, Color color)
    {
        Target = target;
        _icon.sprite = sprite;
        _icon.color = color;
    }

    // 매니저에서 호출할 위치 갱신
    public void UpdatePosition(Vector3 playerPos, float playerRotY, float mapScale, float maxRadius)
    {
        // 타겟 없으면 무시
        if (Target == null)
            return;

        // 플레이어와 타겟 사이의 거리 계산
        Vector3 dir = Target.position - playerPos;
        // 플레이어 회전값만큼 반대로 (오른쪽보면 왼쪽으로)
        Vector3 rotatedDir = Quaternion.Euler(0, -playerRotY, 0) * dir;
        // UI 좌표만큼 줄이기
        Vector2 uiPos = new Vector2(rotatedDir.x, rotatedDir.z) * mapScale;

        // 원형 레이더 밖으로 나가지 않게
        if (uiPos.magnitude > maxRadius)
        {
            uiPos = uiPos.normalized * maxRadius;
        }

        // UI 적용
        _rect.anchoredPosition = uiPos;
    }
}
