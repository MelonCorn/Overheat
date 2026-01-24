using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SocketData
{
    public string ItemName = "";        // 아이템명
    public GameObject VisualObject = null; // 오브젝트
    public bool IsPredicting = false;   // 예측(잠금)상태
    public int PredictingSlot = -1;     // 예측 연동된 퀵슬롯

    // 초기화/청소 함수
    public void Clear()
    {
        ItemName = "";
        IsPredicting = false;
        PredictingSlot = -1;
    }
}

public class CargoNode : TrainNode
{
    [Header("선반")]
    [SerializeField] CargoSocket[] _sockets;

    private SocketData[] _socketItems;  // 선반 아이템 데이터

    public override void Init(TrainData data, int level)
    {
        base.Init(data, level);

        // 선반 소켓 수 만큼 초기화
        int count = _sockets.Length;

        // 배열 생성
        _socketItems = new SocketData[count];

        // 배열 순회
        for(int i = 0; i < count; i++)
        {
            // 소켓 초기화
            if (_sockets[i] != null)
                _sockets[i].Init(this, i);

            // 데이터 생성
            _socketItems[i] = new SocketData();
        }
    }


    // 소켓 상호작용
    public void InteractSocket(int index)
    {
        Debug.Log("소켓 상호작용 시도");

        // 예측 상태면 상호작용 무시 (잠금)
        if (index < 0 || index >= _socketItems.Length) return;

        // index 소켓의 아이템 데이터
        SocketData data = _socketItems[index];

        // 예측 중이면 무시
        if (data.IsPredicting) return;

        // 아이템 있으면 픽업 시도
        if (string.IsNullOrEmpty(data.ItemName) == false)
        {
            Debug.Log("선반에 아이템 존재");

            // 아이템 이름 백업
            string targetItemName = data.ItemName;

            // 현재 퀵슬롯 아이템
            string handItem = QuickSlotManager.Instance.CurrentSlotItemName;

            // 손에 어떤 아이템 들고있으면 픽업 패스
            if (string.IsNullOrEmpty(handItem) == false)
            {
                Debug.Log("손이 비어있지 않아서 선반 아이템을 집을 수 없습니다.");
                return;
            }

            // 일단 퀵슬롯에 아이템 추가
            int slot = QuickSlotManager.Instance.TryAddItem(targetItemName);

            // 인벤토리 공간 있으면
            if (slot != -1)
            {
                data.PredictingSlot = slot;  // 예측중인 퀵슬롯 설정
                data.IsPredicting = true;    // 예측 상태로 변경, 상호작용 잠금

                // 로컬에서 일단 아이템 안보이게 설정
                // 여기서 data.ItemName ""로 초기화
                ApplyVisual(index, null);

                // 열차 매니저에게 현재 화물칸의 index번째 소켓의 아이템을 퀵슬롯에 등록 요청
                // 여기서 백업해둔 targetItemName 사용
                TrainManager.Instance.RequestSocketInteract(this, index, "", targetItemName, slot);
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
                ApplyVisual(index, handItem);

                data.IsPredicting = true;  // 예측 상태로 변경, 상호작용 잠금
                data.PredictingSlot = -1;  // 보관은 퀵슬롯 예측 필요 없음

                // 열차 매니저에게 현재 화물칸의 index번째 소켓에 손에 들고있는 아이템 등록 요청
                TrainManager.Instance.RequestSocketInteract(this, index, handItem, "", slotIndex);
            }
        }
    }


    // 판정 실패로 소켓 롤백
    // serverItem 현재 선반 진짜 서버 아이템 (비주얼용)
    // rollbackItem 내가 수납,픽업 시도한 아이템 (인벤토리 복구용)
    public void RollbackSocket(int index, string serverItem, string rollbackItem, int slotIndex, bool isStore)
    {
        // 범위 체크
        if (index < 0 || index >= _socketItems.Length) return;

        // index 소켓의 아이템 데이터
        SocketData data = _socketItems[index];

        data.IsPredicting = false; // 예측 상태 해제, 상호작용 잠금 해제
        data.PredictingSlot = -1;

        ApplyVisual(index, serverItem); // 선반 오브젝트 갱신

        if (isStore) // 수납 실패
        {
            // 수납아이템 퀵슬롯에 복구
            if (QuickSlotManager.Instance != null)
                QuickSlotManager.Instance.RollbackAddItem(slotIndex, rollbackItem);
        }
        else // 픽업 실패
        {
            // 인벤토리에서 다시 삭제
            if (QuickSlotManager.Instance != null)
                QuickSlotManager.Instance.RemoveItem(slotIndex, rollbackItem);
        }
    }


