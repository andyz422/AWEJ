using UnityEngine;

// Idle motion for the 3D shopkeepers: a soft breathing squash and a turn toward the player.
// The model faces local -Z, toward the shop entrance.
public class ShopkeeperIdle : MonoBehaviour
{
    public float breathe = 0.025f;
    public float speed = 2.2f;
    public float maxTurn = 35f;
    public float turnSpeed = 3f;

    private Transform player;
    private Vector3 baseScale;
    private Quaternion baseRotation;
    private float phase;
    private float nextSearch;

    void Start()
    {
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
        phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        float s = Mathf.Sin(Time.time * speed + phase) * breathe;
        transform.localScale = new Vector3(baseScale.x * (1f - s * 0.5f), baseScale.y * (1f + s), baseScale.z * (1f - s * 0.5f));

        if (player == null && Time.time >= nextSearch)
        {
            nextSearch = Time.time + 1f;
            // Each device's shopkeeper turns to that device's own player.
            player = PlayerRegistry.LocalTransform;
        }

        Quaternion target = baseRotation;
        if (player != null)
        {
            Vector3 toPlayer = player.position - transform.position;
            if (transform.parent != null)
            {
                toPlayer = transform.parent.InverseTransformDirection(toPlayer);
            }
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f)
            {
                float yaw = Mathf.Atan2(-toPlayer.x, -toPlayer.z) * Mathf.Rad2Deg;
                target = baseRotation * Quaternion.Euler(0f, Mathf.Clamp(yaw, -maxTurn, maxTurn), 0f);
            }
        }
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * turnSpeed);
    }
}
