using UnityEngine;

public class CargoSocket : MonoBehaviour, IInteractable
{
    [Header("수납 오디오 데이터")]
    [SerializeField] ObjectAudioData _storeAudioData;

    private CargoNode _parentNode;      // 속한 화물칸
    private int _index;                 // 소켓 번호


    public void Init(CargoNode cargo, int index)
    {
        _parentNode = cargo;
        _index = index;
    }



    // 플레이어의 Ray로 상호작용이 일어났을 때
    public AudioClip OnInteract()
    {
        if (_parentNode != null)
        {
            _parentNode.InteractSocket(_index);

            return _storeAudioData.GetRandomClip();
        }

        return null;
    }
    public string GetInteractText(out bool canInteract)
    {
        canInteract = false;

        if (_parentNode != null)
        {
            canInteract = true;
            // 화물칸에서 현재 소켓 아이템 여부에 따라 문구 반환
            return _parentNode.GetInteractText(_index);
        }
        return "";
    }
}
