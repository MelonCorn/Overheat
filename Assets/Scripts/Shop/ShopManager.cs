using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

public class ShopManager : MonoBehaviour
{
    [Header("상점 품목 버튼 설정")]
    [SerializeField] ShopSlot _slotPrefab;        // 항목 버튼 프리팹
    [SerializeField] Transform _slotParent;       // 버튼 부모 트랜스폼

    [Header("테스트용 아이템 목록")]
    [SerializeField] List<ShopItem> _shopPlayerItems;

    private void Start()
    {
        GenerateSlots();
    }

    private void GenerateSlots()
    {
        // 아이템 목록
        foreach (var item in _shopPlayerItems)
        {
            CreateSlot(item);
        }

        // 열차 목록
        // TrainManager에 있는 열차 데이터 Dict 사용
        if (TrainManager.Instance != null && TrainManager.Instance.TrainDict != null)
        {
            foreach (var trainData in TrainManager.Instance.TrainDict.Values)
            {
                // 엔진은 항목에 넣을 필요 없음
                if (trainData is TrainEngineData) continue;

                CreateSlot(trainData);
            }
        }
    }


    private void CreateSlot(ShopItem itemData)
    {
        // 생성
        ShopSlot slot = Instantiate(_slotPrefab, _slotParent);

        if (slot != null)
        {
            // 초기화 (ShopManager, ShopItem)
            slot.Init(this, itemData);
        }
    }

    public void TryPurchaseItem(ShopItem itemData)
    {
        // 골드 있는지 확인, 차감
        if (GameManager.Instance.TryUseGold(itemData.price))
        {
            // 열차일 때
            if (itemData is TrainData trainItem)
            {
                // 생성 요청 후 생성 시 룸 프로퍼티 갱신
                TrainManager.Instance.RequestAddTrain(trainItem.type);

                Debug.Log($"열차 구매 완료: {trainItem.itemName}");
            }
            // 아이템일 때
            else if (itemData is PlayerItemData playerItem)
            {
                // Inventory에 아이템 생성 요청
                // 혹은 바닥에 오브젝트로 떨구기
                //InventoryManager.Instance.AddItem(playerItem);

                Debug.Log($"아이템 구매 완료: {playerItem.itemName}");
            }
        }
        else
        {
            Debug.Log("골드이 부족합니다!");

            // 골드 부족 팝업 띄우기
        }
    }
}
