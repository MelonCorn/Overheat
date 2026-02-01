using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;

public class NetworkItem : MonoBehaviourPun, IPunInstantiateMagicCallback, IInteractable
{
    public string ItemName { get; private set; }

    [Header("상호작용 오디오 데이터")]
    [SerializeField] ObjectAudioData _audioData;

    private Collider _collider;         // 줍기용
    private Renderer[] _renderers;      // 일단 피드백용 렌더러
    private PhotonView _pv;             // 일반, 네트워크 스위칭

    // 상태 플래그
    private bool _isPredicting = false;     // 픽업 시 승인 예측 (승인 대기 상태로 일단 로컬은 픽업한 걸로 침)
    private bool _isConfirmed = false;      // 픽업 시 승인 성공 (로컬은 승인 완료로 확실히 본인 것으로 받아들임)
    private bool _isPickUped = false;       // 누군가 픽업한 상태

    // 들어간 퀵슬롯 번호 기억
    private int _slotIndex = -1;

    private void Awake()
    {
        _collider = GetComponentInChildren<Collider>();
        _renderers = GetComponentsInChildren<Renderer>();
        _pv = GetComponent<PhotonView>();
    }

    // 네트워크 풀링 대비
    private void OnEnable()
    {
        // 상태 리셋
        _isPredicting = false;
        _isConfirmed = false;
        _isPickUped = false;
        _slotIndex = -1;

        // 비주얼 다시 켜기
        SetVisual(true);

        // 기본 상태는 "Item" 레이어
        SetLayer(false);

        // 이전 선반용으로 사용되었을 수 있기 때문에 켜줌
        if (_collider != null) _collider.enabled = true;
        if (_pv != null) _pv.enabled = true;
    }
    
    // 비활성화될 때 (보험용 / RPC 누락 대비)
    private void OnDisable()
    {
        // 예측 중인데 확정도 못 받았다면
        if (_isPredicting && !_isConfirmed)
        {
            // 롤백
            RollbackItem();
        }
    }

    // 비주얼 세팅 (스크립트는 돌아야 하기 때문)
    private void SetVisual(bool isActive)
    {
        if (_collider != null) _collider.enabled = isActive;
        foreach (var rend in _renderers) rend.enabled = isActive;
    }

    #region 픽업

    // 아이템 픽업 시
    public void OnPickItem(int slotIndex)
    {
        _isPredicting = true;       // 예측중 (미리 선조치)
        _isConfirmed = false;       // 픽업 확정

        _slotIndex = slotIndex;     // 슬롯 번호 저장

        SetVisual(false);           // 예측으로 일단 숨김

        // 방장에게 픽업 요청
        photonView.RPC(nameof(RPC_TryPickUp), RpcTarget.MasterClient);
    }

    // 방장이 픽업 판정
    [PunRPC]
    private void RPC_TryPickUp(PhotonMessageInfo info)
    {
        if (_isPickUped) return; // 이미 누가 가져가서 무시

        _isPickUped = true; // 픽업됨

        // 픽업 주인 알림
        photonView.RPC(nameof(RPC_PickUpComplete), RpcTarget.All, info.Sender);

        // 지연시간 두고 파괴
        // 픽업 주인을 알려서 살짝 늦게 픽업한 사람은 롤백을 시키기 위함
        StartCoroutine(DestroyCoroutine());
    }

    // 픽업 완료 알림
    [PunRPC]
    private void RPC_PickUpComplete(Player player)
    {
        // 본인이 픽업했다면
        if (player.IsLocal)
        {
            _isConfirmed = true; // 픽업 확정

            // 퀵슬롯 사용 허가
            if (QuickSlotManager.Instance != null && _slotIndex != -1)
            {
                QuickSlotManager.Instance.ConfirmItem(_slotIndex, ItemName);
                Debug.Log($"아이템 픽업 확정");
            }
        }
        // 본인이 픽업했지만 살짝 늦은 경우
        else if (_isPredicting == true)
        {
            Debug.Log($"아이템 픽업 늦음. 롤백");
            // 롤백으로 퀵슬롯 제거
            RollbackItem();
        }
        // 아예 관련 없는 플레이어
        else
        {
            SetVisual(false);
        }
    }

    // 픽업 살짝 늦어서 아이템 롤백
    private void RollbackItem()
    {
        if (QuickSlotManager.Instance != null && _slotIndex != -1)
        {
            Debug.Log($"[픽업 실패] {_slotIndex}번 슬롯 {ItemName} 롤백합니다.");

            // 퀵슬롯에서 제거 (슬롯 번호, 아이템 ID)
            QuickSlotManager.Instance.RemoveItem(_slotIndex, ItemName);

            // 롤백 완료했으니까 예측 상태 해제 (OnDestroy에서 두 번 실행 안 되게)
            _isPredicting = false;
            _slotIndex = -1;
        }
    }
    #endregion

    #region 파괴
    // 파괴 코루틴
    private IEnumerator DestroyCoroutine()
    {
        // 픽업 완료 알림을 받고 나서 파괴하기 위함
        yield return new WaitForSeconds(0.3f);

        // 네트워크 객체 파괴
        if (photonView.IsMine)
        {
            // 내가 주인이면 바로 파괴
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            // 주인이 따로 있으면 파괴 명령
            photonView.RPC(nameof(RPC_DestroySelf), photonView.Owner);
        }
    }

    // 파괴
    [PunRPC]
    private void RPC_DestroySelf()
    {
        // 주인만
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    #endregion



    // 생성 시 네트워크 데이터 풀어서 이름 적용
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = info.photonView.InstantiationData;

        if (data != null && data.Length > 0)
        {
            ItemName  = (string)data[0];
        }
    }

    public AudioClip OnInteract()
    {
        // 활성화 상태 체크
        if (gameObject.activeSelf == false) return null;

        Debug.Log("아이템을 퀵슬롯에 추가 시도");

        // 퀵슬롯 매니저에게 넣어달라고 요청
        int slotIndex = QuickSlotManager.Instance.TryAddItem(ItemName);

        // 성공 시 픽업 처리
        if (slotIndex != -1)
        {
            OnPickItem(slotIndex);
            return _audioData.GetRandomClip();
        }
        else
        {
            Debug.Log("인벤토리가 꽉 찼습니다");
            return null;
        }
    }

    public string GetInteractText(out bool canInteract)
    {
        canInteract = false;

        // 예측 중이거나 비활성이면 텍스트 없음
        if (_isPredicting || _isPickUped || !gameObject.activeSelf) return "";

        canInteract = true;
        return "획득";
    }


    // 선반에 놓을 때 호출
    // 선반모드
    public void SwitchToLocalMode()
    {
        // 소켓보다 콜라이더 크면 안되니까
        if (_collider != null) _collider.enabled = false;

        // ViewID 충돌 있을 수도 있고 끄면 걍 네트워크 동기화 차단됨
        if (_pv != null) _pv.enabled = false;

        // 위치 초기화
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }


    // 레이어 변경 (로컬인지)
    public void SetLayer(bool isLocal)
    {
        // 렌더러 전부 가져오기
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        // 로컬이면 LocalItem
        // 아니면 Item
        int targetLayer = isLocal ?
                          LayerMask.NameToLayer("LocalItem") :
                          LayerMask.NameToLayer("Item");

        // 내 자신 레이어 변경
        gameObject.layer = targetLayer;

        // 렌더러들 레이어 변경
        if (renderers != null)
        {
            foreach (var renderer in renderers)
            {
                if (renderer != null) renderer.gameObject.layer = targetLayer;
            }
        }
    }
}
