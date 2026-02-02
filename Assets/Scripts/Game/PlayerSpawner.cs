using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] GameObject _playerPrefab;
    [SerializeField] Transform[] _spawnPoints;

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

        // 번호의 스폰 포인트
        Transform spawnPoint = _spawnPoints[actorNumber - 1];

        // 객체 스폰
        GameObject player = PhotonNetwork.Instantiate(_playerPrefab.name, spawnPoint.position, Quaternion.identity);
        PlayerHandler.localPlayer = player.GetComponent<PlayerHandler>();

        Debug.Log("로컬 플레이어 스폰 완료");
    }
}
