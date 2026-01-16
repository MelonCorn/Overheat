using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractHandler : MonoBehaviour
{
    [Header("상호작용 설정")]
    [SerializeField] float _reachDistance = 3f; // 사거리
    [SerializeField] LayerMask _itemLayer;      // 아이템 레이어

    private Transform _camera;
    private RaycastHit _hit;

    private void Start()
    {
        _camera = PlayerHandler.localPlayer.CameraTrans;
    }

    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TryPickUp();
        }
    }

    private void TryPickUp()
    {
        Ray ray = new Ray(_camera.position, _camera.forward);

        if (Physics.Raycast(ray, out _hit, _reachDistance, _itemLayer))
        {
            NetworkItem item = _hit.collider.GetComponent<NetworkItem>();

            // item이 있고 아직 파괴되지 않은 상태인지 확인
            if (item != null && item.gameObject.activeSelf)
            {
                // 퀵슬롯에 데이터 넣기 시도
                if (QuickSlotManager.Instance.TryAddItem(item.ItemID))
                {
                    // 성공했으면 바닥에 있는 객체 파괴
                    item.OnPickItem();
                }
                else
                {
                    Debug.Log("인벤토리가 꽉 찼습니다");
                }
            }
        }
    }
}
