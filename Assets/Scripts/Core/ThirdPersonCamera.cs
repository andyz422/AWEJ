using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 3.5f, -6f);
    public float lookHeight = 1.2f;

    // How quickly the camera swings round to sit behind the character (higher = snappier).
    public float followSpeed = 6f;

    // Follow speed used while the player is pulling back (S, with or without A/D).
    public float reverseFollowSpeed = 1.5f;

    private float yaw;
    private Transform yawTarget;
    private ThirdPersonController targetController;

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

        float targetYaw = target.eulerAngles.y;

        // Snap to the character when we first see it (or it changes), so the camera doesn't swing in from elsewhere.
        if (yawTarget != target)
        {
            yawTarget = target;
            targetController = target.GetComponent<ThirdPersonController>();
            yaw = targetYaw;
        }
        else if (targetController != null && targetController.IsMovingBackward)
        {
            // Walking straight back: hold the angle.
        }
        else
        {
            // Swing the camera round until it sits behind the character's facing direction.
            // While the player is walking backward the angle is held still.
            // Backing up diagonally, the camera swings more slowly so the big turn isn't abrupt.
            bool reversing = targetController != null && targetController.IsReversing;
            float speed = reversing ? reverseFollowSpeed : followSpeed;
            float t = 1f - Mathf.Exp(-speed * Time.deltaTime);
            yaw = Mathf.LerpAngle(yaw, targetYaw, t);
        }

        Quaternion orbit = Quaternion.Euler(0f, yaw, 0f);
        transform.position = target.position + orbit * offset;

        Vector3 lookPoint = target.position + Vector3.up * lookHeight;
        Vector3 lookDirection = lookPoint - transform.position;

        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }
}
