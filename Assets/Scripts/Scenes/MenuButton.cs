using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("Button Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite pressedSprite;

    [Header("References")]
    [SerializeField] private Image buttonImage;

    public delegate void ClickHandler();
    public event ClickHandler OnClick;

    void Awake()
    {
        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (pressedSprite != null && buttonImage != null)
        {
            buttonImage.sprite = pressedSprite;
        }

        transform.localScale = new Vector3(0.95f, 0.95f, 1f);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (normalSprite != null && buttonImage != null)
        {
            buttonImage.sprite = normalSprite;
        }

        transform.localScale = Vector3.one;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClick?.Invoke();
    }

    public void SetSprites(Sprite normal, Sprite pressed)
    {
        normalSprite = normal;
        pressedSprite = pressed;

        if (buttonImage != null && normalSprite != null)
        {
            buttonImage.sprite = normalSprite;
        }
    }
}
