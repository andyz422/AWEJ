using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 7;
    public int currentHealth;
    public float invincibilityDuration = 1f;

    private bool isInvincible = false;
    private float invincibilityTimer = 0f;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        if (GameManager.Instance != null)
        {
            currentHealth = GameManager.Instance.playerHealth;
            maxHealth = GameManager.Instance.maxPlayerHealth;
        }
        else
        {
            currentHealth = maxHealth;
        }

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;

            if (spriteRenderer != null)
            {
                float alpha = Mathf.PingPong(Time.time * 10f, 1f) > 0.5f ? 1f : 0.5f;
                Color color = spriteRenderer.color;
                color.a = alpha;
                spriteRenderer.color = color;
            }

            if (invincibilityTimer <= 0f)
            {
                isInvincible = false;
                if (spriteRenderer != null)
                {
                    Color color = spriteRenderer.color;
                    color.a = 1f;
                    spriteRenderer.color = color;
                }
            }
        }
    }

    public void TakeDamage(int amount)
    {
        if (isInvincible || currentHealth <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerHealth = currentHealth;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            isInvincible = true;
            invincibilityTimer = invincibilityDuration;
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerHealth = currentHealth;
        }
    }

    void Die()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }
}
