using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerItemHandler : MonoBehaviourPun, IPunObservable
{
    private PlayerSoundHandler _soundHandler;

    [Header("아이템 장착 위치)")]
    [SerializeField] Transform _fpsHolder; // 1인칭(로컬)
    [SerializeField] Transform _tpsHolder; // 3인칭(리모트)

    [Header("적 레이어")]
    [SerializeField] LayerMask _enemyLayer;

    private PlayerHandler _playerHandler;       // 두뇌
    private PlayerInputHandler _inputHandler;   // 입력
    private PlayerStatHandler _statHandler;     // 스탯
    private PlayerRecoilHandler _recoilHandler; // 반동
    private PlayerItemMoveHandler _itemMover;   // 아이템 움직임
    private Transform _camera;

    // 현재 들고있는 아이템
    private GameObject _currentItem;             // 모델
    private PoolableObject _currentItemPoolable; // 반납용

    private ItemVisualHandler _currentVisualHandler;// 현재 무기 비주얼 핸들러
    private WeaponData _currentWeaponData;          // 현재 무기 데이터
    private bool _isFiringEffectOn = false;         // 이펙트 중복 실행 방지용

    // 네트워크 동기화 데이터
    private bool _isRemoteFiring = false;
    private int _poseID = 0;                // 무기 자세 ID
    private float _aimAngle = 0f;           // 조준 각도 (위아래)
    private float _targetWeight = 0f;       // 목표 웨이트

    private float _lastFireTime;    // 쿨타임 관리용
    private RaycastHit _hit;        // 공격 레이캐스트용

    private Coroutine _equipCoroutine; // 로컬 장착 코루틴

    private void Awake()
    {
        _playerHandler = GetComponent<PlayerHandler>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        _statHandler = GetComponent<PlayerStatHandler>();
        _soundHandler = GetComponent<PlayerSoundHandler>();
        _recoilHandler = GetComponentInChildren<PlayerRecoilHandler>();
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
        // 내꺼 아니면
        if (photonView.IsMine == false)
        {
            // 애니메이터 레이어 웨이트 부드럽게 전환
            UpdateAnimatorWeight();

            // 딱 그까지만
            return;
        }

        // 각도 계산
        AimAngle();

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


    #region 애니메이션
    private void AimAngle()
    {
        if (_camera == null) return;

        // 카메라의 X축 회전값 가져오기
        float angle = _camera.localEulerAngles.x;

        // 0 ~ 360 를 -180 ~ 180으로 변환
        if (angle > 180) angle -= 360;

        // 동기화 변수에 저장
        _aimAngle = angle;
    }

    // 무기 타입 -> 자세 ID 변환 (매핑)
    private int GetPoseID(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Revolver:
                return 1;

            case WeaponType.SMG:
            case WeaponType.Shotgun:
            case WeaponType.BoltAction:
                return 2;

            case WeaponType.Welder:
            case WeaponType.Extinguisher:
                return 0; 

            default:
                return 0;
        }
    }

    // 애니메이터 갱신
    private void UpdateAnimPose(int poseID)
    {
        if (_playerHandler.PlayerAnim == null) return;

        _playerHandler.PlayerAnim.SetInteger("PoseID", poseID);

        // 포즈에 따라 상체 웨이트 값 목표 지정
        if (poseID == 0) _targetWeight = 0f; // 맨손
        else _targetWeight = 1f;             // 무기
    }

    // 애니메이터 레이어 웨이트 부드럽게 전환
    private void UpdateAnimatorWeight()
    {
        if (_playerHandler.PlayerAnim == null) return;

        // 현재 웨이트값
        float currentWeight = _playerHandler.PlayerAnim.GetLayerWeight(1);

        // 현재 값과 목표 값이 다르면 
        if (Mathf.Abs(currentWeight - _targetWeight) > 0.01f)
        {
            // 부드럽게 전환
            float newWeight = Mathf.Lerp(currentWeight, _targetWeight, Time.deltaTime * 10f);
            _playerHandler.PlayerAnim.SetLayerWeight(1, newWeight);
        }
    }
    #endregion


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
        if (_currentWeaponData != null && _recoilHandler != null)
        {
            // 반동핸들러에도 적용
            _recoilHandler.SetWeaponData(_currentWeaponData);
        }

        // PlayerItemData이면 새 아이템
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
            PoolableObject newObj = null;

            // 풀에서 가져오기
            if (PoolManager.Instance != null)
            {
                newObj = PoolManager.Instance.Spawn(prefabPoolable, parent);
            }

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

        // 무기면 
        if (_currentVisualHandler != null && data is WeaponData weaponData)
        {
            // 로컬 확인
            _currentVisualHandler.Init(photonView.IsMine);

            // 확실하게
            _currentWeaponData = weaponData;

            // 무기 타입의 자세 ID
            _poseID = GetPoseID(weaponData.type);
        }
        else
        {
            // 맨손 자세
            _poseID = 0;
            _currentWeaponData = null;
        }
        
        // 애니메이터 즉시 적용
        UpdateAnimPose(_poseID);
    }

    // 아이템 장착 해제
    private void UnequipItem()
    {
        // 이펙트 켜져있으면 강제로 끄기
        if (_isFiringEffectOn == true && _currentVisualHandler != null)
        {
            _currentVisualHandler.StopLoop();

            // 리모트한테도 전달
            if (photonView.IsMine)
            {
                photonView.RPC(nameof(RPC_SetAutoVisual), RpcTarget.Others, false);
            }
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
            _currentVisualHandler = null;
            _currentWeaponData = null;
            _isFiringEffectOn = false;
        }

        // 자세 초기화
        _poseID = 0;
        UpdateAnimPose(0);
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

        // 체력 회복
        _statHandler.Heal(data.healAmount);

        // 섭취 소리 재생
        if (_soundHandler != null)
        {
            _soundHandler.PlayEatSound(data.itemName);
        }

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

        // 카메라 반동
        if (_recoilHandler != null)
            _recoilHandler.RecoilFire(data);

        // 비주얼 반동
        if (_itemMover != null)
            _itemMover.AddWeaponRecoil(data);

        // 탄 수가 1개보다 많으면 산탄 로직
        if (data.pelletCount > 1)
        {
            UseShotgun(data);
        }
        // 아니면 단발 로직
        else
        {
            UseOneShot(data);
        }
    }

    // 단발로직
    private void UseOneShot(WeaponData data)
    {
        Vector3 hitPoint = GetHitPoint(data, out bool isEnemy, out Vector3 normal);

        if (data.type == WeaponType.Extinguisher)
        {
            Attack(data, _hit);
            return;
        }

        // 맞았는지 안맞았는지
        bool isHit = _hit.collider != null;

        // 발사 사운드
        if (_soundHandler != null)
        {
            // 사운드 재생
            _soundHandler.PlayFireSound(data);
        }


        if (_currentVisualHandler != null) _currentVisualHandler.FireImpact(hitPoint, isEnemy, normal, isHit);

        photonView.RPC(nameof(RPC_FireOneShot), RpcTarget.Others, hitPoint, isEnemy, normal, isHit);

        Attack(data, _hit);
    }

    // 산탄총 발사 로직
    private void UseShotgun(WeaponData data)
    {
        // 여러 발 히트 정보 계산
        // 네트워크 전송용 리스트
        List<Vector3> hitPoints = new List<Vector3>();
        List<bool> isEnemies = new List<bool>();
        List<Vector3> normals = new List<Vector3>();
        List<bool> isHits = new List<bool>();

        // 산탄 수 만큼
        for (int i = 0; i < data.pelletCount; i++)
        {
            // 랜덤 확산 적용된 히트 정보
            RaycastHit hitInfo;
            Vector3 hitPoint = GetSpreadHitPoint(data, out bool isEnemy, out Vector3 normal, out hitInfo);

            // 맞았는지
            bool isHit = hitInfo.collider != null;

            // 리스트에 추가 (이펙트용)
            hitPoints.Add(hitPoint);
            isEnemies.Add(isEnemy);
            normals.Add(normal);
            isHits.Add(isHit);


            // 개별 데미지 처리 (히트만)
            if (hitInfo.collider != null)
            {
                Attack(data, hitInfo);
            }
        }

        // 발사 사운드
        if (_soundHandler != null)
        {
            // 사운드 재생
            _soundHandler.PlayFireSound(data);
        }

        // 로컬 연출
        if (_currentVisualHandler != null)
        {
            // 반복문으로 
            for (int i = 0; i < hitPoints.Count; i++)
            {
                _currentVisualHandler.FireImpact(hitPoints[i], isEnemies[i], normals[i], isHits[i]);
            }
        }

        // 리모트 배열로 변환해서 전송
        photonView.RPC(nameof(RPC_FireShotgun), RpcTarget.Others,
            hitPoints.ToArray(), isEnemies.ToArray(), normals.ToArray(), isHits.ToArray());
    }

    // 탄퍼짐 히트 포인트 계산
    private Vector3 GetSpreadHitPoint(WeaponData data, out bool isEnemy, out Vector3 normal, out RaycastHit hitInfo)
    {
        isEnemy = false;
        normal = Vector3.zero;
        hitInfo = new RaycastHit();

        // 카메라 없으면 그냥 앞에
        if (_camera == null) return transform.position + transform.forward * data.range;

        // 탄퍼짐 계산
        // 화면 중앙에서 랜덤한 원형 범위 내의 점
        Vector2 spread = Random.insideUnitCircle * data.spreadAngle * 0.01f; // 0.01f는 민감도 조절용

        // 카메라 앞방향 + 랜덤 퍼짐 (X, Y축 기준)
        Vector3 direction = _camera.forward +
                           (_camera.right * spread.x) +
                           (_camera.up * spread.y);

        // 정규화
        direction.Normalize();

        // 그 방향 레이
        Ray ray = new Ray(_camera.position, direction);

        // 레이캐스트
        if (Physics.Raycast(ray, out hitInfo, data.range, data.hitLayer))
        {
            // 적이면 적 트루
            if ((_enemyLayer.value & (1 << hitInfo.collider.gameObject.layer)) != 0)
            {
                isEnemy = true;
            }
            // 표면 방향
            normal = hitInfo.normal;
            
            // 히트 포인트
            return hitInfo.point;
        }
        // 아무도 안맞앗으면
        else
        {
            // 안맞으면 Quaternion.LookRotation(Vector3.zero) 되니까
            normal = -direction;

            // 끝점
            return ray.origin + (ray.direction * data.range);
        }
    }

    // 레이캐스트 맞은 위치 계산
    private Vector3 GetHitPoint(WeaponData data, out bool isEnemy, out Vector3 normal)
    {
        // out 기본값  
        isEnemy = false;        // (벽)
        normal = Vector3.zero;  // 표면 방향

        // 카메라 없으면 정면에 사거리만큼
        if (_camera == null) return transform.position + transform.forward * data.range;

        // 원점으로부터 정면 방향으로 레이 생성
        Ray ray = new Ray(_camera.transform.position, _camera.transform.forward);

        // 레이를 쏴서 맞으면 그 위치 안 맞으면 사거리 끝 위치
        if (Physics.Raycast(ray, out _hit, data.range, data.hitLayer))
        {
            // 적인지 체크
            if ((_enemyLayer.value & (1 << _hit.collider.gameObject.layer)) != 0)
            {
                isEnemy = true;
            }

            // 표면 방향
            normal = _hit.normal;

            // 위치
            return _hit.point;
        }
        // 안 맞았으면 끝점
        else
        {
            // 안맞으면 Quaternion.LookRotation(Vector3.zero) 되니까
            normal = -ray.direction;

            return ray.origin + (ray.direction * data.range);
        }
    }

    // 산탄 RPC (배열로 받음)
    [PunRPC]
    private void RPC_FireShotgun(Vector3[] hitPoints, bool[] isEnemies, Vector3[] normals, bool[] isHits)
    {
        // 발사 사운드
        if (_soundHandler != null)
        {
            // 사운드 재생
            _soundHandler.PlayFireSound(_currentWeaponData);
        }

        if (_currentVisualHandler != null)
        {
            // 받은 배열만큼 반복해서 이펙트
            for (int i = 0; i < hitPoints.Length; i++)
            {
                _currentVisualHandler.FireImpact(hitPoints[i], isEnemies[i], normals[i], isHits[i]);
            }
        }
    }

    // 단발 RPC
    [PunRPC]
    private void RPC_FireOneShot(Vector3 hitPoint, bool isEnemy, Vector3 normal, bool isHit)
    {
        // 발사 사운드
        if (_soundHandler != null)
        {
            // 사운드 재생
            _soundHandler.PlayFireSound(_currentWeaponData);
        }


        // 리모트들은 3인칭전용에서 발사 이뤄짐
        if (_currentVisualHandler != null)
        {
            _currentVisualHandler.FireImpact(hitPoint, isEnemy, normal, isHit);
        }
    }

    // 발사 레이캐스트
    private void Attack(WeaponData data, RaycastHit hitInfo)
    {
        // 레이캐스트 계산에서 히트되었을 때
        if (hitInfo.collider != null)
        {
            GameObject target = hitInfo.collider.gameObject;

            // 수리 도구인지 확인
            if (data.isRepairTool)
            {
                // 수리 가능한지 먼저 체크
                IRepairable repairable = target.GetComponentInParent<IRepairable>();

                if (repairable != null)
                {
                    // 수리 대상이면 수리하고 끝
                    repairable.TakeRepair(data.damage);
                    return; 
                }

                // 수리 대상이 아니면 공격시도
                IDamageable damageable = target.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    // 공격
                    damageable.TakeDamage(data.damage);
                }
            }
            // 일반 무기면
            else
            {
                // 수리 가능하면 데미지 안줌 (소화기는 제외 불때문에 패런츠가 열차로 잡힘)
                if (target.GetComponentInParent<IRepairable>() != null && _currentWeaponData.type != WeaponType.Extinguisher)
                {
                    return; // 팀킬 방지
                }

                // 그냥 공격
                IDamageable damageable = target.GetComponentInParent<IDamageable>();
                if (damageable != null) damageable.TakeDamage(data.damage);
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
            // 발사 상태
            stream.SendNext(_inputHandler.IsFiring);

            // 조준 각도
            stream.SendNext(_aimAngle);
        }
        else
        {
            // 데이터 받아서
            bool receiveFiring = (bool)stream.ReceiveNext();
            _aimAngle = (float)stream.ReceiveNext();

            // 값 바뀌면 갱신

            // 발사 상태
            if (_isRemoteFiring != receiveFiring)
            {
                _isRemoteFiring = receiveFiring;

                // 여기서 바로 갱신
                UpdateVisualState(_isRemoteFiring);
            }
        }
    }
}
