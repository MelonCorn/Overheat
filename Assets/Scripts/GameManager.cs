using Photon.Pun;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;

public class GameManager : MonoBehaviourPun, IPunObservable
{
    public static GameManager Instance;

    // 골드 (편의성)
    public int Gold => GameData.Gold;

    public event Action<int> OnGoldChanged;         // 골드 변경 시 이벤트
    public event Action<int> OnSurviveDayChanged;   // 생존일 변경 시 이벤트


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
        if (PhotonNetwork.IsMasterClient == false) return;

        // 씬 변경 시 골드 갱신
        OnGoldChanged?.Invoke(GameData.Gold);
    }

    // 골드 추가
    public void AddGold(int amount)
    {
        // 방장만, 데이터 있을 때만
        if (PhotonNetwork.IsMasterClient == false) return;

        // 데이터에 추가
        GameData.Gold += amount;

        // 이벤트 실행
        OnGoldChanged?.Invoke(GameData.Gold);
    }

    // 골드 사용 시도
    public bool TryUseMoney(int amount)
    {
        if (PhotonNetwork.IsMasterClient == false) return false;

        // 보유 골드가 요구 골드 이상이면
        if (GameData.Gold >= amount)
        {
            // 골드 사용
            GameData.Gold -= amount;

            // 이벤트 실행
            OnGoldChanged?.Invoke(GameData.Gold);

            return true;
        }

        return false;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(GameData.Gold);
        }
        else
        {
            GameData.Gold = (int)stream.ReceiveNext();

            OnGoldChanged?.Invoke(GameData.Gold);
        }
    }

}
