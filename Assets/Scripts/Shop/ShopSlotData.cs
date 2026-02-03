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
        if(_shopManager) _shopManager.TryPurchaseItem(_data);

        // 버튼 선택되는거 바로 풀기
        EventSystem.current.SetSelectedGameObject(null);
    }

    // 가격 생상 갱신
    public void UpdatePriceState(int currentGold)
    {
        if (_data == null || _priceText == null) return;

        // 열차 인지 확인
        // 그리고 열차가 가득찼으면
        if(_data is TrainData && TrainManager.Instance != null && TrainManager.Instance.IsTrainFull)
        {
            // 꽉 찼으면
            _priceText.SetText("구매 불가"); // 텍스트 변경
            _priceText.color = Color.red;   // 빨간색
            _button.interactable = false;   // 버튼 비활성화
        }
        // 열차 아니거나 열차 꽉 안찼으면
        else
        {
            // 가격 표시
            _priceText.SetText($"{_data.price:N0}");

            // 가격 색
            bool canBuy = currentGold >= _data.price;
            _priceText.color = canBuy ? Color.white : Color.red;

            // 버튼 온오프
            _button.interactable = canBuy;
        }
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
