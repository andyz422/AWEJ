using UnityEngine;
using UnityEngine.UI;

public class UISetup : MonoBehaviour
{
    [Header("Canvas Settings")]
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920, 1080);
    [SerializeField] private CanvasScaler.ScaleMode scaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    [SerializeField] private float matchWidthOrHeight = 0.5f;

    [Header("Created References")]
    public Canvas mainCanvas;
    public VirtualJoystick joystick;
    public ActionButton button2;
    public ActionButton button3;
    public HealthBar playerHealthBar;
    public GameObject dialoguePanel;
    public Image dialogueImage;

    public static UISetup CreateGameUI()
    {
        GameObject uiRoot = new GameObject("GameUI");
        UISetup setup = uiRoot.AddComponent<UISetup>();
        setup.Initialize();
        return setup;
    }

    public void Initialize()
    {
        CreateCanvas();
        CreateJoystick();
        CreateActionButtons();
        CreateHealthBar();
        CreateDialoguePanel();
        CreateInputManager();
    }

    void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("MainCanvas");
        canvasObj.transform.SetParent(transform);

        mainCanvas = canvasObj.AddComponent<Canvas>();
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mainCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = scaleMode;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = matchWidthOrHeight;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    void CreateJoystick()
    {
        GameObject joystickContainer = new GameObject("Joystick");
        joystickContainer.transform.SetParent(mainCanvas.transform);

        RectTransform containerRect = joystickContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(0, 0);
        containerRect.pivot = new Vector2(0, 0);
        containerRect.anchoredPosition = new Vector2(100, 100);
        containerRect.sizeDelta = new Vector2(200, 200);

        GameObject baseObj = new GameObject("JoystickBase");
        baseObj.transform.SetParent(joystickContainer.transform);

        RectTransform baseRect = baseObj.AddComponent<RectTransform>();
        baseRect.anchorMin = new Vector2(0.5f, 0.5f);
        baseRect.anchorMax = new Vector2(0.5f, 0.5f);
        baseRect.pivot = new Vector2(0.5f, 0.5f);
        baseRect.anchoredPosition = Vector2.zero;
        baseRect.sizeDelta = new Vector2(150, 150);

        Image baseImage = baseObj.AddComponent<Image>();
        baseImage.color = new Color(1, 1, 1, 0.3f);

        GameObject handleObj = new GameObject("JoystickHandle");
        handleObj.transform.SetParent(baseObj.transform);

        RectTransform handleRect = handleObj.AddComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(60, 60);

        Image handleImage = handleObj.AddComponent<Image>();
        handleImage.color = new Color(1, 1, 1, 0.8f);

        joystick = joystickContainer.AddComponent<VirtualJoystick>();
    }

    void CreateActionButtons()
    {
        button2 = CreateActionButton("Button2", ActionButtonType.Button2,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-200, 100));

        button3 = CreateActionButton("Button3", ActionButtonType.Button3,
            new Vector2(1, 0), new Vector2(1, 0), new Vector2(-100, 100));
    }

    ActionButton CreateActionButton(string name, ActionButtonType type, Vector2 anchorMin, Vector2 anchorMax, Vector2 position)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(mainCanvas.transform);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(80, 80);

        Image image = buttonObj.AddComponent<Image>();
        image.color = Color.white;

        ActionButton button = buttonObj.AddComponent<ActionButton>();
        return button;
    }

    void CreateHealthBar()
    {
        GameObject healthBarObj = new GameObject("PlayerHealthBar");
        healthBarObj.transform.SetParent(mainCanvas.transform);

        RectTransform rect = healthBarObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(20, -20);
        rect.sizeDelta = new Vector2(200, 30);

        Image image = healthBarObj.AddComponent<Image>();
        image.color = Color.green;

        playerHealthBar = healthBarObj.AddComponent<HealthBar>();
    }

    void CreateDialoguePanel()
    {
        dialoguePanel = new GameObject("DialoguePanel");
        dialoguePanel.transform.SetParent(mainCanvas.transform);

        RectTransform rect = dialoguePanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0.1f);
        rect.anchorMax = new Vector2(0.9f, 0.4f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image bgImage = dialoguePanel.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.8f);

        GameObject imageObj = new GameObject("DialogueImage");
        imageObj.transform.SetParent(dialoguePanel.transform);

        RectTransform imageRect = imageObj.AddComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.pivot = new Vector2(0.5f, 0.5f);
        imageRect.offsetMin = new Vector2(10, 10);
        imageRect.offsetMax = new Vector2(-10, -10);

        dialogueImage = imageObj.AddComponent<Image>();
        dialogueImage.preserveAspect = true;

        dialoguePanel.SetActive(false);
    }

    void CreateInputManager()
    {
        GameObject inputObj = new GameObject("InputManager");
        inputObj.transform.SetParent(transform);
        InputManager inputManager = inputObj.AddComponent<InputManager>();
        inputManager.SetJoystick(joystick);
    }
}
