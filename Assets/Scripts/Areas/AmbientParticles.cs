using UnityEngine;

// Cheap weather: a box of drifting motes that follows the player (snow, embers, fireflies).
public class AmbientParticles : MonoBehaviour
{
    public int count = 120;
    public Vector3 boxSize = new Vector3(36f, 12f, 36f);
    public Vector3 drift = new Vector3(0.3f, -1.5f, 0f);
    public float wobble = 0.5f;
    public float moteSize = 0.12f;
    public Color color = Color.white;

    private Transform[] motes;
    private float[] phases;
    private Transform follow;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        follow = player != null ? player.transform : transform;

        motes = new Transform[count];
        phases = new float[count];
        Material material = WorldKit.Colored(color);

        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-0.5f, 0.5f) * boxSize.x,
                Random.Range(0f, 1f) * boxSize.y,
                Random.Range(-0.5f, 0.5f) * boxSize.z);
            GameObject mote = WorldKit.Prim(PrimitiveType.Sphere, transform, Vector3.zero, Vector3.one * moteSize, material, false);
            mote.transform.position = follow.position + offset;
            mote.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            motes[i] = mote.transform;
            phases[i] = Random.value * 10f;
        }
    }

    void Update()
    {
        if (motes == null)
        {
            return;
        }

        Vector3 center = follow.position;
        Vector3 half = boxSize * 0.5f;

        for (int i = 0; i < motes.Length; i++)
        {
            float t = Time.time + phases[i];
            Vector3 wiggle = new Vector3(Mathf.Sin(t), Mathf.Cos(t * 0.7f), Mathf.Sin(t * 1.3f)) * wobble;
            Vector3 position = motes[i].position + (drift + wiggle) * Time.deltaTime;

            // Wrap around the box so motes never run out.
            Vector3 local = position - center;
            local.x = Wrap(local.x, -half.x, half.x);
            local.z = Wrap(local.z, -half.z, half.z);
            local.y = Wrap(local.y, -1f, boxSize.y);
            motes[i].position = center + local;
        }
    }

    static float Wrap(float value, float min, float max)
    {
        float range = max - min;
        if (value < min) return value + range;
        if (value > max) return value - range;
        return value;
    }
}
