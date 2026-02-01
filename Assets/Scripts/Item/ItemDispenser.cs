using UnityEngine;

public class ItemDispenser : MonoBehaviour, IInteractable
{
    [Header("공급할 아이템")]
    [SerializeField] string _targetItemName = "Coal";   // 일단 석탄 포대 밖에 없어서 기본은 Coal

    [Header("상호작용 오디오 데이터")]
    [SerializeField] ObjectAudioData _audioData;

    private string _targetDisplayName;  // 표시 이름 (한글)

    private void Start()
    {
        // 화면에 표시될 이름 설정
        if (ItemManager.Instance.ItemDict.TryGetValue(_targetItemName, out ShopItem itemData))
            _targetDisplayName = itemData.displayName;
    }

    public string GetInteractText(out bool canInteract)
    {
        canInteract = true;

        return $"{_targetDisplayName} 획득";
    }
                   
    // 상호작용 인터페이스 구현
    // 오디오 클립 반환
    public AudioClip OnInteract()
    {
        // 아이템 획득 시도 그리고 퀵슬롯 번호 가져옴 (false는 예측 안해도 된다는 의미)
        int slot = QuickSlotManager.Instance.TryAddItem(_targetItemName, false);

        // 획득
        if (slot != -1)
            return _audioData.GetRandomClip();
        // 실패
        else
            return null;
    }
}
