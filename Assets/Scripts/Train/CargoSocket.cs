using UnityEngine;

public class CargoSocket : MonoBehaviour, IInteractable
{
    private CargoNode _parentNode;      // 속한 화물칸
    private int _index;                 // 소켓 번호


    public void Init(CargoNode cargo, int index)
    {
        _parentNode = cargo;
        _index = index;
    }



    // 플레이어의 Ray로 상호작용이 일어났을 때
    public void OnInteract()
    {
        if (_parentNode != null)
        {
            Debug.Log("상호작용 시도");
            _parentNode.InteractSocket(_index);
        }
    }
    public string GetInteractText()
    {
        if (_parentNode != null)
        {
            // 화물칸에서 현재 소켓 아이템 여부에 따라 문구 반환
            return _parentNode.GetInteractText(_index);
        }
        return "";
    }
}
