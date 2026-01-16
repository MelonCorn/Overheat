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
}
