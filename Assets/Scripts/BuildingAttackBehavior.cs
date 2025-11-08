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
    private float _damageBoostAdditive = 0f;

    /// <summary>
    /// Gets the final attack damage with all boosts applied
    /// </summary>
    public float EffectiveAttackDamage => attackDamage * (1f + _damageBoostAdditive);

    /// <summary>
    /// Adds a damage boost to this building's attack (additive stacking)
    /// For example, a multiplier of 1.5 adds 0.5 (50%) bonus damage
    /// </summary>
    public void AddDamageMultiplier(float multiplier)
    {
        // Convert multiplier to additive bonus (1.5 -> 0.5)
        _damageBoostAdditive += (multiplier - 1f);
    }

    /// <summary>
    /// Removes a damage boost from this building's attack
    /// </summary>
    public void RemoveDamageMultiplier(float multiplier)
    {
        // Convert multiplier to additive bonus and subtract
        _damageBoostAdditive -= (multiplier - 1f);
    }

    /// <summary>
    /// Resets all damage boosts
    /// </summary>
    public void ResetDamageMultipliers()
    {
        _damageBoostAdditive = 0f;
    }

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
