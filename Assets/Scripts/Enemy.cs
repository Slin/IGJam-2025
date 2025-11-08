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
        UpdateHealthBar();
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

        Destroy(gameObject);
    }

    void Update()
    {
        if (_arrived) return;

        // Check for nearby buildings to attack (excluding center base)
        Building nearbyBuilding = SpawnerManager.Instance?.GetClosestBuildingExcludingCenterBase(transform.position, attackRange);

        if (nearbyBuilding != null)
        {
            // Attack the building
            AttackBuilding(nearbyBuilding);

            // If stopToAttack is true, don't move this frame
            if (stopToAttack)
            {
                return;
            }
        }

        // Continue moving towards target
        Vector3 current = transform.position;
        float step = moveSpeed * Time.deltaTime;
        Vector3 next = Vector3.MoveTowards(current, targetPosition, step);
        transform.position = next;

        if ((next - targetPosition).sqrMagnitude <= (arrivalDistance * arrivalDistance))
        {
            HandleArrived();
        }
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




