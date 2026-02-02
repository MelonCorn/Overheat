using System.Collections.Generic;
using UnityEngine;

public class RailSpawner : MonoBehaviour
{
    [Header("레일 설정")]
    [SerializeField] PoolableObject _railPrefab; // 레일 프리팹

    [Header("범위 설정")]
    [SerializeField] float _spawnZ = 150f;       // 앞 어디까지 채울지
    [SerializeField] float _startBackZ = 200f;   // 뒤로 얼마나 깔아둘지
    [SerializeField] float _despawnOffset = 220f;// 반납 기준   (_startBackZ 보다 커야 제대로 작동 무한 삭제생성가능성)

    // 활성화된 레일 리스트
    private List<RailMove> _activeRails = new List<RailMove>();

    public float Speed { get; private set; }

    private Transform _target; // 기준점

    private void Start()
    {
        // 시작하자마자 꽉
        Reposition();
    }

    private void Update()
    {
        // 타겟 설정
        if (_target == null)
        {
            if (PlayerHandler.localPlayer != null)
                _target = PlayerHandler.localPlayer.transform;
            else _target = transform;
        }

        // 엔진 속도
        Speed = (TrainManager.Instance != null && TrainManager.Instance.MainEngine != null) ?
            TrainManager.Instance.MainEngine.CurrentSpeed : 0f;

        // 레일 관리
        CheckSpawnRail();
        CheckDespawnRail();
    }

    // 레일 채우기 (씬 시작, 타임라인)
    public void Reposition()
    {
        if (_target == null) _target = transform;

        // 기존 레일 싹 반납
        for (int i = _activeRails.Count - 1; i >= 0; i--)
        {
            if (_activeRails[i] != null)
                _activeRails[i].Poolable.Release();
        }
        _activeRails.Clear();

        // 시작 위치 잡기
        // 타겟 위치부터 뒤로 쭉 뺀 Z 부터
        Vector3 currentPos = new Vector3(0, transform.position.y, _target.position.z - _startBackZ);
        Quaternion currentRot = Quaternion.identity;

        //  앞쪽 한계까지 계속 이어붙이기
        while (currentPos.z < _target.position.z + _spawnZ)
        {
            // 레일 생성
            RailMove newRail = SpawnRail(currentPos, currentRot);

            // 다음 위치는 방금 만든 레일 소켓 위치로
            if (newRail != null)
            {
                currentPos = newRail.Socket.position;
                currentRot = newRail.Socket.rotation;
            }
            else
            {
                // 혹시 생성 실패하면 무한루프 방지
                break; 
            }
        }

        Debug.Log("[레일] 리셋 완료");
    }


    // 열차 생성 체크
    private void CheckSpawnRail()
    {
        // 하나도 없으면 리셋
        if (_activeRails.Count == 0)
        {
            Reposition();
            return;
        }

        // 맨 앞 레일 (리스트상 마지막)
        RailMove lastRail = _activeRails[_activeRails.Count - 1];

        // 맨 앞 레일의 소켓이 생성범위 안에 들어오면
        if (lastRail.Socket.position.z < _target.position.z + _spawnZ)
        {
            // 맨 앞 레일 소켓 기준 생성 
            SpawnRail(lastRail.Socket.position, lastRail.Socket.rotation);
        }
    }

    // 뒤쪽 레일 삭제 체크
    private void CheckDespawnRail()
    {
        // 레일 없으면 안해도됨
        if (_activeRails.Count == 0) return;

        // 맨 뒤 레일 (리스트상 맨 앞)
        RailMove firstRail = _activeRails[0];

        // 타겟 위치 - 오프셋 보다 작으면 삭제
        float despawnZLimit = _target.position.z - _despawnOffset;

        if (firstRail.transform.position.z < despawnZLimit)
        {
            // 반납
            firstRail.Poolable.Release();

            // 리스트에서 제거
            _activeRails.RemoveAt(0);
        }
    }

    // 레일 생성
    private RailMove SpawnRail(Vector3 pos, Quaternion rot)
    {
        Debug.Log("열차 생성1");
        // 풀 매니저 있어야 함
        if (PoolManager.Instance == null) return null;
        Debug.Log("열차 생성2");

        // 풀에서 레일 가져옴
        PoolableObject obj = PoolManager.Instance.Spawn(_railPrefab, pos, rot);

        RailMove rail = obj.GetComponent<RailMove>();

        if (rail != null)
        {
            // 레일 초기화
            rail.Setup(this);

            // 리스트에 등록
            _activeRails.Add(rail);
        }

        return rail;
    }
}
