using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GameOverSceneSetup : MonoBehaviour
{
    void Start()
    {
        SetupScene();
    }

    void SetupScene()
    {
        // Create EventSystem
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        // Create UI
        GameObject canvasObj = new GameObject("GameOverCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Game Over text
        CreateText(canvasObj.transform, "GAME OVER", new Vector2(0, 100), 72, Color.red);

        // Buttons
        CreateButton(canvasObj.transform, "Restart", new Vector2(0, -50), OnRestart);
        CreateButton(canvasObj.transform, "Menu", new Vector2(0, -130), OnMenu);

        Debug.Log("Game Over - Press R to restart or Escape for menu");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            OnRestart();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            OnMenu();
        }
    }

    void CreateText(Transform parent, string content, Vector2 position, int fontSize, Color color)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(600, 100);

        Text text = textObj.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
    }

    void CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(label + "Button");
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(200, 50);

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.3f, 1f);

        Button button = buttonObj.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        // Text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
    }

    void OnRestart()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetHealth();
            GameManager.Instance.GoToPreviousScene();
        }
    }

    void OnMenu()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ReturnToMenu();
        }
    }
}
