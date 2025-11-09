using UnityEngine;

/// <summary>
/// Radar jammer behavior - acts like a base and lures enemies towards it.
/// This component should be added to a building with BuildingType.RadarJammer.
/// </summary>
public class RadarJammerBehavior : MonoBehaviour
{
    [Header("Radar Jammer Settings")]
    public float lureRange = 20f; // Range within which enemies are lured
    public float updateInterval = 0.5f; // How often to update enemy targets

    Building _building;
    float _updateTimer;

    void Awake()
    {
        _building = GetComponent<Building>();
    }

    void Update()
    {
        // Only work if building is placed and alive
        if (_building == null || !_building.IsPlaced || _building.IsDead)
            return;

        _updateTimer -= Time.deltaTime;
        if (_updateTimer <= 0)
        {
            _updateTimer = updateInterval;
            LureNearbyEnemies();
        }
    }

    void LureNearbyEnemies()
    {
        if (SpawnerManager.Instance == null) return;

        var enemies = SpawnerManager.Instance.GetActiveEnemiesSnapshot();
        if (enemies == null) return;

        Vector3 myPosition = transform.position;

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            float distance = Vector3.Distance(myPosition, enemy.transform.position);
            if (distance <= lureRange)
            {
                // Set this radar jammer as the enemy's target
                enemy.targetPosition = myPosition;
            }
        }
    }
}
