using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopSlotData : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI")]
    [SerializeField] Image _icon;
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _priceText;

    private ShopManager _shopManager;
    private ShopItem _data; // 등록된 so

    private Button _button;

    public void Init(ShopManager shopManager, ShopItem data)
    {
        _button = GetComponent<Button>();

        _shopManager = shopManager;
        _data = data;

        // UI 갱신
        if (_icon != null) _icon.sprite = data.icon;
        if (_nameText != null) _nameText.SetText(data.displayName);
        if (_priceText != null) _priceText.SetText($"{data.price:N0}");

        // 버튼 기능 추가 (결제 요청)
        _button.onClick.AddListener(OnClickSlot);

        // 생성 되면 한 번 갱신
        UpdatePriceState(GameData.Gold);
    }

    // 클릭
    private void OnClickSlot()
    {
        // 데이터로 결제 요청
        _shopManager?.TryPurchaseItem(_data);

        // 버튼 선택되는거 바로 풀기
        EventSystem.current.SetSelectedGameObject(null);
    }

    // 가격 생상 갱신
    public void UpdatePriceState(int currentGold)
    {
        if (_data == null || _priceText == null) return;

        // 가격보다 돈이 많으면 흰색, 적으면 빨간색
        _priceText.color = (currentGold >= _data.price) ? Color.white : Color.red;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_data == null) return;

        // 타입에 맞게 팝업 호출
        // 열차
        if (_data is TrainData)
        {
            _shopManager?.ShowTrainInfo(_data);
        }
        // 아이템
        else
        {
            _shopManager?.ShowItemInfo(_data);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (_data == null) return;

        _shopManager?.HideItemInfo();
        _shopManager?.HideTrainInfo();
    }
}
