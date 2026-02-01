using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimHandler : MonoBehaviour
{
    private PhotonView _pv;
    private PlayerHandler _playerHandler;
    private PlayerItemHandler _itemHandler;

    [Header("부드러움 설정")]
    [SerializeField] float _rotSpeed = 20f; // 반응 속도

    [Header("상하 회전 증폭")]
    [SerializeField] float _upMultiplier = 1.1f;
    [SerializeField] float _downMultiplier = 1.1f;     

    // 척추 리스트
    private List<Transform> _spines = new List<Transform>();

    // 현재 각도
    private float _currentAngle = 0f;

    private void Awake()
    {
        _pv = GetComponent<PhotonView>();
        _playerHandler = GetComponent<PlayerHandler>();
        _itemHandler = GetComponent<PlayerItemHandler>();
    }
    private void Start()
    {
        // 척추 리스트에 추가
        AddSpine(HumanBodyBones.Spine);
        AddSpine(HumanBodyBones.Chest);
        AddSpine(HumanBodyBones.UpperChest);
        //AddSpine(HumanBodyBones.Neck);
        //AddSpine(HumanBodyBones.Head);
    }

    // 척추 리스트 추가
    private void AddSpine(HumanBodyBones boneType)
    {
        // 애니메이터의 본타입의 트랜스폼
        Transform bone = _playerHandler.PlayerAnim.GetBoneTransform(boneType);

        // 리스트에 추가
        if (bone != null) _spines.Add(bone);
    }


    // 애니메이션 재생 이후에 본 수정
    private void LateUpdate()
    {
        // 내껀 안해도 됨
        if (_pv.IsMine) return;

        if (_spines.Count == 0 || _itemHandler == null) return;

        // 무기 데이터 상하좌우 보정값
        float yawOffset = 0f;
        float pitchOffset = 0f;

        if (_itemHandler.CurrentWeaponData != null)
        {
            yawOffset = _itemHandler.CurrentWeaponData.spineYawOffset;
            pitchOffset = _itemHandler.CurrentWeaponData.spinePitchOffset;
        }

        // 허리 좌우 보정
        // 목표 방향
        Vector3 targetForward = transform.forward;
        targetForward.y = 0;
        targetForward.Normalize();

        // 현재 허리 방향
        Vector3 currentForward = _spines[0].forward;
        currentForward.y = 0;
        currentForward.Normalize();

        // 두 방향의 차이 (목표로부터 얼마나 틀어졌나)
        float yawDiff = Vector3.SignedAngle(currentForward, targetForward, Vector3.up);

        // (기본 보정 + 무기 보정 ) * 웨이트
        float finalYaw = (yawDiff + yawOffset) * _itemHandler.TargetWeight;

        // 차이만큼 돌려주기
        // 웨이트 값 곱해서 부드럽게 섞이게
        _spines[0].Rotate(Vector3.up, finalYaw, Space.World);


        // X 기울기 보정
        // 현재 허리 방향
        Vector3 currentDir = _spines[0].forward;

        // 수평 방향
        Vector3 horizonDir = currentDir;
        horizonDir.y = 0;
        horizonDir.Normalize();

        // 수평선과 허리가 얼마나 기울어졌는지 계산
        float pitchDiff = Vector3.SignedAngle(horizonDir, currentDir, _spines[0].right);

        // 기울어진만큼 반대로 돌려서 수평으로 만듬
        _spines[0].Rotate(Vector3.right, -pitchDiff * _itemHandler.TargetWeight, Space.Self);

        // 상하 증폭 기본값
        float finalDown = _downMultiplier;
        float finalUp = _upMultiplier;

        // 무기 있으면
        if (_itemHandler.CurrentWeaponData != null)
        {
            // 가져와서
            float weaponDown = _itemHandler.CurrentWeaponData._downMultiplier;
            float weaponUp = _itemHandler.CurrentWeaponData._upMultiplier;

            // 웨이트 0 ~ 1 에 따라 사이를 변동
            finalDown = Mathf.Lerp(_downMultiplier, weaponDown, _itemHandler.TargetWeight);
            finalUp = Mathf.Lerp(_upMultiplier, weaponUp, _itemHandler.TargetWeight);
        }


        // 허리 상하 보정
        // 목표 각도
        // 에임 각도 + (무기 보정 * 웨이트)
        float targetAngle = _itemHandler.AimAngle + (pitchOffset * _itemHandler.TargetWeight);

        // 아래를 본다면
        if (targetAngle > 0)
        {
            targetAngle *= finalDown;
        }
        // 위를 본다면
        else
        {
            targetAngle *= finalUp;
        }

        // 각도 부드럽게 설정
        _currentAngle = Mathf.Lerp(_currentAngle, targetAngle, Time.deltaTime * _rotSpeed);
        
        // 척추본 수 만큼 각도 나누기
        float anglePerBone = _currentAngle / _spines.Count;

        // 바라보는 방향
        targetForward = transform.forward;
        targetForward.y = 0;
        targetForward.Normalize();

        // 목표 방향 기준 수평 오른쪽 축
        Vector3 targetRightAxis = Vector3.Cross(Vector3.up, targetForward);

        // 회전 적용
        foreach (Transform spine in _spines)
        {
            // 나눈 각도 회전
            spine.Rotate(targetRightAxis, anglePerBone, Space.World);
        }
    }
}
