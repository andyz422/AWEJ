using Unity.Netcode;

// The networked half of an enemy. The host runs the real enemy (AI, hits, death); guests get a
// puppet that shows the same type, position and health bar.
public class EnemyNet : NetworkBehaviour
{
    public readonly NetworkVariable<int> Type = new NetworkVariable<int>();
    public readonly NetworkVariable<int> Health = new NetworkVariable<int>();
    public readonly NetworkVariable<int> MaxHealth = new NetworkVariable<int>();

    private Enemy enemy;

    public override void OnNetworkSpawn()
    {
        enemy = GetComponent<Enemy>();
        if (IsServer)
        {
            return;
        }
        enemy.ConfigurePuppet((EnemyType)Type.Value, MaxHealth.Value);
        enemy.SetPuppetHealth(Health.Value);
        Health.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        Health.OnValueChanged -= OnHealthChanged;
    }

    void OnHealthChanged(int previous, int current)
    {
        enemy.SetPuppetHealth(current);
    }

    [Rpc(SendTo.NotServer)]
    public void FlashRpc()
    {
        if (enemy != null)
        {
            enemy.Flash();
        }
    }
}
