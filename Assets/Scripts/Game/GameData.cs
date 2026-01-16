using System;
using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance;


    [Header("골드")]
    [SerializeField] int _gold; // 인스펙터 확인용
    public int Gold => _gold;
    public int CurrentDay { get; private set; } // 버틴 날

    public event Action<int> OnGoldChanged;   // 골드 변경 시 이벤트

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 넘어가도 파괴 안 됨!
        }
        else
        {
            // 이미 전 씬에서 넘어온 데이터가 있다면, 새로 생긴 건 부수기
            Destroy(gameObject);
        }
    }

    // 돈 변경
    public void SetGold(int amount)
    {
        // 골드 다를 때만
        if (_gold != amount)
        {
            _gold = amount;
            OnGoldChanged?.Invoke(_gold);
        }
    }

    // 돈 추가/사용
    public void AddGold(int amount)
    {
        _gold += amount;
        OnGoldChanged?.Invoke(_gold);
    }
}
