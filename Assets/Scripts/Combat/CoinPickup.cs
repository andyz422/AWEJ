using UnityEngine;

// Coin that pops out of a defeated enemy and then flies to the player.
public class CoinPickup : MonoBehaviour
{
    const float PopDuration = 0.45f;

    public int value = 1;

    private Transform player;
    private Vector3 velocity;
    private float age;

    public static void Spawn(Vector3 position, int value)
    {
        GameObject coin = WorldKit.Prim(PrimitiveType.Cylinder, null, position, new Vector3(0.5f, 0.04f, 0.5f), new Color(1f, 0.82f, 0.2f), false);
        coin.name = "Coin";
        coin.transform.rotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);

        CoinPickup pickup = coin.AddComponent<CoinPickup>();
        pickup.value = value;
        Vector2 spread = Random.insideUnitCircle * 4f;
        pickup.velocity = new Vector3(spread.x, Random.Range(5f, 8f), spread.y);
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void Update()
    {
        age += Time.deltaTime;
        transform.Rotate(Vector3.up * 360f * Time.deltaTime, Space.World);

        if (age < PopDuration || player == null)
        {
            velocity += Vector3.down * 20f * Time.deltaTime;
            Vector3 position = transform.position + velocity * Time.deltaTime;
            position.y = Mathf.Max(position.y, 0.4f);
            transform.position = position;
            return;
        }

        Vector3 target = player.position + Vector3.up * 0.3f;
        float speed = 8f + (age - PopDuration) * 30f;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.5f)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.playerCoins += value;
            }
            Destroy(gameObject);
        }
    }
}
