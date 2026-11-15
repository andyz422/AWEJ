using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuSceneSetup : MonoBehaviour
{
    private Font cachedFont;

    // Main menu widgets, hidden while the character select screen is open.
    private readonly System.Collections.Generic.List<GameObject> mainMenuItems = new System.Collections.Generic.List<GameObject>();
    private Transform canvasRoot;
    private GameObject characterSelect;
    private string selectedCharacter;
    private readonly System.Collections.Generic.Dictionary<string, Image> characterCards = new System.Collections.Generic.Dictionary<string, Image>();

    static readonly Color CardColor = new Color(0.16f, 0.18f, 0.28f, 1f);
    static readonly Color CardSelectedColor = new Color(0.3f, 0.5f, 0.95f, 1f);

    void Awake()
    {
        Debug.Log("MenuSceneSetup Awake called");
        SetupScene();
    }

    Font GetFont()
    {
        if (cachedFont != null) return cachedFont;

        // Try different font names that might work in Unity 6
        // Note: GetBuiltinResource throws ArgumentException (not null) for
        // names Unity no longer recognizes, e.g. "Arial.ttf" was replaced by
        // "LegacyRuntime.ttf", so each attempt must be caught individually.
        string[] fontNames = new string[] { "LegacyRuntime.ttf", "Arial.ttf", "Arial" };
        foreach (string fontName in fontNames)
        {
            try
            {
                cachedFont = Resources.GetBuiltinResource<Font>(fontName);
            }
            catch (System.ArgumentException)
            {
                cachedFont = null;
            }
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

        canvasRoot = canvasObj.transform;

        // Create title text
        mainMenuItems.Add(CreateText(canvasObj.transform, "AWEJ", new Vector2(0, 200), 72));

        // Create menu buttons
        float buttonY = 50;
        float buttonSpacing = 80;

        mainMenuItems.Add(CreateMenuButton(canvasObj.transform, "New Game", new Vector2(0, buttonY), OnNewGame));
        mainMenuItems.Add(CreateMenuButton(canvasObj.transform, "Load Game", new Vector2(0, buttonY - buttonSpacing), OnLoadGame));
        mainMenuItems.Add(CreateMenuButton(canvasObj.transform, "Controls", new Vector2(0, buttonY - buttonSpacing * 2), OnControls));
        mainMenuItems.Add(CreateMenuButton(canvasObj.transform, "Options", new Vector2(0, buttonY - buttonSpacing * 3), OnOptions));
        mainMenuItems.Add(CreateMenuButton(canvasObj.transform, "Quit", new Vector2(0, buttonY - buttonSpacing * 4), OnQuit));

        Debug.Log("Menu setup complete");
    }

    GameObject CreateText(Transform parent, string text, Vector2 position, int fontSize)
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
        return textObj;
    }

    GameObject CreateMenuButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
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
        return buttonObj;
    }

    void OnNewGame()
    {
        Debug.Log("New Game button clicked");
        ShowCharacterSelect();
    }

    // ---------------------------------------------------------------- character select

    void ShowCharacterSelect()
    {
        foreach (GameObject item in mainMenuItems)
        {
            item.SetActive(false);
        }
        if (characterSelect == null)
        {
            BuildCharacterSelect();
        }
        characterSelect.SetActive(true);
        SelectCharacter(PlayerCharacters.LastChosen);
    }

    void HideCharacterSelect()
    {
        if (characterSelect != null)
        {
            characterSelect.SetActive(false);
        }
        foreach (GameObject item in mainMenuItems)
        {
            item.SetActive(true);
        }
    }

    void BuildCharacterSelect()
    {
        characterSelect = new GameObject("CharacterSelect");
        RectTransform root = characterSelect.AddComponent<RectTransform>();
        root.SetParent(canvasRoot, false);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        CreateText(root, "Choose your hero", new Vector2(0, 390), 64).GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 100);

        const float cardWidth = 320f;
        const float spacing = 40f;
        int count = PlayerCharacters.All.Length;
        float startX = -(count - 1) * (cardWidth + spacing) * 0.5f;
        for (int i = 0; i < count; i++)
        {
            BuildCharacterCard(root, PlayerCharacters.All[i], new Vector2(startX + i * (cardWidth + spacing), 20f), cardWidth);
        }

        GameObject back = CreateMenuButton(root, "Back", new Vector2(-180, -380), HideCharacterSelect);
        GameObject start = CreateMenuButton(root, "Start", new Vector2(180, -380), OnStartWithCharacter);
        Button startButton = start.GetComponent<Button>();
        ColorBlock colors = startButton.colors;
        colors.normalColor = new Color(0.25f, 0.6f, 0.35f, 1f);
        colors.highlightedColor = new Color(0.32f, 0.72f, 0.42f, 1f);
        colors.pressedColor = new Color(0.2f, 0.5f, 0.3f, 1f);
        startButton.colors = colors;
        start.GetComponent<Image>().color = colors.normalColor;
        back.transform.SetAsLastSibling();
    }

    void BuildCharacterCard(Transform parent, PlayerCharacters.Character character, Vector2 position, float width)
    {
        GameObject card = new GameObject("Card_" + character.id);
        RectTransform rect = card.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(width, 460f);

        Image background = card.AddComponent<Image>();
        background.color = CardColor;
        characterCards[character.id] = background;

        Button button = card.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        string id = character.id;
        button.onClick.AddListener(() => SelectCharacter(id));

        GameObject portraitObj = new GameObject("Portrait");
        RectTransform portraitRect = portraitObj.AddComponent<RectTransform>();
        portraitRect.SetParent(rect, false);
        portraitRect.anchorMin = portraitRect.anchorMax = portraitRect.pivot = new Vector2(0.5f, 1f);
        portraitRect.anchoredPosition = new Vector2(0f, -16f);
        portraitRect.sizeDelta = new Vector2(width - 32f, width - 32f);
        Image portrait = portraitObj.AddComponent<Image>();
        portrait.preserveAspect = true;
        portrait.raycastTarget = false;
        Sprite sprite = PlayerCharacters.Portrait(character.id);
        if (sprite != null)
        {
            portrait.sprite = sprite;
        }
        else
        {
            portrait.color = new Color(0.2f, 0.4f, 0.9f, 1f);
        }

        Text name = CreateCardText(rect, character.name, new Vector2(0f, 92f), 44, FontStyle.Bold);
        name.color = Color.white;
        Text tagline = CreateCardText(rect, character.tagline, new Vector2(0f, 42f), 24, FontStyle.Normal);
        tagline.color = new Color(0.82f, 0.86f, 0.95f, 1f);
    }

    Text CreateCardText(RectTransform card, string value, Vector2 position, int fontSize, FontStyle style)
    {
        GameObject obj = new GameObject("Text");
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.SetParent(card, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(card.sizeDelta.x - 20f, 60f);
        Text text = obj.AddComponent<Text>();
        text.text = value;
        text.font = GetFont();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    void SelectCharacter(string id)
    {
        selectedCharacter = PlayerCharacters.Get(id).id;
        foreach (var pair in characterCards)
        {
            bool selected = pair.Key == selectedCharacter;
            pair.Value.color = selected ? CardSelectedColor : CardColor;
            pair.Value.transform.localScale = Vector3.one * (selected ? 1.06f : 1f);
        }
    }

    void OnStartWithCharacter()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is null!");
            return;
        }
        PlayerCharacters.LastChosen = selectedCharacter;
        GameManager.Instance.playerCharacter = selectedCharacter;
        Debug.Log("Starting new game as " + selectedCharacter);
        GameManager.Instance.NewGame();
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
