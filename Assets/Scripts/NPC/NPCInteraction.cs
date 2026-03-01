using UnityEngine;
using UnityEngine.InputSystem;

public class NPCInteraction : MonoBehaviour
{
    public string npcName = "NPC";
    public bool canTalk = true;
    public Sprite[] dialogueSprites;

    public GameObject exclamationMark;
    public float exclamationYOffset = 1f;
    public float interactionRadius = 1.5f;

    public bool isSceneTrigger = false;
    public string targetScene = "";

    private InputSystem_Actions inputActions;
    private InputAction interactAction;
    private bool playerInRange = false;
    private bool hasInteracted = false;
    private Transform playerTransform;

    void OnEnable()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
        interactAction = inputActions.Player.Jump;
    }

    void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Disable();
        }
    }

    void Start()
    {
        if (exclamationMark != null)
        {
            exclamationMark.SetActive(false);
            exclamationMark.transform.localPosition = new Vector3(0, exclamationYOffset, 0);
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null || !canTalk)
        {
            return;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= interactionRadius;

        if (exclamationMark != null && !hasInteracted)
        {
            exclamationMark.SetActive(playerInRange);
        }

        if (playerInRange && (interactAction.WasPressedThisFrame() || Input.GetKeyDown(KeyCode.E)))
        {
            Interact();
        }
    }

    void Interact()
    {
        if (GameManager.Instance != null && GameManager.Instance.isInDialogue)
        {
            return;
        }

        hasInteracted = true;

        if (exclamationMark != null)
        {
            exclamationMark.SetActive(false);
        }

        if (isSceneTrigger && !string.IsNullOrEmpty(targetScene))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GoToScene(targetScene);
            }
            return;
        }

        if (DialogueSystem.Instance != null && dialogueSprites != null && dialogueSprites.Length > 0)
        {
            DialogueSystem.Instance.StartDialogue(dialogueSprites, OnDialogueComplete);
        }
    }

    void OnDialogueComplete()
    {
        // Can add post-dialogue logic here
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            if (exclamationMark != null && canTalk && !hasInteracted)
            {
                exclamationMark.SetActive(true);
            }

            if (isSceneTrigger && !string.IsNullOrEmpty(targetScene))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.GoToScene(targetScene);
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (exclamationMark != null)
            {
                exclamationMark.SetActive(false);
            }
        }
    }

    public void ResetInteraction()
    {
        hasInteracted = false;
    }
}
