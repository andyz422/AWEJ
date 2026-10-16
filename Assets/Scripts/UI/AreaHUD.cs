using UnityEngine;
using UnityEngine.UI;

// On-screen info while exploring: location, progress, health, coins, weapon,
// the interaction prompt, short toast messages and big battle banners.
public class AreaHUD : MonoBehaviour
{
    public static AreaHUD Instance { get; private set; }

    private Text titleText;
    private Text subtitleText;
    private Text healthText;
    private Image healthFill;
    private Text coinsText;
    private Text weaponText;
    private GameObject promptPanel;
    private Text promptText;
    private Text toastText;
    private Text bannerText;

    private float toastTimer;
    private float toastDuration = 1f;
    private float bannerTimer;
    private float bannerDuration = 1f;
    private int shownCoins = -1;

    public static AreaHUD Create(string title, string subtitle)
    {
        GameObject obj = new GameObject("AreaHUD");
        AreaHUD hud = obj.AddComponent<AreaHUD>();
        hud.Build();
        hud.SetTitle(title, subtitle);
        return hud;
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
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        Transform root = transform;

        // Location, top-left.
        RectTransform location = Panel(root, "Location", new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(520f, 110f));
        titleText = MakeText(location, "Title", new Vector2(20f, -12f), new Vector2(480f, 52f), 40, TextAnchor.UpperLeft, FontStyle.Bold);
        subtitleText = MakeText(location, "Subtitle", new Vector2(20f, -62f), new Vector2(480f, 36f), 24, TextAnchor.UpperLeft, FontStyle.Normal);
        subtitleText.color = new Color(0.85f, 0.85f, 0.8f);

        // Player status, top-right.
        RectTransform status = Panel(root, "Status", new Vector2(1f, 1f), new Vector2(-20f, -20f), new Vector2(400f, 150f));
        healthText = MakeText(status, "Health", new Vector2(20f, -10f), new Vector2(360f, 36f), 26, TextAnchor.UpperLeft, FontStyle.Bold);

        RectTransform barBack = Box(status, "HealthBar", new Vector2(20f, -48f), new Vector2(360f, 18f), new Color(0.15f, 0.05f, 0.05f, 1f));
        healthFill = Box(barBack, "Fill", Vector2.zero, Vector2.zero, new Color(0.9f, 0.2f, 0.25f, 1f)).GetComponent<Image>();
        RectTransform fillRect = healthFill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        coinsText = MakeText(status, "Coins", new Vector2(20f, -74f), new Vector2(360f, 34f), 26, TextAnchor.UpperLeft, FontStyle.Bold);
        coinsText.color = new Color(1f, 0.85f, 0.3f);
        weaponText = MakeText(status, "Weapon", new Vector2(20f, -110f), new Vector2(360f, 32f), 22, TextAnchor.UpperLeft, FontStyle.Normal);

        // Interaction prompt, bottom-center.
        RectTransform prompt = Panel(root, "Prompt", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(980f, 100f));
        prompt.pivot = new Vector2(0.5f, 0f);
        promptPanel = prompt.gameObject;
        promptText = MakeText(prompt, "Text", new Vector2(20f, -10f), new Vector2(940f, 80f), 30, TextAnchor.MiddleCenter, FontStyle.Normal);
        promptPanel.SetActive(false);

        toastText = MakeText(root, "Toast", Vector2.zero, new Vector2(1200f, 140f), 34, TextAnchor.MiddleCenter, FontStyle.Bold);
        Center(toastText.rectTransform, new Vector2(0.5f, 0.74f));
        toastText.gameObject.AddComponent<Outline>().effectDistance = new Vector2(2f, -2f);

        bannerText = MakeText(root, "Banner", Vector2.zero, new Vector2(1400f, 180f), 88, TextAnchor.MiddleCenter, FontStyle.Bold);
        Center(bannerText.rectTransform, new Vector2(0.5f, 0.56f));
        bannerText.color = new Color(1f, 0.92f, 0.6f);
        bannerText.gameObject.AddComponent<Outline>().effectDistance = new Vector2(3f, -3f);

        SetAlpha(toastText, 0f);
        SetAlpha(bannerText, 0f);

        if (MobileControls.ShouldShow)
        {
            MobileControls.Create();
        }
    }

    public bool HasPrompt
    {
        get { return promptPanel.activeSelf; }
    }

    static RectTransform Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform rect = Box(parent, name, position, size, new Color(0f, 0f, 0f, 0.55f));
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        return rect;
    }

    static RectTransform Box(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    static Text MakeText(Transform parent, string name, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment, FontStyle style)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = obj.AddComponent<Text>();
        text.font = WorldKit.Font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    static void Center(RectTransform rect, Vector2 anchor)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
    }

    static void SetAlpha(Text text, float alpha)
    {
        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }

    public void SetTitle(string title, string subtitle)
    {
        titleText.text = title;
        subtitleText.text = subtitle;
    }

    public void SetPrompt(string text)
    {
        // On phones the Use button replaces the E key.
        if (text != null && MobileControls.Instance != null)
        {
            text = text.Replace("[E] ", "");
        }

        bool show = !string.IsNullOrEmpty(text);
        if (promptPanel.activeSelf != show)
        {
            promptPanel.SetActive(show);
        }
        if (show && promptText.text != text)
        {
            promptText.text = text;
        }
    }

    public void Toast(string text, float duration = 3f)
    {
        toastText.text = text;
        toastDuration = duration;
        toastTimer = duration;
    }

    public void Banner(string text, float duration = 1.6f)
    {
        bannerText.text = text;
        bannerDuration = duration;
        bannerTimer = duration;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            healthText.text = "HP  " + gm.playerHealth + " / " + gm.maxPlayerHealth;
            float fraction = gm.maxPlayerHealth > 0 ? Mathf.Clamp01(gm.playerHealth / (float)gm.maxPlayerHealth) : 0f;
            healthFill.rectTransform.anchorMax = new Vector2(fraction, 1f);

            if (gm.playerCoins != shownCoins)
            {
                shownCoins = gm.playerCoins;
                coinsText.text = "Coins  " + gm.playerCoins;
            }

            string weapon = WeaponCatalog.GetOrDefault(gm.equippedWeapon).name;
            weaponText.text = gm.ownedWeapons.Count > 1 && MobileControls.Instance == null ? weapon + "   (Q to swap)" : weapon;
        }

        toastTimer -= Time.deltaTime;
        SetAlpha(toastText, FadeAlpha(toastTimer, toastDuration));

        bannerTimer -= Time.deltaTime;
        SetAlpha(bannerText, FadeAlpha(bannerTimer, bannerDuration));
        float pop = Mathf.Clamp01((bannerDuration - bannerTimer) / 0.15f);
        bannerText.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.3f, 1f, pop);
    }

    // Fully visible, then fades out over the last half second.
    static float FadeAlpha(float timer, float duration)
    {
        if (timer <= 0f)
        {
            return 0f;
        }
        return Mathf.Clamp01(timer / Mathf.Min(0.5f, duration));
    }
}
