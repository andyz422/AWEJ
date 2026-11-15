using System.Collections.Generic;
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

        // Lets the co-op vote buttons receive taps/clicks.
        gameObject.AddComponent<GraphicRaycaster>();

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

    // ---------------------------------------------------------------- co-op door vote

    private GameObject votePanel;
    private Text voteTitle;
    private Text voteStatus;
    private int shownVoteId = -1;

    void BuildVotePanel()
    {
        RectTransform panel = Panel(transform, "Vote", new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(900f, 230f));
        panel.pivot = new Vector2(0.5f, 1f);
        votePanel = panel.gameObject;

        voteTitle = MakeText(panel, "Title", new Vector2(20f, -14f), new Vector2(860f, 70f), 34, TextAnchor.MiddleCenter, FontStyle.Bold);
        voteStatus = MakeText(panel, "Status", new Vector2(20f, -84f), new Vector2(860f, 40f), 26, TextAnchor.MiddleCenter, FontStyle.Normal);

        VoteButton(panel, "Yes (Y)", new Vector2(250f, -150f), new Color(0.25f, 0.6f, 0.35f, 0.95f), true);
        VoteButton(panel, "No (N)", new Vector2(650f, -150f), new Color(0.7f, 0.25f, 0.22f, 0.95f), false);
        votePanel.SetActive(false);
    }

    void VoteButton(RectTransform panel, string label, Vector2 center, Color color, bool yes)
    {
        RectTransform rect = Box(panel, label, new Vector2(center.x - 160f, center.y), new Vector2(320f, 64f), color);
        Image image = rect.GetComponent<Image>();
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() =>
        {
            if (PartyState.Instance != null)
            {
                PartyState.Instance.SendVote(yes);
            }
        });
        Text text = MakeText(rect, "Label", Vector2.zero, new Vector2(320f, 64f), 30, TextAnchor.MiddleCenter, FontStyle.Bold);
        text.raycastTarget = false;
    }

    public void ShowVote(int id, string title, int yes, int needed, float secondsLeft)
    {
        if (votePanel == null)
        {
            BuildVotePanel();
        }
        if (!votePanel.activeSelf || id != shownVoteId)
        {
            shownVoteId = id;
            votePanel.SetActive(true);
        }
        voteTitle.text = title;
        voteStatus.text = "Yes " + yes + " / " + needed + "   -   " + Mathf.CeilToInt(secondsLeft) + "s";
    }

    public void HideVote()
    {
        if (votePanel != null && votePanel.activeSelf)
        {
            votePanel.SetActive(false);
        }
    }

    // ---------------------------------------------------------------- co-op party health

    const int MaxTeammates = NetSession.MaxPlayers - 1;

    struct PartyRow
    {
        public GameObject root;
        public Image portrait;
        public Text nameText;
        public Text hpText;
        public Image fill;
        public string shownCharacter;
    }

    private RectTransform partyPanel;
    private PartyRow[] partyRows;
    private readonly List<NetPlayer> teammates = new List<NetPlayer>(4);

    public int ShownTeammateCount { get; private set; }

    public string FirstTeammateStatus
    {
        get
        {
            return ShownTeammateCount > 0 && partyRows != null
                ? partyRows[0].nameText.text + " " + partyRows[0].hpText.text
                : "";
        }
    }

    void BuildPartyPanel()
    {
        partyPanel = Panel(transform, "Party", new Vector2(1f, 1f), new Vector2(-20f, -184f), new Vector2(400f, 74f));
        partyRows = new PartyRow[MaxTeammates];
        for (int i = 0; i < MaxTeammates; i++)
        {
            GameObject rowObj = new GameObject("Teammate" + (i + 1));
            rowObj.transform.SetParent(partyPanel, false);
            RectTransform rowRect = rowObj.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(0f, 1f);
            rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, -6f - i * 62f);
            rowRect.sizeDelta = new Vector2(400f, 58f);

            RectTransform portraitRect = Box(rowRect, "Portrait", new Vector2(16f, -5f), new Vector2(48f, 48f), Color.white);
            Image portrait = portraitRect.GetComponent<Image>();
            portrait.preserveAspect = true;

            Text nameText = MakeText(rowRect, "Name", new Vector2(76f, -4f), new Vector2(200f, 28f), 22, TextAnchor.UpperLeft, FontStyle.Bold);
            Text hpText = MakeText(rowRect, "HP", new Vector2(76f, -4f), new Vector2(304f, 28f), 20, TextAnchor.UpperRight, FontStyle.Bold);

            RectTransform barBack = Box(rowRect, "HealthBar", new Vector2(76f, -36f), new Vector2(304f, 14f), new Color(0.15f, 0.05f, 0.05f, 1f));
            Image fill = Box(barBack, "Fill", Vector2.zero, Vector2.zero, new Color(0.9f, 0.2f, 0.25f, 1f)).GetComponent<Image>();
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            partyRows[i] = new PartyRow
            {
                root = rowObj,
                portrait = portrait,
                nameText = nameText,
                hpText = hpText,
                fill = fill,
                shownCharacter = "",
            };
        }
        partyPanel.gameObject.SetActive(false);
    }

    void UpdatePartyPanel()
    {
        teammates.Clear();
        if (Net.Active)
        {
            foreach (PlayerAvatar p in PlayerRegistry.All)
            {
                if (p != null && !p.IsLocal && p.Net != null && p.Net.IsSpawned)
                {
                    teammates.Add(p.Net);
                }
            }
            teammates.Sort((a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));
        }

        int count = Mathf.Min(teammates.Count, MaxTeammates);
        ShownTeammateCount = count;
        if (count == 0)
        {
            if (partyPanel != null && partyPanel.gameObject.activeSelf)
            {
                partyPanel.gameObject.SetActive(false);
            }
            return;
        }

        if (partyPanel == null)
        {
            BuildPartyPanel();
        }
        if (!partyPanel.gameObject.activeSelf)
        {
            partyPanel.gameObject.SetActive(true);
        }
        partyPanel.sizeDelta = new Vector2(400f, 12f + count * 62f);

        for (int i = 0; i < MaxTeammates; i++)
        {
            PartyRow row = partyRows[i];
            bool active = i < count;
            if (row.root.activeSelf != active)
            {
                row.root.SetActive(active);
            }
            if (!active)
            {
                continue;
            }

            NetPlayer net = teammates[i];
            string charId = net.CharacterId;
            if (row.shownCharacter != charId)
            {
                row.shownCharacter = charId;
                Sprite sprite = PlayerCharacters.Portrait(charId);
                row.portrait.sprite = sprite;
                row.portrait.color = sprite != null ? Color.white : new Color(0.25f, 0.45f, 0.85f, 1f);
                partyRows[i] = row;
            }

            row.nameText.text = net.DisplayName;
            bool downed = net.Downed.Value;
            int hp = Mathf.Max(0, net.Health.Value);
            int maxHp = Mathf.Max(1, net.MaxHealth.Value);
            if (downed)
            {
                row.hpText.text = "DOWN";
                row.hpText.color = new Color(1f, 0.45f, 0.3f, 1f);
                row.fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            }
            else
            {
                row.hpText.text = hp + " / " + maxHp;
                row.hpText.color = new Color(0.9f, 0.9f, 0.9f, 1f);
                row.fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(hp / (float)maxHp), 1f);
            }
        }
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

        UpdatePartyPanel();

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
