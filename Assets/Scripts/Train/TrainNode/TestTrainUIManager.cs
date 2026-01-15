using TMPro;
using UnityEngine;

public class TestTrainUIManager : MonoBehaviour
{
    public static TestTrainUIManager Instance;

    [SerializeField] GameObject _uiPrefab;
    [SerializeField] Transform _uiContentParent;

    private EngineNode _engine; // 엔진

    [Header("엔진 정보 텍스트")] // 현재 속도, 현재 연료
    [SerializeField] TextMeshProUGUI _engineInfoText;

    private void Awake() => Instance = this;

    public void CreateUI(TrainNode node)
    {
        // UI 생성 및 부모 설정
        GameObject go = Instantiate(_uiPrefab, _uiContentParent);

        // 스크립트 가져와서 초기화
        TestNodeUI ui = go.GetComponent<TestNodeUI>();
        if (ui != null)
        {
            ui.Init(node);
        }
    }

    public void SetEngine(EngineNode engine)
    {
        _engine = engine;
    }
}
