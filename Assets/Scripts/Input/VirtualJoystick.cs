using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick Components")]
    [SerializeField] private RectTransform joystickBase;
    [SerializeField] private RectTransform joystickHandle;

    [Header("Settings")]
    [SerializeField] private float handleRange = 50f;
    [SerializeField] private float deadZone = 0.1f;
    [SerializeField] private float innerZoneRadius = 0.3f;
    [SerializeField] private float innerZoneSpeedMultiplier = 0.5f;

    private Vector2 inputVector = Vector2.zero;
    private float currentSpeedMultiplier = 1f;
    private Canvas canvas;
    private Camera cam;

    public Vector2 Direction => inputVector;
    public float Magnitude => inputVector.magnitude;
    public float Angle => Mathf.Atan2(inputVector.y, inputVector.x) * Mathf.Rad2Deg;
    public float SpeedMultiplier => currentSpeedMultiplier;

    public static VirtualJoystick Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        canvas = GetComponentInParent<Canvas>();

        if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            cam = canvas.worldCamera;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBase,
            eventData.position,
            cam,
            out position
        );

        position = position / handleRange;

        float distance = position.magnitude;

        if (distance < innerZoneRadius)
        {
            currentSpeedMultiplier = innerZoneSpeedMultiplier;
        }
        else
        {
            currentSpeedMultiplier = 1f;
        }

        if (distance > 1f)
        {
            position = position.normalized;
        }

        inputVector = position;

        if (inputVector.magnitude < deadZone)
        {
            inputVector = Vector2.zero;
        }

        joystickHandle.anchoredPosition = inputVector * handleRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        currentSpeedMultiplier = 1f;
        joystickHandle.anchoredPosition = Vector2.zero;
    }

    public void SetInputFromKeyboard(Vector2 input)
    {
        inputVector = input.normalized;
        currentSpeedMultiplier = 1f;

        if (inputVector.magnitude < deadZone)
        {
            inputVector = Vector2.zero;
        }
    }
}
