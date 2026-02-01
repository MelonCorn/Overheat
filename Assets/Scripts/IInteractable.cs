
using UnityEngine;

public interface IInteractable
{
    // 상호작용 (반환은 상호작용 클립)
    public AudioClip OnInteract();


    // 상호작용 문구
    public string GetInteractText(out bool canInteract);
}
