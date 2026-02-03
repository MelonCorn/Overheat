using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHandler : MonoBehaviourPun, IPunObservable, IDamageable
{
    public static PlayerHandler localPlayer;

    private PlayerMovementHandler _movementHandler;
    private PlayerInputHandler _inputHandler;
    private PlayerCameraHandler _cameraHandler;
    private PlayerStatHandler _statHandler;
    private PlayerInteractHandler _interactHandler;
    private PlayerItemHandler _itemHandler;
    private PlayerSoundHandler _soundHandler;
    private AudioListener _audioListener;

    [Header("로컬 켤 것")]
    [SerializeField] MonoBehaviour[] _scripts; // PlayerInputHandler 같은 것들

    [Header("리모트 끌 것")]
    [SerializeField] GameObject _cameraHolder;  // 카메라홀더
    [SerializeField] GameObject _canvas;        // 캔버스

    [Header("네트워크 동기화 설정")]
    [SerializeField] float _moveSmoothSpeed = 10f; // 이동
    [SerializeField] float _rotSmoothSpeed = 10f;  // 회전
    [SerializeField] float _teleportDistance = 5f; // 순간이동

    [Header("모델 애니메이터")]
    [SerializeField] Animator _animator;

    [Header("사망 설정")]
    [SerializeField] Transform _deathParticlePoint;           // 사망 파티클 재생 포인트
    [SerializeField] PoolableObject _deathParticle;           // 사망 파티클
    [SerializeField] private float _spectatorDelay = 2.5f;    // 사망 후 관전 대기 시간

    [Header("플레이어 에임")]
    [SerializeField] GameObject _aimObj;        // 에임

    // 네트워크 데이터 캐싱
    private Vector3 _remotePosition;       // 네트워크 위치
    private Quaternion _remoteRotation;    // 네트워크 회전
    private float _remoteInputX;           // X 인풋
    private float _remoteInputY;           // Y 인풋
    private bool _remoteIsJump;            // 점프 상태

    private PoolableObject _currentDeathParticle;   // 사망 파티클 캐싱

    public Transform CameraHolderTrans => _cameraHolder.transform;          // 홀더 트랜스폼
    public Transform CameraTrans => _cameraHandler.LocalCamera.transform;   // 진짜 카메라 트랜스폼
    public GameObject LocalAim => _aimObj;
    public Animator PlayerAnim => _animator;
    public string CurrentItem = "";

    public bool IsDead { get; private set; }
    private bool _isDying;  // 사망 연출 중

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
        _movementHandler = GetComponent<PlayerMovementHandler>();
        _cameraHandler = GetComponent<PlayerCameraHandler>();
        _statHandler = GetComponent<PlayerStatHandler>();
        _interactHandler = GetComponent<PlayerInteractHandler>();
        _itemHandler = GetComponent<PlayerItemHandler>();
        _soundHandler = GetComponent<PlayerSoundHandler>();
        _audioListener = GetComponentInChildren<AudioListener>();

        // 스폰위치, 회전 넣고 시작
        _remotePosition = transform.position;
        _remoteRotation = transform.rotation;
    }

    private void OnEnable()
    {
        if (GameManager.Instance == null) return;

        // 활성화된 플레이어 중 이 플레이어가 없다면
        if (GameManager.Instance.ActivePlayers.Contains(this) == false)
        {
            // 플레이어 리스트에 추가
            GameManager.Instance.ActivePlayers.Add(this);
        }
    }

    private void OnDisable()
    {
        if(photonView.IsMine == true)
            // 메인 카메라 오디오 리스너 켜기 
            SetMainCameraAudioListener(true);

        // 파티클 반납
        if(_currentDeathParticle != null) _currentDeathParticle.Release();

        if (GameManager.Instance == null) return;

        // 플레이어 리스트에서 제거
        GameManager.Instance.ActivePlayers.Remove(this);
    }

    private void Start()
    {
        // OnEnable에서 못해씅면 한번 더 시도
        if (GameManager.Instance != null && GameManager.Instance.ActivePlayers.Contains(this) == false)
        {
            GameManager.Instance.ActivePlayers.Add(this);
        }

        // 로컬 객체일 때
        if (photonView.IsMine == true)
        {
            // 로컬 플레이어 등록
            localPlayer = this;

            // 로컬 렌더러 끄기
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers) renderer.enabled = false;

            // 켤 스크립트 켜기
            EnableLocalComponents();

            // 내 캐릭터 닉네임 설정

            // 로컬 플레이어 레이어 설정
            int layerIndex = LayerMask.NameToLayer("LocalPlayer");
            if (layerIndex != -1) gameObject.layer = layerIndex;

            // 카메라 설정
            SetCamera();

            // 메인 카메라 오디오 리스너 끄기 
            SetMainCameraAudioListener(false);

            // 퀵슬롯 기초 설정
            if (QuickSlotManager.Instance != null)
            {
                // UI 갱신, 손 새로고침
                QuickSlotManager.Instance.SetUIActive(true);
                QuickSlotManager.Instance.UpdateUI();
                QuickSlotManager.Instance.RefreshHand();
            }
        }
        // 리모트 객체일 때
        else
        {
            // 끌 것들 끄기 
            DisableRemoteComponents();

            // 미니맵에 등록
            if (MiniMapHandler.Instance != null)
            {
                MiniMapHandler.Instance.RegisterPlayer(transform);
            }
        }

        // 로컬, 리모트 오디오 소스 설정
        _soundHandler.SetttingAudioSource(photonView.IsMine);
    }

    private void Update()
    {
        // 내 객체는 할 필요 없음
        if (photonView.IsMine == true) return;

        // 위치 보간
        // 거리 차이가 너무 벌어지면 텔포
        if (Vector3.Distance(transform.position, _remotePosition) > _teleportDistance)
            transform.position = _remotePosition;
        else
            transform.position = Vector3.Lerp(transform.position, _remotePosition, Time.deltaTime * _moveSmoothSpeed);

        // 회전 보간
        transform.rotation = Quaternion.Lerp(transform.rotation, _remoteRotation, Time.deltaTime * _rotSmoothSpeed);

        // 애니메이션 갱신
        if (_animator != null)
        {
            _animator.SetFloat("InputX", _remoteInputX, 0.1f, Time.deltaTime);
            _animator.SetFloat("InputY", _remoteInputY, 0.1f, Time.deltaTime);
            _animator.SetBool("IsJump", _remoteIsJump);
        }
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
        if (_cameraHolder != null) _cameraHolder.SetActive(false);
        if (_canvas != null) _canvas.SetActive(false);

        // 혹시 몰라서 끔
        if (_audioListener != null) _audioListener.enabled = false;
    }


    // 카메라 설정
    private void SetCamera()
    {
        _interactHandler.SetCamera(CameraTrans);
        _itemHandler.SetCamera(CameraHolderTrans);
    }

    // 메인 카메라 오디오 리스터 상태 전환
    private void SetMainCameraAudioListener(bool active)
    {
        if (_audioListener != null) _audioListener.enabled = !active;

        // 플레이어 있으면 끄고 없으면 켜고
        if (Camera.main != null)
        {
            var mainListener = Camera.main.GetComponent<AudioListener>();
            if (mainListener != null) mainListener.enabled = active;
        }
    }

    // 퀵슬롯 변경 시
    public void ChangeQuickSlot(string itemName)
    {
        if (QuickSlotManager.Instance == null) return;

        // 현재 아이템 갱신
        CurrentItem = itemName;

        // 로컬은 바로 장착
        _itemHandler.EquipItem(CurrentItem);
    }

    

    // 플레이어 피격
    public void TakeDamage(int dmg)
    {
        photonView.RPC(nameof(RPC_Damage), photonView.Owner, dmg);
    }

    // 클라어언트에게 호출
    [PunRPC]
    private void RPC_Damage(int dmg)
    {
        _statHandler.TakeDamage(dmg);
    }


    // 사망
    public void Die()
    {
        // 이미 죽었거나, 사망 연출 중이라면 무시
        if (IsDead == true || _isDying == true) return;

        // 아니면 사망 연출 시작
        _isDying = true;

        // 퀵슬롯 비활성화
        if (QuickSlotManager.Instance != null)
        {
            // UI 비활성화
            // 퀵슬롯 올 초기화
            // 1번 슬롯 들게
            QuickSlotManager.Instance.SetUIActive(false);
            QuickSlotManager.Instance.ClearAllSlots();
            QuickSlotManager.Instance.SelectSlot(0);
            // 손 비우기
            ChangeQuickSlot("");
        }

        // 플레이어 비활성화
        DisablePlayer();

        // 사망 연출 시작
        StartCoroutine(DeathSequence());
    }

    // 플레이어 비활성화
    private void DisablePlayer()
    {
        var input = GetComponent<PlayerInputHandler>();
        var move = GetComponent<PlayerMovementHandler>();
        var cam = GetComponent<PlayerCameraHandler>();
        var playerInput = GetComponent<PlayerInput>();

        if (input != null) input.enabled = false;               // 키보드 입력 차단
        if (move != null) move.enabled = false;                 // 이동 차단
        if (cam != null) cam.enabled = false;                   // 카메라 차단
        if (playerInput != null) playerInput.enabled = false;   // 플레이어 인풋 차단
    }

    // 사망 연출
    private IEnumerator DeathSequence()
    {
        // 사망 처리, 효과
        DeathEffect();

        // 데이터 기록 (상점 부활용)
        if (GameManager.Instance != null)
            GameManager.Instance.LocalPlayerDead(true);

        // 방장이 죽었을 시 바로 체크
        if (PhotonNetwork.IsMasterClient == true)
            GameManager.Instance.CheckAllPlayersDead();

        // 다른 플레이어들에게 신체 폭발 명령
        photonView.RPC(nameof(RPC_ExplodeBody), RpcTarget.Others);

        // 죽음 여운 딜레이
        yield return new WaitForSeconds(_spectatorDelay);

        // 로컬 카메라, UI 끄기
        if (_cameraHolder != null) _cameraHolder.gameObject.SetActive(false);
        if (_canvas != null) _canvas.gameObject.SetActive(false);

        // 관전 카메라 활성화
        if (GameManager.Instance != null)
            GameManager.Instance.SpectatorMode();
    }

    // 사망 효과
    private void DeathEffect()
    { 
        // 사망 확정
        IsDead = true;

        // 미니맵에서 제거 요청
        if (MiniMapHandler.Instance != null)
        {
            // 내꺼라면 안에서 무시됨
            MiniMapHandler.Instance.Unregister(transform);
        }

        // 사망 파티클
        if (_deathParticle != null && _deathParticlePoint != null && PoolManager.Instance != null)
        {
            _currentDeathParticle = PoolManager.Instance.Spawn(_deathParticle, _deathParticlePoint.position, Quaternion.identity);
        }

        // 사망 사운드 재생
        if(_soundHandler != null) _soundHandler.PlayDieSound();

        // 애니메이터의 모델 끄기
        if (_animator != null) _animator.gameObject.SetActive(false);

        // 콜라이더도 끄기
        Collider collider = GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
    }
    
    // 신체 폭발 알림
    [PunRPC]
    private void RPC_ExplodeBody()
    {
        // 리모트 사망 처리, 효과
        DeathEffect();

        // 여기서부턴 방장이 체크
        if (PhotonNetwork.IsMasterClient == true)
            // 사망한 플레이어 체크
            GameManager.Instance.CheckAllPlayersDead();
    }

    // 로컬 전용 인게임 바닥 감지
    private void OnControllerColliderHit(ControllerColliderHit other)
    {
        // 내꺼 아니거나 사망상태면 무시
        if (photonView.IsMine == false || IsDead == true) return;

        // Warning 이라는 물체에 닿으면 사망
        if (other.gameObject.layer == LayerMask.NameToLayer("Warning"))
        {
            Die();
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 트랜스폼
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);

            // 입력값
            stream.SendNext(_inputHandler.MoveInput.x);
            stream.SendNext(_inputHandler.MoveInput.y);

            // 점프 상태
            stream.SendNext(_movementHandler.IsJump);

            // 들고있는 아이템
            stream.SendNext(CurrentItem);
        }
        else
        {
            // 트랜스폼
            _remotePosition = (Vector3)stream.ReceiveNext();
            _remoteRotation = (Quaternion)stream.ReceiveNext();

            // 애니메이션
            _remoteInputX = (float)stream.ReceiveNext();
            _remoteInputY = (float)stream.ReceiveNext();
            _remoteIsJump = (bool)stream.ReceiveNext();

            // 들고있는 아이템
            string receiveItem = (string)stream.ReceiveNext();

            if (CurrentItem != receiveItem)
            {
                CurrentItem = receiveItem;

                // 아이템 장착
                _itemHandler.EquipItem(CurrentItem);
            }
        }
    }
}
