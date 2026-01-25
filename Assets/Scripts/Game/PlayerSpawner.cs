using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] GameObject _playerPrefab;
    [SerializeField] Transform _spawnPoint;

    //private IEnumerator Start()
    //{
    //    // 네트워크 준비될 때 까지 대기
    //    while (PhotonNetwork.IsConnectedAndReady == false)
    //    {
    //        yield return null;
    //    }
    //
    //    // 프로퍼티 사용하지 않고 조용히 가져오기
    //    TrainManager trainManager = FindAnyObjectByType<TrainManager>();
    //
    //    // 대기실에서 스킵
    //    // 열차 매니저 준비 완료할 때까지 대기
    //    // TrainManager가 존재하고, Ready될 때 까지
    //    if (trainManager != null)
    //    {
    //        // 열차 생성 완료될 때까지 대기
    //        yield return new WaitUntil(() => TrainManager.Instance.IsTrainReady);
    //    }
    //
    //    // 객체 스폰해서 로컬 플레이어로 지정
    //    PlayerHandler.localPlayer = PhotonNetwork.Instantiate(_playerPrefab.name, _spawnPoint.position, Quaternion.identity).GetComponent<PlayerHandler>();
    //}

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

        // 객체 스폰
        GameObject player = PhotonNetwork.Instantiate(_playerPrefab.name, _spawnPoint.position, Quaternion.identity);
        PlayerHandler.localPlayer = player.GetComponent<PlayerHandler>();

        Debug.Log("로컬 플레이어 스폰 완료");
    }
}
