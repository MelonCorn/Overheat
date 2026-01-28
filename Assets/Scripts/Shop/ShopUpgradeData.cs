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

    private int _upgradePrice;      // 업그레이드 비용
    private bool _isMaxLevel;       // 만렙 여부

    public void Init(ShopManager manager, TrainData data, int index, int currentLevel)
    {
        _shopManager = manager;
        _data = data;
        _trainIndex = index;

        if (_icon != null) _icon.sprite = data.icon;
        if (_nameText != null) _nameText.text = data.displayName;
        if (_levelText != null) _levelText.text = $"{currentLevel} / {data.GetMaxLevel()}";

        // 버튼 기능: 클릭 시 업그레이드 팝업 열기
        if (_upgradeButton != null)
        {
            // 풀링 오브젝트라 싹 비우기
            _upgradeButton.onClick.RemoveAllListeners();
            _upgradeButton.onClick.AddListener(OnClickSlot);
        }
        
        // 최대 레벨 캐싱
        _isMaxLevel = data.IsMaxLevel(currentLevel);
        if (_isMaxLevel == false)
        {
            // 가격도 최대 레벨 아니면
            _upgradePrice = data.GetBasicStat(currentLevel).upgradePrice;
        }

        // 초기화 시점에 한번 실행
        UpdateUIState(GameData.Gold);
    }
    private void OnClickSlot()
    {
        // 열차 업그레이드 시도
        _shopManager?.TryUpgradeTrain(_trainIndex);

        // 버튼 선택되는거 바로 풀기
        EventSystem.current.SetSelectedGameObject(null);
    }

    // UI갱신
    public void UpdateUIState(int currentGold)
    {
        if (_upgradeButton == null) return;

        // 만렙이면 비활성화
        if (_isMaxLevel == true)
        {
            _upgradeButton.interactable = false;
            return;
        }

        // 돈이 충분하면 버튼 활성화
        _upgradeButton.interactable = (currentGold >= _upgradePrice);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _shopManager?.ShowTrainUpgradeInfo(_trainIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _shopManager?.HideTrainUpgradeInfo();
    }
}
