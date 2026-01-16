using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QuickSlotManager : MonoBehaviour
{
    public static QuickSlotManager Instance;

    public string[] QuickSlot { get; private set; } = new string[3];    // 퀵슬롯 아이템 정보
    public int CurrentSlotIndex { get; private set; }                   // 현재 퀵슬롯

    public string CurrentSlotItemName { get; private set; }             // 현재 슬롯 아이템 이름

    [Header("퀵슬롯 UI 설정")]
    [SerializeField] Image[] _slotImages;   // 퀵슬롯 각 이미지
    [SerializeField] Sprite _defaultSprite; // 빈칸 스프라이트



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    // 아이템 추가 시도
    public bool TryAddItem(string itemName)
    {
        // 현재 슬롯 빈칸 체크
        if (string.IsNullOrEmpty(QuickSlot[CurrentSlotIndex]))
        {
            // 퀵슬롯에 이름 장착
            QuickSlot[CurrentSlotIndex] = itemName;
            // UI 갱신
            UpdateUI();
            return true;
        }

        // 다른 퀵슬롯 체크
        for (int i = 0; i < QuickSlot.Length; i++)
        {
            // 빈 슬롯 발견
            if (string.IsNullOrEmpty(QuickSlot[i]))
            {
                // 퀵슬롯에 이름 장착
                QuickSlot[i] = itemName;
                // UI 갱신
                UpdateUI();
                return true;
            }
        }

        // 빈 슬롯 찾을 수 없음 실패
        return false;
    }


    // 현재 슬롯 아이템 사용
    public void UseItem()
    {
        if (!string.IsNullOrEmpty(QuickSlot[CurrentSlotIndex]))
        {
            QuickSlot[CurrentSlotIndex] = null;
            UpdateUI();
        }
    }


    // 현재 슬롯 아이템 이름 반환
    public string GetCurrentItem()
    {
        return QuickSlot[CurrentSlotIndex];
    }

    // 슬롯 변경
    public void SelectSlot(int index)
    {
        if (index >= 0 && index < QuickSlot.Length)
        {
            CurrentSlotIndex = index;
            UpdateUI();
        }
    }

    // 퀵슬롯 UI 갱신
    public void UpdateUI()
    {
        // 퀵슬롯 순회
        for (int i = 0; i < QuickSlot.Length; i++)
        {
            // 슬롯 비어있으면
            if (string.IsNullOrEmpty(QuickSlot[i]))
            {
                // 빈 스프라이트 넣어주기
                _slotImages[i].sprite = _defaultSprite;

                // 안보이게 (투명)
                Color tempColor = _slotImages[i].color;
                tempColor.a = 0f;
                _slotImages[i].color = tempColor;
            }
            else
            {
                // 아이템 리스트에서 아이콘 가져오기
                if (ItemManager.Instance.ItemDict.ContainsKey(QuickSlot[i]))
                {
                    _slotImages[i].sprite = ItemManager.Instance.ItemDict[QuickSlot[i]].icon;

                    // 보이게
                    Color tempColor = _slotImages[i].color;
                    tempColor.a = 1f;
                    _slotImages[i].color = tempColor;
                }
            }

            // 플레이어 아이템 변경
            PlayerHandler.localPlayer.ChangeQuickSlot(QuickSlot[i]);
        }
    }

    // 키보드 입력 처리 (테스트용으로 임시)
    private void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectSlot(2);
    }
}
