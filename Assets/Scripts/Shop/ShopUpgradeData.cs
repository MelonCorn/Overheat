using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopUpgradeData : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI 연결")]
    [SerializeField] Image _icon;                   // 이미지
    [SerializeField] TextMeshProUGUI _nameText;     // 이름
    [SerializeField] TextMeshProUGUI _levelText;    // 레벨
    [SerializeField] Button _upgradeButton;         // 업그레이드 버튼

    private ShopManager _shopManager;    // 상점 매니저
    private TrainData _data;             // 열차 데이터
    private int _trainIndex;             // 인덱스

    public void Init(ShopManager manager, TrainData data, int index, int currentLevel)
    {
        _shopManager = manager;
        _trainIndex = index;

        if (_icon != null) _icon.sprite = data.icon;
        if (_nameText != null) _nameText.text = data.displayName;
        if (_levelText != null) _levelText.text = $"{currentLevel} / {data.GetMaxLevel()}";

        // 버튼 기능: 클릭 시 업그레이드 팝업 열기
        if (_upgradeButton != null)
        {
            _upgradeButton.onClick.AddListener(OnClickSlot);
        }
    }
    private void OnClickSlot()
    {
        // 열차 업그레이드 시도
        _shopManager?.TryUpgradeTrain(_trainIndex);

        // 버튼 선택되는거 바로 풀기
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
    }

    public void OnPointerExit(PointerEventData eventData)
    {
    }
}
