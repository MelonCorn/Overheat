using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] GameObject _playerPrefab;
    [SerializeField] Transform _spawnPoint;
    private IEnumerator Start()
    {
        // 네트워크 준비될 때 까지 대기
        while (PhotonNetwork.IsConnectedAndReady == false)
        {
            yield return null;
        }

        // 열차 매니저 준비 완료할 때까지 대기
        // TrainManager가 존재하고, Ready될 때 까지
        yield return new WaitUntil(() => TrainManager.Instance != null && TrainManager.Instance.IsTrainReady);

        // 객체 스폰해서 로컬 플레이어로 지정
        PlayerHandler.localPlayer = PhotonNetwork.Instantiate(_playerPrefab.name, _spawnPoint.position, Quaternion.identity).GetComponent<PlayerHandler>();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
    }
}
