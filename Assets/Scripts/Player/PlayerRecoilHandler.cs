using UnityEngine;

public class PlayerRecoilHandler : MonoBehaviour
{
    
    private Vector3 _targetRotation;    // 목표 회전값 (반동 목표 지점)
    private Vector3 _currentRotation;   // 현재 회전값 

    // 무기 데이터
    private float _returnSpeed;         // 복귀 속도
    private float _snappiness;          // 반동 속도

    private void OnEnable()
    {
        _targetRotation = Vector3.zero;
        _currentRotation = Vector3.zero;

        transform.localRotation = Quaternion.identity;
    }

    private void Update()
    {
        // 무기 없어도 이전 무기 속도만치로 돌아감

        // 목표 회전값 서서히 원점으로 복구
        _targetRotation = Vector3.Lerp(_targetRotation, Vector3.zero, _returnSpeed * Time.deltaTime);

        // 현재 회전값은 목표 회전값 부드럽게 따라감
        _currentRotation = Vector3.Slerp(_currentRotation, _targetRotation, _snappiness * Time.deltaTime);

        // 실제 카메라 로컬 회전에 적용
        transform.localRotation = Quaternion.Euler(_currentRotation);
    }


    // 무기 데이터 변경
    public void SetWeaponData(WeaponData data)
    {
        if (data == null) return;

        // 무기 데이터 할당
        _snappiness = data.snappiness;
        _returnSpeed = data.returnSpeed;
    }

    // 총 쏠 때 호출
    public void RecoilFire(WeaponData data)
    {
        if (data == null) return;

        // 반동 추가
        // X 무조건 위로, Y는 랜덤
        _targetRotation += new Vector3(
            -data.recoilX,
            Random.Range(-data.recoilY, data.recoilY),
            0f);

        // 최대 각도 제한두기 maxRecoilX보다 위로 올라가지 않게
        float clampedX = Mathf.Clamp(_targetRotation.x, -data.maxRecoilX, 0f);
        _targetRotation = new Vector3(clampedX, _targetRotation.y, _targetRotation.z);
    }
}
