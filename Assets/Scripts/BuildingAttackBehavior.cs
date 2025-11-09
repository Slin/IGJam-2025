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

    /// <summary>
    /// Gets the final attack damage with all boosts applied by checking nearby BoostBuildings and singularity effects
    /// </summary>
    public float EffectiveAttackDamage
    {
        get
        {
            float boostMultiplier = CalculateBoostMultiplier();
            float effectMultiplier = GetSingularityDamageMultiplier();
            return attackDamage * boostMultiplier * effectMultiplier;
        }
    }

    /// <summary>
    /// Gets the effective attack range with singularity effects applied
    /// </summary>
    public float EffectiveAttackRange
    {
        get
        {
            if (_building == null) return attackRange;
            float effectMultiplier = GetSingularityRangeMultiplier();
            return attackRange * effectMultiplier;
        }
    }

    /// <summary>
    /// Gets the singularity effect multiplier for building attack damage
    /// </summary>
    private float GetSingularityDamageMultiplier()
    {
        if (SingularityEffectManager.Instance == null || _building == null) return 1f;
        return SingularityEffectManager.Instance.GetEffectMultiplier(SingularityEffectType.BuildingAttackDamage, _building.buildingType);
    }

    /// <summary>
    /// Gets the singularity effect multiplier for building attack range
    /// </summary>
    private float GetSingularityRangeMultiplier()
    {
        if (SingularityEffectManager.Instance == null || _building == null) return 1f;
        return SingularityEffectManager.Instance.GetEffectMultiplier(SingularityEffectType.BuildingAttackRange, _building.buildingType);
    }

    /// <summary>
    /// Calculates the total boost multiplier from nearby BoostBuildings
    /// Boosts stack additively (1.5 + 1.5 = 2.0x damage, not 2.25x)
    /// </summary>
    private float CalculateBoostMultiplier()
    {
        if (BuildingManager.Instance == null) return 1f;

        float additiveBoost = 0f;
        int boostCount = 0;

        // Find all BoostBuildings
        var boostBuildings = BuildingManager.Instance.GetBuildingsOfType(BuildingType.BoostBuilding);

        foreach (var building in boostBuildings)
        {
            if (building == null) continue;

            var boostComponent = building.GetComponent<BoostBuilding>();
            if (boostComponent != null && boostComponent.IsInRange(transform.position))
            {
                // Add the bonus (1.5 multiplier = 0.5 bonus)
                additiveBoost += (boostComponent.damageMultiplier - 1f);
                boostCount++;
            }
        }

        return 1f + additiveBoost;
    }

    /// <summary>
    /// Gets the number of active boosts affecting this building
    /// </summary>
    public int GetActiveBoostCount()
    {
        if (BuildingManager.Instance == null) return 0;

        int count = 0;
        var boostBuildings = BuildingManager.Instance.GetBuildingsOfType(BuildingType.BoostBuilding);

        foreach (var building in boostBuildings)
        {
            if (building == null) continue;

            var boostComponent = building.GetComponent<BoostBuilding>();
            if (boostComponent != null && boostComponent.IsInRange(transform.position))
            {
                count++;
            }
        }

        return count;
    }

    protected virtual void Awake()
    {
        _building = GetComponent<Building>();
    }

    protected virtual void Start()
    {
        // Add tooltip component if not present
        var tooltipType = System.Type.GetType("BuildingTooltip");
        if (tooltipType != null && GetComponent(tooltipType) == null)
        {
            gameObject.AddComponent(tooltipType);
        }

        // Add attack range indicator if not present
        var rangeIndicatorType = System.Type.GetType("AttackRangeIndicator");
        if (rangeIndicatorType != null && GetComponent(rangeIndicatorType) == null)
        {
            gameObject.AddComponent(rangeIndicatorType);
        }
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
        return SpawnerManager.Instance?.GetClosestEnemy(transform.position, EffectiveAttackRange);
    }

    /// <summary>
    /// Performs the actual attack. Override this for different attack behaviors.
    /// </summary>
    /// <param name="target">The enemy to attack</param>
    protected abstract void PerformAttack(Enemy target);
}
