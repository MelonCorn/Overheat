using Photon.Pun;
using UnityEngine;

public class NetworkItem : MonoBehaviourPun
{
    [Header("아이템 정보")]
    public string ItemID { get; private set; }

    private Collider _collider; // 줍기용

    private void Awake()
    {
        _collider = GetComponentInChildren<Collider>();
    }


    public void OnPickItem()
    {
        // 콜라이더 끄기
        if (_collider != null) _collider.enabled = false;


        if (PhotonNetwork.IsMasterClient == true)
        {
            // 방장이면 직접 파괴
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            // 방장이 아니면 방장에게 파괴요청
            photonView.RPC(nameof(RPC_RequestDestroy), RpcTarget.MasterClient);
        }
    }

    // 방장이 요청받는 함수
    [PunRPC]
    private void RPC_RequestDestroy()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
