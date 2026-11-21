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
    private float baseMoveSpeed;

    // True while walking straight back (S alone, no sideways input). The character and camera hold their
    // facing then. Backing up diagonally (S + A/D) still turns, so the camera keeps swinging.
    public bool IsMovingBackward { get; private set; }

    // True whenever the stick is pulled back, straight or diagonal.
    public bool IsReversing { get; private set; }

    // Turn rate used while backing up diagonally. The camera is locked to the character's facing then,
    // so this also sets how fast the camera swings. Lower = slower swing.
    public float reverseTurnSpeed = 0.5f;
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        baseMoveSpeed = moveSpeed;

        // Every player registers through a PlayerAvatar (see PlayerRegistry).
        if (GetComponent<PlayerAvatar>() == null)
        {
            gameObject.AddComponent<PlayerAvatar>();
        }

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.material.color = new Color(0.2f, 0.4f, 0.9f, 1f);
        }

        // The chosen pill character replaces the plain capsule (which stays as the fallback).
        PlayerCharacters.ApplyTo(gameObject);
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

        RefreshStats();
    }

    // Applies purchased Swift Boots levels on top of the inspector speed.
    public void RefreshStats()
    {
        if (GameManager.Instance != null)
        {
            moveSpeed = baseMoveSpeed + GameManager.Instance.GetUpgradeLevel("boots") * ShopCatalog.BootsSpeedPerLevel;
        }
    }

    // Moves the player without the CharacterController snapping them back.
    public void Teleport(Vector3 position, float yaw)
    {
        controller.enabled = false;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        controller.enabled = true;
        verticalVelocity = 0f;
    }

    void Update()
    {
        bool blocked = !canMove;

        if (GameManager.Instance != null)
        {
            blocked = blocked || GameManager.Instance.isInDialogue || GameManager.Instance.isTransitioning;
        }

        // Downed players wait for a teammate.
        PlayerAvatar avatar = PlayerRegistry.Local;
        blocked = blocked || (avatar != null && avatar.gameObject == gameObject && avatar.IsDowned);

        Vector2 moveInput = Vector2.zero;
        if (!blocked)
        {
            moveInput = inputActions.Player.Move.ReadValue<Vector2>();
            if (moveInput.sqrMagnitude < 0.01f)
            {
                moveInput = MobileControls.Move;
            }
        }
        Vector3 horizontalMove = Vector3.zero;
        IsReversing = moveInput.y < -0.1f;
        IsMovingBackward = IsReversing && Mathf.Abs(moveInput.x) < 0.2f;

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

                // Walking backward keeps the character facing the same way, so it moves without turning.
                if (!IsMovingBackward)
                {
                    float turnSpeed = IsReversing ? reverseTurnSpeed : rotationSpeed;
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
                }
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
