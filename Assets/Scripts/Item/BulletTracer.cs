using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BulletTracer : MonoBehaviour
{
    private LineRenderer _line;      // 궤적용
    private PoolableObject _poolObj; // 반납용

    private PoolableObject _impactPrefab;       // 도착해서 터트릴 이펙트
    private Vector3 _startPoint;                // 시작점
    private Vector3 _hitPoint;                  // 도착점
    private float _speed = 300f;                // 속도 (초속)

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _poolObj = GetComponent<PoolableObject>();
    }

    // 초기화, 발사
    public void InitAndShoot(Vector3 start, Vector3 end, PoolableObject impactPrefab)
    {
        _startPoint = start;            // 시작점
        _hitPoint = end;                // 도착점
        _impactPrefab = impactPrefab;   // 이펙트

        // 궤적 On
        _line.enabled = true;
        // 첨엔 시작점에 뭉치기
        _line.SetPosition(0, start);
        _line.SetPosition(1, start);

        // 날아가는 연출 시작
        StartCoroutine(Fly());
    }

    private IEnumerator Fly()
    {
        // 총 거리
        float totalDist = Vector3.Distance(_startPoint, _hitPoint);
        // 현재 거리
        float currentDist = 0;

        // 도착할 때까지 이동
        while (currentDist < totalDist)
        {
            currentDist += Time.deltaTime * _speed; // 속도만큼 이동

            // 비율 계산
            float t = currentDist / totalDist;
            if (t > 1) t = 1;

            // 선 늘어나게
            Vector3 currentPos = Vector3.Lerp(_startPoint, _hitPoint, t);
            _line.SetPosition(1, currentPos);

            yield return null;
        }

        // 도착
        _line.SetPosition(1, _hitPoint);

        // 이펙트 생성
        if (_impactPrefab != null && PoolManager.Instance != null)
        {
            PoolManager.Instance.Spawn(_impactPrefab, _hitPoint, Quaternion.identity);
        }

        // 잔상 사라지기 시간
        yield return new WaitForSeconds(0.05f);

        // 반납
        if (_poolObj != null) _poolObj.Release();
        else gameObject.SetActive(false);
    }
}
