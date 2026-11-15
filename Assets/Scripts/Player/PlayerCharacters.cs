using UnityEngine;

// The four playable pill characters (Blender/build_player_characters.py).
// Models live at Resources/Players/<id>, menu portraits at Resources/Players/Portraits/<id>.
public static class PlayerCharacters
{
    public struct Character
    {
        public string id;
        public string name;
        public string tagline;

        public Character(string id, string name, string tagline)
        {
            this.id = id;
            this.name = name;
            this.tagline = tagline;
        }
    }

    public static readonly Character[] All =
    {
        new Character("bean", "Bean", "Bright-eyed and bouncy"),
        new Character("mochi", "Mochi", "Curious little kitty"),
        new Character("sprout", "Sprout", "Growing up brave"),
        new Character("pip", "Pip", "A hero in a tiny cape"),
    };

    public const string DefaultId = "bean";
    const string PrefsKey = "AWEJ.PlayerCharacter";

    public static Character Get(string id)
    {
        foreach (Character c in All)
        {
            if (c.id == id)
            {
                return c;
            }
        }
        return All[0];
    }

    // The character picked last time on this device, so the menu remembers it.
    public static string LastChosen
    {
        get { return Get(PlayerPrefs.GetString(PrefsKey, DefaultId)).id; }
        set
        {
            PlayerPrefs.SetString(PrefsKey, value);
            PlayerPrefs.Save();
        }
    }

    public static Sprite Portrait(string id)
    {
        return Resources.Load<Sprite>("Players/Portraits/" + id);
    }

    // Swaps the player's capsule look for the chosen character model. The capsule renderer is hidden,
    // but the CharacterController (and so collision and movement) is untouched.
    public static GameObject ApplyTo(GameObject player)
    {
        return ApplyTo(player, GameManager.Instance != null ? GameManager.Instance.playerCharacter : DefaultId);
    }

    // Co-op: each player shows the character its owner picked.
    public static GameObject ApplyTo(GameObject player, string id)
    {
        GameObject prefab = Resources.Load<GameObject>("Players/" + Get(id).id);
        if (prefab == null)
        {
            return null;
        }

        Transform old = player.transform.Find("Model");
        if (old != null)
        {
            old.name = "OldModel";
            old.gameObject.SetActive(false);
            Object.Destroy(old.gameObject);
        }

        GameObject model = Object.Instantiate(prefab, player.transform, false);
        model.name = "Model";
        CharacterController controller = player.GetComponent<CharacterController>();
        float footOffset = controller != null ? controller.center.y - controller.height * 0.5f : -1f;
        model.transform.localPosition = new Vector3(0f, footOffset, 0f);
        model.transform.localRotation = Quaternion.identity;
        foreach (Collider c in model.GetComponentsInChildren<Collider>())
        {
            Object.Destroy(c);
        }

        MeshRenderer capsule = player.GetComponent<MeshRenderer>();
        if (capsule != null)
        {
            capsule.enabled = false;
        }

        model.AddComponent<PlayerModelBounce>();
        return model;
    }
}

// A little life for the pill: a soft breathing squash when standing still, and a bouncy hop while walking.
public class PlayerModelBounce : MonoBehaviour
{
    public float hopHeight = 0.12f;
    public float hopSpeed = 11f;
    public float lean = 8f;

    private Vector3 basePosition;
    private Vector3 lastWorldPosition;
    private float phase;
    private float moving;

    void Start()
    {
        basePosition = transform.localPosition;
        lastWorldPosition = transform.parent != null ? transform.parent.position : transform.position;
    }

    void LateUpdate()
    {
        Transform body = transform.parent != null ? transform.parent : transform;

        // Downed in co-op: tip over and lie still until a teammate helps.
        PlayerAvatar avatar = body.GetComponent<PlayerAvatar>();
        if (avatar != null && avatar.IsDowned)
        {
            transform.localPosition = basePosition + new Vector3(0f, 0.5f, 0f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(-82f, 0f, 0f), Time.deltaTime * 8f);
            transform.localScale = Vector3.one;
            lastWorldPosition = body.position;
            moving = 0f;
            return;
        }
        Vector3 delta = body.position - lastWorldPosition;
        lastWorldPosition = body.position;
        delta.y = 0f;
        float speed = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        moving = Mathf.MoveTowards(moving, speed > 0.5f ? 1f : 0f, Time.deltaTime * 6f);

        phase += Time.deltaTime * Mathf.Lerp(2.4f, hopSpeed, moving);
        float hop = Mathf.Abs(Mathf.Sin(phase)) * hopHeight * moving;
        float squash = Mathf.Sin(phase * (moving > 0.5f ? 2f : 1f)) * Mathf.Lerp(0.025f, 0.05f, moving);

        transform.localPosition = basePosition + Vector3.up * hop;
        transform.localScale = new Vector3(1f - squash * 0.5f, 1f + squash, 1f - squash * 0.5f);
        transform.localRotation = Quaternion.Euler(lean * moving, 0f, 0f);
    }
}
