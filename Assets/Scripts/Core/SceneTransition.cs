using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    public float transitionDuration = 1f;

    private Image fadeImage;
    private Canvas fadeCanvas;
    private bool isTransitioning = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateFadeCanvas();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void CreateFadeCanvas()
    {
        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform);

        fadeCanvas = canvasObj.AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform);

        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0);
        fadeImage.raycastTarget = false;

        RectTransform rect = fadeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public void TransitionToScene(string sceneName)
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionCoroutine(sceneName));
        }
    }

    IEnumerator TransitionCoroutine(string sceneName)
    {
        isTransitioning = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.isTransitioning = true;
        }

        yield return StartCoroutine(Fade(0f, 1f));

        SceneManager.LoadScene(sceneName);

        yield return null;

        yield return StartCoroutine(Fade(1f, 0f));

        isTransitioning = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.isTransitioning = false;
        }
    }

    IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / transitionDuration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, endAlpha);
    }
}
