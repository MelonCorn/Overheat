using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] GameObject _playerPrefab;
    [SerializeField] Transform _spawnPoint;
    [SerializeField] float interval = 1.5f; // 간격

    // 플레이어 스폰
    public void PlayerSpawn()
    {
        StartCoroutine(Spawn());
    }

    private IEnumerator Spawn()
    {
        // 네트워크 준비될 때 까지 대기 (안전장치)
        while (PhotonNetwork.IsConnectedAndReady == false)
        {
            yield return null;
        }

        // 이미 내 캐릭터가 있다면 중복 생성 방지
        if (PlayerHandler.localPlayer != null) yield break;

        // 내 네트워크 번호
        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

        // 번호만큼 간격 띄워서
        Vector3 offset = new Vector3(0f, 0f, (actorNumber - 1) * interval);

        // 최종 스폰 위치
        Vector3 finalPos = _spawnPoint.position + offset;

        // 객체 스폰
        GameObject player = PhotonNetwork.Instantiate(_playerPrefab.name, finalPos, Quaternion.identity);
        PlayerHandler.localPlayer = player.GetComponent<PlayerHandler>();

        Debug.Log("로컬 플레이어 스폰 완료");
    }
}
