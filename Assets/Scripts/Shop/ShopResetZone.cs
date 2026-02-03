using UnityEngine;

public class ShopResetZone : MonoBehaviour
{
    [Header("복귀할 위치")]
    [SerializeField] Transform _respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어인지 확인
        PlayerHandler player = other.GetComponent<PlayerHandler>();

        if (player != null)
        {
            // 내 캐릭터인지 확인
            if (player.photonView.IsMine)
            {
                // 강제 리스폰
                Respawn(player);
            }
        }
    }

    private void Respawn(PlayerHandler player)
    {
        CharacterController cc = player.GetComponent<CharacterController>();

        if (cc != null)
        {
            // CC를 잠시 끄고
            cc.enabled = false;

            // 이동 시키고
            player.transform.position = _respawnPoint.position;

            // CC 다시 켬
            cc.enabled = true;
        }
    }
}
