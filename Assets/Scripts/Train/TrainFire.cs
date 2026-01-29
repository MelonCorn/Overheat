using Photon.Pun;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TrainFire : MonoBehaviourPun, IDamageable, IPunObservable, IPunInstantiateMagicCallback
{
    [Header("불 설정")]
    [SerializeField] int _maxHp = 100;       // 불의 생명력
    [SerializeField] int _damage = 5;        // 열차에 주는 도트 데미지
    [SerializeField] float _interval = 2.0f; // 데미지 주기
    [SerializeField] float _shrinkSpeed = 5f;// 줄어드는 속도

    private int _currentHp;                  // 현재 체력
    private float _defaultScale;             // 기본 크기
    private Vector3 _targetScale;            // 목표 크기

    private TrainNode _targetTrain;          // 타겟 열차

    // Update에서 반납 체크하는데 혹시나 찰나 생길까봐 넣음
    private bool _isInit = false;

    private void Awake()
    {
        // 초기화
        _currentHp = _maxHp;
        _defaultScale = transform.localScale.x;
    }

    private void OnEnable()
    {
        // 상태 초기화 (재사용)
        _currentHp = _maxHp;
        _isInit = false;
        _targetTrain = null;

        _targetScale = Vector3.one * _defaultScale;
        transform.localScale = Vector3.one * _defaultScale;

        // 도트 데미지 시작 (방장만)
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(BurnCoroutine());
        }
    }
    private void OnDisable()
    {
        // 돌고 있던 코루틴 올 스톱
        StopAllCoroutines();
        // 걍 또 해버림
        _targetTrain = null;
        _isInit = false;
    }

    private void Update()
    {
        if (transform.localScale != _targetScale)
        {
            // 크기 부드럽게 축소
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * _shrinkSpeed);
        }

        // 초기화 이후 타겟 열차가 있을 때
        if (_isInit && _targetTrain != null)
        {
            // 열차 밖으로 나와지거나, 비활성화되면 소멸
            if (transform.parent == null || _targetTrain.gameObject.activeInHierarchy == false)
            {
                // 즉시 소멸 처리
                Extinguish();
            }
        }
    }

    // 소화기 피격
    public void TakeDamage(int damage)
    {
        if (_currentHp <= 0) return;

        // 방장에게 데미지 보고
        photonView.RPC(nameof(RPC_HitFire), RpcTarget.MasterClient, damage);
    }

    // 방장이 받을 피격 리포트
    [PunRPC]
    private void RPC_HitFire(int amount)
    {
        if (PhotonNetwork.IsMasterClient == false) return;

        // 체력까기
        _currentHp -= amount;

        // 크기 조절
        UpdateFireSize();

        // 불 꺼짐
        if (_currentHp <= 0)
        {
            Extinguish();
        }
    }

    // 크기 갱신
    private void UpdateFireSize()
    {
        // 체력 비율
        float ratio = (float)_currentHp / _maxHp;
        // 최소 0.3크기
        float targetScale = Mathf.Max(_defaultScale * 0.3f, _defaultScale * ratio);
        // 목표 크기 설정
        _targetScale = Vector3.one * targetScale;
    }

    // 소화
    private void Extinguish()
    {
        // 중복 실행 방지
        if (gameObject.activeInHierarchy == false) return;

        // 풀 반납
        if (PhotonNetwork.IsMasterClient == true)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            // 클라이언트는 숨김
            gameObject.SetActive(false);
        }
    }

    // 불태우기 코루틴
    private IEnumerator BurnCoroutine()
    {
        // 체력 있으면 계속
        while (_currentHp > 0)
        {
            yield return new WaitForSeconds(_interval);

            // 열차 있고 체력 있으면
            if (_targetTrain != null && _targetTrain.CurrentHp > 0)
            {
                // 피해
                _targetTrain.TakeDamage(_damage);
            }
            // 열차 사라지면                      // 코루틴에서도 혹시 모르니까 추가
            else if (_targetTrain == null || _targetTrain.gameObject.activeInHierarchy == false) 
            {
                // 파괴
                if (PhotonNetwork.IsMasterClient) PhotonNetwork.Destroy(gameObject);
                yield break;
            }
        }
    }


    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // 보낸 데이터 꺼내기
        object[] data = info.photonView.InstantiationData;

        if (data != null && data.Length > 0)
        {
            // 부모 ID 찾기
            int parentID = (int)data[0];

            PhotonView parentView = PhotonView.Find(parentID);

            if (parentView != null)
            {
                // 부모 찾아서 세팅
                _targetTrain = parentView.GetComponent<TrainNode>();

                // 열차 하위 객체로 들어감
                transform.SetParent(_targetTrain.transform);

                // 초기화 완료
                _isInit = true;
            }
        }
    }
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(_currentHp);
        }
        else
        {
            int receiveHp = (int)stream.ReceiveNext();

            // 체력 받고 크기 지정
            if (_currentHp != receiveHp)
            {
                _currentHp = receiveHp;
                UpdateFireSize();

                // HP 0 이하면 끄기
                if (_currentHp <= 0) gameObject.SetActive(false);
            }
        }
    }
}
