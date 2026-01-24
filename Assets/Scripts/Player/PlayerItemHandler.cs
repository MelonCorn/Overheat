using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerItemHandler : MonoBehaviourPun
{
    [Header("아이템 장착 위치)")]
    [SerializeField] Transform _fpsHandPos; // 1인칭(로컬)
    [SerializeField] Transform _tpvHandPos; // 3인칭(리모트)

    private PlayerInputHandler _inputHandler;
    private PlayerStatHandler _statHandler;
    private Transform _camera;

    // 현재 들고있는 아이템
    private GameObject _currentItem;             // 모델
    private Animator _currentItemAnim;           // 애니메이터
    private PoolableObject _currentItemPoolable; // 반납용

    private float _lastFireTime;    // 쿨타임 관리용
    private RaycastHit _hit;        // 공격 레이캐스트용

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
        _statHandler = GetComponent<PlayerStatHandler>();
    }
    private void Start()
    {
        // 내꺼 아니면 무시
        if (photonView.IsMine == false) return;

        // 단발 입력
        _inputHandler.OnFireEvent += OnTryUse;
    }
    private void Update()
    {
        // 내꺼 아니면 무시
        if (photonView.IsMine == false) return;

        // 연사 입력
        if (_inputHandler.IsFiring)
        {
            OnTryHoldUse();
        }
    }

    private void OnDisable()
    {
        // 여긴 로컬 구분없이 그냥 실행
        _inputHandler.OnFireEvent -= OnTryUse;

        // 아이템반납
        UnequipItem();
    }

    // 아이템 장착
    public void EquipItem(string itemName)
    {
        // 기존 아이템 장착 해제
        UnequipItem(); 

        // 아이템 이름 비어있으면 장착 해제까지
        if (string.IsNullOrEmpty(itemName)) return;
        if (GameManager.Instance == null) return;

        // 아이템 이름으로 데이터 가져오기
        ShopItem itemData = GameManager.Instance.FindItemData(itemName);

        // 데이터가 PlayerItemData 타입이면
        if (itemData is PlayerItemData data && data.prefab != null)
        {
            // 로컬인지 확인하고 부모 결정 (1인칭 / 3인칭)
            Transform parent = photonView.IsMine == true ? _fpsHandPos : _tpvHandPos;
            if (parent == null) return;

            // 풀링 과정
            GameObject itemObj = null;

            // 데이터의 프리팹 풀 스크립트 가져와서
            PoolableObject prefabPoolable = data.prefab.GetComponent<PoolableObject>();

            // 있으면
            if (prefabPoolable != null)
            {
                // 풀에서 가져오기
                PoolableObject newObj = PoolManager.Instance.Spawn(prefabPoolable, parent);

                // 반납용 저장
                _currentItemPoolable = newObj;

                // 생성된 오브젝트
                itemObj = newObj.gameObject;
            }
            else
            {
                // 프리팹에 풀 스크립트 없으면 그냥 생성
                itemObj = Instantiate(data.prefab, parent);
            }

            // 네트워크 아이템 스크립트
            NetworkItem networkItem = itemObj.GetComponent<NetworkItem>();

            // 네트워크 아이템이면
            if (networkItem != null)
            {
                // 로컬모드로 전환
                networkItem.SwitchToLocalMode();
            }
            // 네트워크 아이템 아니며
            else
            {
                // 수동 정리 (혹시나해서 넣음)
                var col = itemObj.GetComponent<Collider>();
                if (col) col.enabled = false;
                var pv = itemObj.GetComponent<PhotonView>();
                if (pv) pv.enabled = false;

                itemObj.transform.localPosition = Vector3.zero;
                itemObj.transform.localRotation = Quaternion.identity;
            }

            // 현재 장착 아이템 변경
            _currentItem = itemObj;

            // 아이템의 애니메이터 가져오기
            _currentItemAnim = _currentItem.GetComponent<Animator>();
        }
    }


    // 아이템 장착 해제
    private void UnequipItem()
    {
        // 장착 아이템 있으면
        if (_currentItem != null)
        {
            // 풀 반납
            if (_currentItemPoolable != null) _currentItemPoolable.Release();
            // 풀 오브젝트 아니면 파괴
            else Destroy(_currentItem);

            // 데이터 비우기
            _currentItem = null;
            _currentItemPoolable = null;
            _currentItemAnim = null;
        }
    }
    // 아이템 사용 시도 (단발/클릭)
    private void OnTryUse() => UseItem(false);

    // 아이템 사용 시도 (연사/홀드)
    private void OnTryHoldUse() => UseItem(true);

    // 아이템 사용 로직
    private void UseItem(bool isHold)
    {
        // 퀵슬롯 매니저 확인
        if (QuickSlotManager.Instance == null) return;

        // 현재 들고 있는 아이템 이름
        string itemName = QuickSlotManager.Instance.CurrentSlotItemName;
        // 현재 퀵슬롯 번호
        int slotIndex = QuickSlotManager.Instance.CurrentSlotIndex;

        // 아이템 없으면 무시
        if (string.IsNullOrEmpty(itemName)) return;

        // 아이템 데이터 검색
        if (ItemManager.Instance.ItemDict.TryGetValue(itemName, out ShopItem shopItem))
        {
            // 사용 불가능 상태(예측) 무시
            if (QuickSlotManager.Instance.IsUsable(slotIndex) == false) return;

            // 타입별 처리

            // 포션
            if (shopItem is PotionData potion)
            {
                // 포션은 클릭 시에만
                if (isHold == false)
                    UsePotion(potion, slotIndex, itemName);
            }
            // 무기
            else if (shopItem is WeaponData weapon)
            {
                // 무기는 연사 설정에 따라
                if (weapon.isAuto == isHold)
                {
                    UseWeapon(weapon);
                }
            }
        }
    }


    // 포션 사용
    private void UsePotion(PotionData data, int slotIndex, string itemName)
    {
        if (_statHandler == null) return;

        // 섭취 소리 재생

        // 체력 회복
        _statHandler.Heal(data.healAmount);

        Debug.Log($"포션 사용: 체력 {data.healAmount} 회복");

        // 퀵슬롯에서 제거
        QuickSlotManager.Instance.RemoveItem(slotIndex, itemName);
    }



    // 무기 사용
    private void UseWeapon(WeaponData data)
    {
        // 쿨타임 체크
        if (Time.time < _lastFireTime + data.fireRate) return;

        // 쿨타임 갱신
        _lastFireTime = Time.time;

        // 로컬 발사 애니메이션
        if (_currentItemAnim != null) _currentItemAnim.SetTrigger("Fire");

        // 리모트 발사 애니메이션
        photonView.RPC(nameof(RPC_PlayFireAnim), RpcTarget.Others);

        // 발사
        FireRaycast(data);
    }

    // 리모드 발사 애니메이션 재생
    [PunRPC]
    private void RPC_PlayFireAnim()
    {
        if (_currentItemAnim != null) _currentItemAnim.SetTrigger("Fire");
    }

    // 발사 레이캐스트
    private void FireRaycast(WeaponData data)
    {
        if (_camera == null) return;
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        if (Physics.Raycast(ray, out _hit, data.range, data.hitLayer))
        {
            GameObject target = _hit.collider.gameObject;
            IDamageable damageable = target.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(data.damage);
            }

            if (data.isRepairTool)
            {
                IRepairable repairable = target.GetComponentInParent<IRepairable>();
                if (repairable != null) repairable.TakeRepair(data.damage);
            }
        }
    }



    // 카메라 설정
    public void SetCamera(Transform cameraTrans)
    {
        _camera = cameraTrans;
    }
}
