using UnityEngine;

public class ItemDispenser : MonoBehaviour, IInteractable
{
    [Header("공급할 아이템")]
    [SerializeField] string _targetItemName = "Coal";   // 일단 석탄 포대 밖에 없어서 기본은 Fuel

    public string GetInteractText()
    {
        return $"{_targetItemName} 획득";
    }

    public void OnInteract()
    {
        Debug.Log("아이템 수급 시도");

        // 아이템 획득 시도 그리고 퀵슬롯 번호 가져옴 (false는 예측 안해도 된다는 의미)
        int slot = QuickSlotManager.Instance.TryAddItem(_targetItemName, false);

        // 획득
        if (slot != -1)
        {
            Debug.Log($"{_targetItemName} 획득!");
            // 사운드, 이펙트 재생
        }
        // 실패
        else
        {
            Debug.Log("인벤토리가 가득 찼습니다.");
        }
    }
}
