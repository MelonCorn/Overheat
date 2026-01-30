using UnityEngine;

public class GroundScoller : MonoBehaviour
{
    [Header("속도 보정 값")]
    // 바닥 타일링 수치에 따라 달라지는데
    // 최대한 주변 환경오브젝트에 맞추기
    [SerializeField] float _scrollScale = 0.1f;

    private Renderer _renderer;
    private float _currentOffsetY; // 누적 오프셋 저장용

    void Start()
    {
        _renderer = GetComponent<Renderer>();
    }

    void Update()
    {
        // 엔진 속도 가져오기
        float speed = (TrainManager.Instance != null && TrainManager.Instance.MainEngine != null) ?
            TrainManager.Instance.MainEngine.CurrentSpeed : 0f;

        // 속도가 있을 때
        if (speed > 0)
        {
            // 이동 거리 = 속도 * 시간
            float moveAmount = speed * Time.deltaTime * _scrollScale;

            // 오프셋 누적 (Y축 스크롤 기준)
            _currentOffsetY += moveAmount;

            // 1.0 넘어가면 초기화
            // 혹시나 너무 커지면 문제 생길 지도..
            _currentOffsetY = _currentOffsetY % 1.0f;

            // 머티리얼 메인텍스쳐에 y 옵셋 적용
            _renderer.material.mainTextureOffset = new Vector2(0, _currentOffsetY);
        }
    }
}
