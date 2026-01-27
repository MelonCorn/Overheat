using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class LobbyConnector : MonoBehaviourPunCallbacks
{
    [SerializeField] Button joinButton;

    private void Awake()
    {
        // 이제 비동기 씬 로딩을 위해서 씬 동기화 끄기
        PhotonNetwork.AutomaticallySyncScene = false;
        // 네트워크 메세지 허용
        PhotonNetwork.IsMessageQueueRunning = true;

        // 일단 버튼 끄기
        joinButton.interactable = false;

        // 이미 로비면 버튼
        if (PhotonNetwork.InLobby)
        {
            joinButton.interactable = true;
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
        // 페이드 인
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.RequestFadeIn();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Lobby] 콜백 수신: OnConnectedToMaster -> JoinLobby() 호출");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[Lobby] 콜백 수신: OnJoinedLobby -> 버튼 활성화 완료!");
        joinButton.interactable = true;
    }
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[Lobby] 연결 끊김! 이유: {cause}");
    }

    public void ClickJoin()
    {
        PhotonNetwork.JoinRandomOrCreateRoom();
    }

    public override void OnJoinedRoom()
    {
        if (LoadingManager.Instance != null)
        {
            LoadingManager.Instance.RequestLoadScene("Room");
        }
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
