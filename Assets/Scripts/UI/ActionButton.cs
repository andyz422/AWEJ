using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum ActionButtonType
{
    Button1,
    Button2,
    Button3
}

public class ActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Button Settings")]
    [SerializeField] private ActionButtonType buttonType = ActionButtonType.Button1;

    [Header("Sprites")]
    [SerializeField] private Sprite unpressedSprite;
    [SerializeField] private Sprite pressedSprite;

    [Header("References")]
    [SerializeField] private Image buttonImage;

    void Awake()
    {
        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }

        if (buttonImage != null && unpressedSprite != null)
        {
            buttonImage.sprite = unpressedSprite;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (buttonImage != null && pressedSprite != null)
        {
            buttonImage.sprite = pressedSprite;
        }

        if (InputManager.Instance != null)
        {
            switch (buttonType)
            {
                case ActionButtonType.Button1:
                    InputManager.Instance.OnButton1Down();
                    break;
                case ActionButtonType.Button2:
                    InputManager.Instance.OnButton2Down();
                    break;
                case ActionButtonType.Button3:
                    InputManager.Instance.OnButton3Down();
                    break;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (buttonImage != null && unpressedSprite != null)
        {
            buttonImage.sprite = unpressedSprite;
        }

        if (InputManager.Instance != null)
        {
            switch (buttonType)
            {
                case ActionButtonType.Button1:
                    InputManager.Instance.OnButton1Up();
                    break;
                case ActionButtonType.Button2:
                    InputManager.Instance.OnButton2Up();
                    break;
                case ActionButtonType.Button3:
                    InputManager.Instance.OnButton3Up();
                    break;
            }
        }
    }

    public void SetSprites(Sprite unpressed, Sprite pressed)
    {
        unpressedSprite = unpressed;
        pressedSprite = pressed;

        if (buttonImage != null && unpressedSprite != null)
        {
            buttonImage.sprite = unpressedSprite;
        }
    }
}
