using UnityEngine;
using UnityEngine.Events;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private VirtualJoystick joystick;

    [Header("Input Events")]
    public UnityEvent onButton1Press;
    public UnityEvent onButton2Press;
    public UnityEvent onButton3Press;
    public UnityEvent onButton1Release;
    public UnityEvent onButton2Release;
    public UnityEvent onButton3Release;

    private bool button1Held = false;
    private bool button2Held = false;
    private bool button3Held = false;

    public Vector2 MovementInput
    {
        get
        {
            Vector2 keyboardInput = GetKeyboardMovement();
            if (keyboardInput.magnitude > 0.1f)
            {
                return keyboardInput;
            }

            if (joystick != null)
            {
                return joystick.Direction;
            }

            return Vector2.zero;
        }
    }

    public float SpeedMultiplier
    {
        get
        {
            if (joystick != null && joystick.Direction.magnitude > 0.1f)
            {
                return joystick.SpeedMultiplier;
            }
            return 1f;
        }
    }

    public float MovementAngle
    {
        get
        {
            if (joystick != null)
            {
                return joystick.Angle;
            }
            return 0f;
        }
    }

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

        if (onButton1Press == null) onButton1Press = new UnityEvent();
        if (onButton2Press == null) onButton2Press = new UnityEvent();
        if (onButton3Press == null) onButton3Press = new UnityEvent();
        if (onButton1Release == null) onButton1Release = new UnityEvent();
        if (onButton2Release == null) onButton2Release = new UnityEvent();
        if (onButton3Release == null) onButton3Release = new UnityEvent();
    }

    void Update()
    {
        CheckKeyboardButtons();

        if (joystick != null)
        {
            Vector2 keyboardInput = GetKeyboardMovement();
            if (keyboardInput.magnitude > 0.1f)
            {
                joystick.SetInputFromKeyboard(keyboardInput);
            }
        }
    }

    Vector2 GetKeyboardMovement()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            vertical += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            vertical -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontal += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontal -= 1f;

        return new Vector2(horizontal, vertical).normalized;
    }

    void CheckKeyboardButtons()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            button1Held = true;
            onButton1Press?.Invoke();
        }
        if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.KeypadEnter))
        {
            button1Held = false;
            onButton1Release?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            button2Held = true;
            onButton2Press?.Invoke();
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            button2Held = false;
            onButton2Release?.Invoke();
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            button3Held = true;
            onButton3Press?.Invoke();
        }
        if (Input.GetKeyUp(KeyCode.E))
        {
            button3Held = false;
            onButton3Release?.Invoke();
        }
    }

    public void OnButton1Down() => onButton1Press?.Invoke();
    public void OnButton1Up() => onButton1Release?.Invoke();
    public void OnButton2Down() => onButton2Press?.Invoke();
    public void OnButton2Up() => onButton2Release?.Invoke();
    public void OnButton3Down() => onButton3Press?.Invoke();
    public void OnButton3Up() => onButton3Release?.Invoke();

    public bool IsButton1Held => button1Held || Input.GetKey(KeyCode.Return);
    public bool IsButton2Held => button2Held || Input.GetKey(KeyCode.Space);
    public bool IsButton3Held => button3Held || Input.GetKey(KeyCode.E);

    public void SetJoystick(VirtualJoystick js)
    {
        joystick = js;
    }
}
