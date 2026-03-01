using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Sprite[] healthSprites;
    public Image healthImage;

    private int currentHealth = 7;
    private int maxHealth = 7;

    void Awake()
    {
        if (healthImage == null)
        {
            healthImage = GetComponent<Image>();
        }
    }

    public void SetHealth(int current, int max)
    {
        currentHealth = Mathf.Clamp(current, 0, max);
        maxHealth = max;
        UpdateVisual();
    }

    public void SetHealth(int current)
    {
        currentHealth = Mathf.Clamp(current, 0, maxHealth);
        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (healthSprites == null || healthSprites.Length == 0)
        {
            return;
        }

        if (healthImage == null)
        {
            return;
        }

        if (currentHealth <= 0)
        {
            healthImage.enabled = false;
            return;
        }

        healthImage.enabled = true;

        int spriteIndex = Mathf.Clamp(currentHealth - 1, 0, healthSprites.Length - 1);

        if (spriteIndex >= 0 && spriteIndex < healthSprites.Length)
        {
            healthImage.sprite = healthSprites[spriteIndex];
        }
    }
}
