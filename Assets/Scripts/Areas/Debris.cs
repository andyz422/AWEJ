using UnityEngine;

// Short-lived chunk that flies outward and shrinks; spawned when enemies pop.
public class Debris : MonoBehaviour
{
    public Vector3 velocity;
    public float lifetime = 0.6f;

    private float age;
    private Vector3 startScale;

    void Start()
    {
        startScale = transform.localScale;
    }

    void Update()
    {
        age += Time.deltaTime;
        velocity += Vector3.down * 18f * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        transform.localScale = startScale * Mathf.Clamp01(1f - age / lifetime);

        if (age >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    public static void Burst(Vector3 position, Color color, int count, float size)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject chunk = WorldKit.Prim(PrimitiveType.Cube, null, position, Vector3.one * size, color, false);
            Debris debris = chunk.AddComponent<Debris>();
            Vector3 direction = Random.onUnitSphere;
            direction.y = Mathf.Abs(direction.y) + 0.4f;
            debris.velocity = direction * Random.Range(4f, 8f);
        }
    }
}
