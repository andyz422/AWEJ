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

    // Co-op menu state
    private bool coopMode;
    private GameObject coopPanel;
    private GameObject lobbyPanel;
    private InputField addressField;
    private InputField codeField;
    private Text lobbyText;
    private GameObject startButton;
    private Text statusText;
    private static string pendingNotice = "";

    void OnEnable()
    {
        NetSession.Ended += OnSessionEnded;
    }

    void OnDisable()
    {
        NetSession.Ended -= OnSessionEnded;
    }

    static void OnSessionEnded(string reason)
    {
        pendingNotice = reason ?? "";
    }

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

        HideLegacyMenu();

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
        mainMenuItems.Add(CreateMenuButton(canvasObj.transform, "Co-op", new Vector2(0, buttonY - buttonSpacing), OnCoop));
        mainMenuItems.Add(CreateMenuButton(canvasObj.transform, "Load Game", new Vector2(0, buttonY - buttonSpacing * 2), OnLoadGame));
        mainMenuItems.Add(CreateMenuButton(canvasObj.transform, "Controls", new Vector2(0, buttonY - buttonSpacing * 3), OnControls));
        mainMenuItems.Add(CreateMenuButton(canvasObj.transform, "Options", new Vector2(0, buttonY - buttonSpacing * 4), OnOptions));
        mainMenuItems.Add(CreateMenuButton(canvasObj.transform, "Quit", new Vector2(0, buttonY - buttonSpacing * 5), OnQuit));

        // Why the last co-op session ended (host left, connection lost, party full...).
        statusText = CreateText(canvasObj.transform, pendingNotice, new Vector2(0, -470), 28).GetComponent<Text>();
        statusText.GetComponent<RectTransform>().sizeDelta = new Vector2(1400, 60);
        statusText.color = new Color(1f, 0.85f, 0.5f);
        pendingNotice = "";

        Debug.Log("Menu setup complete");
    }

    // MenuScene still contains an older menu saved in the scene (its own canvas with New Game / Load /
    // Options / Quit, driven by MenuControllerUI). This code-built menu replaces it; switch the old one
    // off so it can't show through, or be clicked, behind the character select and co-op screens.
    static void HideLegacyMenu()
    {
        foreach (MenuControllerUI legacy in FindObjectsByType<MenuControllerUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject[] buttons = { legacy.newGameButton, legacy.loadGameButton, legacy.optionsButton, legacy.quitButton };
            foreach (GameObject button in buttons)
            {
                Canvas canvas = button != null ? button.GetComponentInParent<Canvas>(true) : null;
                if (canvas != null)
                {
                    canvas.rootCanvas.gameObject.SetActive(false);
                }
            }
            legacy.gameObject.SetActive(false);
        }
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
        coopMode = false;
        ShowCharacterSelect();
    }

    void OnCoop()
    {
        coopMode = true;
        ShowCharacterSelect();
    }

    // ---------------------------------------------------------------- co-op host / join / lobby

    void ShowCoopPanel()
    {
        if (characterSelect != null)
        {
            characterSelect.SetActive(false);
        }
        if (coopPanel == null)
        {
            BuildCoopPanel();
        }
        coopPanel.SetActive(true);
    }

    void BuildCoopPanel()
    {
        coopPanel = FullPanel("CoopPanel");
        Transform root = coopPanel.transform;
        CreateText(root, "Co-op  (up to 4 players)", new Vector2(0, 330), 60).GetComponent<RectTransform>().sizeDelta = new Vector2(1200, 100);
        // Same Wi-Fi: join with the host's IP address.
        CreateText(root, "Same Wi-Fi: one player hosts, the others join with the host's IP address.",
            new Vector2(0, 245), 28).GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 60);
        CreateMenuButton(root, "Host on Wi-Fi", new Vector2(0, 165), OnHostLan).GetComponent<RectTransform>().sizeDelta = new Vector2(420, 70);
        addressField = CreateInputField(root, new Vector2(-120, 75), "Host's IP address, e.g. 192.168.1.20");
        CreateMenuButton(root, "Join", new Vector2(260, 75), OnJoinLan);

        // Anywhere: online with a room code (Unity Relay).
        CreateText(root, "Anywhere: host online and share the room code.",
            new Vector2(0, -35), 28).GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 60);
        CreateMenuButton(root, "Host Online", new Vector2(0, -115), OnHostOnline).GetComponent<RectTransform>().sizeDelta = new Vector2(420, 70);
        codeField = CreateInputField(root, new Vector2(-120, -205), "Room code");
        CreateMenuButton(root, "Join Online", new Vector2(260, -205), OnJoinOnline);

        CreateMenuButton(root, "Back", new Vector2(0, -380), () =>
        {
            coopPanel.SetActive(false);
            ShowCharacterSelect();
        });
    }

    async void OnHostOnline()
    {
        GameManager.Instance.ResetRun();
        NetSession.Ensure().PrepareOnline();
        ShowNotice("Creating an online game...");
        try
        {
            string code = await OnlineSession.HostAsync();
            ShowNotice("");
            ShowLobby(true);
            Debug.Log("[Menu] Hosting online, room code " + code);
        }
        catch (System.Exception e)
        {
            ShowNotice(e.Message);
        }
    }

    async void OnJoinOnline()
    {
        GameManager.Instance.ResetRun();
        NetSession.Ensure().PrepareOnline();
        ShowNotice("Joining...");
        try
        {
            await OnlineSession.JoinAsync(codeField != null ? codeField.text : "");
            ShowNotice("");
            ShowLobby(false);
        }
        catch (System.Exception e)
        {
            ShowNotice(e.Message);
        }
    }

    void OnHostLan()
    {
        GameManager.Instance.ResetRun();
        if (!NetSession.Ensure().HostLan())
        {
            ShowNotice("Couldn't start hosting. Is another copy of the game already hosting?");
            return;
        }
        ShowLobby(true);
    }

    void OnJoinLan()
    {
        GameManager.Instance.ResetRun();
        string address = addressField != null ? addressField.text : "";
        if (!NetSession.Ensure().JoinLan(address))
        {
            ShowNotice("Couldn't connect. Check the address and try again.");
            return;
        }
        ShowLobby(false);
    }

    void ShowLobby(bool host)
    {
        coopPanel.SetActive(false);
        if (lobbyPanel == null)
        {
            lobbyPanel = FullPanel("Lobby");
            Transform root = lobbyPanel.transform;
            CreateText(root, "Lobby", new Vector2(0, 330), 60);
            lobbyText = CreateText(root, "", new Vector2(0, 80), 34).GetComponent<Text>();
            lobbyText.GetComponent<RectTransform>().sizeDelta = new Vector2(1500, 360);
            startButton = CreateMenuButton(root, "Start Game", new Vector2(0, -230), () => NetSession.Instance.StartGame());
            CreateMenuButton(root, "Leave", new Vector2(0, -330), () =>
            {
                NetSession.Instance.Leave();
            });
        }
        lobbyPanel.SetActive(true);
        startButton.SetActive(host);
    }

    void Update()
    {
        if (lobbyPanel == null || !lobbyPanel.activeSelf || lobbyText == null)
        {
            return;
        }
        Unity.Netcode.NetworkManager nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm == null || !nm.IsListening)
        {
            lobbyText.text = "Not connected.";
            return;
        }
        if (nm.IsServer)
        {
            string howToJoin = OnlineSession.IsOnline
                ? "Friends choose Join Online with this room code:\n" + OnlineSession.Code
                : "Friends join with this address:\n" + LocalAddresses();
            lobbyText.text = "You're hosting.\nPlayers: " + nm.ConnectedClientsIds.Count + " / " + NetSession.MaxPlayers
                + "\n\n" + howToJoin
                + "\n\nPress Start Game when everyone is in.";
        }
        else
        {
            lobbyText.text = nm.IsConnectedClient
                ? "Connected!\nWaiting for the host to start the game..."
                : "Connecting...";
        }
    }

    static string LocalAddresses()
    {
        try
        {
            System.Collections.Generic.List<string> found = new System.Collections.Generic.List<string>();
            foreach (System.Net.IPAddress ip in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(ip))
                {
                    found.Add(ip.ToString());
                }
            }
            return found.Count > 0 ? string.Join("   or   ", found) : "(check your device's Wi-Fi settings)";
        }
        catch (System.Exception)
        {
            return "(check your device's Wi-Fi settings)";
        }
    }

    void ShowNotice(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log("[Menu] " + message);
    }

    GameObject FullPanel(string name)
    {
        GameObject panel = new GameObject(name);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.SetParent(canvasRoot, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
    }

    InputField CreateInputField(Transform parent, Vector2 position, string placeholder)
    {
        GameObject obj = new GameObject("AddressField");
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(560, 64);
        Image background = obj.AddComponent<Image>();
        background.color = new Color(0.95f, 0.95f, 0.98f, 1f);

        Text text = CreateFieldText(rect, "Text", "", new Color(0.1f, 0.1f, 0.15f));
        Text hint = CreateFieldText(rect, "Placeholder", placeholder, new Color(0.45f, 0.45f, 0.55f));
        hint.fontStyle = FontStyle.Italic;

        InputField field = obj.AddComponent<InputField>();
        field.textComponent = text;
        field.placeholder = hint;
        field.contentType = InputField.ContentType.Standard;
        field.characterLimit = 64;
        return field;
    }

    Text CreateFieldText(RectTransform parent, string name, string value, Color color)
    {
        GameObject obj = new GameObject(name);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(16, 6);
        rect.offsetMax = new Vector2(-16, -6);
        Text text = obj.AddComponent<Text>();
        text.text = value;
        text.font = GetFont();
        text.fontSize = 28;
        text.color = color;
        text.alignment = TextAnchor.MiddleLeft;
        text.supportRichText = false;
        return text;
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
        if (coopMode)
        {
            ShowCoopPanel();
            return;
        }
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
