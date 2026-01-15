using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.Events;

public class TrainNode : MonoBehaviourPun, IPunObservable, IPunInstantiateMagicCallback // 네트워크 객체 생성 후 데이터 콜백
{
    protected int _maxHp;         // 최대 체력
    protected int _currentHp;     // 현재 체력

    [Header("후방 연결부")]
    [SerializeField] Transform _rearSocket;

    public event Action<int, int> OnHpChanged;
    public Transform RearSocket => _rearSocket;

    // 참조 데이터
    public TrainDataSO Data { get; private set; }
    public int CurrentHp => _currentHp;
    public int MaxHp => _maxHp;

    protected TrainNode _prevTrain;   // 앞차
    protected TrainNode _nextTrain;   // 뒷차

    public virtual void Init(TrainDataSO data, int level)
    {
        // 데이터
        Data = data;

        // 레벨의 스탯
        var stat = Data.GetBasicStat(level);

        // 체력
        _maxHp = stat.maxHP;
        _currentHp = _maxHp;

        // 이름
        gameObject.name = Data.trainName;

        if (TestTrainUIManager.Instance != null)
        {
            TestTrainUIManager.Instance.CreateUI(this);
        }
    }

    // 업그레이드
    public virtual void Upgrade(int level)
    {
        // 레벨의 스탯
        var stat = Data.GetBasicStat(level);

        // 체력
        _maxHp = stat.maxHP;
    }

    // 앞차의 후방 연결부에 붙임
    public void Attach(TrainNode prevTrain)
    {
        // 앞차 저장
        _prevTrain = prevTrain;

        //  앞차가 없다면 (아마 엔진)
        if (prevTrain == null)
        {
            // 0,0,0
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            return;
        }

        // 앞차 소켓 확인
        if (prevTrain.RearSocket != null)
        {
            // 소켓의 위치와 회전 그대로 적용
            transform.position = prevTrain.RearSocket.position;
            transform.rotation = prevTrain.RearSocket.rotation;
        }
        else
        {
            Debug.LogError($"{prevTrain.name}에 RearSocket이 연결되지 않았습니다!");
        }
    }

    // 다음 칸 생기면 정보 받음
    public void ConnectNextTrain(TrainNode nextTrain)
    {
        // 뒷차 저장
        _nextTrain = nextTrain;
    }

    // 피해
    public void TakeDamage(int amount)
    {
        _currentHp -= amount;

        if (_currentHp < 0)
            _currentHp = 0;

        // 본인 권한의 SerializeView는 읽기가 안됨
        // 그래서 직접 호출
        OnHpChanged?.Invoke(_currentHp, _maxHp);
    }

    // 수리
    public void TakeRepair(int amount)
    {
        _currentHp += amount;

        if (_currentHp > _maxHp)
            _currentHp = _maxHp;

        OnHpChanged?.Invoke(_currentHp, _maxHp);
    }


    // 열차 화재
    public void StartFire()
    {
        // 체력 일정 이하면 화재
        // 내부 아이템 전소
        // 내부에 플레이어 있으면 계속 대미지
    }


    // 열차 파괴 
    public void Explode()
    {
        // 체력 0 이하면 폭발
        // 내부 아이템 전소
        // 내부 플레이어 사망
        // 다음 차에 데미지 99999 줘서 계속 연쇄 폭발
    }


    // 네트워크로 생성되면 호출
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // 보낸 데이터 꺼내기
        object[] data = info.photonView.InstantiationData;

        // 데이터 없으면 무시 (상점) 
        if (data == null || data.Length < 3) return;

        // 데이터 언박싱
        int index = (int)data[0];            // 순서
        int level = (int)data[1];            // 레벨
        TrainType type = (TrainType)data[2]; // 타입

        // 매니저에 등록, Init 요청
        if (TrainManager.Instance != null)
        {
            TrainManager.Instance.RegisterNetworkTrain(this, index, type, level);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // 룸 소유라 방장이 실행
        if (stream.IsWriting)
        {
            stream.SendNext(_currentHp);
        }
        else
        {
            int receivedHp = (int)stream.ReceiveNext();
            // 값이 달라졌을 때만 반응
            if (_currentHp != receivedHp)
            {
                _currentHp = receivedHp;

                // UI 갱신 알림
                OnHpChanged?.Invoke(_currentHp, _maxHp);
            }
        }
    }
}
