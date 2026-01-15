using Photon.Pun;
using UnityEngine;

public class EngineNode : TrainNode
{
    [Header("엔진 스탯")]
    private float _maxSpeed;         // 최고 속도
    private float _minSpeed;         // 최저 속도
    private float _maxfuel;          // 최대 연료
    private float _burnRate;         // 초당 연료 소모

    // 실시간 변수 (동기화용)
    private float _currentSpeed;     // 현재 속도
    private float _currentFuel;      // 현재 연료

    public float CurrentSpeed => _currentSpeed;

    // UI 표시용 연료 비율 (0.0 ~ 1.0)
    public float FuelRatio => _maxfuel > 0 ? _currentFuel / _maxfuel : 0f;

    public override void Init(TrainDataSO data, int level)
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
        if (Data is TrainEngineDataSO engineData)
        {
            // 레벨의 스탯
            var engineStat = engineData.GetEngineStat(level);

            // 속도
            _maxSpeed = engineStat.maxSpeed;
            _minSpeed = engineStat.minSpeed;

            // 연료
            _maxfuel = engineStat.maxfuel;

            // 효율
            _burnRate = engineStat.burnRate;
        }
    }
    private void Update()
    {
        // 방장만
        if (PhotonNetwork.IsMasterClient) return;

        // 연료 소모
        BurnFuel();

        // 속도 계산
        CalculateSpeed();
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
        // 비율 계산 (프로퍼티로 계산)
        float ratio = FuelRatio;

        // 비율 1 : 최고속도, 0 : 최저속도
        _currentSpeed = Mathf.Lerp(_minSpeed, _maxSpeed, ratio);
    }

    // 연료 추가
    // 연료 아이템에서 호출
    public void AddFuel(float amount)
    {
        _currentFuel += amount;

        if (_currentFuel > _maxfuel)
            _currentFuel = _maxfuel;

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
            float receivedSpeed = (float)stream.ReceiveNext();
            float receivedFuel = (float)stream.ReceiveNext();

            _currentFuel = receivedFuel;
            _currentSpeed = receivedSpeed;
        }
    }
}
