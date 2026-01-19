using UnityEngine;

public class CargoNode : TrainNode
{
    [Header("선반")]
    [SerializeField] CargoSocket[] _sockets;

    // 소켓 관리
    private string[] _socketItems;       // 아이템명
    private GameObject[] _itemObjects;   // 오브젝트
    private bool[] _isPredicting;       // 예측 상태
    private int[] _predictingSlots;     // 예측중인 퀵슬롯

    public override void Init(TrainData data, int level)
    {
        base.Init(data, level);

        // 선반 소켓 수 만큼 초기화
        int count = _sockets.Length;
        _socketItems = new string[count];
        _itemObjects = new GameObject[count];
        _isPredicting = new bool[count];
        _predictingSlots = new int[count];

        // 소켓 초기화
        for (int i = 0; i < count; i++)
        {
            if (_sockets[i] != null)
                _sockets[i].Init(this, i);

            _predictingSlots[i] = -1;
        }
    }


    // 소켓 상호작용
    public void InteractSocket(int socketIndex)
    {
        Debug.Log("소켓 상호작용 시도");
        // 예측 상태면 상호작용 무시 (잠금)
        if (socketIndex < 0 || socketIndex >= _socketItems.Length) return;
        if (_isPredicting[socketIndex]) return;

        // 소켓 아이템 이름
        string currentItem = _socketItems[socketIndex];

        // 아이템 있으면 픽업 시도
        if (string.IsNullOrEmpty(currentItem) == false)
        {
            Debug.Log("선반에 아이템 존재");

            // 현재 퀵슬롯이 비어있는지 확인
            string handItem = QuickSlotManager.Instance.CurrentSlotItemName;

            // 손에 어떤 아이템 들고있으면 픽업 금지
            if (string.IsNullOrEmpty(handItem) == false)
            {
                Debug.Log("손이 비어있지 않아서 선반 아이템을 집을 수 없습니다.");
                return;
            }


            // 일단 퀵슬롯에 아이템 추가
            int slot = QuickSlotManager.Instance.TryAddItem(currentItem);

            // 인벤토리 공간 있으면
            if (slot != -1)
            {
                // 예측중인 퀵슬롯 설정
                _predictingSlots[socketIndex] = slot;

                // 로컬에서 일단 아이템 안보이게 설정
                ApplyVisual(socketIndex, null);

                _isPredicting[socketIndex] = true; // 예측 상태로 변경, 상호작용 잠금

                // 열차 매니저에게 현재 화물칸의 index번째 소켓의 아이템을 퀵슬롯에 등록 요청
                TrainManager.Instance.RequestSocketInteract(this, socketIndex, "", currentItem, slot);
            }
        }
        // 비어있으면 수납 시도
        else
        {
            Debug.Log("선반에 아이템 없음");
            // 현재 손에 들고있는 아이템
            string handItem = QuickSlotManager.Instance.CurrentSlotItemName;

            // 손에 아이템 있으면
            if (string.IsNullOrEmpty(handItem) == false)
            {
                Debug.Log("손에 아이템 있음");
                // 현재 퀵슬롯 번호
                int slotIndex = QuickSlotManager.Instance.CurrentSlotIndex;

                // 퀵슬롯에서 아이템 제거
                QuickSlotManager.Instance.RemoveItem(slotIndex, handItem);

                // 선반 오브젝트 갱신
                ApplyVisual(socketIndex, handItem);
                _isPredicting[socketIndex] = true; // 예측 상태로 변경, 상호작용 잠금

                // 보관할 땐 퀵슬롯 예측 필요없음
                if (_predictingSlots != null) _predictingSlots[socketIndex] = -1;

                // 열차 매니저에게 현재 화물칸의 index번째 소켓에 손에 들고있는 아이템 등록 요청
                TrainManager.Instance.RequestSocketInteract(this, socketIndex, handItem, "", slotIndex);
            }
        }
    }


