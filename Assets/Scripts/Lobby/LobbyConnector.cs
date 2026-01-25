using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class LobbyConnector : MonoBehaviourPunCallbacks
{
    [SerializeField] Button joinButton;

    private void Awake()
    {
        // 이제 비동기 씬 로딩을 위해서 씬 동기화 끄기
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.ConnectUsingSettings();
        joinButton.interactable = false;
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        joinButton.interactable = true;
    }
    
    public void ClickJoin()
    {
        PhotonNetwork.JoinRandomOrCreateRoom();
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel("Room");
    }
}
