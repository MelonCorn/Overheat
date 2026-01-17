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
            // 부모의 컴포넌트 가져오기
            NetworkItem item = _hit.collider.GetComponentInParent<NetworkItem>();

            // item이 있고 아직 파괴되지 않은 상태인지 확인
            if (item != null && item.gameObject.activeSelf)
            {
                Debug.Log("아이템 감지, 퀵슬롯 추가 시도");

                // 퀵슬롯에 데이터 넣기 시도 후 슬롯 번호 가져오기
                int slotIndex = QuickSlotManager.Instance.TryAddItem(item.ItemName);

                // 퀵슬롯에 데이터 넣기 시도 성공 시
                if (slotIndex != -1)
                {
                    Debug.Log("퀵슬롯 아이템 예측 추가 완료");

                    // 로컬 플레이어 ViewID
                    int playerViewID = PlayerHandler.localPlayer.photonView.ViewID;

                    // 일단 아이템 픽업 함수 호출 (로컬 플레이어 ID, 슬롯 번호)
                    item.OnPickItem(playerViewID, slotIndex);
                }
                else
                {
                    Debug.Log("인벤토리가 꽉 찼습니다");
                }
            }
        }
    }
}
