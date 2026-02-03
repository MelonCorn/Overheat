using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrainUI : MonoBehaviour
{
    [SerializeField] Image _icon;
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] TextMeshProUGUI _hpText;
    [SerializeField] Button _takeDmgButton;
    [SerializeField] TextColorData _colorData;

    private TrainNode _targetNode;

    public void Init(TrainNode node)
    {
        if (node == null) return;

        _targetNode = node;

        // 아이콘
        if (_icon != null) _icon.sprite = node.Data.icon;

        // 텍스트 초기화
        if(_nameText != null) _nameText.text = node.Data.displayName;
        UpdateHPText(node.CurrentHp, node.MaxHp);

        // 체력 변화 이벤트 구독
        _targetNode.OnHpChanged += UpdateHPText;
        // 폭발 이벤트 구독
        _targetNode.OnExplode += Explode;
    }

    // 체력 텍스트 갱신
    private void UpdateHPText(int current, int max)
    {
        if (_hpText == null || _colorData == null) return; 

        // 텍스트 갱신
        _hpText.text = $"{current} / {max}";

        // 체력 비율
        float ratio = (float)current / max;

        // 색상 갱신
        _hpText.color = _colorData.gradient.Evaluate(ratio);
    }

    // 폭발
    private void Explode()
    {
        Destroy(gameObject);
    }

    // UI 파괴 시 구독 해제
    private void OnDestroy()
    {
        if (_targetNode != null)
        {
            _targetNode.OnHpChanged -= UpdateHPText;
            _targetNode.OnExplode -= Explode;
        }
    }
}
