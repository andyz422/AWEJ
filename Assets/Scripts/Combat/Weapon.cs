using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 0.4f;
    public float projectileSpeed = 15f;
    public int damage = 1;

    private InputSystem_Actions inputActions;
    private InputAction fireAction;
    private float nextFireTime = 0f;

    void OnEnable()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
        fireAction = inputActions.Player.Attack;
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
        if (fireAction != null && fireAction.WasPressedThisFrame())
        {
            Fire();
        }
    }

    public void Fire()
    {
        FireInDirection(firePoint.forward);
    }

    public void FireInDirection(Vector3 direction)
    {
        if (Time.time < nextFireTime || projectilePrefab == null)
        {
            return;
        }

        nextFireTime = Time.time + fireRate;

        GameObject projectileObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
        Projectile projectile = projectileObj.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.Initialize(direction.normalized, projectileSpeed, damage);
        }
    }
}
