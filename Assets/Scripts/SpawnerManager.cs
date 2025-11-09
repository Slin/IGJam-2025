using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class SpawnerManager : MonoBehaviour
{
    public static SpawnerManager Instance { get; private set; }

    [Header("Prefabs")]
    public EnemySpawner spawnerPrefab;
    public Enemy regularEnemyPrefab;
    public Enemy fastEnemyPrefab;
    public Enemy armoredEnemyPrefab;
    public Enemy bossEnemyPrefab;
    public Enemy attackEnemyPrefab;

    [Header("Spawner Movement")]
    public float newSpawnerCircleRadius = 18.0f;

    EnemySpawner _activeSpawner;
    int _currentRound;
    int _remainingInRound;
    List<Enemy> _activeEnemies = new List<Enemy>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void InitializeNewGame()
    {
        // Clear any existing spawner
        if (_activeSpawner != null)
        {
            Destroy(_activeSpawner.gameObject);
            _activeSpawner = null;
        }

        // Clear enemies
        ClearAllEnemies();

        // Create initial spawner for first building phase
        CreateNewSpawner();
    }

    void Update()
    {
        // Debug: Press Space to start defense phase (can be removed once UI is ready)
        var kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame)
        {
            GameManager.Instance?.RequestStartDefensePhase();
        }
    }

    public GameObject InstantiateObject(GameObject prefab, Vector3 position)
    {
        if (prefab == null) return null;
        return Instantiate(prefab, position, Quaternion.identity);
    }

    public EnemySpawner InstantiateSpawner(Vector3 position)
    {
        if (spawnerPrefab == null)
        {
            Debug.LogWarning("SpawnerManager: No spawner prefab assigned.");
            return null;
        }

        var spawner = Instantiate(spawnerPrefab, position, Quaternion.identity);
        return spawner;
    }

    public void StartRound(int roundMoneyValue, int roundNumber)
    {
        _currentRound = roundNumber;
        _activeEnemies.Clear();

        // Create or reuse spawner
        if (_activeSpawner == null)
        {
            CreateNewSpawner();
        }

        if (_activeSpawner == null)
        {
            Debug.LogError("SpawnerManager: Failed to create spawner; cannot start round.");
            return;
        }

        Debug.Log($"SpawnerManager: Starting round {_currentRound} with money value {roundMoneyValue}");

        // Spawn enemies based on money value and bosses
        SpawnRoundEnemies(roundMoneyValue, roundNumber);
    }

    void SpawnRoundEnemies(int roundMoneyValue, int round)
    {
        List<EnemyType> enemiesToSpawn = new List<EnemyType>();

        // First, add bosses if this is a boss round (every 5th round)
        int bossCount = GameManager.Instance?.GetBossCountForRound(round) ?? 0;
        for (int i = 0; i < bossCount; i++)
        {
            enemiesToSpawn.Add(EnemyType.Boss);
        }

        // Get list of unlocked enemy types (excluding bosses which are handled separately)
        List<EnemyType> unlockedTypes = GetUnlockedEnemyTypes(round);

        // Spend the money value to buy enemies
        int remainingMoney = roundMoneyValue;
        int maxEnemies = GameManager.Instance?.GetMaxEnemiesForRound(round) ?? 50;

        while (remainingMoney > 0 && enemiesToSpawn.Count < maxEnemies)
        {
            // Try to pick a random enemy we can afford
            List<EnemyType> affordableTypes = new List<EnemyType>();

            foreach (var type in unlockedTypes)
            {
                int enemyCost = GetEnemyRewardValue(type);
                if (enemyCost <= remainingMoney)
                {
                    affordableTypes.Add(type);
                }
            }

            // If we can't afford any enemy, break
            if (affordableTypes.Count == 0)
                break;

            // Pick a random affordable enemy
            EnemyType selectedType = affordableTypes[Random.Range(0, affordableTypes.Count)];
            int cost = GetEnemyRewardValue(selectedType);

            enemiesToSpawn.Add(selectedType);
            remainingMoney -= cost;
        }

        // If we're at the enemy cap and still have money, replace cheaper enemies with stronger ones
        if (enemiesToSpawn.Count >= maxEnemies && remainingMoney > 0)
        {
            ReplaceWeakerEnemiesWithStronger(enemiesToSpawn, unlockedTypes, remainingMoney);
        }

        // Now spawn all the enemies
        _remainingInRound = enemiesToSpawn.Count;
        Debug.Log($"SpawnerManager: Spawning {_remainingInRound} enemies (including {bossCount} bosses)");

        foreach (var type in enemiesToSpawn)
        {
            SpawnEnemyOfType(type);
        }
    }

    List<EnemyType> GetUnlockedEnemyTypes(int round)
    {
        List<EnemyType> types = new List<EnemyType>();

        // Always have Regular
        types.Add(EnemyType.Regular);

        if (GameManager.Instance?.ShouldSpawnEnemyType(EnemyType.Fast, round) ?? false)
            types.Add(EnemyType.Fast);

        if (GameManager.Instance?.ShouldSpawnEnemyType(EnemyType.Armored, round) ?? false)
            types.Add(EnemyType.Armored);

        if (GameManager.Instance?.ShouldSpawnEnemyType(EnemyType.Attack, round) ?? false)
            types.Add(EnemyType.Attack);

        // Note: Boss is handled separately, not in the random pool

        return types;
    }

    int GetEnemyRewardValue(EnemyType type)
    {
        Enemy prefab = GetEnemyPrefab(type);
        if (prefab == null)
            return 10; // Default value

        return prefab.tritiumReward;
    }

    void ReplaceWeakerEnemiesWithStronger(List<EnemyType> enemies, List<EnemyType> unlockedTypes, int remainingMoney)
    {
        // Sort unlocked types by reward value (stronger = higher reward)
        List<EnemyType> sortedTypes = new List<EnemyType>(unlockedTypes);
        sortedTypes.Sort((a, b) => GetEnemyRewardValue(b).CompareTo(GetEnemyRewardValue(a)));

        if (sortedTypes.Count == 0)
            return;

        // Keep trying to upgrade while we have money
        while (remainingMoney > 0)
        {
            // Find the weakest non-boss enemy
            int weakestIndex = -1;
            int weakestValue = int.MaxValue;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == EnemyType.Boss)
                    continue;

                int value = GetEnemyRewardValue(enemies[i]);
                if (value < weakestValue)
                {
                    weakestValue = value;
                    weakestIndex = i;
                }
            }

            if (weakestIndex == -1)
                break; // No non-boss enemies to replace

            // Find a stronger enemy we can afford to upgrade to
            bool upgraded = false;
            foreach (var strongerType in sortedTypes)
            {
                int strongerValue = GetEnemyRewardValue(strongerType);
                int upgradeCost = strongerValue - weakestValue;

                if (upgradeCost > 0 && upgradeCost <= remainingMoney)
                {
                    // Do the upgrade
                    enemies[weakestIndex] = strongerType;
                    remainingMoney -= upgradeCost;
                    upgraded = true;
                    break;
                }
            }

            if (!upgraded)
                break; // Can't afford any more upgrades
        }
    }

    void SpawnEnemyOfType(EnemyType type)
    {
        Enemy prefab = GetEnemyPrefab(type);
        if (prefab == null)
        {
            Debug.LogWarning($"SpawnerManager: No prefab found for enemy type {type}");
            return;
        }

        // Position at spawner location
        Vector3 spawnPos = _activeSpawner.transform.position;
        Vector2 randomOffset = Random.insideUnitCircle.normalized * Random.Range(0f, _activeSpawner.spawnRadius);
        spawnPos += new Vector3(randomOffset.x, randomOffset.y, 0f);

        Enemy enemy = Instantiate(prefab, spawnPos, Quaternion.identity);

        // Initialize enemy to move towards the closest base from spawn position
        Vector3 targetPos = Vector3.zero;
        var closestBase = BuildingManager.Instance?.GetNearestBuilding(spawnPos, BuildingType.Base);
        if (closestBase != null)
        {
            targetPos = closestBase.transform.position;
        }

        enemy.Initialize(targetPos, OnEnemyArrived);
        _activeEnemies.Add(enemy);
    }

    void CreateNewSpawner()
    {
        if (_activeSpawner != null)
        {
            Destroy(_activeSpawner.gameObject);
        }

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 position = new Vector3(Mathf.Cos(angle) * newSpawnerCircleRadius,
            Mathf.Sin(angle) * newSpawnerCircleRadius, 0f);

        _activeSpawner = InstantiateSpawner(position);
        Debug.Log("New spawner created");
    }

    public void OnEnemyArrived(Enemy enemy)
    {
        if (enemy == null) return;

        _activeEnemies.Remove(enemy);

        // Enemy reached a base - deal damage to the closest base
        var closestBase = BuildingManager.Instance?.GetNearestBuilding(enemy.transform.position, BuildingType.Base);
        if (closestBase != null && !closestBase.IsDead)
        {
            closestBase.TakeDamage(enemy.baseDamage);
            AudioManager.Instance?.PlaySFX("enemy_attack");
        }

        // Destroy enemy
        Destroy(enemy.gameObject);

        _remainingInRound = Mathf.Max(0, _remainingInRound - 1);

        // Check if round is complete
        if (_remainingInRound == 0 && _activeEnemies.Count == 0)
        {
            Debug.Log($"SpawnerManager: Round {_currentRound} complete");
            GameManager.Instance?.OnRoundDefeated();
        }
    }

    public void OnEnemyKilled(Enemy enemy)
    {
        if (enemy == null) return;

        _activeEnemies.Remove(enemy);
        _remainingInRound = Mathf.Max(0, _remainingInRound - 1);

        // Enemy is already destroyed by Enemy.HandleDeath()

        // Check if round is complete
        if (_remainingInRound == 0 && _activeEnemies.Count == 0)
        {
            Debug.Log($"SpawnerManager: Round {_currentRound} complete (all enemies killed)");
            GameManager.Instance?.OnRoundDefeated();
        }
    }

    public void ClearAllEnemies()
    {
        foreach (var enemy in _activeEnemies.ToArray())
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }

        _activeEnemies.Clear();
        _remainingInRound = 0;
    }

    Enemy GetEnemyPrefab(EnemyType type)
    {
        return type switch
        {
            EnemyType.Regular => regularEnemyPrefab,
            EnemyType.Fast => fastEnemyPrefab,
            EnemyType.Armored => armoredEnemyPrefab,
            EnemyType.Boss => bossEnemyPrefab,
            EnemyType.Attack => attackEnemyPrefab,
            _ => null
        };
    }

    public void EndRound()
    {
        if (_activeSpawner != null)
        {
            _activeSpawner.StopAllCoroutines();
            Destroy(_activeSpawner.gameObject);
            _activeSpawner = null;
        }

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 position = new Vector3(Mathf.Cos(angle) * newSpawnerCircleRadius,
            Mathf.Sin(angle) * newSpawnerCircleRadius, 0f);
        _activeSpawner = InstantiateSpawner(position);
    }

    public Enemy GetClosestEnemy(Vector3 position, float maxRange = Mathf.Infinity)
    {
        Enemy closest = null;
        float bestSqr = maxRange * maxRange;
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            var e = _activeEnemies[i];
            if (e == null)
            {
                _activeEnemies.RemoveAt(i);
                continue;
            }
            float d2 = (e.transform.position - position).sqrMagnitude;
            if (d2 < bestSqr)
            {
                bestSqr = d2;
                closest = e;
            }
        }
        return closest;
    }

    /// <summary>
    /// Gets the closest building to the specified position, excluding the center base.
    /// </summary>
    /// <param name="position">Position to check from</param>
    /// <param name="maxRange">Maximum range to search within</param>
    /// <returns>Closest non-center-base building, or null if none found</returns>
    public Building GetClosestBuildingExcludingCenterBase(Vector3 position, float maxRange = Mathf.Infinity)
    {
        if (BuildingManager.Instance == null) return null;

        Building closestBuilding = null;
        float closestDistSqr = maxRange * maxRange;

        foreach (var building in BuildingManager.Instance.AllBuildings)
        {
            if (building == null || building.IsDead) continue;

            // Skip bases that are at the center (or very close to center)
            if (building.buildingType == BuildingType.Base && building.transform.position.sqrMagnitude < 0.1f)
                continue;

            float distSqr = (building.transform.position - position).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                closestBuilding = building;
            }
        }

        return closestBuilding;
    }
}
