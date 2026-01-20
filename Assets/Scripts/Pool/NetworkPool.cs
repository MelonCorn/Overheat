using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public class NetworkPool : MonoBehaviour, IPunPrefabPool
{
    [Header("네트워크 프리팹")]
    [SerializeField] List<PoolableObject> _networkPrefabs;

    // 빠른 검색용
    private Dictionary<string, PoolableObject> _prefabDict = new Dictionary<string, PoolableObject>();

    private void Awake()
    {
        // 리스트를 딕셔너리로 변환
        foreach (var prefab in _networkPrefabs)
        {
            if (prefab != null)
            {
                _prefabDict[prefab.name] = prefab;
            }
        }
    }

    // 생성 요청 (포톤 -> 로컬 -> 풀매니저)
    public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
    {
        // 등록된 프리팹 검색
        if (_prefabDict.TryGetValue(prefabId, out PoolableObject prefab))
        {
            // 있으면 PoolManager에 스폰 요청
            PoolableObject poolObj = PoolManager.Instance.Spawn(prefab, position, rotation);

            // 포톤 초기화 때문에 잠깐 꺼두기
            // ID 설정할 땐 꺼져있어야 한다고 하는데
            poolObj.gameObject.SetActive(false);

            return poolObj.gameObject;
        }

        // 목록에 없으면 그냥 쌩으로 생성 (풀링 X)
        GameObject obj = Resources.Load<GameObject>(prefabId);
        if (obj != null)
        {
            GameObject newObj = Instantiate(obj, position, rotation);

            // 얘도 포톤 규칙대로 꺼서 줌
            newObj.SetActive(false);

            return newObj;
        }

        Debug.LogError($"[NetworkPool] 프리팹을 찾을 수 없습니다: {prefabId}");
        return null;
    }

    // 파괴 요청
    public void Destroy(GameObject gameObject)
    {
        // 플레이어같이 아예 그냥 지혼자 파괴되는 애들 예외
        if (gameObject == null) return;

        // PoolableObject 컴포넌트 있는지 확인
        PoolableObject poolObj = gameObject.GetComponent<PoolableObject>();

        if (poolObj != null)
        {
            // 있으면 내 PoolManager로 반납
            poolObj.Release();
        }
        else
        {
            // 풀링 대상 아니면 진짜 파괴
            // GameObject붙여서 무한 재귀 방지
            GameObject.Destroy(gameObject);
        }
    }
}
