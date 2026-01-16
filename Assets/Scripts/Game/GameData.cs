using System;
using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData Instance;

    [Header("확인용")]
    [SerializeField] int _gold; 
    [SerializeField] int _surviveDay;
    public int Gold // 골드
    {
        get => _gold;
        set => _gold = value;
    }

    public int SurviveDay   // 생존일
    {
        get => _surviveDay;
        set => _surviveDay = value;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
