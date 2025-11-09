using UnityEngine;

/// <summary>
/// Freeze tower attack - fires multiple projectiles that apply a slow effect
/// </summary>
public class FreezeTowerAttack : BuildingAttackBehavior
{
    [Header("Freeze Tower Settings")]
    public GameObject freezeProjectilePrefab;
    public int projectileCount = 5;
    public float projectileSpeed = 12f;
    public float slowPercentage = 0.5f; // 50% slow
    public float slowDuration = 2f; // 2 seconds
    public Vector3 launchOffset = new Vector3(0, 0.5f, 0);
    public float spreadAngle = 15f; // Degrees of spread between projectiles

    protected override void PerformAttack(Enemy target)
    {
        if (target == null || target.IsDead) return;

        // Find up to projectileCount different enemies to target
        var targets = FindMultipleTargets(projectileCount);

        // Fire one projectile at each target
        for (int i = 0; i < targets.Count; i++)
        {
            FireProjectile(targets[i], i);
        }
    }

    System.Collections.Generic.List<Enemy> FindMultipleTargets(int maxTargets)
    {
        var targets = new System.Collections.Generic.List<Enemy>();
        if (SpawnerManager.Instance == null) return targets;

        var enemies = SpawnerManager.Instance.GetActiveEnemiesSnapshot();
        if (enemies == null) return targets;

        // Find enemies within range, sorted by distance
        var enemiesInRange = new System.Collections.Generic.List<(Enemy enemy, float distance)>();
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance <= attackRange)
            {
                enemiesInRange.Add((enemy, distance));
            }
        }

        // Sort by distance (closest first)
        enemiesInRange.Sort((a, b) => a.distance.CompareTo(b.distance));

        // Take up to maxTargets enemies
        int count = Mathf.Min(maxTargets, enemiesInRange.Count);
        for (int i = 0; i < count; i++)
        {
            targets.Add(enemiesInRange[i].enemy);
        }

        return targets;
    }

    void FireProjectile(Enemy target, int index)
    {
        if (target == null || target.IsDead) return;

        if (freezeProjectilePrefab == null)
        {
            // Fallback: apply slow effect directly if no prefab
            ApplySlowDirectly(target);
            return;
        }

        Vector3 spawnPos = transform.position + launchOffset;
        GameObject projectileObj = Instantiate(freezeProjectilePrefab, spawnPos, Quaternion.identity);

        FreezeProjectile projectile = projectileObj.GetComponent<FreezeProjectile>();
        if (projectile == null)
        {
            projectile = projectileObj.AddComponent<FreezeProjectile>();
        }

        projectile.Initialize(target, slowPercentage, slowDuration, projectileSpeed);

        // Point directly at target
        Vector3 directionToTarget = (target.transform.position - spawnPos).normalized;
        float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void ApplySlowDirectly(Enemy target)
    {
        if (target == null || target.IsDead) return;

        // Get or add SlowEffect component
        SlowEffect slowEffect = target.GetComponent<SlowEffect>();
        if (slowEffect == null)
        {
            slowEffect = target.gameObject.AddComponent<SlowEffect>();
        }

        // Apply the slow
        slowEffect.ApplySlow(slowPercentage, slowDuration);
    }
}
