using UnityEngine;

/// <summary>
/// Exploder enemy behavior - deals AOE damage to all surrounding buildings and enemies when it dies.
/// This component should be added to an Enemy with EnemyType.Exploder.
/// </summary>
public class ExploderEnemyBehavior : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float explosionRadius = 5f;
    public float explosionDamage = 50f;
    public bool damageEnemies = true; // Whether to damage other enemies
    public bool damageBuildings = true; // Whether to damage buildings

    Enemy _enemy;

    void Awake()
    {
        _enemy = GetComponent<Enemy>();
        if (_enemy != null)
        {
            // Subscribe to death event
            _enemy.onDeath.AddListener(OnDeath);
        }
    }

    void OnDeath()
    {
        // Explode when we die
        Explode();
    }

    void Explode()
    {
        Vector3 explosionCenter = transform.position;

        // Damage buildings
        if (damageBuildings && BuildingManager.Instance != null)
        {
            foreach (var building in BuildingManager.Instance.AllBuildings)
            {
                if (building == null || building.IsDead) continue;

                float distance = Vector3.Distance(explosionCenter, building.transform.position);
                if (distance <= explosionRadius)
                {
                    // Apply damage with falloff based on distance
                    float damageMultiplier = 1f - (distance / explosionRadius);
                    float actualDamage = explosionDamage * damageMultiplier;
                    building.TakeDamage(actualDamage);
                }
            }
        }

        // Damage other enemies
        if (damageEnemies && SpawnerManager.Instance != null)
        {
            var enemies = SpawnerManager.Instance.GetActiveEnemiesSnapshot();
            if (enemies != null)
            {
                foreach (var enemy in enemies)
                {
                    if (enemy == null || enemy.IsDead || enemy == _enemy) continue;

                    float distance = Vector3.Distance(explosionCenter, enemy.transform.position);
                    if (distance <= explosionRadius)
                    {
                        // Apply damage with falloff based on distance
                        float damageMultiplier = 1f - (distance / explosionRadius);
                        float actualDamage = explosionDamage * damageMultiplier;
                        enemy.TakeDamage(actualDamage);
                    }
                }
            }
        }

        // Play explosion effect/sound
        AudioManager.Instance?.PlaySFX("explosion");

        // Optional: Create visual explosion effect here
        // You could instantiate a particle system or other visual effect
    }

    void OnDestroy()
    {
        if (_enemy != null)
        {
            _enemy.onDeath.RemoveListener(OnDeath);
        }
    }

    // Draw explosion radius in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
