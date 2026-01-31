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
        AddSpine(HumanBodyBones.Neck);
        AddSpine(HumanBodyBones.Head);
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
        float angleDiff = Vector3.SignedAngle(currentForward, targetForward, Vector3.up);

        // 차이만큼 돌려주기
        // 웨이트 값 곱해서 부드럽게 섞이게
        _spines[0].Rotate(Vector3.up, angleDiff * _itemHandler.TargetWeight, Space.World);




        // 허리 상하 보정
        // 목표 각도
        float targetAngle = _itemHandler.AimAngle;
        
        // 각도 부드럽게 설정
        _currentAngle = Mathf.Lerp(_currentAngle, targetAngle, Time.deltaTime * _rotSpeed);
        
        // 척추본 수 만큼 각도 나누기
        float anglePerBone = _currentAngle / _spines.Count;
        
        // 회전 적용
        foreach (Transform spine in _spines)
        {
            // 나눈 각도 회전
            spine.Rotate(Vector3.right, anglePerBone);
        }
    }
}
