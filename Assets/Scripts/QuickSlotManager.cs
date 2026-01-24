using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class QuickSlotManager : MonoBehaviour
{
    public static QuickSlotManager Instance;

    public string[] QuickSlot { get; private set; } = new string[3];    // 퀵슬롯 아이템 정보
    public int CurrentSlotIndex { get; private set; }                   // 현재 퀵슬롯

    public string CurrentSlotItemName => QuickSlot[CurrentSlotIndex];    // 현재 슬롯 아이템 이름


    // 예측중인 슬롯
    private bool[] _isPredicting = new bool[3];
    public bool[] IsPredicting => _isPredicting;

    [Header("퀵슬롯 UI 설정")]
    [SerializeField] Image[] _slotImages;   // 퀵슬롯 각 이미지
    [SerializeField] Sprite _defaultSprite; // 빈칸 스프라이트
    [SerializeField] Outline[] _slotOutlines;   // 퀵슬롯 각 아웃라인

    [Header("테스트용 스타터팩")]
    [SerializeField] string _starterItemName = "Pistol";



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


    private IEnumerator Start()
    {
        // 로컬 플레이어 할당될 때까지 대기
        yield return new WaitUntil(() => PlayerHandler.localPlayer != null);

        // 플레이어 초기화할 때 꼬일수 있으니까 일단한 프레임 더 대기
        yield return null;

        // 스타터팩 아직 지급받지 않았다면
        if (GameData.HasStarterItem == false)
        {
            // 아이템 추가 시도 (예측 없이 즉시)
            int index = TryAddItem(_starterItemName, false);

            if (index != -1)
            {
                Debug.Log($"[스타터팩] {_starterItemName}을 {index}번 슬롯에 지급");

                // 지급 완료 처리
                GameData.HasStarterItem = true; 

                // 지급받은 아이템 바로 손에 들게
                SelectSlot(index);
            }
            else
            {
                Debug.LogWarning($"[스타터팩 지급 실패] {_starterItemName}을 넣을 공간이 없거나 아이템 이름이 잘못됨");
            }
        }
    }


    // 아이템 추가 시도
    // 반환은 슬롯 인덱스 값 (실패는 -1)
    public int TryAddItem(string itemName, bool isPredicting = true)
    {
        // 현재 슬롯 빈칸 체크
        if (string.IsNullOrEmpty(QuickSlot[CurrentSlotIndex]))
        {
            // 슬롯 설정
            SetSlot(CurrentSlotIndex, itemName, isPredicting);

            // 현재 슬롯 번호 반환
            return CurrentSlotIndex;
        }

        // 다른 퀵슬롯 체크
        for (int i = 0; i < QuickSlot.Length; i++)
        {
            // 빈 슬롯 발견
            if (string.IsNullOrEmpty(QuickSlot[i]))
            {
                // 슬롯 설정
                SetSlot(i, itemName, isPredicting);

                // 넣은 슬롯 번호 반환
                return i;
            }
        }

        // 빈 슬롯 찾을 수 없음 실패
        return -1;
    }
    
    // 슬롯 설정 (슬롯 번호, 아이템 이름, 예측 상태)
    private void SetSlot(int slotIndex, string itemName, bool isPending)
    {
        QuickSlot[slotIndex] = itemName;
        IsPredicting[slotIndex] = isPending;

        // UI 갱신
        UpdateUI();
    }

    // 사용 가능 상태 확인 (외부용)
    public bool IsUsable(int index)
    {
        // 아이템이 없거나 예측 상태면 사용 불가
        if (string.IsNullOrEmpty(QuickSlot[index])) return false;
        if (IsPredicting[index]) return false;

        return true;
    }

    // 아이템 픽업 예측 성공으로 예측 상태 해지
    public void ConfirmItem(int slotIndex, string checkName)
    {
        // 슬롯 범위 체크
        if (slotIndex < 0 && slotIndex >= QuickSlot.Length) return;

        // 이름 맞는지 확인
        if (QuickSlot[slotIndex] != checkName) return;

        IsPredicting[slotIndex] = false; // 예측 끝
        UpdateUI();     // UI 갱신
        Debug.Log($"[확정] {slotIndex}번 슬롯 아이템 사용 가능");

    }

    // 현재 슬롯 아이템 사용
    public void UseItem(int slotIndex, string itemName)
    {
        // 현재 퀵슬롯이 비어있으면 무시
        if (string.IsNullOrEmpty(QuickSlot[CurrentSlotIndex]) == true) return;

        QuickSlot[CurrentSlotIndex] = null;
        UpdateUI();

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

                _slotImages[i].color = Color.black;
            }
            else
            {
                Debug.Log("아이템 아이콘 가져오기 시도");
                // 아이템 리스트에서 아이콘 가져오기
                if (ItemManager.Instance.ItemDict.ContainsKey(QuickSlot[i]))
                {
                    Debug.Log("아이템 아이콘 발견");
                    _slotImages[i].sprite = ItemManager.Instance.ItemDict[QuickSlot[i]].icon;

                    // 예측 상태에 따라 반투명, 불투명
                    _slotImages[i].color = IsPredicting[i] ? new Color(1, 1, 1, 0.5f) : Color.white;
                }
            }

            // 아이템 테두리 
            _slotOutlines[i].enabled = (i == CurrentSlotIndex);
        }
        
        if (PlayerHandler.localPlayer != null)
        {
            // 현재 슬롯의 아이템 이름 전달
            PlayerHandler.localPlayer.ChangeQuickSlot(QuickSlot[CurrentSlotIndex]);
        }
    }

    // 아이템 삭제 (버리기, 선반 보관, 롤백)
    public void RemoveItem(int slotIndex, string itemName)
    {
        // 범위 체크
        if (slotIndex < 0 || slotIndex >= QuickSlot.Length) return;

        // 안전장치
        if (itemName != null && QuickSlot[slotIndex] != itemName)
        {
            Debug.LogWarning($"[삭제 취소] {slotIndex}번에 {itemName}이 있어야 하는데 {QuickSlot[slotIndex]}가 있어서 삭제 스킵");
            return;
        }

        // 삭제
        QuickSlot[slotIndex] = null;

        // UI 갱신 (사라짐)
        UpdateUI();
    }


    // 선반에 아이템 수납 실패 시 특정 슬롯에 아이템 강제로 다시 롤백
    public void RollbackAddItem(int slotIndex, string itemName)
    {
        // 범위 체크
        if (slotIndex < 0 || slotIndex >= QuickSlot.Length) return;

        // 해당 슬롯이 비어있다면 (정상적)
        if (string.IsNullOrEmpty(QuickSlot[slotIndex]))
        {
            QuickSlot[slotIndex] = itemName;
            UpdateUI();
        }
        else
        {
            // 혹시나 뭔가 있다면 다른 칸 찾아서 넣음
            Debug.LogWarning($"롤백하려는 {slotIndex}번 슬롯이 이미 차있습니다. 다른 곳에 넣습니다.");
            TryAddItem(itemName);
        }
    }


    // 아이템 버리기 시도
    public string TryDropItem()
    {
        int index = CurrentSlotIndex;

        // 아이템이 없으면 실패
        if (string.IsNullOrEmpty(QuickSlot[index])) return null;

        // 예측 중이면 금지
        if (IsPredicting[index] == true)
        {
            Debug.LogWarning("상호작용 대기 중인 아이템은 버릴 수 없습니다.");
            return null;
        }

        // 반환할 아이템
        string itemName = QuickSlot[index];

        // 아이템 기록 제거
        QuickSlot[index] = null;

        // UI 갱신
        UpdateUI();

        return itemName;
    }
}
