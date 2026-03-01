using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuSceneSetup : MonoBehaviour
{
    private Font cachedFont;

    void Awake()
    {
        Debug.Log("MenuSceneSetup Awake called");
        SetupScene();
    }

    Font GetFont()
    {
        if (cachedFont != null) return cachedFont;

        // Try different font names that might work in Unity 6
        string[] fontNames = new string[] { "Arial.ttf", "LegacyRuntime.ttf", "Arial" };
        foreach (string fontName in fontNames)
        {
            cachedFont = Resources.GetBuiltinResource<Font>(fontName);
            if (cachedFont != null)
            {
                Debug.Log($"Found font: {fontName}");
                return cachedFont;
            }
        }

        // Last resort - find any font
        cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
        if (cachedFont == null)
        {
            string[] osFonts = Font.GetOSInstalledFontNames();
            if (osFonts.Length > 0)
            {
                cachedFont = Font.CreateDynamicFontFromOSFont(osFonts[0], 14);
                Debug.Log($"Using OS font: {osFonts[0]}");
            }
        }

        return cachedFont;
    }

    void SetupScene()
    {
        Debug.Log("MenuSceneSetup SetupScene called");

        // Ensure GameManager exists
        if (GameManager.Instance == null)
        {
            GameObject gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
            Debug.Log("Created GameManager");
        }

        // Create EventSystem if needed
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
            Debug.Log("Created EventSystem");
        }

        // Create Canvas
        GameObject canvasObj = new GameObject("MenuCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        Debug.Log("Created MenuCanvas");

        // Create title text
        CreateText(canvasObj.transform, "AWEJ", new Vector2(0, 200), 72);

        // Create menu buttons
        float buttonY = 50;
        float buttonSpacing = 80;

        CreateMenuButton(canvasObj.transform, "New Game", new Vector2(0, buttonY), OnNewGame);
        CreateMenuButton(canvasObj.transform, "Load Game", new Vector2(0, buttonY - buttonSpacing), OnLoadGame);
        CreateMenuButton(canvasObj.transform, "Controls", new Vector2(0, buttonY - buttonSpacing * 2), OnControls);
        CreateMenuButton(canvasObj.transform, "Options", new Vector2(0, buttonY - buttonSpacing * 3), OnOptions);
        CreateMenuButton(canvasObj.transform, "Quit", new Vector2(0, buttonY - buttonSpacing * 4), OnQuit);

        Debug.Log("Menu setup complete");
    }

    void CreateText(Transform parent, string text, Vector2 position, int fontSize)
    {
        GameObject textObj = new GameObject("Title");
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(400, 100);

        Text textComp = textObj.AddComponent<Text>();
        textComp.text = text;
        textComp.font = GetFont();
        textComp.fontSize = fontSize;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.color = Color.white;
    }

    void CreateMenuButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject(label + "Button");
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(300, 60);

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.3f, 1f);

        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.3f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.5f, 1f);
        colors.pressedColor = new Color(0.15f, 0.15f, 0.25f, 1f);
        button.colors = colors;
        button.onClick.AddListener(onClick);

        // Create text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = GetFont();
        text.fontSize = 32;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
    }

    void OnNewGame()
    {
        Debug.Log("New Game button clicked");
        if (GameManager.Instance != null)
        {
            Debug.Log("Starting new game...");
            GameManager.Instance.NewGame();
        }
        else
        {
            Debug.LogError("GameManager.Instance is null!");
        }
    }

    void OnLoadGame()
    {
        Debug.Log("Load Game - Not implemented");
    }

    void OnControls()
    {
        Debug.Log("Controls - Not implemented");
    }

    void OnOptions()
    {
        Debug.Log("Options - Not implemented");
    }

    void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
