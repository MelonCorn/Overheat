using Photon.Pun;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerItemHandler : MonoBehaviourPun, IPunObservable
{
    [Header("아이템 장착 위치)")]
    [SerializeField] Transform _fpsHolder; // 1인칭(로컬)
    [SerializeField] Transform _tpsHolder; // 3인칭(리모트)

    private PlayerInputHandler _inputHandler;   // 입력
    private PlayerStatHandler _statHandler;     // 스탯
    private PlayerItemMoveHandler _itemMover;   // 아이템 움직임
    private Transform _camera;

    // 현재 들고있는 아이템
    private GameObject _currentItem;             // 모델
    private Animator _currentItemAnim;           // 애니메이터
    private PoolableObject _currentItemPoolable; // 반납용

    private ItemVisualHandler _currentVisualHandler;// 현재 무기 비주얼 핸들러
    private WeaponData _currentWeaponData;          // 현재 무기 데이터
    private bool _isFiringEffectOn = false;         // 이펙트 중복 실행 방지용
    private bool _isRemoteFiring = false;

    private float _lastFireTime;    // 쿨타임 관리용
    private RaycastHit _hit;        // 공격 레이캐스트용

    private Coroutine _equipCoroutine; // 로컬 장착 코루틴

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
        _statHandler = GetComponent<PlayerStatHandler>();
        _itemMover = GetComponentInChildren<PlayerItemMoveHandler>();
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

        // 단발 아이템은 무시해도 됨
        if (_currentWeaponData == null || _currentWeaponData.isAuto == false) return;

        // 발사 상태
        bool isFiring = _inputHandler.IsFiring;

        // 자동 무기 비주얼 처리
        UpdateVisualState(isFiring);

        // 연사 입력
        if (isFiring == true)
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


    #region 비주얼

    // 클라이언트/리모트 공용 비주얼 상태 업데이트
    private void UpdateVisualState(bool isFiring)
    {
        if (_currentVisualHandler == null) return;

        // 이미 같은 상태로 들어오면 무시
        if (_isFiringEffectOn == isFiring) return;

        // 상태 갱신
        _isFiringEffectOn = isFiring;

        // 상태 변경
        if (isFiring) _currentVisualHandler.PlayLoop();
        else _currentVisualHandler.StopLoop();
    }

    // 리모트들 바뀔 때만 온오프
    [PunRPC]
    private void RPC_SetAutoVisual(bool isPlaying)
    {
        if (_currentVisualHandler == null) return;

        // 상태 따라서 루프 온오프
        if (isPlaying) _currentVisualHandler.PlayLoop();
        else _currentVisualHandler.StopLoop();
    }
    #endregion




    #region 아이템 장착

    // 아이템 장착
    public void EquipItem(string itemName)
    {
        // 로컬 플레이어
        if (photonView.IsMine == true)
        {
            // 장착 코루틴 실행중이면 중단
            if (_equipCoroutine != null) StopCoroutine(_equipCoroutine);

            // 장착 코루틴 실행
            _equipCoroutine = StartCoroutine(LocalEquip(itemName));
        }
        // 리모트 플레이어
        else
        {
            // 리모트 장착
            RemoteEquip(itemName);
        }
    }

    // 로컬 아이템 장착 코루틴
    private IEnumerator LocalEquip(string newItem)
    {
        // 기존 아이템 즉시 장착 해제
        UnequipItem();

        // 빈손이면 여기서 끝
        if (string.IsNullOrEmpty(newItem))
        {
            _equipCoroutine = null;
            yield break;
        }

        // 게임매니저 없으면 끝
        if (GameManager.Instance == null) yield break;

        // 아이템 데이터 가져오기
        ShopItem itemData = GameManager.Instance.FindItemData(newItem);

        // 무기 데이터 캐싱
        _currentWeaponData = itemData as WeaponData;

        // PlayerItemData이면 새 아이템
        // 
        if (itemData is PlayerItemData data && data.prefab != null)
        {
            CreateItem(data, _fpsHolder);
        }

        // 생성 직후 화면 아래로 강제 이동 후 올리기
        if (_itemMover != null)
        {
            // 아래로 강제 이동
            _itemMover.SnapToStartPos();

            // 혹시 몰라서 1프레임 대기
            yield return null;

            // 장착 상태 True
            _itemMover.SetEquipState(true);
        }

        // 다 실행했으니까 비우기
        _equipCoroutine = null;
    }

    // 리모트 아이템 장착
    private void RemoteEquip(string newItem)
    {
        // 아이템 즉시 장착 해제
        UnequipItem();

        // 빈손이면 여기까지
        if (string.IsNullOrEmpty(newItem)) return;
        if (GameManager.Instance == null) return;

        // 아이템 데이터 가져오기
        ShopItem itemData = GameManager.Instance.FindItemData(newItem);

        // PlayerItemData 타입이면 아이템 생성
        if (itemData is PlayerItemData data && data.prefab != null)
        {
            // 3인칭 위치에 생성
            CreateItem(data, _tpsHolder);
        }
    }


    // 아이템 생성
    private void CreateItem(PlayerItemData data, Transform parent)
    {
        // 부모 null이면 패스
        if (parent == null) return;

        // 풀링 오브젝트
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

            // 풀링 오브젝트에 생성된 오브젝트 할당
            itemObj = newObj.gameObject;
        }
        else
        {
            // 프리팹에 풀 스크립트 없으면 그냥 생성해서 할당
            itemObj = Instantiate(data.prefab, parent);
        }

        // 네트워크 아이템 스크립트
        NetworkItem networkItem = itemObj.GetComponent<NetworkItem>();

        // 네트워크 아이템이면
        if (networkItem != null)
        {
            // 로컬모드로 전환
            networkItem.SwitchToLocalMode();

            // 로컬 체크 (내꺼고, 부모가 1인칭 홀더일 때)
            bool isLocalMode = photonView.IsMine && parent == _fpsHolder;

            // 로컬 1인칭 보정
            if (isLocalMode)
            {
                // 일단 부모 기준 0,0,0
                itemObj.transform.localPosition = Vector3.zero;
                itemObj.transform.localRotation = Quaternion.identity;

                // 오프셋 적용
                itemObj.transform.localPosition = data.handPosOffset;
                itemObj.transform.localRotation = Quaternion.Euler(data.handRotOffset);
            }

            // 로컬 레이어로 변경
            networkItem.SetLayer(isLocalMode);
        }
        // 네트워크 아이템 아니면
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

        // 아이템의 비주얼 핸들러 가져오기
        _currentVisualHandler = _currentItem.GetComponent<ItemVisualHandler>();

        // 아이템의 애니메이터 가져오기
        _currentItemAnim = _currentItem.GetComponent<Animator>();
    }

    // 아이템 장착 해제
    private void UnequipItem()
    {
        // 이펙트 켜져있으면 강제로 끄기
        if (_isFiringEffectOn == true && _currentVisualHandler != null)
        {
            _currentVisualHandler.StopLoop();
            // 리모트한테도 전달
            photonView.RPC(nameof(RPC_SetAutoVisual), RpcTarget.Others, false);
        }

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
            _currentVisualHandler = null;
            _currentWeaponData = null;
            _isFiringEffectOn = false;
        }
    }
    #endregion



    #region 아이템 사용
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

        // 레이캐스트 맞은 위치 계산
        Vector3 hitPoint = GetHitPoint(data);

        // 로컬 연출 실행
        if (_currentVisualHandler != null) _currentVisualHandler.FireOneShot(hitPoint);
        // 로컬 발사 애니메이션
        if (_currentItemAnim != null) _currentItemAnim.SetTrigger("Fire");

        // 리모트 발사, 애니메이션
        photonView.RPC(nameof(RPC_FireOneShot), RpcTarget.Others, hitPoint);

        // 발사
        Attack(data);
    }

    // 레이캐스트 맞은 위치 계산
    private Vector3 GetHitPoint(WeaponData data)
    {
        // 카메라 없으면 정면에 사거리만큼
        if (_camera == null) return transform.position + transform.forward * data.range;

        // 원점으로부터 정면 방향으로 레이 생성
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        // 레이를 쏴서 맞으면 그 위치 안 맞으면 사거리 끝 위치
        if (Physics.Raycast(ray, out _hit, data.range, data.hitLayer))
        {
            return _hit.point;
        }
        else
        {
            return ray.origin + (ray.direction * data.range);
        }
    }

    [PunRPC]
    private void RPC_FireOneShot(Vector3 hitPoint)
    {
        // 리모트들은 3인칭전용에서 발사 이뤄짐
        if (_currentVisualHandler != null)
        {
            _currentVisualHandler.FireOneShot(hitPoint);
        }

        // 애니메이션까지
        if (_currentItemAnim != null) _currentItemAnim.SetTrigger("Fire");
    }

    // 발사 레이캐스트
    private void Attack(WeaponData data)
    {
        // 레이캐스트 계산에서 히트되었을 때
        if (_hit.collider != null)
        {
            // 히트 콜라이더의 오브젝트
            GameObject target = _hit.collider.gameObject;
            // 그 오브젝트의 부모의 피해 인터페이스
            IDamageable damageable = target.GetComponentInParent<IDamageable>();
            // 피해주기
            if (damageable != null) damageable.TakeDamage(data.damage);

            // 수리도구면
            if (data.isRepairTool == true)
            {
                // 수리 인터페이스
                IRepairable repairable = target.GetComponentInParent<IRepairable>();
                // 수리
                if (repairable != null) repairable.TakeRepair(data.damage);
            }
        }
    }

    #endregion



    // 카메라 설정
    public void SetCamera(Transform cameraTrans)
    {
        _camera = cameraTrans;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) 
        {
            stream.SendNext(_inputHandler.IsFiring);
        }
        else
        {
            // 데이터 받아서
            bool receiveFiring = (bool)stream.ReceiveNext();

            // 값 바뀌면 갱신
            if (_isRemoteFiring != receiveFiring)
            {
                _isRemoteFiring = receiveFiring;

                // 여기서 바로 갱신
                UpdateVisualState(_isRemoteFiring);
            }
        }
    }
}
