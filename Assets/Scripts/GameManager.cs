using Photon.Pun;
using System;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviourPun, IPunObservable
{
    public static GameManager Instance;

    [Header("상점 체크")]
    [SerializeField] bool _isShop;

    [Header("텍스트")]
    [SerializeField] TextMeshProUGUI _goldText;            // 골드
    [SerializeField] TextMeshProUGUI _SurviveDayText;      // 생존일

    public bool IsShop => _isShop;


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

        // 씬 변경 시 새로운 GameManager

        // 상점이면 생존일 추가
        if (IsShop == true)
            Survive();

        // 텍스트 갱신
        _goldText.SetText(GameData.Gold.ToString());
        _SurviveDayText.SetText(GameData.SurviveDay.ToString());
    }

    // 생존일 추가
    public void Survive()
    {
        // 방장만
        if (PhotonNetwork.IsMasterClient == false) return;

        // 추가
        GameData.SurviveDay++;
    }

    // 골드 추가
    public void AddGold(int amount)
    {
        // 방장만
        if (PhotonNetwork.IsMasterClient == false) return;

        // 데이터에 추가
        GameData.Gold += amount;

        // UI 갱신
        _goldText.SetText(GameData.Gold.ToString());
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

            // UI 갱신
            _goldText.SetText(GameData.Gold.ToString());

            return true;
        }

        return false;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(GameData.Gold);
            stream.SendNext(GameData.SurviveDay);
        }
        else
        {
            int receiveGold = (int)stream.ReceiveNext();
            int receiveDay = (int)stream.ReceiveNext();

            // 골드
            if (GameData.Gold != receiveGold)
            {
                GameData.Gold = receiveGold;

                _goldText.SetText(GameData.Gold.ToString());
            }

            // 생존일
            if (GameData.SurviveDay != receiveDay)
            {
                GameData.SurviveDay = receiveDay;

                _SurviveDayText.SetText(GameData.SurviveDay.ToString());
            }
        }
    }

}
