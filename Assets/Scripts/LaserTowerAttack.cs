using UnityEngine;

/// <summary>
/// Laser tower attack - instant hit with laser beam visual
/// </summary>
public class LaserTowerAttack : BuildingAttackBehavior
{
    [Header("Laser Settings")]
    public Color laserColor = Color.cyan;
    public float laserWidth = 0.15f;
    public float laserDuration = 0.3f;

    protected override void PerformAttack(Enemy target)
    {
        if (target == null || target.IsDead) return;

        // Deal damage instantly
        target.TakeDamage(attackDamage);

        // Create laser visual effect
        LaserBeam.Create(transform.position, target.transform.position, laserColor, laserWidth, laserDuration);

        // Play attack sound
        AudioManager.Instance?.PlaySFX("laser_fire");
    }
}
