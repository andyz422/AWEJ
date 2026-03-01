using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public bool worldScrolling = true;
    public Transform worldTransform;

    private InputSystem_Actions inputActions;
    private Vector2 moveInput;
    private bool canMove = true;

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
        if (worldScrolling && worldTransform == null)
        {
            GameObject world = GameObject.Find("World");
            if (world != null)
            {
                worldTransform = world.transform;
            }
        }
    }

    void Update()
    {
        if (!canMove)
        {
            return;
        }

        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.isInDialogue || GameManager.Instance.isTransitioning)
            {
                return;
            }
        }

        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        if (moveInput.magnitude > 0.1f)
        {
            Move(moveInput);
        }
    }

    void Move(Vector2 direction)
    {
        Vector2 movement = direction.normalized * moveSpeed * Time.deltaTime;

        if (worldScrolling && worldTransform != null)
        {
            worldTransform.position -= new Vector3(movement.x, movement.y, 0);
        }
        else
        {
            transform.position += new Vector3(movement.x, movement.y, 0);
        }
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
