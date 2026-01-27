using Photon.Pun;
using Photon.Realtime;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviourPun
{
    [Header("구매 대기 UI")]
    [SerializeField] GameObject _loadingPopup; // 결제 대기 팝업

    [Header("구매 물품이 나올 위치")]
    [SerializeField] Transform _itemSpawnPoint;

    [Header("상점 품목 버튼 설정")]
    [SerializeField] ShopSlotData _slotPrefab;      // 항목 버튼 프리팹
    [SerializeField] Transform _slotItemParent;     // 아이템 부모 트랜스폼
    [SerializeField] Transform _slotTrainParent;    // 열차 부모 트랜스폼

    [Header("아이템 정보 UI")]
    [SerializeField] ShopInfoPanel _itemInfoUI;          // 아이템용 패널 묶음
    [SerializeField] ShopInfoPanel _trainInfoUI;         // 열차용 패널 묶음
    [SerializeField] ShopInfoPanel _trainUpgradeInfoUI;  // 열차용 업그레이드 패널 묶음

    [Header("열차 업그레이드 설정")]
    [SerializeField] ShopUpgradeData _upgradePrefab;    // 열차 업그레이드 리스트용 프리팹
    [SerializeField] Transform _trainUpgradeParent;     // 열차 리스트 부모

    private void Start()
    {
        // 시작하면 상점 항목들 생성
        GenerateSlots();

        if (TrainManager.Instance != null)
        {
            // 시작하자마자 한번
            RefreshTrainList();

            // 열차 룸 프로퍼티 변경될 때마다 실행
            TrainManager.Instance.OnTrainListUpdated += RefreshTrainList;
        }

        // 일단 정보 패널 비활성화
        if (_itemInfoUI != null) _itemInfoUI.Hide();
        if (_trainInfoUI != null) _trainInfoUI.Hide();
        if (_trainUpgradeInfoUI != null) _trainUpgradeInfoUI.Hide();
    }
    private void OnDestroy()
    {
        if (TrainManager.Instance != null)
        {
            TrainManager.Instance.OnTrainListUpdated -= RefreshTrainList;
        }
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
        // 부모
        Transform parentTrans = null;

        // 부모 지정
        if (itemData is PlayerItemData)
            parentTrans = _slotItemParent;
        else
            parentTrans = _slotTrainParent;

        // 버튼 생성
        ShopSlotData slot = Instantiate(_slotPrefab, parentTrans);

        if (slot != null)
        {
            // 초기화 (ShopManager, ShopItem)
            slot.Init(this, itemData);
        }
    }

   

    #region 상점 호버
    // 아이템 정보 켜기
    public void ShowItemInfo(ShopItem itemData)
    {
        _itemInfoUI.Show(itemData);
    }

    // 아이템 정보 숨기기
    public void HideItemInfo()
    { 
        _itemInfoUI.Hide();
    }

    // 열차 정보 켜기
    public void ShowTrainInfo(ShopItem itemData)
    {
        _trainInfoUI.Show(itemData);
    }

    // 열차 정보 숨기기
    public void HideTrainInfo()
    {
        _trainInfoUI.Hide();
    }

    // 열차 업그레이드 정보 켜기
    public void ShowTrainUpgradeInfo(int index)
    {
        if (TrainManager.Instance == null) return;

        // 열차 가져오기
        var trains = TrainManager.Instance.CurrentTrains;

        // 범위 체크
        if (index < 0 || index >= trains.Count) return;

        // 데이터 확보
        Train info = trains[index];

        // 열차 딕셔너리 검색해서 데이터 가져오기
        if (TrainManager.Instance.TrainDict.TryGetValue(info.type, out TrainData data) == false) return;

        // 패널 기본 정보 켜기
        _trainUpgradeInfoUI.Show(data);

        // 스탯 리스트 문자열 합치기
        var statList = data.GetUpgradeInfos(info.level);

        // 스트링빌더로 줄바꿈 쌓기
        StringBuilder nameBuilder = new StringBuilder();
        StringBuilder valueBuilder = new StringBuilder();

        // 스탯별로
        foreach (var stat in statList)
        {
            // 이름 줄바꿈 추가
            nameBuilder.AppendLine(stat.name);
            // 값 줄바꿈 추가
            valueBuilder.AppendLine(stat.value);
        }

        // 텍스트 설정
        if (_trainUpgradeInfoUI.statNameText)
            _trainUpgradeInfoUI.statNameText.SetText(nameBuilder);
        if (_trainUpgradeInfoUI.statValueText)
            _trainUpgradeInfoUI.statValueText.SetText(valueBuilder);
    }

    // 열차 업그레이드 정보 숨기기
    public void HideTrainUpgradeInfo()
    {
        _trainUpgradeInfoUI.Hide();
    }

    #endregion




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

            // 좀 흩뿌리기
            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f));

            // itemName 포장
            object[] initData = new object[] { playerItem.itemName };

            // 네트워크 객체 생성
            PhotonNetwork.Instantiate(playerItem.prefab.name, spawnPos + randomOffset, Quaternion.identity, 0, initData);
        }
    }

    // 업그레이드 버튼에 연결할 것
    public void TryUpgradeTrain(int index)
    {
        TrainManager.Instance.RequestUpgradeTrain(index);
    }


    // 열차 갱신 시 리스트 버튼 새로고침
    public void RefreshTrainList()
    {
        if (_trainUpgradeParent == null || TrainManager.Instance == null) return;

        // 기존 프리팹 싹 밀기 (혹시 몰라서 역순)
        for (int i = _trainUpgradeParent.childCount - 1; i >= 0; i--)
        {
            Transform child = _trainUpgradeParent.GetChild(i);
            PoolableObject poolObj = child.GetComponent<PoolableObject>();

            if (poolObj != null)
            {
                // 풀로 반환
                poolObj.Release();
            }
            else
            {
                // 혹시나 풀 안붙어있으면 없으면 파괴
                Destroy(child.gameObject);
            }
        }

        var trains = TrainManager.Instance.CurrentTrains;   // 현재 열차
        var trainDict = TrainManager.Instance.TrainDict;    // 열차 딕셔너리 (타입이 키, 데이터가 밸류)

        PoolableObject poolPrefab = _upgradePrefab.GetComponent<PoolableObject>();

        // 풀 오브젝트 아님 ㄷㄷ
        if (poolPrefab == null) return;

        // 현재 열차 순회
        for (int i = 0; i < trains.Count; i++)
        {
            // {type, level}
            Train info = trains[i];

            // 딕셔너리에서 타입에 데이터 가져오기
            if (trainDict.TryGetValue(info.type, out TrainData data))
            {
                // 풀에서 가져오기
                PoolableObject spawnedObj = PoolManager.Instance.Spawn(poolPrefab, _trainUpgradeParent);

                // 정면보기
                spawnedObj.transform.localRotation = Quaternion.identity;
                // 월드 스페이스 캔버스라 이거 안해주면 겁나 크게 나옴
                spawnedObj.transform.localScale = Vector3.one;

                // 스크립트 가져와서
                ShopUpgradeData slot = spawnedObj.GetComponent<ShopUpgradeData>();

                // 초기화
                slot.Init(this, data, i, info.level);
            }
        }
    }

}







// 상점 패널용 클래스

[System.Serializable]
public class ShopInfoPanel
{
    [Header("UI 연결")]
    public GameObject panelObj;         // 패널 전체
    public Image iconImage;             // 아이콘
    public TextMeshProUGUI nameText;    // 이름
    public TextMeshProUGUI priceText;   // 가격
    public TextMeshProUGUI descText;    // 설명


    [Header("업그레이드용")]
    public TextMeshProUGUI statNameText;    // 스탯 이름
    public TextMeshProUGUI statValueText;   // 스탯 수치

    // 정보 갱신,켜기
    public void Show(ShopItem data)
    {
        if (panelObj == null) return;

        // 패널 켜기
        panelObj.SetActive(true);

        // 데이터 채우기 (null 체크 포함)
        if (iconImage != null) iconImage.sprite = data.icon;
        if (nameText != null) nameText.SetText(data.displayName);
        if (priceText != null) priceText.SetText($"{data.price:N0} G");
        if (descText != null) descText.SetText(data.desc);


        if (statNameText != null) descText.SetText("");
        if (statValueText != null) descText.SetText("");
    }

    // 끄기
    public void Hide()
    {
        if (panelObj != null) panelObj.SetActive(false);
    }
}