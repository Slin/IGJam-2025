using UnityEngine;

/// <summary>
/// Teleporter enemy behavior - teleports short distances towards its target,
/// and teleports back when taking damage from buildings.
/// This component should be added to an Enemy with EnemyType.Teleporter.
/// </summary>
public class TeleporterEnemyBehavior : MonoBehaviour
{
    [Header("Teleport Settings")]
    public float teleportDistance = 3f; // Distance to teleport each time
    public float forwardTeleportInterval = 2f; // Time between forward teleports
    public float backTeleportDistance = 4f; // Distance to teleport back when damaged
    public float teleportCooldown = 0.5f; // Cooldown after any teleport

    Enemy _enemy;
    float _forwardTeleportTimer;
    float _teleportCooldownTimer;
    Vector3 _lastPosition;

    void Awake()
    {
        _enemy = GetComponent<Enemy>();
        if (_enemy != null)
        {
            // Subscribe to damage events
            _enemy.onDamaged.AddListener(OnDamaged);
        }
        _lastPosition = transform.position;
    }

    void Start()
    {
        _forwardTeleportTimer = forwardTeleportInterval;
    }

    void Update()
    {
        if (_enemy == null || _enemy.IsDead) return;

        // Update cooldown timer
        if (_teleportCooldownTimer > 0)
        {
            _teleportCooldownTimer -= Time.deltaTime;
            return; // Don't teleport while on cooldown
        }

        // Update forward teleport timer
        _forwardTeleportTimer -= Time.deltaTime;
        if (_forwardTeleportTimer <= 0)
        {
            _forwardTeleportTimer = forwardTeleportInterval;
            TeleportTowardsTarget();
        }
    }

    void OnDamaged(float currentHealth)
    {
        // Check if damage came from a building by checking if we're near any buildings
        if (BuildingManager.Instance == null) return;

        bool nearBuilding = false;
        foreach (var building in BuildingManager.Instance.AllBuildings)
        {
            if (building == null || building.IsDead) continue;

            // Check if building is in attack range (likely attacking us)
            var attackBehavior = building.GetComponent<BuildingAttackBehavior>();
            if (attackBehavior != null)
            {
                float distance = Vector3.Distance(transform.position, building.transform.position);
                if (distance <= attackBehavior.attackRange + 1f) // Add margin
                {
                    nearBuilding = true;
                    break;
                }
            }
        }

        // Teleport back if near a building
        if (nearBuilding && _teleportCooldownTimer <= 0)
        {
            TeleportAwayFromTarget();
        }
    }

    void TeleportTowardsTarget()
    {
        if (_enemy == null || _enemy.IsDead) return;

        // Find closest target (base or radar jammer)
        Vector3 targetPos = _enemy.targetPosition;
        Vector3 currentPos = transform.position;
        Vector3 direction = (targetPos - currentPos).normalized;

        // Calculate teleport position
        Vector3 newPosition = currentPos + direction * teleportDistance;

        // Teleport
        transform.position = newPosition;
        _teleportCooldownTimer = teleportCooldown;

        // Visual/audio effect
        AudioManager.Instance?.PlaySFX("teleport");
    }

    void TeleportAwayFromTarget()
    {
        if (_enemy == null || _enemy.IsDead) return;

        // Teleport away from target
        Vector3 targetPos = _enemy.targetPosition;
        Vector3 currentPos = transform.position;
        Vector3 direction = (currentPos - targetPos).normalized; // Away from target

        // Calculate teleport position
        Vector3 newPosition = currentPos + direction * backTeleportDistance;

        // Teleport
        transform.position = newPosition;
        _teleportCooldownTimer = teleportCooldown;

        // Reset forward timer to prevent immediate forward teleport
        _forwardTeleportTimer = forwardTeleportInterval;

        // Visual/audio effect
        AudioManager.Instance?.PlaySFX("teleport");
    }

    void OnDestroy()
    {
        if (_enemy != null)
        {
            _enemy.onDamaged.RemoveListener(OnDamaged);
        }
    }
}
