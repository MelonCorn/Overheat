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
    private float _length = 3.0f;               // 길이

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
        // 방향
        Vector3 dir = (_hitPoint - _startPoint).normalized;
        // 현재 거리
        float currentDist = 0;

        // 도착할 때까지 이동
        while (currentDist < totalDist)
        {
            currentDist += Time.deltaTime * _speed; // 속도만큼 이동

            // 머리부분 도착점 넘으면 안되니까
            float headDist = Mathf.Min(currentDist, totalDist);
            Vector3 headPos = _startPoint + dir * headDist;

            // 꼬리는 머리에서 길이만큼 뒤에서 따라감
            float tailDist = Mathf.Max(0, headDist - _length);
            Vector3 tailPos = _startPoint + dir * tailDist;

            // 적용
            _line.SetPosition(0, tailPos);
            _line.SetPosition(1, headPos);

            yield return null;
        }

        // 도착
        _line.SetPosition(0, _hitPoint);
        _line.SetPosition(1, _hitPoint);

        // 이펙트 생성
        if (_impactPrefab != null && PoolManager.Instance != null)
        {
            // 생성되고 알아서 이펙트 뿜음
            PoolManager.Instance.Spawn(_impactPrefab, _hitPoint, Quaternion.identity);
        }

        // 잔상 사라지기 시간
        yield return new WaitForSeconds(0.05f);

        // 반납
        if (_poolObj != null) _poolObj.Release();
        else gameObject.SetActive(false);
    }
}
