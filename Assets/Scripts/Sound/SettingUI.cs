using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] GameObject _settingPanel;
    [SerializeField] GameObject _exitBlind; // 퇴장 시 가림막

    [Header("화면 모드 드롭다운")]
    public TMP_Dropdown screenModeDropdown;

    [Header("마우스 감도")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityText;

    [Header("사운드 슬라이더")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    [Header("사운드 토글")]
    public Toggle masterToggle;
    public Toggle bgmToggle;
    public Toggle sfxToggle;

    [Header("사운드 텍스트")]
    public TextMeshProUGUI masterText;
    public TextMeshProUGUI bgmText;
    public TextMeshProUGUI sfxText;

    [Header("사운드 아이콘")]
    public Image masterIcon;
    public Image bgmIcon;
    public Image sfxIcon;

    [Header("아이콘 리소스")]
    [SerializeField] Sprite _soundOnSprite;
    [SerializeField] Sprite _soundOffSprite;

    [Header("버튼")]
    public Button leaveButton;
    public Button closeButton;

    // 패널 활성화 여부
    public bool IsActive => _settingPanel.activeSelf;


    // 세팅 패널 끄기
    public void SetPanelActive(bool active)
    {
        _settingPanel.SetActive(active);
        // 패널 끄면 가림막도 꺼주기
        if (!active && _exitBlind != null) _exitBlind.SetActive(false);
    }

    // 퇴장 가림막 켜기
    public void SetExitBlind(bool active)
    {
        if (_exitBlind != null) _exitBlind.SetActive(active);
    }

    // 텍스트 업데이트 (소수점)
    public void UpdateSensitivityText(float value)
    {
        if (sensitivityText != null)
            sensitivityText.text = $"{(value * 0.1f):0.0}";
    }

    // 텍스트 업데이트 (퍼센트)
    public void UpdateVolumeText(TextMeshProUGUI text, float value)
    {
        if (text != null)
            text.text = Mathf.RoundToInt(value * 100).ToString();
    }

    // 아이콘 업데이트
    public void UpdateToggleIcon(Image targetImage, bool isOn)
    {
        if (targetImage != null)
            targetImage.sprite = isOn ? _soundOnSprite : _soundOffSprite;
    }
}
