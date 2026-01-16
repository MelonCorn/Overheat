using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.Playables;

public class GameManager : MonoBehaviourPun, IPunObservable
{
    public static GameManager Instance;

    [Header("골드")]
    [SerializeField] int _gold; // 인스펙터 확인용
    public int Gold => _gold;
    public int CurrentDay { get; private set; } // 버틴 날

    public event Action<int> OnGoldChanged;   // 골드 변경 시 이벤트

    private GameData _gameData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    // 골드 추가
    public void AddGold(int amount)
    {
        // 방장만, 데이터 있을 때만
        if (PhotonNetwork.IsMasterClient == false) return;
        if (_gameData == null) return;

        _gameData.AddGold(amount);
    }

    // 골드 사용 시도
    public bool TryUseMoney(int amount)
    {
        if (PhotonNetwork.IsMasterClient == false) return false;
        if (_gameData == null) return false;

        // 보유 골드가 요구 골드 이상이면
        if (_gameData.Gold >= amount)
        {
            // -로 Add
            _gameData.AddGold(-amount);

            return true;
        }

        return false;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(_gameData.Gold);
        }
        else
        {
            _gameData.SetGold((int)stream.ReceiveNext());
        }
    }

}
