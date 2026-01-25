using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance;

    [Header("로딩 UI")]
    [SerializeField] CanvasGroup _canvasGroup;     // 로딩 캔버스 그룹
    [SerializeField] Slider _progressBar;          // 로딩 바

    [Header("로딩 설정")]
    [SerializeField] float _fadeDuration = 0.5f;   // 페이드 시간

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 초기화
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 씬 로딩 요청
    public void RequestLoadScene(string sceneName)
    {
        // 네트워크 메시지 큐 일시정지
        // LoadLevel이 아닌 수동으로 씬을 불러올 경우 자동으로 해주지 않음
        PhotonNetwork.IsMessageQueueRunning = false;

        // 활성화
        gameObject.SetActive(true);

        // 씬 로드 코루틴 실행
        StartCoroutine(LoadScene(sceneName));
    }

    private IEnumerator LoadScene(string sceneName)
    {
        // 초기화
        _progressBar.value = 0;
        _canvasGroup.alpha = 0f;

        // 페이드 아웃
        yield return StartCoroutine(Fade(0, 1));

        // 비동기 로딩 시작
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        // 90% 에서 멈추게 함
        op.allowSceneActivation = false;

        float timer = 0.0f;

        // 로딩 완료 될 때까지
        while (op.isDone == false)
        {
            yield return null;
            timer += Time.deltaTime;

            // 90%까지 실제 로딩
            if (op.progress < 0.9f)
            {
                _progressBar.value = Mathf.Lerp(_progressBar.value, op.progress, timer);
                if (_progressBar.value >= op.progress) timer = 0f;
            }
            // 실제로 로딩 다 된 90%부터는 그냥 보이기용 로딩
            else
            {
                // 꽉채우기
                _progressBar.value = Mathf.Lerp(_progressBar.value, 1f, timer);
                // 씬 전환 허용
                if (_progressBar.value >= 0.99f)
                    op.allowSceneActivation = true;
            }
        }

        // 준비 완료
        _progressBar.gameObject.SetActive(false);
    }



    #region 페이드
    // GameManager에서 호출하는 페이드 인, 아웃
    public void RequestFadeIn()
    {
        StartCoroutine(FadeIn());
    }
    public void RequestFadeOut()
    {
        gameObject.SetActive(true);
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeIn()
    {
        // 페이드 인
        yield return StartCoroutine(Fade(1, 0));

        // 다 끝나면 비활성화
        gameObject.SetActive(false);
        // 다음 로딩 할 때 바로 보이게 켜기
        _progressBar.gameObject.SetActive(true);
    }
    private IEnumerator FadeOut()
    {
        // 페이드 아웃
        yield return StartCoroutine(Fade(0, 1));
    }

    // 페이드 효과
    private IEnumerator Fade(float start, float end)
    {
        Debug.Log($"페이드 {start} to  {end}");
        float timer = 0f;
        _canvasGroup.alpha = start;

        // 페이드 중 클릭 차단
        _canvasGroup.blocksRaycasts = true;

        // 설정한 시간동안 페이드
        while (timer < _fadeDuration)
        {
            timer += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(start, end, timer / _fadeDuration);
            yield return null;
        }

        // 확실하게 알파 적용
        _canvasGroup.alpha = end;

        // 밝아지면 클릭 허용
        _canvasGroup.blocksRaycasts = (end > 0.5f);
    }
    #endregion


}
