using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public int maxHealth = 7;
    public int currentHealth;

    public float moveSpeed = 2f;
    public float patrolDistance = 4f;
    public float patrolTime = 3f;

    public Weapon weapon;
    public float fireInterval = 0.5f;
    public bool autoFire = true;

    public bool hasTwoPhases = false;
    public int phase2HealthThreshold = 7;
    public bool trackPlayerInPhase2 = true;

    public Transform playerTransform;
    public GameObject exclamationMark;

    private Vector3 startPosition;
    private float patrolTimer = 0f;
    private float fireTimer = 0f;
    private int currentPhase = 1;
    private bool isDead = false;
    private bool isPatrolling = true;
    private SpriteRenderer spriteRenderer;

    void Start()
    {
        currentHealth = maxHealth;
        startPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        if (exclamationMark != null)
        {
            exclamationMark.SetActive(false);
        }
    }

    void Update()
    {
        if (isDead)
        {
            return;
        }

        if (isPatrolling)
        {
            PatrolMovement();
        }
        else if (currentPhase == 2 && trackPlayerInPhase2)
        {
            TrackPlayerMovement();
        }

        if (autoFire && weapon != null)
        {
            fireTimer += Time.deltaTime;
            if (fireTimer >= fireInterval)
            {
                fireTimer = 0f;
                weapon.Fire();
            }
        }
    }

    void PatrolMovement()
    {
        patrolTimer += Time.deltaTime;

        float progress = (patrolTimer % (patrolTime * 2)) / patrolTime;
        if (progress > 1f)
        {
            progress = 2f - progress;
        }

        float xOffset = (progress - 0.5f) * patrolDistance;
        transform.position = new Vector3(startPosition.x + xOffset, startPosition.y, startPosition.z);
    }

    void TrackPlayerMovement()
    {
        if (playerTransform == null)
        {
            return;
        }

        float targetX = playerTransform.position.x;
        float currentX = transform.position.x;
        float newX = Mathf.MoveTowards(currentX, targetX, moveSpeed * Time.deltaTime);

        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    public void TakeDamage(int amount)
    {
        if (isDead)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);

        StartCoroutine(FlashDamage());

        if (hasTwoPhases && currentPhase == 1 && currentHealth <= phase2HealthThreshold)
        {
            EnterPhase2();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    System.Collections.IEnumerator FlashDamage()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = Color.white;
        }
    }

    void EnterPhase2()
    {
        currentPhase = 2;
        isPatrolling = false;
    }

    void Die()
    {
        isDead = true;
        autoFire = false;

        if (exclamationMark != null)
        {
            exclamationMark.SetActive(true);
        }
    }

    public bool IsDead()
    {
        return isDead;
    }
}
