using Photon.Pun;
using System;
using UnityEngine;

public class EngineNode : TrainNode
{
    [Header("보일러")]
    [SerializeField] Boiler _boiler;

    [Header("네트워크 동기화 무시 설정")]
    [SerializeField] float _networkIgnoreTime;

    private float _maxSpeed;         // 최고 속도
    private float _minSpeed;         // 최저 속도
    private float _accel;            // 가속도



    // 실시간 변수 (동기화용)
    private float _currentSpeed;     // 현재 속도
    private float _lastNetworkIgnoreTime;// 네트워크 데이터 무시 시간

    public float CurrentSpeed => _currentSpeed;
    public float MaxSpeed => _maxSpeed;

    public event Action<float, float, float> OnEngineStatChanged;


    public override void Init(TrainData data, int level)
    {
        base.Init(data, level);

        SetData(level);
    }


    public override void Upgrade(int level)
    {
        base.Upgrade(level);

        SetData(level);
    }


    // 레벨 데이터 설정
    private void SetData(int level)
    {
        if (Data is TrainEngineData engineData)
        {
            // 레벨의 스탯
            var engineStat = engineData.GetEngineStat(level);

            // 속도
            _maxSpeed = engineStat.maxSpeed;
            _minSpeed = engineStat.minSpeed;

            // 가속도
            _accel = engineStat.accel;

            // 초기 속도는 최저 속도
            _currentSpeed = _minSpeed;

            // 보일러 연료 설정
            if (_boiler != null)
            {
                _boiler.Init(this, engineStat.maxFuel, engineStat.burnRate);
            }
        }
    }
    private void Update()
    {
        // 방장만
        if (PhotonNetwork.IsMasterClient == false) return;

        // 연료 소모
        if (_boiler != null)
        {
            _boiler.BurnFuel();
        }

        // 속도 계산
        CalculateSpeed();

        // 엔진 정보 UI 갱신
        if (_boiler != null)
        {
            OnEngineStatChanged?.Invoke(_currentSpeed, _boiler.CurrentFuel, _boiler.MaxFuel);
        }
    }


    // 연료 비율에 따른 속도 계산
    private void CalculateSpeed()
    {
        // 연료 비율에 따라 목표 속도 설정
        float targetSpeed = Mathf.Lerp(_minSpeed, _maxSpeed, _boiler.FuelRatio);

        // 가속도
        float smoothRate = _accel;

        // 감속할 때는 더 천천히 멈추게
        if (_currentSpeed > targetSpeed)
            smoothRate = _accel * 0.3f; // 관성 느낌

        // 
        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, smoothRate * Time.deltaTime);
    }

    // 연료 충전 요청보내기 (Boiler)
    public void AddFuelRequest(float amount)
    {
        // 로컬에서 먼저 즉시 반영
        // 예측으로 미리 해두고 방장이 뿌려주는 데이터로 동기화
        if (_boiler != null)
        {
            // 미리 연료 채우기
            _boiler.AddFuel(amount);

            // UI도 즉시 갱신
            OnEngineStatChanged?.Invoke(_currentSpeed, _boiler.CurrentFuel, _boiler.MaxFuel);

            // 예측을 위해서 _networkIgnoreTime 시간동안 오는 옛날 데이터 무시
            _lastNetworkIgnoreTime = Time.time + _networkIgnoreTime;
        }

        // 다 하고 실제로 방장에게 연료 추가 요청
        photonView.RPC(nameof(RPC_AddFuel), RpcTarget.MasterClient, amount);
    }

    // 방장이 실제 연료 추가
    [PunRPC]
    private void RPC_AddFuel(float amount)
    {
        if (_boiler != null)
        {
            _boiler.AddFuel(amount);
        }
    }

    // 동기화 (방장이 계산한 연료와 속도를 클라이언트에게 전송)
    public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        base.OnPhotonSerializeView(stream, info);

        // 방장
        if (stream.IsWriting)
        {
            stream.SendNext(_currentSpeed); // 현재 속도
            stream.SendNext(_boiler.CurrentFuel); // 보일러 연료
        }
        // 클라
        else
        {
            _currentSpeed = (float)stream.ReceiveNext();
            float fuel = (float)stream.ReceiveNext();

            // 네트워크 무시 시간 지나면
            if (Time.time >= _lastNetworkIgnoreTime)
            {
                // 보일러에 연료 적용
                _boiler.SetFuel(fuel);

                // 엔진 정보 UI 갱신
                OnEngineStatChanged?.Invoke(_currentSpeed, _boiler.CurrentFuel, _boiler.MaxFuel);
            }
        }
    }
}
