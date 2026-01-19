using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections;

public class ShopManager : MonoBehaviourPun
{
    [Header("구매 대기 UI")]
    [SerializeField] GameObject _loadingPopup; // 결제 대기 팝업

    [Header("구매 물품이 나올 위치")]
    [SerializeField] Transform _itemSpawnPoint;

    [Header("상점 품목 버튼 설정")]
    [SerializeField] ShopSlotData _slotPrefab;        // 항목 버튼 프리팹
    [SerializeField] Transform _slotParent;       // 버튼 부모 트랜스폼

    private void Start()
    {
        // 시작하면 상점 항목들 생성
        GenerateSlots();
    }

    // 항목 생성
    private void GenerateSlots()
    {

        // 아이템 목록
        // ItemManager 있는 아이템 데이터 Dict 사용
        if (ItemManager.Instance != null && ItemManager.Instance.ItemDict != null)
        {
            foreach (var itemData in ItemManager.Instance.ItemDict.Values)
            {
                // 석탄은 항목에 넣을 필요 없음
                if (itemData is FuelData) continue;

                // 상점 항목 생성
                CreateSlot(itemData);
            }
        }

        // 열차 목록
        // TrainManager에 있는 열차 데이터 Dict 사용
        if (TrainManager.Instance != null && TrainManager.Instance.TrainDict != null)
        {
            // 모든 열차
            foreach (var trainData in TrainManager.Instance.TrainDict.Values)
            {
                // 엔진은 항목에 넣을 필요 없음
                if (trainData is TrainEngineData) continue;

                // 상점 항목 생성
                CreateSlot(trainData);
            }
        }
    }



    // 슬롯 버튼 생성
    private void CreateSlot(ShopItem itemData)
    {
        // 생성
        ShopSlotData slot = Instantiate(_slotPrefab, _slotParent);

        if (slot != null)
        {
            // 초기화 (ShopManager, ShopItem)
            slot.Init(this, itemData);
        }
    }


    // 상점 구매 버튼
    public void TryPurchaseItem(ShopItem itemData)
    {
        // 구매 버튼을 누른 사람은 로딩 팝업 활성화
        if (_loadingPopup != null) _loadingPopup.SetActive(true);

        if (PhotonNetwork.IsMasterClient)
        {
            PurchaseProgress(itemData.itemName, PhotonNetwork.LocalPlayer);
        }
        else
        {
            // 방장한테 구매 요청
            photonView.RPC(nameof(RPC_RequestPurchase), RpcTarget.MasterClient, itemData.itemName);
        }
    }


    // 방장에게 구매 요청 (아이템명, RPC 정보)
    [PunRPC]
    private void RPC_RequestPurchase(string itemName, PhotonMessageInfo info)
    {
        // 혹~시 몰라서 일단 방어
        if (PhotonNetwork.IsMasterClient == false) return;

        // 주문 처리 시작 (아이템명, 요청자)
        PurchaseProgress(itemName, info.Sender);
    }

    // 구매 진행 (아이템명, 요청자)
    private void PurchaseProgress(string itemName, Player player)
    {
        // 항목 데이터 검색
        ShopItem targetItem = GameManager.Instance.FindItemData(itemName);

        bool isSuccess = false;

        // 아이템 존재하고, 골드 사용 완료 시
        if (targetItem != null && GameManager.Instance.TryUseGold(targetItem.price))
        {
            // 구매 성공
            isSuccess = true;
            // 아이템 생성
            SpawnPurchasedItem(targetItem);
        }


        // 구매 요청자에게만 결과 RPC
        if (player.IsLocal) RPC_PurchaseResult(isSuccess);                  // 방장 본인은 그냥 함수 호출
        else photonView.RPC(nameof(RPC_PurchaseResult), player, isSuccess); // 아니면 결과 전송
    }

    // 구매 결과
    [PunRPC]
    private void RPC_PurchaseResult(bool isSuccess)
    {
        // 결과 받았으니까 로딩 UI 비활성화
        if (_loadingPopup != null) _loadingPopup.SetActive(false);

        if (isSuccess)
        {
            Debug.Log($"구매 성공");
            // 나중에 뭐 사운드 SoundManager.Instance.PlaySFX("BuySuccess");
        }
        else
        {
            Debug.Log($"구매 실패");
            // 나중에 UI 알림창 UIManager.Instance.ShowPopup(메세지);
        }
    }

    // 결제 성공한 항목 객체 생성
    private void SpawnPurchasedItem(ShopItem itemData)
    {
        // 열차
        if (itemData is TrainData trainItem)
        {
            // 생성 요청 후 생성 시 룸 프로퍼티 갱신
            if (TrainManager.Instance != null)
                TrainManager.Instance.RequestAddTrain(trainItem.type);
        }
        // 아이템
        else if (itemData is PlayerItemData playerItem)
        {
            // 생성 위치
            Vector3 spawnPos = _itemSpawnPoint != null ? _itemSpawnPoint.position : Vector3.zero;

            // itemName 포장
            object[] initData = new object[] { playerItem.itemName };

            // 네트워크 객체 생성
            PhotonNetwork.Instantiate(playerItem.prefab.name, spawnPos, Quaternion.identity, 0, initData);
        }
    }
}
