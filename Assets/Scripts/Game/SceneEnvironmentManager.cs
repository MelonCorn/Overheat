using UnityEngine;

[System.Serializable]
public struct EnvironmentData
{
    [Header("스카이박스 설정")]
    public Material skyboxMaterial; // 스카이박스 머티리얼

    [Header("디렉셔널 라이트 설정")]
    public Color lightColor;        // 빛 색

    [Header("안개 설정")]
    public Color fogColor;          // 안개 색

    [Header("환경광 설정")]
    public Color ambientColor;      // 전체적인 틴트 (밤에는 어둡게)
}

public class SceneEnvironmentManager : MonoBehaviour
{
    public static SceneEnvironmentManager Instance { get; private set; }

    [Header("디렉셔널 라이트")]
    [SerializeField] Light _directionalLight; // 씬 주 조명

    [Header("설정값")]
    [SerializeField] EnvironmentData _daySettings;   // 홀수일 (낮)
    [SerializeField] EnvironmentData _nightSettings; // 짝수일 (밤)

    private void Awake()
    {
        Instance = this;
    }

    public void SetEnvironmentTime()                
    {
        bool isNight = (GameData.SurviveDay % 2 == 0);

        if (isNight)
        {
            // 짝수일 밤
            ApplySettings(_nightSettings);
        }
        else
        {
            // 홀수일 낮
            ApplySettings(_daySettings);
        }
    }

    private void ApplySettings(EnvironmentData data)
    {
        // 스카이박스 변경
        if (data.skyboxMaterial != null)
        {
            RenderSettings.skybox = data.skyboxMaterial;
        }

        // 라이트 설정
        if (_directionalLight != null)
        {
            _directionalLight.color = data.lightColor;
        }

        // 안개 설정
        RenderSettings.fog = true;                   // 켜기
        RenderSettings.fogColor = data.fogColor;     // 색

        // 환경광(Ambient) 설정
        // 단색모드로 변경
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        // 색상 변경
        RenderSettings.ambientLight = data.ambientColor;

        // 반사 업데이트 갱신
        DynamicGI.UpdateEnvironment();
    }
}
