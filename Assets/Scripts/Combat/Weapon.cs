using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 0.5f;
    public float projectileSpeed = 10f;
    public int damage = 1;
    public bool isPlayerWeapon = true;
    public bool useBombInput = false;

    private InputSystem_Actions inputActions;
    private InputAction fireAction;
    private float nextFireTime = 0f;

    void OnEnable()
    {
        if (isPlayerWeapon)
        {
            inputActions = new InputSystem_Actions();
            inputActions.Enable();

            if (useBombInput)
            {
                fireAction = inputActions.Player.Attack;
            }
            else
            {
                fireAction = inputActions.Player.Jump;
            }
        }
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
        if (firePoint == null)
        {
            firePoint = transform;
        }

        if (projectilePrefab == null)
        {
            projectilePrefab = Resources.Load<GameObject>("Prefabs/Projectile");
        }
    }

    void Update()
    {
        if (isPlayerWeapon)
        {
            bool shouldFire = false;

            if (fireAction != null && fireAction.WasPressedThisFrame())
            {
                shouldFire = true;
            }

            if (!useBombInput && Input.GetKeyDown(KeyCode.Space))
            {
                shouldFire = true;
            }

            if (shouldFire)
            {
                Fire();
            }
        }
    }

    public void Fire()
    {
        if (Time.time < nextFireTime)
        {
            return;
        }

        if (projectilePrefab == null)
        {
            return;
        }

        nextFireTime = Time.time + fireRate;

        GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile projectile = projectileObj.GetComponent<Projectile>();

        if (projectile != null)
        {
            Vector2 direction = isPlayerWeapon ? Vector2.up : Vector2.down;
            projectile.Initialize(direction, projectileSpeed, damage, isPlayerWeapon);
        }
    }

    public void FireInDirection(Vector2 direction)
    {
        if (Time.time < nextFireTime)
        {
            return;
        }

        if (projectilePrefab == null)
        {
            return;
        }

        nextFireTime = Time.time + fireRate;

        GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        Projectile projectile = projectileObj.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.Initialize(direction.normalized, projectileSpeed, damage, isPlayerWeapon);
        }
    }
}
