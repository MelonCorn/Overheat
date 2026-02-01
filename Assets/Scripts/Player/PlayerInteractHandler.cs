using UnityEngine;
using Photon.Pun;
using TMPro;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerInteractHandler : MonoBehaviour
{
    private PlayerMovementHandler _movementHandler;
    private PlayerInputHandler _inputHandler;
    private PlayerItemHandler _itemHandler;
    private PlayerSoundHandler _soundHandler;

    [Header("드랍 오디오 데이터")]
    [SerializeField] ObjectAudioData _dropAudioData;

    [Header("상호작용 텍스트")]
    [SerializeField] TextMeshProUGUI _interactText;

    [Header("상호작용 설정")]
    [SerializeField] float _reachDistance = 3f;     // 사거리
    [SerializeField] LayerMask _interactLayer;      // 상호작용 레이어

    private Transform _camera;
    private RaycastHit _hit;

    // 현재 바라보고 있는 상호작용 대상
    private IInteractable _currentInteractable;

    private void Awake()
    {
        _movementHandler = GetComponent<PlayerMovementHandler>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        _itemHandler = GetComponent<PlayerItemHandler>();
        _soundHandler = GetComponent<PlayerSoundHandler>();
    }

    private void Start()
    {
        _inputHandler.OnInteractEvent += Interact;  // 상호작용 이벤트 등록
        _inputHandler.OnDropEvent += TryDropItem;   // 아이템 버리기 등록
    }

    void Update()
    {
        // 상호작용 대상 찾기
        CheckHoverInteract();
    }

    // 카메라 설정
    public void SetCamera(Transform cameraTrans)
    {
        _camera = cameraTrans;
    }


    // 상호작용 대상 탐색
    private void CheckHoverInteract()
    {
        // 초기화
        _currentInteractable = null;
        
        string prompt = "";         // 내용
        bool canInteract = false;   // 상호작용 가능 여부

        // 레이 시작점, 방향
        Ray ray = new Ray(_camera.position, _camera.forward);

        // 레이캐스트
        if (Physics.Raycast(ray, out _hit, _reachDistance, _interactLayer))
        {
            _currentInteractable = _hit.collider.GetComponentInParent<IInteractable>();

            if (_currentInteractable != null)
            {
                // 상호작용 문구 가져오기
                prompt = _currentInteractable.GetInteractText(out canInteract);

                // 만약 텍스트가 비어있으면 상호작용 불가 상태
                if (string.IsNullOrEmpty(prompt))
                {
                    _currentInteractable = null;
                }
            }
        }

        // UI 적용
        if (_interactText != null)
        {
            // 상호작용 가능하면 'F' 붙이고
            // 아니면 텍스트만 출력
            if (canInteract)
                _interactText.SetText($"<color=#FFD000>[F]</color> {prompt}");
            else
                // 안내 문구
                _interactText.SetText($"{prompt}");

            _interactText.gameObject.SetActive(!string.IsNullOrEmpty(prompt));
        }
    }


    // 인풋 핸들러에 등록
    // 상호작용
    private void Interact()
    {
        // 상호작용 대상 존재하면
        if (_currentInteractable != null)
        {
            // 대상이 아이템이면
            bool isNetworkItem = _currentInteractable is NetworkItem;

            if (isNetworkItem == true && _itemHandler != null)
            {
                // 장착 사운드 스킵 설정
                _itemHandler.SkipEquipSound();
            }

            // 상호작용 실행
            AudioClip clip = _currentInteractable.OnInteract();

            if (clip != null && _soundHandler != null)
            {
                // 상호작용 사운드 재생
                _soundHandler.PlayInteractSound(clip);
            }
        }
    }

    // 인풋 핸들러에 등록
    // 아이템 버리기 시도
    private void TryDropItem()
    {
        // 퀵슬롯 매니저에서 들고있는 아이템 떨구기 요청
        // (예측 중이면 null 반환)
        string itemName = QuickSlotManager.Instance.TryDropItem();

        // 꺼낸 게 없으면 중단
        if (string.IsNullOrEmpty(itemName)) return;

        // 아이템 버리기 (생성)
        SpawnItemNetwork(itemName);

        // 드랍 사운드
        if(_dropAudioData != null & _soundHandler != null)
        {
            AudioClip clip = _dropAudioData.GetRandomClip();

            _soundHandler.PlayInteractSound(clip);
        }
    }


    // 모든 아이템 드랍 (사망 시)
    public void DropAllItems()
    {
        if (QuickSlotManager.Instance == null) return;

        // 퀵슬롯 만큼 순회
        for (int i = 0; i < QuickSlotManager.Instance.QuickSlot.Length; i++)
        {
            // 매니저한테 i번째 아이템 요구
            string itemName = QuickSlotManager.Instance.PopItem(i);

            // 아이템이 있다면 바닥에 생성
            if (string.IsNullOrEmpty(itemName) == false)
            {
                SpawnItemNetwork(itemName);
            }
        }
    }


    // 네트워크 아이템 생성
    private void SpawnItemNetwork(string itemName)
    {
        // 아이템 데이터 찾기
        if (ItemManager.Instance.ItemDict.TryGetValue(itemName, out ShopItem data))
        {
            // 데이터가 플레이어 아이템 맞으면
            if (data is PlayerItemData itemData)
            {
                // 위치 계산
                Vector3 rayOrigin = transform.position + (Vector3.up * 1.0f);

                // 기본값으로 본인 위치
                Vector3 spawnPos = transform.position;

                // 바닥 체크
                if (Physics.Raycast(rayOrigin, Vector3.down, out _hit, 10.0f, _movementHandler.GroundLayer))
                {
                    spawnPos = _hit.point;
                }

                // 랜덤하게 조금 흩뿌리기
                Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f));

                // 아이템 이름 박싱
                object[] initData = new object[] { itemName };

                // 생성
                PhotonNetwork.Instantiate(itemData.prefab.name, spawnPos + randomOffset, Quaternion.Euler(0, transform.eulerAngles.y, 0), 0, initData);
            }
        }
    }
}
