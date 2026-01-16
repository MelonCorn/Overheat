using Photon.Pun;
using System;
using UnityEngine;

public class EngineNode : TrainNode
{
    [Header("엔진 스탯")]
    private float _maxSpeed;         // 최고 속도
    private float _minSpeed;         // 최저 속도
    private float _maxFuel;          // 최대 연료
    private float _burnRate;         // 초당 연료 소모
    private float _accel;            // 가속도

    // 실시간 변수 (동기화용)
    private float _currentSpeed;     // 현재 속도
    private float _currentFuel;      // 현재 연료

    public float CurrentSpeed => _currentSpeed;
    public float CurrentFuel => _currentFuel;
    public float MaxFuel => _maxFuel;
    public float MaxSpeed => _maxSpeed;

    // UI 표시용 연료 비율 (0.0 ~ 1.0)
    public float FuelRatio => _maxFuel > 0 ? _currentFuel / _maxFuel : 0f;

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

            // 초기 속도는 최저 속도
            _currentSpeed = _minSpeed;

            // 연료
            _maxFuel = engineStat.maxFuel;

            // 효율
            _burnRate = engineStat.burnRate;

            // 가속도
            _accel = engineStat.accel;
        }
    }
    private void Update()
    {
        // 방장만
        if (PhotonNetwork.IsMasterClient == false) return;

        // 연료 소모
        BurnFuel();

        // 속도 계산
        CalculateSpeed();

        // 엔진 정보 갱신
        OnEngineStatChanged?.Invoke(_currentSpeed, _currentFuel, _maxFuel);
    }


    // 연료 소모
    private void BurnFuel()
    {
        // 연료 남아있으면
        if (_currentFuel > 0)
        {
            // 초당 소모
            _currentFuel -= _burnRate * Time.deltaTime;

            // 0 맞추기
            if (_currentFuel < 0) _currentFuel = 0;
        }
    }

    // 연료 비율에 따른 속도 계산
    private void CalculateSpeed()
    {
        // 연료 비율에 따라 목표 속도 설정
        float targetSpeed = Mathf.Lerp(_minSpeed, _maxSpeed, FuelRatio);

        // 가속도
        float smoothRate = _accel;

        // 감속할 때는 더 천천히 멈추게
        if (_currentSpeed > targetSpeed)
            smoothRate = _accel * 0.3f; // 관성 느낌

        // 
        _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, smoothRate * Time.deltaTime);
    }

    // 연료 추가
    // 연료 아이템에서 호출
    public void AddFuel(float amount)
    {
        _currentFuel += amount;

        if (_currentFuel > _maxFuel)
            _currentFuel = _maxFuel;

        // 연료 UI 갱신 이벤트 호출
    }


    // 동기화 (방장이 계산한 연료와 속도를 클라이언트에게 전송)
    public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        base.OnPhotonSerializeView(stream, info);

        // 방장
        if (stream.IsWriting)
        {
            stream.SendNext(_currentSpeed); // 현재 속도
            stream.SendNext(_currentFuel);  // 현재 연료
        }
        // 클라
        else
        {
            _currentSpeed = (float)stream.ReceiveNext();
            _currentFuel = (float)stream.ReceiveNext();

            // 엔진 정보 갱신
            OnEngineStatChanged?.Invoke(_currentSpeed, _currentFuel, _maxFuel);
        }
    }
}
