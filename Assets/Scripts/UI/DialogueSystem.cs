using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    public GameObject dialoguePanel;
    public Image dialogueImage;
    public Sprite[] currentDialogueSprites;

    private InputSystem_Actions inputActions;
    private InputAction advanceAction;
    private int currentIndex = 0;
    private bool isDialogueActive = false;
    private System.Action onDialogueComplete;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
        advanceAction = inputActions.Player.Jump;
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
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    void Update()
    {
        if (isDialogueActive)
        {
            if (advanceAction.WasPressedThisFrame() || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
            {
                AdvanceDialogue();
            }
        }
    }

    public void StartDialogue(Sprite[] sprites, System.Action onComplete = null)
    {
        if (sprites == null || sprites.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        currentDialogueSprites = sprites;
        onDialogueComplete = onComplete;
        currentIndex = 0;
        isDialogueActive = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.isInDialogue = true;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        ShowCurrentDialogue();
    }

    void ShowCurrentDialogue()
    {
        if (currentDialogueSprites == null || dialogueImage == null)
        {
            return;
        }

        if (currentIndex < currentDialogueSprites.Length)
        {
            dialogueImage.sprite = currentDialogueSprites[currentIndex];
        }
    }

    public void AdvanceDialogue()
    {
        if (!isDialogueActive || currentDialogueSprites == null)
        {
            return;
        }

        currentIndex++;

        if (currentIndex >= currentDialogueSprites.Length)
        {
            EndDialogue();
        }
        else
        {
            ShowCurrentDialogue();
        }
    }

    void EndDialogue()
    {
        isDialogueActive = false;
        currentDialogueSprites = null;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.isInDialogue = false;
        }

        onDialogueComplete?.Invoke();
        onDialogueComplete = null;
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }
}
