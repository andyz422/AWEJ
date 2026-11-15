using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 3.5f, -6f);
    public float lookHeight = 1.2f;

    void LateUpdate()
    {
        // Follow this device's player; in co-op it may spawn after the camera.
        if (target == null)
        {
            target = PlayerRegistry.LocalTransform;
            if (target == null)
            {
                return;
            }
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
