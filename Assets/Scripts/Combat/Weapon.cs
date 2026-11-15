using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 0.4f;
    public float projectileSpeed = 15f;
    public int damage = 1;
    public int projectileCount = 1;
    public float spreadAngle = 0f;
    public float projectileScale = 0.3f;
    public bool pierce = false;
    public Color projectileColor = Color.yellow;

    [Header("Aim Assist")]
    public float aimAssistRange = 22f;
    public float aimAssistAngle = 40f;

    private InputSystem_Actions inputActions;
    private InputAction fireAction;
    private InputAction nextAction;
    private InputAction previousAction;
    private float nextFireTime = 0f;

    void OnEnable()
    {
        inputActions = new InputSystem_Actions();
        inputActions.Enable();
        fireAction = inputActions.Player.Attack;
        nextAction = inputActions.Player.Next;
        previousAction = inputActions.Player.Previous;
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

        RefreshStats();
    }

    // Applies the equipped weapon and purchased upgrades from GameManager.
    public void RefreshStats()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            return;
        }

        WeaponDef def = WeaponCatalog.GetOrDefault(gm.equippedWeapon);
        damage = def.damage + gm.GetUpgradeLevel("dmg");
        fireRate = def.fireRate * Mathf.Pow(ShopCatalog.FireRateMultiplierPerLevel, gm.GetUpgradeLevel("rate"));
        projectileSpeed = def.projectileSpeed;
        projectileCount = def.projectileCount;
        spreadAngle = def.spreadAngle;
        projectileScale = def.projectileScale;
        pierce = def.pierce;
        projectileColor = def.color;
    }

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && (gm.isInDialogue || gm.isTransitioning))
        {
            return;
        }

        PlayerAvatar avatar = GetComponent<PlayerAvatar>();
        if (avatar != null && avatar.IsDowned)
        {
            return;
        }

        if (nextAction.WasPressedThisFrame() || Input.GetKeyDown(KeyCode.Q) || MobileControls.SwapPressed)
        {
            CycleWeapon(1);
        }
        else if (previousAction.WasPressedThisFrame())
        {
            CycleWeapon(-1);
        }

        // Holding fire keeps shooting at the weapon's fire rate.
        if ((fireAction != null && fireAction.IsPressed()) || MobileControls.FireHeld)
        {
            Fire();
        }
    }

    void CycleWeapon(int step)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.ownedWeapons.Count <= 1)
        {
            return;
        }

        int index = gm.ownedWeapons.IndexOf(gm.equippedWeapon);
        index = (index + step + gm.ownedWeapons.Count) % gm.ownedWeapons.Count;
        gm.equippedWeapon = gm.ownedWeapons[index];
        RefreshStats();

        if (AreaHUD.Instance != null)
        {
            AreaHUD.Instance.Toast("Switched to " + WeaponCatalog.GetOrDefault(gm.equippedWeapon).name, 1.5f);
        }
    }

    public void Fire()
    {
        FireInDirection(GetAssistedDirection(firePoint.forward));
    }

    // Nudges the shot toward the closest enemy roughly in front of the player.
    Vector3 GetAssistedDirection(Vector3 forward)
    {
        forward.y = 0f;
        Enemy best = null;
        float bestScore = float.MaxValue;

        foreach (Enemy enemy in Enemy.Alive)
        {
            Vector3 toEnemy = enemy.transform.position - firePoint.position;
            toEnemy.y = 0f;
            float distance = toEnemy.magnitude;

            if (distance > aimAssistRange || distance < 0.01f)
            {
                continue;
            }

            float angle = Vector3.Angle(forward, toEnemy);
            if (angle > aimAssistAngle)
            {
                continue;
            }

            float score = distance + angle * 0.3f;
            if (score < bestScore)
            {
                bestScore = score;
                best = enemy;
            }
        }

        if (best == null)
        {
            return forward;
        }

        Vector3 direction = best.transform.position - firePoint.position;
        direction.y = 0f;
        return direction;
    }

    public void FireInDirection(Vector3 direction)
    {
        if (Time.time < nextFireTime || projectilePrefab == null || direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        nextFireTime = Time.time + fireRate;
        direction.Normalize();

        for (int i = 0; i < projectileCount; i++)
        {
            float angle = projectileCount > 1 ? Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, i / (float)(projectileCount - 1)) : 0f;
            Vector3 shotDirection = Quaternion.Euler(0f, angle, 0f) * direction;

            ShotData shot = new ShotData
            {
                origin = firePoint.position,
                direction = shotDirection,
                speed = projectileSpeed,
                damage = damage,
                pierce = pierce,
                fromEnemy = false,
                color = projectileColor,
                scale = projectileScale,
                shooter = Net.LocalClientId,
            };

            if (!Net.Active)
            {
                Projectile.Spawn(shot, false);
            }
            else if (Net.IsAuthority)
            {
                // Host: the real shot is simulated here; guests see copies.
                Projectile.Spawn(shot, false);
                PartyState.Instance?.ShotRpc(shot);
            }
            else
            {
                // Guest: show our shot right away; the host simulates the real one and shows the others.
                Projectile.Spawn(shot, true);
                PartyState.Instance?.PlayerShotRpc(shot);
            }
        }
    }
}
