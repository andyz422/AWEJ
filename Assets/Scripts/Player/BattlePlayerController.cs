using UnityEngine;
using UnityEngine.InputSystem;

public class BattlePlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float leftBoundary = -4f;
    public float rightBoundary = 4f;

    public Weapon laserWeapon;
    public Weapon bombWeapon;

    private InputSystem_Actions inputActions;
    private bool canMove = true;

    void OnEnable()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
    }

    void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Disable();
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

        HandleMovement();
    }

    void HandleMovement()
    {
        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        float horizontalMovement = moveInput.x * moveSpeed * Time.deltaTime;
        float newX = transform.position.x + horizontalMovement;

        newX = Mathf.Clamp(newX, leftBoundary, rightBoundary);

        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
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
