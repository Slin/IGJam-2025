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
    public Enemy teleporterEnemyPrefab;
    public Enemy exploderEnemyPrefab;

    [Header("Spawner Movement")]
    public float newSpawnerCircleRadius = 18.0f;
    public float defaultSpawnRadius = 8f;

    EnemySpawner _activeSpawner;
    int _currentRound;
    int _remainingInRound;
    List<Enemy> _activeEnemies = new List<Enemy>();
    List<EnemySpawner> _roundSpawners = new List<EnemySpawner>();
    List<Vector3> _currentSpawnPositions = new List<Vector3>();

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

        ClearRoundSpawners();

        // Clear enemies
        ClearAllEnemies();

        // Create initial spawner for first building phase
        CreateNewSpawner();
        // Prepare visible spawners for the upcoming first defense
        SetupUpcomingRoundSpawners();
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

        // Randomize Z rotation for visual variety
        Quaternion rot = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        var spawner = Instantiate(spawnerPrefab, position, rot);
        return spawner;
    }

    public void StartRound(int roundMoneyValue, int roundNumber)
    {
        _currentRound = roundNumber;
        _activeEnemies.Clear();
        _currentSpawnPositions.Clear();
        ClearRoundSpawners();

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

        // Ensure we have spawn positions prepared (typically set during building phase)
        if (_currentSpawnPositions == null || _currentSpawnPositions.Count == 0)
        {
            int spawnPositions = GetSpawnPositionCountForRound(_currentRound);
            GenerateSpawnPositions(spawnPositions);
            InstantiateRoundSpawners();
        }

        // Now spawn all the enemies (split across positions)
        _remainingInRound = enemiesToSpawn.Count;
        Debug.Log($"SpawnerManager: Spawning {_remainingInRound} enemies across {Mathf.Max(1, _currentSpawnPositions.Count)} positions (including {bossCount} bosses)");

        int posCount = Mathf.Max(1, _currentSpawnPositions.Count);
        int total = enemiesToSpawn.Count;
        int basePer = total / posCount;
        int rem = total % posCount;
        int idx = 0;
        for (int p = 0; p < posCount; p++)
        {
            int countForP = basePer + (p < rem ? 1 : 0);
            Vector3 basePos = _currentSpawnPositions[p];
            float spawnRadius = GetSpawnRadiusForPositionIndex(p);
            for (int j = 0; j < countForP && idx < total; j++, idx++)
            {
                var type = enemiesToSpawn[idx];
                SpawnEnemyOfTypeAt(type, basePos, spawnRadius);
            }
        }
    }

    void GenerateSpawnPositions(int count)
    {
        _currentSpawnPositions.Clear();
        float radius = newSpawnerCircleRadius;
        // Use active spawner angle as base so the first position matches the visible spawner
        float baseAngle;
        if (_activeSpawner != null)
        {
            baseAngle = Mathf.Atan2(_activeSpawner.transform.position.y, _activeSpawner.transform.position.x) * Mathf.Rad2Deg;
        }
        else
        {
            baseAngle = Random.Range(0f, 360f);
        }
        for (int i = 0; i < count; i++)
        {
            float angle = baseAngle + (360f / count) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 pos = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f);
            _currentSpawnPositions.Add(pos);
        }
    }

    void SetupUpcomingRoundSpawners()
    {
        int completed = PlayerStatsManager.Instance != null ? PlayerStatsManager.Instance.CurrentRound : 0;
        int upcomingRound = completed + 1;
        int spawnPositions = GetSpawnPositionCountForRound(upcomingRound);
        GenerateSpawnPositions(spawnPositions);
        InstantiateRoundSpawners();
    }

    int GetSpawnPositionCountForRound(int round)
    {
        // Rounds 1-5 => 1, 6-10 => 2, 11-15 => 3, ...
        return Mathf.Max(1, Mathf.FloorToInt((round - 1) / 5f) + 1);
    }

    void InstantiateRoundSpawners()
    {
        // Ensure we have an active spawner at position 0; create extras for the rest for visibility
        for (int i = 0; i < _currentSpawnPositions.Count; i++)
        {
            if (i == 0)
            {
                // Move the active spawner to the first position if it exists
                if (_activeSpawner != null)
                {
                    _activeSpawner.transform.position = _currentSpawnPositions[0];
                    _activeSpawner.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                }
                else
                {
                    _activeSpawner = InstantiateSpawner(_currentSpawnPositions[0]);
                }
            }
            else
            {
                var sp = InstantiateSpawner(_currentSpawnPositions[i]);
                if (sp != null)
                {
                    _roundSpawners.Add(sp);
                }
            }
        }
    }

    float GetSpawnRadiusForPositionIndex(int index)
    {
        if (index == 0 && _activeSpawner != null) return _activeSpawner.spawnRadius;
        int extraIndex = index - 1;
        if (extraIndex >= 0 && extraIndex < _roundSpawners.Count && _roundSpawners[extraIndex] != null)
            return _roundSpawners[extraIndex].spawnRadius;
        return defaultSpawnRadius;
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

        if (GameManager.Instance?.ShouldSpawnEnemyType(EnemyType.Teleporter, round) ?? false)
            types.Add(EnemyType.Teleporter);

        if (GameManager.Instance?.ShouldSpawnEnemyType(EnemyType.Exploder, round) ?? false)
            types.Add(EnemyType.Exploder);

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
        // Backwards compatible spawn at active spawner
        Vector3 basePos = _activeSpawner != null ? _activeSpawner.transform.position : Vector3.zero;
        float spawnRadius = _activeSpawner != null ? _activeSpawner.spawnRadius : defaultSpawnRadius;
        SpawnEnemyOfTypeAt(type, basePos, spawnRadius);
    }

    void SpawnEnemyOfTypeAt(EnemyType type, Vector3 basePos, float spawnRadius)
    {
        Enemy prefab = GetEnemyPrefab(type);
        if (prefab == null)
        {
            Debug.LogWarning($"SpawnerManager: No prefab found for enemy type {type}");
            return;
        }

        // Position at provided base position
        Vector3 spawnPos = basePos;
        Vector2 randomOffset = Random.insideUnitCircle.normalized * Random.Range(0f, spawnRadius);
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
            AudioManager.Instance?.PlaySFX("laser_enemy");
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
            EnemyType.Teleporter => teleporterEnemyPrefab,
            EnemyType.Exploder => exploderEnemyPrefab,
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

        ClearRoundSpawners();

        float angle = Random.Range(0f, Mathf.PI * 2f);
        Vector3 position = new Vector3(Mathf.Cos(angle) * newSpawnerCircleRadius,
            Mathf.Sin(angle) * newSpawnerCircleRadius, 0f);
        _activeSpawner = InstantiateSpawner(position);
        // Prepare and display spawners for the next defense during building
        SetupUpcomingRoundSpawners();
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

    public System.Collections.Generic.IReadOnlyList<Enemy> GetActiveEnemiesSnapshot()
    {
        _activeEnemies.RemoveAll(e => e == null);
        return _activeEnemies;
    }

    public System.Collections.Generic.IReadOnlyList<Vector3> GetCurrentSpawnPositions()
    {
        return _currentSpawnPositions;
    }

    void ClearRoundSpawners()
    {
        if (_roundSpawners == null || _roundSpawners.Count == 0) return;
        for (int i = 0; i < _roundSpawners.Count; i++)
        {
            var sp = _roundSpawners[i];
            if (sp != null)
            {
                Destroy(sp.gameObject);
            }
        }
        _roundSpawners.Clear();
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
