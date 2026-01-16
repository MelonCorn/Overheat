using Photon.Pun;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class GameManager : MonoBehaviourPun, IPunObservable
{
    public static GameManager Instance;
    private GameData _gameData;

    // 편의성으로 만듬
    public int Gold => _gameData != null ? _gameData.Gold : 0;

    public event Action<int> OnGoldChanged;   // 골드 변경 시 이벤트


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

    private void Start()
    {
        _gameData = GameData.Instance;

        if (PhotonNetwork.IsMasterClient == false) return;

        // 씬 변경 시 골드 갱신
        if (_gameData != null)
        {
            OnGoldChanged?.Invoke(_gameData.Gold);
        }
    }

    // 골드 추가
    public void AddGold(int amount)
    {
        // 방장만, 데이터 있을 때만
        if (PhotonNetwork.IsMasterClient == false) return;
        if (_gameData == null) return;

        // 데이터에 추가
        _gameData.Gold += amount;

        // 이벤트 실행
        OnGoldChanged?.Invoke(_gameData.Gold);
    }

    // 골드 사용 시도
    public bool TryUseMoney(int amount)
    {
        if (PhotonNetwork.IsMasterClient == false) return false;
        if (_gameData == null) return false;

        // 보유 골드가 요구 골드 이상이면
        if (_gameData.Gold >= amount)
        {
            // 골드 사용
            _gameData.Gold -= amount;

            // 이벤트 실행
            OnGoldChanged?.Invoke(_gameData.Gold);

            return true;
        }

        return false;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            if (_gameData != null)
                stream.SendNext(_gameData.Gold);
        }
        else
        {
            if (_gameData != null)
            {
                _gameData.Gold = (int)stream.ReceiveNext();

                OnGoldChanged?.Invoke(_gameData.Gold);
            }
        }
    }

}
