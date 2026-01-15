using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

public class LobbyConnector : MonoBehaviourPunCallbacks
{
    [SerializeField] Button joinButton;

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
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
            PhotonNetwork.LoadLevel(1);
    }
}
