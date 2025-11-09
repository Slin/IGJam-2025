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

        // Fire multiple projectiles in a spread pattern
        for (int i = 0; i < projectileCount; i++)
        {
            FireProjectile(target, i);
        }

        // Play attack sound
        AudioManager.Instance?.PlaySFX("freeze_fire");
    }

    void FireProjectile(Enemy target, int index)
    {
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

        // Apply spread: center projectiles around the target direction
        Vector3 directionToTarget = (target.transform.position - spawnPos).normalized;
        float baseAngle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;

        // Calculate spread offset for this projectile
        float totalSpread = spreadAngle * (projectileCount - 1);
        float angleOffset = -totalSpread / 2f + (spreadAngle * index);
        float finalAngle = baseAngle + angleOffset;

        projectile.transform.rotation = Quaternion.Euler(0f, 0f, finalAngle);
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
