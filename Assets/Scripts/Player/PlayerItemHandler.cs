using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerItemHandler : MonoBehaviour
{
    private PlayerInputHandler _inputHandler;
    private PlayerStatHandler _statHandler;
    private Transform _camera;

    private float _lastFireTime;    // 쿨타임 관리용
    private RaycastHit _hit;        // 공격 레이캐스트용

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
        _statHandler = GetComponent<PlayerStatHandler>();
    }
    private void Start()
    {
        // 단발 입력
        _inputHandler.OnFireEvent += OnTryUse;
    }
    private void Update()
    {
        // 연사 입력
        if (_inputHandler.IsFiring)
        {
            OnTryHoldUse();
        }
    }

    private void OnDisable()
    {
        _inputHandler.OnFireEvent -= OnTryUse;
    }

    // 아이템 사용 시도 (단발/클릭)
    private void OnTryUse()
    {
        UseItem(false);
    }

    // 아이템 사용 시도 (연사/홀드)
    private void OnTryHoldUse()
    {
        UseItem(true);
    }

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

        // 체력 회복
        _statHandler.Heal(data.healAmount);

        Debug.Log($"포션 사용: 체력 {data.healAmount} 회복");

        // 퀵슬롯에서 제거
        QuickSlotManager.Instance.RemoveItem(slotIndex, itemName);

        // + 연출
    }



    // 무기 사용
    private void UseWeapon(WeaponData data)
    {
        // 쿨타임 체크
        if (Time.time < _lastFireTime + data.fireRate) return;

        // 쿨타임 갱신
        _lastFireTime = Time.time;

        // 공격 로직
        // 카메라 중앙에서 발사
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        Debug.Log($"[공격] {data.displayName} 발사!");

        // 레이캐스트
        if (Physics.Raycast(ray, out _hit, data.range, data.hitLayer))
        {
            // 맞은 대상
            GameObject target = _hit.collider.gameObject;

            // 수리 도구면
            if (data.isRepairTool == true)
            {
                // 수리 가능한 대상인지 확인
                IRepairable repairable = target.GetComponentInParent<IRepairable>();
                if (repairable != null)
                {
                    // 수리
                    repairable.TakeRepair(data.damage);
                }
                else
                {
                    // 수리 도구로 적을 때리면 피해
                    IDamageable damageable = target.GetComponentInParent<IDamageable>();
                    if (damageable != null) damageable.TakeDamage(data.damage);
                }
            }
            // 공격 무기면
            else
            {
                // 공격 가능한 대상인지 확인
                IDamageable damageable = target.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    // 타격
                    damageable.TakeDamage(data.damage);
                }
            }
        }
    }

    // 카메라 설정
    public void SetCamera(Transform cameraTrans)
    {
        _camera = cameraTrans;
    }
}
