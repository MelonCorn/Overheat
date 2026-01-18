using UnityEngine;

public class CargoSocket : MonoBehaviour
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
}
