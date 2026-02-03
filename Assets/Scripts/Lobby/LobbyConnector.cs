using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class LobbyConnector : MonoBehaviourPunCallbacks
{
    [SerializeField] Button _joinButton;
    [SerializeField] Button _settingButton;
    [SerializeField] Button _applyButton;

    [SerializeField] GameObject _loadingUI;

    private void Awake()
    {
        // 이제 비동기 씬 로딩을 위해서 씬 동기화 끄기
        PhotonNetwork.AutomaticallySyncScene = false;
        // 네트워크 메세지 허용
        PhotonNetwork.IsMessageQueueRunning = true;

        // 일단 버튼 끄기
        _joinButton.interactable = false;

        // 이미 로비면 버튼
        if (PhotonNetwork.InLobby)
        {
            _joinButton.interactable = true;
        }
        // 마스터 서버 연결 완료
        else if (PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer)
        {
            PhotonNetwork.JoinLobby();
        }
        // 연결이 끊겨있으면 연결 시도
        else if (PhotonNetwork.IsConnected == false)
        {
            Debug.Log("[Lobby] 연결 끊김 감지 재연결 시도");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    private void Start()
    {
        if(SettingManager.Instance != null)
        {
            // 세팅 버튼에 설정 패널 열기 설정
            _settingButton.onClick.AddListener( () =>
            {
                if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SFXType.Click);
                SettingManager.Instance.OpenSetting();
            } );

            if(_applyButton != null)
                _applyButton.onClick.AddListener(() =>
                {
                    if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SFXType.PopupClose);
                });
        }

        // 페이드 인
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.RequestFadeIn();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Lobby] 콜백 OnConnectedToMaster");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[Lobby] 콜백 OnJoinedLobby");
        _joinButton.interactable = true;
    }
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[Lobby] 연결 끊김. 이유: {cause}");
    }

    public void ClickJoin()
    {
        // 소리 재생
        if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SFXType.Click);

        // 로딩 UI
        if (_loadingUI != null) _loadingUI.SetActive(true);

        // 버튼 비활성화
        if (_joinButton != null) _joinButton.interactable = false;

        // 최대 인원 4명 설정
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4; 

        // 매칭 속성 필터, 최대인원 필터, 매칭 모드, 로비, SQL필터, 방 이름, 방 옵션
        PhotonNetwork.JoinRandomOrCreateRoom(null, 0, MatchmakingMode.FillRoom, null, null, null, roomOptions);
    }

    public override void OnJoinedRoom()
    {
        if (LoadingManager.Instance != null)
        {
            // 소리 재생
            if (SoundManager.Instance != null) SoundManager.Instance.PlaySFX(SFXType.PopupClose);

            LoadingManager.Instance.RequestLoadScene("Room");
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        ResetLobby();
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        ResetLobby();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        ResetLobby();
    }

    // UI 초기화
    private void ResetLobby()
    {
        if (_loadingUI != null) _loadingUI.SetActive(false);
        if (_joinButton != null) _joinButton.interactable = true;
    }

    // 게임 종료 버튼
    public void OnClickQuitGame()
    {
        Application.Quit();

        // 에디터에서는 종료 시 플레이 풀기
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

}
