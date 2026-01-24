using UnityEngine;
using Photon.Pun;
using TMPro;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerInteractHandler : MonoBehaviour
{
    private PlayerMovementHandler _movementHandler;
    private PlayerInputHandler _inputHandler;

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
            // 상호작용 실행
            _currentInteractable.OnInteract();

            // 손 새로고침
            if (QuickSlotManager.Instance != null)
                QuickSlotManager.Instance.RefreshHand();
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

        // 아이템 데이터 찾기
        if (ItemManager.Instance.ItemDict.TryGetValue(itemName, out ShopItem data))
        {
            // PlayerItemData 타입인지 확인
            if (data is PlayerItemData itemData)
            {
                // 레이 시작점
                Vector3 rayOrigin = transform.position + (Vector3.up * 1.0f);

                // 생성 지점 (기본은 레이 시작점)
                Vector3 spawnPos = rayOrigin;

                // 레이캐스트 아래로 10 길이 땅 레이어만
                if (Physics.Raycast(rayOrigin, Vector3.down, out _hit, 10.0f, _movementHandler.GroundLayer))
                {
                    // 생성 지점은 히트한 위치
                    spawnPos = _hit.point;
                }
                else
                {
                    // 바닥이 너무 멀면 내 위치
                    spawnPos = transform.position;
                }

                // itemName 포장
                object[] initData = new object[] { itemName };

                // 네트워크 객체 생성
                PhotonNetwork.Instantiate(itemData.prefab.name, spawnPos, Quaternion.Euler(0, transform.eulerAngles.y, 0), 0 , initData);

                Debug.Log($"{itemName} 버리기(네트워크 생성) 성공!");
            }
        }
        else
        {
            Debug.LogError($"아이템 데이터를 찾을 수 없습니다: {itemName}");
        }
    }
}
