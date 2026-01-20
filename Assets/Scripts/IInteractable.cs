
public interface IInteractable
{
    // 상호작용
    public void OnInteract();


    // 상호작용 문구
    public string GetInteractText(out bool canInteract);
}
