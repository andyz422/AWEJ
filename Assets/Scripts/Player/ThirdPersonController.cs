using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    public float gravity = -20f;
    public Transform cameraTransform;

    private CharacterController controller;
    private InputSystem_Actions inputActions;
    private float verticalVelocity;
    private bool canMove = true;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.material.color = new Color(0.2f, 0.4f, 0.9f, 1f);
        }
    }

    void OnEnable()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        bool blocked = !canMove;

        if (GameManager.Instance != null)
        {
            blocked = blocked || GameManager.Instance.isInDialogue || GameManager.Instance.isTransitioning;
        }

        Vector2 moveInput = blocked ? Vector2.zero : inputActions.Player.Move.ReadValue<Vector2>();
        Vector3 horizontalMove = Vector3.zero;

        if (moveInput.magnitude > 0.1f)
        {
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;

            if (cameraTransform != null)
            {
                forward = cameraTransform.forward;
                forward.y = 0f;
                forward.Normalize();

                right = cameraTransform.right;
                right.y = 0f;
                right.Normalize();
            }

            Vector3 rawDirection = forward * moveInput.y + right * moveInput.x;

            if (rawDirection.sqrMagnitude > 0.0001f)
            {
                Vector3 direction = rawDirection.normalized;
                horizontalMove = direction * moveSpeed;

                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
            }
        }

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        Vector3 motion = horizontalMove;
        motion.y = verticalVelocity;

        controller.Move(motion * Time.deltaTime);
    }

    public void Freeze()
    {
        canMove = false;
    }

    public void Unfreeze()
    {
        canMove = true;
    }
}
