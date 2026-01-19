using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHandler : MonoBehaviourPun, IPunObservable
{
    public static PlayerHandler localPlayer;

    [Header("로컬 켤 것")]
    [SerializeField] MonoBehaviour[] _scripts; // PlayerInputHandler 같은 것들

    [Header("리모트 끌 것")]
    [SerializeField] GameObject _camera;        // 카메라
    [SerializeField] GameObject _canvas;        // 캔버스

    public Transform CameraTrans => _camera.transform;
    public string CurrentItem = "";


    private void Awake()
    {
        // 로컬 객체일 때
        if (photonView.IsMine == true)
        {
            // 로컬 플레이어 등록
            localPlayer = this;

            // 켤 스크립트 켜기
            EnableLocalComponents();

            // 내 캐릭터 닉네임 설정

            // 퀵슬롯 기초 설정
            QuickSlotManager.Instance.UpdateUI();
        }
        // 리모트 객체일 때
        else
        {
            // 끌 것들 끄기 
            DisableRemoteComponents();
        }
    }

    private void OnDestroy()
    {
        // 나중에 없는데 참조할 수 있으니까 방지
        if (localPlayer == this)
        {
            localPlayer = null;
        }
    }

    // 로컬 컴포넌트 설정
    private void EnableLocalComponents()
    {
        // 스크립트 켜기
        foreach (var script in _scripts)
        {
            script.enabled = true;
        }

        // PlayerInput 켜기
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = true;
        }

    }

    // 리모트 컴포넌트 끄기
    private void DisableRemoteComponents()
    {
        // 카메라 끄기
        if (_camera != null) _camera.SetActive(false);
        if (_canvas != null) _canvas.SetActive(false);
    }

    // 퀵슬롯 변경 시
    public void ChangeQuickSlot(string itemName)
    {
        if (QuickSlotManager.Instance == null) return;

        // 상태가 달라졌다면 갱신
        if (CurrentItem != itemName)
        {
            CurrentItem = itemName;

            // 내 화면에서도 손 모델을 갱신
            //UpdateHandModel(CurrentItem);
        }
    }



    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(CurrentItem);
        }
        else
        {
            string receiveItem = (string)stream.ReceiveNext();

            if (CurrentItem != receiveItem)
            {
                CurrentItem = receiveItem;

                // 아이템 변경
            }
        }
    }
}
