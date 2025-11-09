using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DroneFactory attack behavior - spawns and manages drones instead of directly attacking.
/// Boosters multiply the maximum number of drones (e.g., 1.5x multiplier = 50% more drones).
/// </summary>
public class DroneFactoryAttackBehavior : BuildingAttackBehavior
{
    [Header("Drone Settings")]
    public GameObject dronePrefab;
    public int baseDroneLimit = 5;
    public float droneSpawnOffset = 1f;

    private List<Drone> _activeDrones = new List<Drone>();

    /// <summary>
    /// Gets the effective maximum number of drones based on nearby boosts.
    /// Boosters multiply the drone limit (e.g., 1.5x multiplier = 50% more drones).
    /// </summary>
    public int EffectiveDroneLimit
    {
        get
        {
            float boostMultiplier = CalculateDroneBoostMultiplier();
            return Mathf.RoundToInt(baseDroneLimit * boostMultiplier);
        }
    }

    /// <summary>
    /// Calculates the boost multiplier for drone limit from nearby BoostBuildings.
    /// Uses the same stacking logic as damage boosts (additive stacking).
    /// </summary>
    private float CalculateDroneBoostMultiplier()
    {
        if (BuildingManager.Instance == null) return 1f;

        float additiveBoost = 0f;

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
            }
        }

        return 1f + additiveBoost;
    }

    protected override void Start()
    {
        base.Start();

        // Register with DroneManager
        DroneManager.Instance?.RegisterFactory(this);
    }

    void OnDestroy()
    {
        // Unregister from DroneManager
        DroneManager.Instance?.UnregisterFactory(this);

        // Notify all active drones that their factory is destroyed
        foreach (var drone in _activeDrones.ToArray())
        {
            if (drone != null)
            {
                drone.OnFactoryDestroyed();
            }
        }
        _activeDrones.Clear();
    }

    protected override void PerformAttack(Enemy target)
    {
        // DroneFactory doesn't attack directly - it spawns drones
        // This method is called when there's an enemy in range and we're off cooldown

        // Check if we can spawn a new drone
        if (CanSpawnDrone())
        {
            SpawnDrone();
        }
    }

    bool CanSpawnDrone()
    {
        // Clean up null references
        _activeDrones.RemoveAll(d => d == null);

        // Check if we're below the drone limit
        return _activeDrones.Count < EffectiveDroneLimit;
    }

    void SpawnDrone()
    {
        if (dronePrefab == null)
        {
            Debug.LogWarning("DroneFactory: No drone prefab assigned!");
            return;
        }

        // Calculate spawn position (offset from factory)
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        Vector3 spawnOffset = new Vector3(randomDirection.x, randomDirection.y, 0) * droneSpawnOffset;
        Vector3 spawnPosition = transform.position + spawnOffset;

        // Create drone
        GameObject droneObj = Instantiate(dronePrefab, spawnPosition, Quaternion.identity);
        Drone drone = droneObj.GetComponent<Drone>();

        if (drone == null)
        {
            drone = droneObj.AddComponent<Drone>();
        }

        drone.Initialize(this);
        _activeDrones.Add(drone);

        // Register with DroneManager
        DroneManager.Instance?.RegisterDrone(drone);

        // Play spawn sound
        AudioManager.Instance?.PlaySFX("place_building");

        Debug.Log($"DroneFactory: Spawned drone ({_activeDrones.Count}/{EffectiveDroneLimit})");
    }

    /// <summary>
    /// Called when a drone is destroyed - removes it from the active list
    /// </summary>
    public void OnDroneDestroyed(Drone drone)
    {
        _activeDrones.Remove(drone);
        Debug.Log($"DroneFactory: Drone destroyed ({_activeDrones.Count}/{EffectiveDroneLimit})");
    }

    /// <summary>
    /// Gets the current number of active drones
    /// </summary>
    public int GetActiveDroneCount()
    {
        _activeDrones.RemoveAll(d => d == null);
        return _activeDrones.Count;
    }

    /// <summary>
    /// Gets all active drones for this factory
    /// </summary>
    public IReadOnlyList<Drone> GetActiveDrones()
    {
        _activeDrones.RemoveAll(d => d == null);
        return _activeDrones.AsReadOnly();
    }

    protected override void Update()
    {
        // Only spawn drones if building is placed and alive
        if (_building == null || !_building.IsPlaced || _building.IsDead)
            return;

        // Update cooldown
        if (_attackCooldown > 0)
        {
            _attackCooldown -= Time.deltaTime;
            return;
        }

        // Check if we should spawn a new drone
        if (CanSpawnDrone())
        {
            // During defense phase, always spawn drones up to the limit
            // During building phase, only spawn if there are enemies nearby
            bool shouldSpawn = false;

            if (GameManager.Instance != null && GameManager.Instance.CurrentPhase == GamePhase.Defense)
            {
                // Defense phase - spawn drones regardless of enemies
                shouldSpawn = true;
            }
            else
            {
                // Building phase - only spawn if enemies are nearby
                Enemy target = FindTarget();
                shouldSpawn = target != null;
            }

            if (shouldSpawn)
            {
                SpawnDrone();
                _attackCooldown = attackDelay;
            }
        }
    }
}
