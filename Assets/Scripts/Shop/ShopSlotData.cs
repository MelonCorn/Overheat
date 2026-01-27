using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopSlotData : MonoBehaviour
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
    }


    // 클릭
    private void OnClickSlot()
    {
        // 데이터로 결제 요청
        _shopManager.TryPurchaseItem(_data);

        // 버튼 선택되는거 바로 풀기
        EventSystem.current.SetSelectedGameObject(null);
    }
}