    // 판정 실패로 소켓 롤백
    // serverItem 현재 선반 진짜 서버 아이템 (비주얼용)
    // rollbackItem 내가 수납,픽업 시도한 아이템 (인벤토리 복구용)
    public void RollbackSocket(int socketIndex, string serverItem, string rollbackItem, int slotIndex, bool isStore)
    {
        _isPredicting[socketIndex] = false; // 예측 상태 해제, 상호작용 잠금 해제

        ApplyVisual(socketIndex, serverItem); // 선반 오브젝트 갱신

        if (isStore) // 수납 실패
        {
            // 수납아이템 퀵슬롯에 복구
            if (QuickSlotManager.Instance != null)
            {
                QuickSlotManager.Instance.RollbackAddItem(slotIndex, rollbackItem);
            }
        }
        else // 픽업 실패
        {
            // 인벤토리에서 다시 삭제
            if (QuickSlotManager.Instance != null)
            {
                QuickSlotManager.Instance.RemoveItem(slotIndex, rollbackItem);
            }
        }

        _predictingSlots[socketIndex] = -1;
    }


    // 소켓 오브젝트 처리
    private void ApplyVisual(int socketIndex, string itemName)
    {
        // 데이터 갱신
        _socketItems[socketIndex] = string.IsNullOrEmpty(itemName) ? "" : itemName;

        // 기존 오브젝트 삭제
        if (_itemObjects[socketIndex] != null)
            Destroy(_itemObjects[socketIndex]);

        // 혹시나 아이템 비어있으면 무시
        if (string.IsNullOrEmpty(itemName)) return;

        // 새 오브젝트 생성
        if (ItemManager.Instance.ItemDict.ContainsKey(itemName))
        {
            // 새 아이템
            var itemData = ItemManager.Instance.ItemDict[itemName];

            // 아이템 맞으면
            if(itemData is PlayerItemData data)
            {
                // 생성
                GameObject newItem = Instantiate(data.prefab, _sockets[socketIndex].transform);

                // 위치 초기화
                newItem.transform.localPosition = Vector3.zero;
                newItem.transform.localRotation = Quaternion.identity;

                // 소켓에 등록
                _itemObjects[socketIndex] = newItem;
            }
        }
    }




    // 룸 프로퍼티로 화물 정보 갱신
    public void ImportData(string content)
    {
        if (content == null) content = "";

        // 한줄인 아이템 목록 소켓별로 분리
        string[] items = content.Split(',');

        // 소켓 수 만큼
        for (int i = 0; i < _socketItems.Length; i++)
        {
            // 새로운 갱신된 아이템
            string newItem = (i < items.Length) ? items[i] : "";

            // 예측 중이 아닐 때 소켓 아이템 바로 설정
            if (_isPredicting[i] == false)
            {
                if (_socketItems[i] != newItem)
                {
                    ApplyVisual(i, newItem);
                }
            }
            // 수납 혹은 픽업으로 예측 중
            else
            {
                // 예측 중인데 데이터 들어왔으면 성공
                // 내가 바꾼 거랑 서버랑 같으면
                if (_socketItems[i] == newItem)
                {
                    _isPredicting[i] = false; // 예측 상태 해제, 상호작용 잠금 해제

                    // 새 아이템이 비어있다면 (픽업)
                    if (string.IsNullOrEmpty(newItem))
                    {
                        // 예측중이던 퀵슬롯
                        int slot = _predictingSlots[i];

                        if (slot != -1 && QuickSlotManager.Instance != null)
                        {
                            // 퀵슬롯의 아이템
                            string itemName = QuickSlotManager.Instance.QuickSlot[slot];

                            // 사용 허가
                            QuickSlotManager.Instance.ConfirmItem(slot, itemName);

                            // 퀵슬롯 예측 삭제
                            _predictingSlots[i] = -1;
                        }
                    }
                }
            }

            // 데이터 최신으로 갱신
            _socketItems[i] = newItem;
        }
    }

    // 소켓 상태 내보내기 (룸프로퍼티 저장용)
    public string ExportData()
    {
        if (_socketItems == null) return "";
        return string.Join(",", _socketItems);
    }
}
