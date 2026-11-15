using UnityEngine;
using UnityEngine.UI;

// A ring floating over a downed co-op player. Dim and pulsing while they wait for help; fills up
// green as a teammate holds Use to revive them. Everyone sees the same progress (NetPlayer.ReviveProgress).
public class ReviveRing : MonoBehaviour
{
    const float Height = 2.6f;

    private NetPlayer player;
    private GameObject visual;
    private Image track;
    private Image fill;
    private Text label;

    private static Sprite ringSprite;

    public static ReviveRing Create(Transform parent, NetPlayer player)
    {
        GameObject root = new GameObject("ReviveRing");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(0f, Height, 0f);
        ReviveRing ring = root.AddComponent<ReviveRing>();
        ring.player = player;
        ring.Build();
        return ring;
    }

    void Build()
    {
        visual = new GameObject("Visual");
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = Vector3.one * 0.01f;
        Canvas canvas = visual.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;
        visual.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 120f);
        visual.AddComponent<Billboard>();

        track = MakeImage("Track", new Color(0f, 0f, 0f, 0.55f), Image.Type.Simple);
        fill = MakeImage("Fill", new Color(0.35f, 1f, 0.45f, 1f), Image.Type.Filled);
        fill.fillMethod = Image.FillMethod.Radial360;
        fill.fillOrigin = (int)Image.Origin360.Top;
        fill.fillClockwise = true;
        fill.fillAmount = 0f;

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(visual.transform, false);
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120f, 120f);
        label = textObj.AddComponent<Text>();
        label.font = WorldKit.Font;
        label.fontSize = 56;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = "+";

        visual.SetActive(false);
    }

    Image MakeImage(string name, Color color, Image.Type type)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(visual.transform, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120f, 120f);
        Image image = obj.AddComponent<Image>();
        image.sprite = RingSprite();
        image.color = color;
        image.type = type;
        image.raycastTarget = false;
        return image;
    }

    void LateUpdate()
    {
        bool downed = player != null && player.IsSpawned && player.Downed.Value;
        if (visual.activeSelf != downed)
        {
            visual.SetActive(downed);
        }
        if (!downed)
        {
            return;
        }

        float progress = player.ReviveProgress.Value;
        fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, progress, Time.deltaTime * 3f);
        if (progress <= 0f)
        {
            // Waiting for help: gently pulse so teammates notice.
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 4f);
            track.color = new Color(1f, 0.35f, 0.3f, Mathf.Lerp(0.35f, 0.85f, pulse));
            label.text = "+";
        }
        else
        {
            track.color = new Color(0f, 0f, 0f, 0.55f);
            label.text = Mathf.FloorToInt(progress * 100f) + "%";
        }
        label.fontSize = progress > 0f ? 34 : 56;
    }

    // A thick ring, drawn once in code.
    static Sprite RingSprite()
    {
        if (ringSprite != null)
        {
            return ringSprite;
        }
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float outer = size * 0.5f;
        float inner = outer * 0.68f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(outer, outer));
                float alpha = Mathf.Clamp01(outer - d) * Mathf.Clamp01(d - inner);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        ringSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return ringSprite;
    }
}
