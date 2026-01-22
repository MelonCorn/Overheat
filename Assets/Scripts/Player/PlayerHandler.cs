using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHandler : MonoBehaviourPun, IPunObservable, IDamageable
{
    public static PlayerHandler localPlayer;

    private PlayerStatHandler _statHandler;
    private PlayerInteractHandler _interactHandler;
    private PlayerItemHandler _itemHandler;

    [Header("로컬 켤 것")]
    [SerializeField] MonoBehaviour[] _scripts; // PlayerInputHandler 같은 것들

    [Header("리모트 끌 것")]
    [SerializeField] GameObject _camera;        // 카메라
    [SerializeField] GameObject _canvas;        // 캔버스

    [Header("네트워크 동기화 설정")]
    [SerializeField] float _moveSmoothSpeed = 10f; // 이동
    [SerializeField] float _rotSmoothSpeed = 10f;  // 회전
    [SerializeField] float _teleportDistance = 5.0f; // 순간이동

    private Vector3 _networkPosition;       // 네트워크 위치
    private Quaternion _networkRotation;    // 네트워크 회전

    public Transform CameraTrans => _camera.transform;
    public string CurrentItem = "";


    private void Awake()
    {
        _statHandler = GetComponent<PlayerStatHandler>();
        _interactHandler = GetComponent<PlayerInteractHandler>();
        _itemHandler = GetComponent<PlayerItemHandler>();

        // 스폰위치, 회전 넣고 시작
        _networkPosition = transform.position;
        _networkRotation = transform.rotation;
    }

    private void OnEnable()
    {
        // 활성화된 플레이어 중 이 플레이어가 없다면
        if (GameManager.Instance.ActivePlayers.Contains(this) == false)
        {
            // 플레이어 리스트에 추가
            GameManager.Instance.ActivePlayers.Add(this);
        }
    }

    private void OnDisable()
    {
        // 플레이어 리스트에서 제거
        GameManager.Instance.ActivePlayers.Remove(this);
    }

    private void Start()
    {
        // 로컬 객체일 때
        if (photonView.IsMine == true)
        {
            // 로컬 플레이어 등록
            localPlayer = this;

            // 켤 스크립트 켜기
            EnableLocalComponents();

            // 내 캐릭터 닉네임 설정

            // 로컬 플레이어 레이어 설정
            int layerIndex = LayerMask.NameToLayer("LocalPlayer");
            if (layerIndex != -1) gameObject.layer = layerIndex;

            // 카메라 설정
            SetCamera();

            // 퀵슬롯 기초 설정
            if(QuickSlotManager.Instance != null)
                QuickSlotManager.Instance.UpdateUI();
        }
        // 리모트 객체일 때
        else
        {
            // 끌 것들 끄기 
            DisableRemoteComponents();
        }
    }

    private void Update()
    {
        // 내 객체는 할 필요 없음
        if (photonView.IsMine == true) return;

        // 위치 보간
        // 거리 차이가 너무 벌어지면 텔포
        if (Vector3.Distance(transform.position, _networkPosition) > _teleportDistance)
            transform.position = _networkPosition;
        else
            transform.position = Vector3.Lerp(transform.position, _networkPosition, Time.deltaTime * _moveSmoothSpeed);

        // 회전 보간
        transform.rotation = Quaternion.Lerp(transform.rotation, _networkRotation, Time.deltaTime * _rotSmoothSpeed);
    }

    private void OnDestroy()
    {
        // 나중에 없는데 참조할 수 있으니까 방지
        if (localPlayer == this)
        {
            localPlayer = null;
        }
    }

    // 로컬 컴포넌트 설정
    private void EnableLocalComponents()
    {
        // 스크립트 켜기
        foreach (var script in _scripts)
        {
            script.enabled = true;
        }

        // PlayerInput 켜기
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = true;
        }

    }

    // 리모트 컴포넌트 끄기
    private void DisableRemoteComponents()
    {
        // 카메라 끄기
        if (_camera != null) _camera.SetActive(false);
        if (_canvas != null) _canvas.SetActive(false);
    }


    // 카메라 설정
    private void SetCamera()
    {
        _interactHandler.SetCamera(_camera.transform);
        _itemHandler.SetCamera(_camera.transform);
    }

    // 퀵슬롯 변경 시
    public void ChangeQuickSlot(string itemName)
    {
        if (QuickSlotManager.Instance == null) return;

        // 상태가 달라졌다면 갱신
        if (CurrentItem != itemName)
        {
            CurrentItem = itemName;

            // 내 화면에서도 손 모델을 갱신
            //UpdateHandModel(CurrentItem);
        }
    }


    // 플레이어 피격
    public void TakeDamage(int dmg)
    {
        _statHandler.TakeDamage(dmg);
    }


    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 트랜스폼
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);

            // 들고있는 아이템
            stream.SendNext(CurrentItem);
        }
        else
        {
            // 트랜스폼
            _networkPosition = (Vector3)stream.ReceiveNext();
            _networkRotation = (Quaternion)stream.ReceiveNext();

            // 들고있는 아이템
            string receiveItem = (string)stream.ReceiveNext();

            if (CurrentItem != receiveItem)
            {
                CurrentItem = receiveItem;

                // 아이템 변경
            }
        }
    }
}
