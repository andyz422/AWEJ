using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
// The Device versions report the simulated phone in the editor's Device Simulator, and the real device in builds.
using Application = UnityEngine.Device.Application;
using Screen = UnityEngine.Device.Screen;

// On-screen touch controls for phones: a move joystick bottom-left, and Fire / Interact / Swap buttons bottom-right.
// Gameplay scripts read the static state below alongside their keyboard and gamepad input.
public class MobileControls : MonoBehaviour
{
    public static MobileControls Instance { get; private set; }

    // Shows the controls in the editor too, for testing without the Device Simulator.
    public static bool forceShow = false;

    public static bool ShouldShow
    {
        get { return forceShow || Application.isMobilePlatform; }
    }

    public static Vector2 Move
    {
        get { return Instance != null && Instance.joystick != null ? Instance.joystick.Direction : Vector2.zero; }
    }

    public static bool FireHeld
    {
        get { return Instance != null && Instance.fireButton.Held; }
    }

    public static bool InteractPressed
    {
        get { return Instance != null && Instance.interactButton.ConsumePress(); }
    }

    public static bool SwapPressed
    {
        get { return Instance != null && Instance.swapButton.ConsumePress(); }
    }

    private RectTransform safeArea;
    private Rect appliedSafeArea;
    private VirtualJoystick joystick;
    private TouchButton fireButton;
    private TouchButton interactButton;
    private TouchButton swapButton;

    public static MobileControls Create()
    {
        GameObject obj = new GameObject("MobileControls");
        MobileControls controls = obj.AddComponent<MobileControls>();
        controls.Build();
        return controls;
    }

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Build()
    {
        EnsureEventSystem();

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        // Keeps the controls clear of the notch and home indicator.
        safeArea = new GameObject("SafeArea").AddComponent<RectTransform>();
        safeArea.SetParent(transform, false);
        ApplySafeArea();

        BuildJoystick();

        fireButton = MakeButton("Fire", new Vector2(-150f, 170f), 220f, new Color(0.9f, 0.3f, 0.25f, 0.55f), 40);
        interactButton = MakeButton("Use", new Vector2(-360f, 330f), 150f, new Color(0.3f, 0.7f, 0.95f, 0.55f), 32);
        swapButton = MakeButton("Swap", new Vector2(-150f, 390f), 120f, new Color(1f, 1f, 1f, 0.3f), 26);
    }

    static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
    }

    void BuildJoystick()
    {
        // The touch area is larger than the visible ring so a slightly-off thumb still catches it.
        RectTransform area = Rect(safeArea, "Joystick", new Vector2(0f, 0f), new Vector2(60f, 40f), new Vector2(380f, 380f));
        Image areaImage = area.gameObject.AddComponent<Image>();
        areaImage.color = Color.clear;

        RectTransform ring = Rect(area, "Base", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 260f));
        Image ringImage = ring.gameObject.AddComponent<Image>();
        ringImage.sprite = CircleSprite();
        ringImage.color = new Color(1f, 1f, 1f, 0.22f);
        ringImage.raycastTarget = false;

        RectTransform handle = Rect(ring, "Handle", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 110f));
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.sprite = CircleSprite();
        handleImage.color = new Color(1f, 1f, 1f, 0.6f);
        handleImage.raycastTarget = false;

        joystick = area.gameObject.AddComponent<VirtualJoystick>();
        joystick.SetParts(ring, handle, 110f);
    }

    TouchButton MakeButton(string label, Vector2 position, float size, Color color, int fontSize)
    {
        RectTransform rect = Rect(safeArea, label + "Button", new Vector2(1f, 0f), position, new Vector2(size, size));
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = CircleSprite();
        image.color = color;

        RectTransform textRect = Rect(rect, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
        Text text = textRect.gameObject.AddComponent<Text>();
        text.font = WorldKit.Font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = label;

        TouchButton button = rect.gameObject.AddComponent<TouchButton>();
        button.image = image;
        return button;
    }

    static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name).AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    void ApplySafeArea()
    {
        Rect area = Screen.safeArea;
        if (area == appliedSafeArea || Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }
        appliedSafeArea = area;

        safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
        safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
        safeArea.offsetMin = Vector2.zero;
        safeArea.offsetMax = Vector2.zero;
    }

    void Update()
    {
        ApplySafeArea();

        GameManager gm = GameManager.Instance;
        bool hasTarget = AreaHUD.Instance != null && AreaHUD.Instance.HasPrompt;
        SetVisible(interactButton, hasTarget);
        SetVisible(swapButton, gm != null && gm.ownedWeapons.Count > 1);
    }

    static void SetVisible(TouchButton button, bool visible)
    {
        if (button.gameObject.activeSelf != visible)
        {
            button.Release();
            button.gameObject.SetActive(visible);
        }
    }

    private static Sprite circle;

    static Sprite CircleSprite()
    {
        if (circle != null)
        {
            return circle;
        }

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radius, radius));
                float alpha = Mathf.Clamp01(radius - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();

        circle = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return circle;
    }
}

// A button that reports being held, and hands out each press once, for use from Update().
public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Image image;

    public bool Held { get; private set; }

    private int pointerId = int.MinValue;
    private int pressedFrame = -10;

    // True once per tap. A press is only good for a frame or so, whichever order
    // the EventSystem and the reading script update in, so stale taps don't fire later.
    public bool ConsumePress()
    {
        if (Time.frameCount - pressedFrame > 1)
        {
            return false;
        }
        pressedFrame = -10;
        return true;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Held)
        {
            return;
        }
        Held = true;
        pressedFrame = Time.frameCount;
        pointerId = eventData.pointerId;
        transform.localScale = Vector3.one * 0.92f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId == pointerId)
        {
            Release();
        }
    }

    public void Release()
    {
        Held = false;
        pointerId = int.MinValue;
        transform.localScale = Vector3.one;
    }

    void OnDisable()
    {
        Release();
    }
}
