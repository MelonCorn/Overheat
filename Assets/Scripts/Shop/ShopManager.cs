using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
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

    [Header("상점 오디오 설정")]
    [SerializeField] ShopAudioData _audioData;
    [SerializeField] AudioSource _audioSource;

    // 활성화된 슬롯들
    private List<ShopSlotData> _activeShopSlots = new List<ShopSlotData>();
    private List<ShopUpgradeData> _activeUpgradeSlots = new List<ShopUpgradeData>();

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
            // 업그레이드 결과 이벤트
            TrainManager.Instance.OnUpgradeResult += UpgradeResult;
        }

        if(GameManager.Instance != null)
        {
            // 골드 변화 이벤트
            GameManager.Instance.OnGoldChanged += GoldChanged;
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
            TrainManager.Instance.OnUpgradeResult -= UpgradeResult;
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGoldChanged -= GoldChanged;
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
        Transform parentTrans = (itemData is PlayerItemData) ? _slotItemParent : _slotTrainParent;

        // 슬롯 생성
        ShopSlotData slot = Instantiate(_slotPrefab, parentTrans);

        if (slot != null)
        {
            // 초기화
            slot.Init(this, itemData);

            // 리스트에 등록
            _activeShopSlots.Add(slot);
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

        // 열차 데이터 확보
        Train info = trains[index];

        // 열차 딕셔너리 검색해서 데이터 가져오기
        if (TrainManager.Instance.TrainDict.TryGetValue(info.type, out TrainData data) == false) return;

        // 최대 레벨
        bool isMaxLevel = data.IsMaxLevel(info.level);

        // 업그레이드 비용
        int upgradePrice = 0;

        // 최대 레벨 아니면 업그레이드 가격 가져옴
        if (isMaxLevel == false)
            upgradePrice = data.GetBasicStat(info.level).upgradePrice;

        // 패널 기본 정보 켜기
        _trainUpgradeInfoUI.ShowUpgradeInfo(data, upgradePrice, isMaxLevel);

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
        // 버튼 누르는 소리
        PlayClickSound();

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

        int itemType = 0; // 0 아이템, 1 열차

        // 아이템 존재하면
        if (targetItem != null )
        {
            // 열차 타입이면
            if (targetItem is TrainData) itemType = 1;

            // 골드 사용 완료 시
            if (GameManager.Instance.TryUseGold(targetItem.price))
            {
                // 구매 성공
                isSuccess = true;
                // 아이템 생성
                SpawnPurchasedItem(targetItem);
            }
        }


        // 결과 RPC
        photonView.RPC(nameof(RPC_PurchaseResult), RpcTarget.All, player, isSuccess, itemType);
    }

    // 구매 결과
    [PunRPC]
    private void RPC_PurchaseResult(Player player, bool isSuccess, int itemType)
    {
        // 성공이면 사운드 재생
        if (isSuccess == true)
        {
            if (itemType == 1)
                PlaySound(_audioData.trainBuySuccess); // 열차
            else
                PlaySound(_audioData.itemBuySuccess);  // 아이템
        }

        // 결과 받고 나서

        // 내가 보낸 요청이면
        if(player == PhotonNetwork.LocalPlayer)
        {
            // 로딩 팝업 끄기
            if (_loadingPopup != null) _loadingPopup.SetActive(false);
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
        // 버튼 누르는소리
        PlayClickSound();

        if (TrainManager.Instance == null) return;

        // 현재 레벨 가져오기
        var trains = TrainManager.Instance.CurrentTrains;
        if (index < 0 || index >= trains.Count) return;

        // index 열차의 현재 레벨
        int currentLevel = trains[index].level;

        // 로딩 팝업 켜기
        if (_loadingPopup != null) _loadingPopup.SetActive(true);

        // 매니저(방장)에게 요청 (인덱스, 현재 레벨)
        TrainManager.Instance.RequestUpgradeTrain(index, currentLevel);
    }

    // 업그레이드 결과 받기
    private void UpgradeResult(bool isSuccess, string message)
    {
        // 결과 처리
        if (isSuccess)
        {
            // 로컬 업그레이드 소리
            PlayUpgradeSound();

            // 나머지들에게 전송
            photonView.RPC(nameof(RPC_PlayUpgradeSound), RpcTarget.All);
        }
        else
        {
            // 로딩 팝업 바로 끄기
            if (_loadingPopup != null) _loadingPopup.SetActive(false);
        }
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

        // 리스트 청소
        _activeUpgradeSlots.Clear();

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

                // 리스트에 추가
                _activeUpgradeSlots.Add(slot);
            }
        }

        // 로딩 팝업 끄기
        if (_loadingPopup != null) _loadingPopup.SetActive(false);
    }



    // 골드 변화
    private void GoldChanged(int currentGold)
    {
        // 일반 슬롯들
        foreach (var slot in _activeShopSlots)
        {
            // 가격 텍스트 갱신
            if (slot != null) slot. UpdatePriceState(currentGold);
        }

        // 업그레이드 슬롯들
        foreach (var slot in _activeUpgradeSlots)
        {
            // 버튼 상태 갱신
            if (slot != null) slot.UpdateUIState(currentGold);
        }

        // 팝업창들 떠있으면 갱신
        if (_itemInfoUI != null) _itemInfoUI.UpdateUIState(currentGold);
        if (_trainInfoUI != null) _trainInfoUI.UpdateUIState(currentGold);
        if (_trainUpgradeInfoUI != null) _trainUpgradeInfoUI.UpdateUIState(currentGold);
    }


    #region 사운드

    // 랜덤 클릭 사운드
    public void PlayClickSound()
    {
        if (_audioData == null) return;

        AudioClip clip = _audioData.GetRandomClickClip();

        PlaySound(clip);
    }

    // 랜덤 업그레이드 사운드

    private void PlayUpgradeSound()
    {
        AudioClip clip = _audioData.GetRandomUpgradeClip();

        PlaySound(clip);
    }

    // 소리 재생
    private void PlaySound(AudioClip clip)
    {
        if (_audioSource == null || SoundManager.Instance == null) return;

        SoundManager.Instance.PlayOneShot3D(_audioSource, clip);
    }

    // 전달 받은 업그레이드 소리 
    [PunRPC]
    private void RPC_PlayUpgradeSound()
    {
        PlayUpgradeSound();
    }
    #endregion
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
    public TextMeshProUGUI upgradePriceText;// 업글 가격

    // 팝업 가격 체크용
    private bool _isMaxLevel;            // 최대 레벨 체크
    private int _currentDisplayPrice;    // 현재 팝업에 표시되는 가격
    private bool _isUpgradePanel;        // 업그레이드팝업인지

    // 정보 갱신,켜기
    public void Show(ShopItem data)
    {
        if (panelObj == null) return;

        // 패널 켜기
        panelObj.SetActive(true);

        // 데이터 채우기
        if (iconImage != null) iconImage.sprite = data.icon;
        if (nameText != null) nameText.SetText(data.displayName);
        if (priceText != null) priceText.SetText($"{data.price:N0} G");
        if (descText != null) descText.SetText(data.desc);

        // 업그레이드 데이터
        if (statNameText != null) statNameText.SetText("");
        if (statValueText != null) statValueText.SetText("");
        if (upgradePriceText != null) upgradePriceText.SetText("");

        // 상태 기억
        _isMaxLevel = false;
        _isUpgradePanel = false;
        _currentDisplayPrice = data.price;

        // 팝업 켜면 일단 색 맞추기
        UpdateUIState(GameData.Gold);
    }

    // 끄기
    public void Hide()
    {
        if (panelObj != null) panelObj.SetActive(false);
    }

    // 업그레이드용 정보
    public void ShowUpgradeInfo(ShopItem data, int upgradePrice, bool isMaxLevel)
    {
        // 기본 정보는 똑같이
        Show(data);
        
        // 업그레이드 상태 기억
        _isUpgradePanel = true;
        // 업그레이드 가격
        _currentDisplayPrice = upgradePrice;

        // 가격은 혹시 텍스트 연결 되어있을 수 있어서 그냥 비움
        if (priceText != null) priceText.SetText("");

        // 업그레이드 가격 넣음
        if (upgradePriceText != null)
        {
            if (isMaxLevel)
            {
                _isMaxLevel = true;
                upgradePriceText.SetText("최대 레벨");
                upgradePriceText.color = Color.red;
               _currentDisplayPrice = 0; // 만렙은 비교 안함
            }
            else
            {
                upgradePriceText.SetText($"{upgradePrice:N0} G");
                upgradePriceText.color = Color.white;
            }
        }

        // 업그레이드 팝업도 갱신
        UpdateUIState(GameData.Gold);
    }


    // 팝업 갱신
    public void UpdateUIState(int currentGold)
    {
        // 꺼져있으면 계산 안 함
        if (panelObj == null || panelObj.activeSelf == false) return;

        // 업그레이드 팝업일 때 (텍스트 널체크)
        if (_isUpgradePanel && upgradePriceText != null)
        {
            // 최대 레벨은 체크 아낳
            if (_isMaxLevel == true) return;

            // 가격 비교해서 색 변경
            upgradePriceText.color = (currentGold >= _currentDisplayPrice) ? Color.white : Color.red;
        }
        // 일반 슬롯 팝업일 때
        else if (_isUpgradePanel == false && priceText != null)
        {
            priceText.color = (currentGold >= _currentDisplayPrice) ? Color.white : Color.red;
        }
    }
}