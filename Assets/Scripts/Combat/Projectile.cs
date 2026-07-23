using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 15f;
    public int damage = 1;
    public float lifetime = 5f;

    private Vector3 direction = Vector3.forward;
    private bool initialized = false;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (!initialized)
        {
            return;
        }

        transform.position += direction * speed * Time.deltaTime;
    }

    public void Initialize(Vector3 dir, float spd, int dmg)
    {
        direction = dir.normalized;
        speed = spd;
        damage = dmg;
        initialized = true;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            return;
        }

        Destroy(gameObject);
    }
}