    // 선반 오브젝트 처리
    private void ApplyVisual(int index, string itemName)
    {
        // index 소켓의 아이템 데이터
        SocketData data = _socketItems[index];

        // 이름 갱신
        data.ItemName = string.IsNullOrEmpty(itemName) ? "" : itemName;

        // 선반 오브젝트가 있을 때
        if (data.VisualObject != null)
        {
            // 반납용 PoolableObject 컴포넌트
            PoolableObject poolObject = data.VisualObject.GetComponent<PoolableObject>();

            // 컴포넌트 있으면
            if (poolObject != null)
                poolObject.Release(); // 풀로 돌아감
            else
                Destroy(data.VisualObject); // 풀링 대상 아니면 파괴

            // 비우기
            data.VisualObject = null;
        }

        // 혹시나 아이템 비어있으면 무시
        if (string.IsNullOrEmpty(itemName)) return;

        // 아이템명으로 찾아오기
        if (ItemManager.Instance.ItemDict.TryGetValue(itemName, out ShopItem itemData))
        {
            // 아이템 맞으면
            if(itemData is PlayerItemData pData)
            {
                // 프리팹에서 풀 컴포넌트 찾기
                PoolableObject prefabPoolable = pData.prefab.GetComponent<PoolableObject>();

                // 풀 오브젝트면
                if (prefabPoolable != null)
                {
                    // 로컬 오브젝트로 활성화
                    PoolableObject newObj = PoolManager.Instance.Spawn(prefabPoolable, _sockets[index].transform);

                    // 선반 모드로 전환
                    NetworkItem netItem = newObj.GetComponent<NetworkItem>();
                    if (netItem != null)
                        netItem.SwitchToLocalMode();

                    // 데이터 등록
                    data.VisualObject = newObj.gameObject;
                }
                else
                {
                    // PoolableObject가 안 붙어 있으면 걍 생성
                    GameObject newItem = Instantiate(pData.prefab, _sockets[index].transform);

                    // 위치 지정
                    newItem.transform.localPosition = Vector3.zero;
                    newItem.transform.localRotation = Quaternion.identity;

                    // 수동 제거
                    var pv = newItem.GetComponent<PhotonView>();
                    if (pv != null) Destroy(pv);
                    var collider = newItem.GetComponent<Collider>();
                    if (collider != null) Destroy(collider);

                    // 데이터 등록
                    data.VisualObject = newItem;
                }
            }
        }
    }

    // 소켓에 마우스를 올렸을 때 상호작용 텍스트 반환 (CargoSocket)
    public string GetInteractText(int index)
    {
        // 범위 체크
        if (index < 0 || index >= _socketItems.Length) return "";

        // index 소켓의 아이템 데이터
        SocketData data = _socketItems[index];

        // 예측 중(잠금) 상태면 텍스트 숨기기
        if (data.IsPredicting) return "";

        // 현재 소켓의 아이템
        string currentItem = data.ItemName;

        // 선반에 아이템이 있을 때 -> 픽업
        return string.IsNullOrEmpty(data.ItemName) ? "수납" : "획득";
    }


    // 화물칸이 파괴될 때
    public void ClearAllSockets()
    {
        if (_socketItems == null) return;

        // 선반 순회
        foreach (var data in _socketItems)
        {
            // 비주얼 있으면
            if (data.VisualObject != null)
            {
                // 풀 스크립트 가져와서
                var poolObj = data.VisualObject.GetComponent<PoolableObject>();
                // 있으면 반납
                if (poolObj != null) poolObj.Release();
                // 없으면 파괴
                else Destroy(data.VisualObject);
                // 비주얼 비우고
                data.VisualObject = null;
            }
            // 나머지 데이터 초기화
            data.Clear();
        }
    }

    // 룸 프로퍼티로 화물 정보 갱신
    public void ImportData(string content)
    {
        Debug.Log($"[ImportData] 데이터 수신: {content}");

        // null로 들어왔으면 "" 일단 할당
        if (content == null) content = "";

        // 한줄인 아이템 목록 소켓별로 분리
        string[] items = content.Split(',');

        // 소켓 수 만큼
        for (int i = 0; i < _socketItems.Length; i++)
        {
            // 새로운 갱신된 아이템
            string newItem = (i < items.Length) ? items[i] : "";
            SocketData data = _socketItems[i];

            // 예측 중이 아닐 때 (남이 바꾼거 갱신)
            if (data.IsPredicting == false)
            {
                // 내거랑 다르면 소켓 아이템 바로 설정
                if (data.ItemName != newItem) ApplyVisual(i, newItem);
            }
            // 수납 혹은 픽업으로 예측 중 (내가 바꾼거 예측)
            else
            {
                Debug.Log("서버 승인 완료! 잠금 해제");
                // 예측 중인데 데이터 들어왔으면 성공
                // 내가 바꾼 거랑 서버랑 같으면
                if (data.ItemName == newItem)
                {
                    data.IsPredicting = false; // 예측 상태 해제, 상호작용 잠금 해제

                    // 새 아이템이 비어있다면 (픽업)
                    if (string.IsNullOrEmpty(newItem))
                    {
                        // 예측 중이던 퀵슬롯이고 퀵슬롯 매니저 있을 때
                        if (data.PredictingSlot != -1 && QuickSlotManager.Instance != null)
                        {
                            // 퀵슬롯의 아이템
                            string itemName = QuickSlotManager.Instance.QuickSlot[data.PredictingSlot];

                            // 사용 허가
                            QuickSlotManager.Instance.ConfirmItem(data.PredictingSlot, itemName);

                            // 퀵슬롯 예측 상태 해지
                            data.PredictingSlot = -1;
                        }
                    }
                }
                else
                {
                    Debug.Log($"데이터 불일치 대기중.. Local: {data.ItemName} / Server: {newItem}");
                }
            }

            // 데이터 최신으로 갱신
            data.ItemName = newItem;
        }
    }

    // 소켓 상태 내보내기 (룸프로퍼티 저장용)
    public string ExportData()
    {
        if (_socketItems == null) return "";

        // 반환용 리스트
        List<string> list = new List<string>();

        // 소켓 순회돌면서 아이템명 리스트
        foreach (var data in _socketItems)
            list.Add(data.ItemName);

        // ,으로 구분해서 화물칸 아이템 데이터 리스트 반환
        return string.Join(",", list);
    }
}
