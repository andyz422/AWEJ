using UnityEngine;

// Gentle up/down float, used for shop items and floating decorations.
public class Bobber : MonoBehaviour
{
    public float amplitude = 0.2f;
    public float speed = 2f;

    private Vector3 origin;
    private float phase;

    void Start()
    {
        origin = transform.localPosition;
        phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        transform.localPosition = origin + Vector3.up * Mathf.Sin(Time.time * speed + phase) * amplitude;
    }
}
