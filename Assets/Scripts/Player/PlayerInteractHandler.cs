using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;

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
            TryDropItem();
        }
    }


    // 상호작용
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


    // 버리기
    private void TryDropItem()
    {
        // 퀵슬롯 매니저에서 들고있는 아이템 떨구기 요청
        // (예측 중이면 null 반환)
        string itemName = QuickSlotManager.Instance.TryDropItem();

        // 꺼낸 게 없으면 중단
        if (string.IsNullOrEmpty(itemName)) return;

        // 아이템 데이터 찾기
        if (ItemManager.Instance.ItemDict.TryGetValue(itemName, out ShopItem data))
        {
            // PlayerItemData 타입인지 확인
            if (data is PlayerItemData itemData)
            {
                // 레이 시작점
                Vector3 rayOrigin = transform.position + (Vector3.up * 1.0f);

                // 생성 지점 (기본은 레이 시작점)
                Vector3 spawnPos = rayOrigin;

                // 레이캐스트 아래로 10 길이
                if (Physics.Raycast(rayOrigin, Vector3.down, out _hit, 10.0f))
                {
                    // 생성 지점은 히트한 위치
                    spawnPos = _hit.point;
                }
                else
                {
                    // 바닥이 너무 멀면 내 위치
                    spawnPos = transform.position;
                }

                // itemName 포장
                object[] initData = new object[] { itemName };

                // 네트워크 객체 생성
                PhotonNetwork.Instantiate(itemData.prefab.name, spawnPos, Quaternion.Euler(0, transform.rotation.y, 0), 0 , initData);

                Debug.Log($"{itemName} 버리기(네트워크 생성) 성공!");
            }
        }
        else
        {
            Debug.LogError($"아이템 데이터를 찾을 수 없습니다: {itemName}");
        }
    }
}
