using Photon.Pun;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TrainNode : MonoBehaviourPun, IPunObservable, IPunInstantiateMagicCallback // 네트워크 객체 생성 후 데이터 콜백
{
    protected int _maxHp;         // 최대 체력
    protected int _currentHp;     // 현재 체력

    protected TrainNode _prevTrain;   // 앞차
    protected TrainNode _nextTrain;   // 뒷차

    [Header("후방 연결부")]
    [SerializeField] Transform _rearSocket;


    // 참조 데이터
    public TrainDataSO Data { get; private set; }

    public event Action<int, int> OnHpChanged;
    public event Action OnExplode;
    public Transform RearSocket => _rearSocket;
    public int CurrentHp => _currentHp;
    public int MaxHp => _maxHp;

    private bool _isExploding = false;

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

        // UI 생성
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
        if (_currentHp <= 0) return;

        _currentHp -= amount;

        // 사망 판정
        if (_currentHp <= 0)
        {
            // 깔끔하게 0
            _currentHp = 0;

            // 폭발 RPC 발송
            photonView.RPC(nameof(ExplodeRPC), RpcTarget.All);
        }

        // 본인 권한의 SerializeView는 읽기가 안됨
        // 그래서 직접 호출
        OnHpChanged?.Invoke(_currentHp, _maxHp);
    }

    // 수리
    public void TakeRepair(int amount)
    {
        if (_currentHp >= _maxHp) return;

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
    [PunRPC]
    public void ExplodeRPC()
    {
        // 앞차랑 연결 끊기
        if (_prevTrain != null)
        {
            _prevTrain.ConnectNextTrain(null);
            _prevTrain = null;
        }

        // 꼬리자르기 (리스트 정리)
        if (TrainManager.Instance != null)
        {
            TrainManager.Instance.CutTail(this);
        }

        // 폭발
        Explode();
    }

    // 호출용 폭발
    public void Explode()
    {
        // 이미 폭발 중이면 무시
        if (_isExploding) return;
        
        _isExploding = true;

        // 폭발 코루틴 시작
        StartCoroutine(ExplodeCoroutine());
    }

    IEnumerator ExplodeCoroutine()
    {
        Debug.Log($"{name} 쾅!");

        // 연출 딜레이
        yield return new WaitForSeconds(0.15f);

        // 뒷차 연쇄 작용
        if (_nextTrain != null)
        {
            _nextTrain.Explode();
        }

        // 나 타고있으면 사망
        // CheckLocalPlayerHit();

        // 내부 아이템 전소

        // UI 파괴
        OnExplode?.Invoke();

        // 삭제 딜레이
        yield return new WaitForSeconds(3f);

        // 진짜 사망
        if (PhotonNetwork.IsMasterClient) PhotonNetwork.Destroy(gameObject);
        else gameObject.SetActive(false);
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

    public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // 룸 소유라 방장이 실행
        if (stream.IsWriting)
        {
            stream.SendNext(_currentHp);
        }
        else
        {
            _currentHp = (int)stream.ReceiveNext();

            // UI 갱신 알림
            OnHpChanged?.Invoke(_currentHp, _maxHp);
        }
    }
}
