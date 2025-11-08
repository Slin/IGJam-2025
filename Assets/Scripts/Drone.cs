using UnityEngine;

/// <summary>
/// Drone unit that acts like an enemy but fights for the player.
/// Spawned by DroneFactory buildings, attacks enemies, and can be targeted by enemies.
/// </summary>
[DisallowMultipleComponent]
public class Drone : MonoBehaviour
{
    [Header("Drone Properties")]
    public float maxHealth = 50f;
    public float moveSpeed = 3f;
    public float detectionRange = 15f; // How far the drone can detect enemies
    public float factoryReturnDistance = 5f; // How close to stay to factory when idle

    [Header("Attack Settings")]
    public float attackRange = 8f;
    public float attackDamage = 10f;
    public float attackDelay = 0.5f;
    public Color laserColor = Color.green;
    public float laserWidth = 0.08f;
    public float laserDuration = 0.2f;

    [Header("Health Bar")]
    public bool showHealthBar = true;
    public Vector3 healthBarOffset = new Vector3(0, 0.6f, 0);
    public Vector2 healthBarSize = new Vector2(0.6f, 0.1f);

    private DroneFactoryAttackBehavior _factory;
    private float _currentHealth;
    private float _attackCooldown;
    private Enemy _currentTarget;
    private HealthBar _healthBar;

    public float CurrentHealth => _currentHealth;
    public bool IsDead => _currentHealth <= 0;
    public Vector3 Position => transform.position;
    public DroneFactoryAttackBehavior Factory => _factory;

    void Awake()
    {
        _currentHealth = maxHealth;
        InitializeHealthBar();
    }

    public void Initialize(DroneFactoryAttackBehavior factory)
    {
        _factory = factory;
        _currentHealth = maxHealth;
        _attackCooldown = 0f;
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
        // Notify factory that this drone is destroyed
        if (_factory != null)
        {
            _factory.OnDroneDestroyed(this);
        }

        // Notify DroneManager
        DroneManager.Instance?.OnDroneDestroyed(this);

        AudioManager.Instance?.PlaySFX("enemy_death");
        Destroy(gameObject);
    }

    void Update()
    {
        if (IsDead) return;

        // Update attack cooldown
        if (_attackCooldown > 0)
        {
            _attackCooldown -= Time.deltaTime;
        }

        // Find closest enemy within detection range
        Enemy nearestEnemy = SpawnerManager.Instance?.GetClosestEnemy(transform.position, detectionRange);

        if (nearestEnemy != null && !nearestEnemy.IsDead)
        {
            _currentTarget = nearestEnemy;
            HandleCombat();
        }
        else
        {
            _currentTarget = null;
            ReturnToFactory();
        }
    }

    void HandleCombat()
    {
        if (_currentTarget == null || _currentTarget.IsDead)
        {
            _currentTarget = null;
            return;
        }

        float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);

        // Move towards enemy if not in attack range
        if (distanceToTarget > attackRange)
        {
            Vector3 direction = (_currentTarget.transform.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
        else
        {
            // In range - attack
            if (_attackCooldown <= 0)
            {
                PerformAttack();
                _attackCooldown = attackDelay;
            }
        }
    }

    void PerformAttack()
    {
        if (_currentTarget == null || _currentTarget.IsDead) return;

        // Deal damage
        _currentTarget.TakeDamage(attackDamage);

        // Create laser visual effect
        LaserBeam.Create(transform.position, _currentTarget.transform.position, laserColor, laserWidth, laserDuration);

        // Play attack sound
        AudioManager.Instance?.PlaySFX("laser_fire");
    }

    void ReturnToFactory()
    {
        // If no factory, stay in place
        if (_factory == null) return;

        Vector3 factoryPos = _factory.transform.position;
        float distanceToFactory = Vector3.Distance(transform.position, factoryPos);

        // Move back towards factory if too far
        if (distanceToFactory > factoryReturnDistance)
        {
            Vector3 direction = (factoryPos - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }

    void InitializeHealthBar()
    {
        if (!showHealthBar) return;

        _healthBar = gameObject.AddComponent<HealthBar>();
        _healthBar.SetOffset(healthBarOffset);
        _healthBar.SetSize(healthBarSize);
        _healthBar.hideWhenFull = true;
        _healthBar.alwaysShow = false;
        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (_healthBar != null && showHealthBar)
        {
            _healthBar.UpdateHealth(_currentHealth, maxHealth);
        }
    }

    /// <summary>
    /// Called when the factory is destroyed - drones continue to exist
    /// </summary>
    public void OnFactoryDestroyed()
    {
        _factory = null;
        // Drone continues to operate independently
    }
}
