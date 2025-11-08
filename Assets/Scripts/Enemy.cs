using System;
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

    [Header("Separation Settings")]
    public float separationRadius = 0.75f; // How close before pushing away
    public float separationForce = 1f; // Strength of separation

    [Header("Events")]
    public UnityEvent onArrived;
    public UnityEvent onDeath;

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

    public float CurrentHealth => _currentHealth;
    public bool IsDead => _currentHealth <= 0;

    void Awake()
    {
        _currentHealth = maxHealth;
        InitializeHealthBar();
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

        _currentHealth -= damage;
        _currentHealth = Mathf.Max(0, _currentHealth);

        UpdateHealthBar();

        if (IsDead)
        {
            HandleDeath();
        }
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

        // Find closest targets of each type
        Drone nearbyDrone = DroneManager.Instance?.GetClosestDrone(transform.position, attackRange);
        Building nearbyBuilding = SpawnerManager.Instance?.GetClosestBuildingExcludingCenterBase(transform.position, attackRange);

        // Debug logging
        if (nearbyDrone != null)
        {
            Debug.Log($"Enemy found nearby drone at distance {Vector3.Distance(transform.position, nearbyDrone.Position)}");
        }

        // Determine which target to attack (prioritize closer one)
        bool shouldAttack = false;

        if (nearbyDrone != null && nearbyBuilding != null)
        {
            // Both available - attack the closer one
            float droneDist = Vector3.Distance(transform.position, nearbyDrone.Position);
            float buildingDist = Vector3.Distance(transform.position, nearbyBuilding.transform.position);

            if (droneDist <= buildingDist)
            {
                AttackDrone(nearbyDrone);
                shouldAttack = true;
            }
            else
            {
                AttackBuilding(nearbyBuilding);
                shouldAttack = true;
            }
        }
        else if (nearbyDrone != null)
        {
            // Only drone available
            AttackDrone(nearbyDrone);
            shouldAttack = true;
        }
        else if (nearbyBuilding != null)
        {
            // Only building available
            AttackBuilding(nearbyBuilding);
            shouldAttack = true;
        }

        // If stopToAttack is true and we're attacking, don't move this frame
        if (shouldAttack && stopToAttack)
        {
            return;
        }

        // Continue moving towards target with separation
        Vector3 current = transform.position;
        Vector3 direction = (targetPosition - current).normalized;
        Vector3 separationOffset = CalculateSeparation();

        // Combine movement direction with separation smoothly
        Vector3 combinedDirection = (direction + separationOffset * 0.3f).normalized;
        float step = moveSpeed * Time.deltaTime;
        Vector3 next = current + combinedDirection * step;

        transform.position = next;

        if ((next - targetPosition).sqrMagnitude <= (arrivalDistance * arrivalDistance))
        {
            HandleArrived();
        }
    }

    Vector3 CalculateSeparation()
    {
        Vector3 separation = Vector3.zero;
        int neighborCount = 0;

        // Check separation from other enemies
        if (SpawnerManager.Instance != null)
        {
            // Get all active enemies (we need a method for this)
            var allEnemies = FindObjectsOfType<Enemy>();
            foreach (var otherEnemy in allEnemies)
            {
                if (otherEnemy == null || otherEnemy == this || otherEnemy.IsDead) continue;

                Vector3 offset = transform.position - otherEnemy.transform.position;
                float distance = offset.magnitude;

                if (distance < separationRadius && distance > 0.01f)
                {
                    // Push away with smooth falloff
                    float strength = 1.0f - (distance / separationRadius);
                    separation += offset.normalized * strength * separationForce;
                    neighborCount++;
                }
            }
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

        // Update cooldown
        _attackCooldown -= Time.deltaTime;

        if (_attackCooldown <= 0)
        {
            // Perform attack
            building.TakeDamage(attackDamage);

            // Create laser visual effect
            LaserBeam.Create(transform.position, building.transform.position, Color.red, 0.1f, 0.2f);

            // Play attack sound
            AudioManager.Instance?.PlaySFX("enemy_attack");

            // Reset cooldown
            _attackCooldown = attackDelay;
        }
    }

    void AttackDrone(Drone drone)
    {
        if (drone == null || drone.IsDead) return;

        // Update cooldown
        _attackCooldown -= Time.deltaTime;

        if (_attackCooldown <= 0)
        {
            // Perform attack
            drone.TakeDamage(attackDamage);

            // Create laser visual effect
            LaserBeam.Create(transform.position, drone.transform.position, Color.red, 0.1f, 0.2f);

            // Play attack sound
            AudioManager.Instance?.PlaySFX("enemy_attack");

            // Reset cooldown
            _attackCooldown = attackDelay;
        }
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




