using TMPro;
using UnityEngine;

public class TrainUIManager : MonoBehaviour
{
    public static TrainUIManager Instance;

    [SerializeField] GameObject _uiPrefab;
    [SerializeField] Transform _uiContentParent;

    [Header("엔진 UI")]
    [SerializeField] EngineUI _engineUI; 

    private void Awake() => Instance = this;

    public void CreateUI(TrainNode node)
    {
        // 엔진 껍데기
        EngineNode engine = node as EngineNode;

        // 엔진이면
        if (engine != null)
        {
            if (_engineUI != null)
            {
                // 엔진 UI와 연결
                _engineUI.ConnectEngine(engine);
            }
        }

        // UI 생성 및 부모 설정
        GameObject trainUI = Instantiate(_uiPrefab, _uiContentParent);

        // 스크립트 가져와서 초기화
        TrainUI ui = trainUI.GetComponent<TrainUI>();
        if (ui != null)
        {
            ui.Init(node);
        }
    }
}
