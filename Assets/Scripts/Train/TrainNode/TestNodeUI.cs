using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestNodeUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _hpText;
    [SerializeField] Button _takeDmgButton;

    private TrainNode _targetNode;

    public void Init(TrainNode node)
    {
        _targetNode = node;

        // 텍스트 초기화
        _nameText.text = node.gameObject.name;
        UpdateHPText(node.CurrentHp, node.MaxHp);

        // 체력 변화 이벤트 구독
        _targetNode.OnHpChanged += UpdateHPText;

        // 버튼 설정
        if (PhotonNetwork.IsMasterClient)
        {
            _takeDmgButton.onClick.AddListener(OnClickDamage);
        }
        else
        {
            _takeDmgButton.gameObject.SetActive(false); // 클라이언트는 숨김
        }
    }

    // 테스트용 피격 버튼
    private void OnClickDamage()
    {
        if (_targetNode != null)
        {
            _targetNode.TakeDamage(10);
        }
    }

    // 체력 텍스트 갱신
    private void UpdateHPText(int current, int max)
    {
        _hpText.text = $"{current} / {max}";
    }

    // UI 파괴 시 구독 해제
    private void OnDestroy()
    {
        if (_targetNode != null)
        {
            _targetNode.OnHpChanged -= UpdateHPText;
        }
    }
}
