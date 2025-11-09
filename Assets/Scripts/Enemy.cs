using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Enemy : MonoBehaviour
{
    [Header("Enemy Properties")]
    public EnemyType enemyType = EnemyType.Regular;
    public float maxHealth = 100f;
    public int tritiumReward = 10;
    public float baseDamage = 50f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float arrivalDistance = 0.05f;
    public Vector3 targetPosition = Vector3.zero;

    [Header("Attack Settings")]
    public float attackRange = 50f;
    public float attackDamage = 15f;
    public float attackDelay = 1.5f;
    public bool stopToAttack = false; // If true, enemy stops moving while attacking
    public int maxAttackTargets = 1; // How many drones/buildings can be attacked simultaneously

    [Header("Separation Settings")]
    public float separationRadius = 0.75f; // How close before pushing away
    public float separationForce = 1f; // Strength of separation

    [Header("Events")]
    public UnityEvent onArrived;
    public UnityEvent onDeath;
    public UnityEvent<float> onDamaged;

    [Header("Health Bar")]
    public bool showHealthBar = true;
    public Vector3 healthBarOffset = new Vector3(0, 0.8f, 0);
    public Vector2 healthBarSize = new Vector2(0.8f, 0.12f);

    Action<Enemy> _arrivedCallback;
    bool _arrived;
    float _currentHealth;
    HealthBar _healthBar;
    float _attackCooldown;
    float _retargetTimer;
    const float RETARGET_INTERVAL = 1f; // Recalculate closest base every second
    // Separation throttling
    const int SEPARATION_INTERVAL_FRAMES = 20;
    static int _nextSeparationPhase;
    int _separationPhase;
    int _lastSeparationFrame = -1;
    Vector3 _cachedSeparation = Vector3.zero;

    public float CurrentHealth => _currentHealth;
    public bool IsDead => _currentHealth <= 0;

    void Awake()
    {
        _currentHealth = maxHealth;
        InitializeHealthBar();
        _separationPhase = (_nextSeparationPhase++) % SEPARATION_INTERVAL_FRAMES;
    }

    public void Initialize(Vector3 target, Action<Enemy> onArrivedCallback = null)
    {
        targetPosition = target;
        _arrivedCallback = onArrivedCallback;
        _currentHealth = maxHealth;
        _arrived = false;
        _retargetTimer = RETARGET_INTERVAL; // Start with full interval
        UpdateHealthBar();
    }

    void UpdateTargetToClosestBase()
    {
        if (BuildingManager.Instance == null) return;

        Building closestBase = BuildingManager.Instance.GetNearestBuilding(transform.position, BuildingType.Base);
        if (closestBase != null && !closestBase.IsDead)
        {
            targetPosition = closestBase.transform.position;
        }
    }

    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        // Apply singularity effect multiplier to damage taken by enemies
        float effectMultiplier = GetDamageTakenMultiplier();
        float actualDamage = damage * effectMultiplier;

        _currentHealth -= actualDamage;
        _currentHealth = Mathf.Max(0, _currentHealth);

        UpdateHealthBar();

        try
        {
            onDamaged?.Invoke(_currentHealth);
        }
        catch (Exception) { /* ignore event exceptions */ }

        if (IsDead)
        {
            HandleDeath();
        }
    }

    /// <summary>
    /// Gets the singularity effect multiplier for damage taken by enemies
    /// </summary>
    float GetDamageTakenMultiplier()
    {
        if (SingularityEffectManager.Instance == null) return 1f;
        return SingularityEffectManager.Instance.GetEffectMultiplier(SingularityEffectType.EnemyDamageTaken, enemyType);
    }

    void HandleDeath()
    {
        // If enemy already arrived at base, don't process death
        // (it was already handled by OnEnemyArrived)
        if (_arrived) return;

        try
        {
            onDeath?.Invoke();
        }
        catch (Exception) { /* ignore user event exceptions */ }

        try
        {
            PlayerStatsManager.Instance?.OnEnemyKilled(this);
        }
        catch (Exception) { /* ignore if manager not available */ }

        try
        {
            SpawnerManager.Instance?.OnEnemyKilled(this);
        }
        catch (Exception) { /* ignore if manager not available */ }

        // Spawn enemy-specific death effect from GameManager
        var deathFx = GameManager.Instance != null ? GameManager.Instance.enemyDeathEffectPrefab : null;
        if (deathFx != null)
        {
            GameObject explosion = Instantiate(deathFx, transform.position, Quaternion.identity);
            Destroy(explosion, 5f);
        }

        Destroy(gameObject);
    }

    void Update()
    {
        if (_arrived || IsDead) return;

        // Periodically update target to closest base
        _retargetTimer -= Time.deltaTime;
        if (_retargetTimer <= 0)
        {
            _retargetTimer = RETARGET_INTERVAL;
            UpdateTargetToClosestBase();
        }

        // Update attack cooldown
        _attackCooldown -= Time.deltaTime;

        // Find all potential targets within range and sort by distance
        List<(object target, float distance)> potentialTargets = new List<(object, float)>();

        // Collect drones within range
        if (DroneManager.Instance != null)
        {
            foreach (var drone in DroneManager.Instance.AllDrones)
            {
                if (drone == null || drone.IsDead) continue;
                float distance = Vector3.Distance(transform.position, drone.Position);
                if (distance <= attackRange)
                {
                    potentialTargets.Add((drone, distance));
                }
            }
        }

        // Collect buildings within range (excluding center base)
        if (BuildingManager.Instance != null)
        {
            foreach (var building in BuildingManager.Instance.AllBuildings)
            {
                if (building == null || building.IsDead) continue;

                // Skip bases that are at the center (or very close to center)
                if (building.buildingType == BuildingType.Base && building.transform.position.sqrMagnitude < 0.1f)
                    continue;

                float distance = Vector3.Distance(transform.position, building.transform.position);
                if (distance <= attackRange)
                {
                    potentialTargets.Add((building, distance));
                }
            }
        }

        // Sort by distance and attack up to maxAttackTargets
        bool shouldAttack = false;
        if (potentialTargets.Count > 0 && _attackCooldown <= 0)
        {
            // Sort by distance (closest first)
            potentialTargets.Sort((a, b) => a.distance.CompareTo(b.distance));

            // Attack up to maxAttackTargets
            int targetsToAttack = Mathf.Min(maxAttackTargets, potentialTargets.Count);
            for (int i = 0; i < targetsToAttack; i++)
            {
                var target = potentialTargets[i].target;
                if (target is Drone drone)
                {
                    AttackDrone(drone);
                    shouldAttack = true;
                }
                else if (target is Building building)
                {
                    AttackBuilding(building);
                    shouldAttack = true;
                }
            }

            // Reset cooldown after attacking
            if (shouldAttack)
            {
                _attackCooldown = attackDelay;
            }
        }

        // If stopToAttack is true and we're attacking, don't move this frame
        if (shouldAttack && stopToAttack)
        {
            return;
        }

        // Continue moving towards target with separation
        Vector3 current = transform.position;
        Vector3 direction = (targetPosition - current).normalized;
        Vector3 separationOffset = GetSeparationThrottled();

        // Combine movement direction with separation smoothly
        Vector3 combinedDirection = (direction + separationOffset * 0.3f).normalized;

        // Calculate effective move speed (accounting for slow effects)
        float effectiveMoveSpeed = GetEffectiveMoveSpeed();
        float step = effectiveMoveSpeed * Time.deltaTime;
        Vector3 next = current + combinedDirection * step;

        transform.position = next;

        if ((next - targetPosition).sqrMagnitude <= (arrivalDistance * arrivalDistance))
        {
            HandleArrived();
        }
    }

    Vector3 GetSeparationThrottled()
    {
        int frame = Time.frameCount;
        if (_lastSeparationFrame < 0 || ((frame + _separationPhase) % SEPARATION_INTERVAL_FRAMES) == 0)
        {
            _cachedSeparation = CalculateSeparationFast();
            _lastSeparationFrame = frame;
        }
        return _cachedSeparation;
    }

    float GetEffectiveMoveSpeed()
    {
        // Start with base move speed
        float speed = moveSpeed;

        // Apply slow effect from freeze towers
        SlowEffect slowEffect = GetComponent<SlowEffect>();
        if (slowEffect != null && slowEffect.IsActive)
        {
            speed *= (1f - slowEffect.SlowPercentage);
        }

        // Apply singularity effect multiplier to enemy speed
        float effectMultiplier = GetSpeedMultiplier();
        speed *= effectMultiplier;

        return speed;
    }

    /// <summary>
    /// Gets the singularity effect multiplier for enemy speed
    /// </summary>
    float GetSpeedMultiplier()
    {
        if (SingularityEffectManager.Instance == null) return 1f;
        return SingularityEffectManager.Instance.GetEffectMultiplier(SingularityEffectType.EnemySpeed, enemyType);
    }

    Vector3 CalculateSeparationFast()
    {
        Vector3 separation = Vector3.zero;
        int neighborCount = 0;

        var sm = SpawnerManager.Instance;
        var enemies = sm != null ? sm.GetActiveEnemiesSnapshot() : null;
        if (enemies == null) return Vector3.zero;

        Vector3 myPos = transform.position;
        float radius = separationRadius;
        float radiusSq = radius * radius;

        for (int i = 0; i < enemies.Count; i++)
        {
            var other = enemies[i];
            if (other == null || other == this || other.IsDead) continue;

            Vector3 offset = myPos - other.transform.position;
            if (Mathf.Abs(offset.x) > radius || Mathf.Abs(offset.y) > radius) continue;

            float d2 = offset.sqrMagnitude;
            if (d2 <= 1e-6f || d2 > radiusSq) continue;
            float distance = Mathf.Sqrt(d2);

            float strength = 1.0f - (distance / radius);
            separation += offset * ((1.0f / distance) * strength * separationForce);
            neighborCount++;
        }

        if (neighborCount > 0)
        {
            separation /= neighborCount;
        }

        return separation;
    }

    void AttackBuilding(Building building)
    {
        if (building == null || building.IsDead) return;

        // Apply singularity effect multiplier to enemy damage dealt
        float effectMultiplier = GetDamageDealtMultiplier();
        float actualDamage = attackDamage * effectMultiplier;

        // Perform attack
        building.TakeDamage(actualDamage);

        // Create laser visual effect
        LaserBeam.Create(transform.position, building.transform.position, Color.red, 0.1f, 0.2f);

        // Play attack sound
        AudioManager.Instance?.PlaySFX("laser_enemy");
    }

    void AttackDrone(Drone drone)
    {
        if (drone == null || drone.IsDead) return;

        // Apply singularity effect multiplier to enemy damage dealt
        float effectMultiplier = GetDamageDealtMultiplier();
        float actualDamage = attackDamage * effectMultiplier;

        // Perform attack
        drone.TakeDamage(actualDamage);

        // Create laser visual effect
        LaserBeam.Create(transform.position, drone.transform.position, Color.red, 0.1f, 0.2f);

        // Play attack sound
        AudioManager.Instance?.PlaySFX("laser_enemy");
    }

    /// <summary>
    /// Gets the singularity effect multiplier for damage dealt by enemies
    /// </summary>
    float GetDamageDealtMultiplier()
    {
        if (SingularityEffectManager.Instance == null) return 1f;
        return SingularityEffectManager.Instance.GetEffectMultiplier(SingularityEffectType.EnemyDamageDealt, enemyType);
    }

    void HandleArrived()
    {
        if (_arrived) return;
        _arrived = true;

        try
        {
            onArrived?.Invoke();
        }
        catch (Exception) { /* ignore user event exceptions */ }

        try
        {
            if (_arrivedCallback != null)
            {
                _arrivedCallback.Invoke(this);
            }
            else
            {
                SpawnerManager.Instance?.OnEnemyArrived(this);
            }
        }
        catch (Exception) { /* ignore callbacks if none registered */ }
    }

    void InitializeHealthBar()
    {
        if (!showHealthBar) return;

        _healthBar = gameObject.AddComponent<HealthBar>();
        _healthBar.SetOffset(healthBarOffset);
        _healthBar.SetSize(healthBarSize);
        _healthBar.hideWhenFull = false; // Always show for enemies
        _healthBar.alwaysShow = true;
        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (_healthBar != null && showHealthBar)
        {
            _healthBar.UpdateHealth(_currentHealth, maxHealth);
        }
    }
}




