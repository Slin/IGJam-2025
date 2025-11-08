using UnityEngine;

/// <summary>
/// Abstract base class for building attack behaviors.
/// Inherit from this to create different attack types (laser, rocket, etc.)
/// </summary>
public abstract class BuildingAttackBehavior : MonoBehaviour
{
    [Header("Attack Settings")]
    public float attackRange = 10f;
    public float attackDamage = 20f;
    public float attackDelay = 1f;

    protected Building _building;
    protected float _attackCooldown;

    protected virtual void Awake()
    {
        _building = GetComponent<Building>();
    }

    protected virtual void Update()
    {
        // Only attack if building is placed and alive
        if (_building == null || !_building.IsPlaced || _building.IsDead)
            return;

        // Update cooldown
        if (_attackCooldown > 0)
        {
            _attackCooldown -= Time.deltaTime;
            return;
        }

        // Find target
        Enemy target = FindTarget();
        if (target != null)
        {
            PerformAttack(target);
            _attackCooldown = attackDelay;
        }
    }

    /// <summary>
    /// Finds the best target to attack within range
    /// </summary>
    protected virtual Enemy FindTarget()
    {
        return SpawnerManager.Instance?.GetClosestEnemy(transform.position, attackRange);
    }

    /// <summary>
    /// Performs the actual attack. Override this for different attack behaviors.
    /// </summary>
    /// <param name="target">The enemy to attack</param>
    protected abstract void PerformAttack(Enemy target);
}
