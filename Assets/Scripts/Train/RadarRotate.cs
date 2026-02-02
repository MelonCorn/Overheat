using UnityEngine;

public class RadarRotate : MonoBehaviour
{
    [Header("회전 설정")]
    [SerializeField] float _speed = 180f; // 회전 속도 (초당)

    private void OnEnable()
    {
        // 조금 랜덤
        _speed = Random.Range(_speed - 5f, _speed + 5f);
    }

    void Update()
    {
        // Space.Self로 열차 기준
        transform.Rotate(Vector3.up * _speed * Time.deltaTime, Space.Self);
    }
}
