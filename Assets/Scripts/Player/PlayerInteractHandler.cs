using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractHandler : MonoBehaviour
{
    [Header("상호작용 설정")]
    [SerializeField] float _reachDistance = 3f;     // 사거리
    [SerializeField] LayerMask _interactLayer;      // 상호작용 레이어

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
            TryInteract();
        }

        if (Keyboard.current.gKey.wasPressedThisFrame)
        {
            // 아이템 버리기
        }
    }

    private void TryInteract()
    {
        Ray ray = new Ray(_camera.position, _camera.forward);

        if (Physics.Raycast(ray, out _hit, _reachDistance, _interactLayer))
        {
            // 선반 소켓 감지
            CargoSocket socket = _hit.collider.GetComponent<CargoSocket>();
            if (socket != null)
            {
                // 선반 발견하면 상호작용 시도
                socket.OnInteract();
                return;
            }

            // 바닥 아이템 감지
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

                    // 일단 아이템 픽업 함수 호출 (슬롯 번호)
                    item.OnPickItem(slotIndex);
                }
                else
                {
                    Debug.Log("인벤토리가 꽉 찼습니다");
                }
            }
        }
    }
}
