using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance;

    [Header("테스트용 아이템 목록")]
    [SerializeField] List<ShopItem> _shopPlayerItems;

    // 검색용 아이템 목록
    public Dictionary<string, ShopItem> ItemDict { get; private set; }

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
            return;
        }

        // 리스트를 딕셔너리로 변환
        ItemDict = new Dictionary<string, ShopItem>();

        foreach (var item in _shopPlayerItems)
        {
            // Key : string (ItemName),     Value : ShopItem(ScriptableObject)
            if (ItemDict.ContainsKey(item.itemName) == false)
                ItemDict.Add(item.itemName, item);
        }
    }
}
