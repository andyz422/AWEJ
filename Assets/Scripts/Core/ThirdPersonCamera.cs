using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 3.5f, -6f);
    public float lookHeight = 1.2f;

    void LateUpdate()
    {
        // Always follow this device's player. (The scene's saved target points at the player prefab
        // asset, which never moves, so it must not win; in co-op the player may also spawn later.)
        Transform local = PlayerRegistry.LocalTransform;
        if (local != null)
        {
            target = local;
        }
        if (target == null || !target.gameObject.scene.IsValid())
        {
            return;
        }

        transform.position = target.position + offset;

        Vector3 lookPoint = target.position + Vector3.up * lookHeight;
        Vector3 lookDirection = lookPoint - transform.position;

        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }
}
