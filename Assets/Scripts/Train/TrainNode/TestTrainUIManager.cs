using UnityEngine;

public class TestTrainUIManager : MonoBehaviour
{
    public static TestTrainUIManager Instance;

    [SerializeField] GameObject _uiPrefab;
    [SerializeField] Transform _uiContentParent;

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
}
