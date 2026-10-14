using UnityEngine;

// Flame flicker for braziers and torches: jitters a light and the flame's scale.
public class Flicker : MonoBehaviour
{
    public Light targetLight;
    public float baseIntensity = 2f;

    private Vector3 baseScale;
    private float seed;

    void Start()
    {
        baseScale = transform.localScale;
        seed = Random.value * 100f;
    }

    void Update()
    {
        float noise = Mathf.PerlinNoise(seed, Time.time * 6f);
        transform.localScale = baseScale * (0.85f + noise * 0.3f);

        if (targetLight != null)
        {
            targetLight.intensity = baseIntensity * (0.7f + noise * 0.6f);
        }
    }
}
