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

    [Header("Spawner Movement")] public float newSpawnerCircleRadius = 18.0f;

    [Header("Enemy Type Distribution")] [Range(0f, 1f)]
    public float fastEnemyChance = 0.2f;

    [Range(0f, 1f)] public float armoredEnemyChance = 0.15f;
    [Range(0f, 1f)] public float bossEnemyChance = 0.05f;

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

    public void StartRound(int enemyCount, int roundNumber)
    {
        _currentRound = roundNumber;
        _remainingInRound = enemyCount;
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

        Debug.Log($"SpawnerManager: Starting round {_currentRound} with {_remainingInRound} enemies");

        // Spawn enemies with variety based on round
        SpawnRoundEnemies(enemyCount, roundNumber);
    }

    void SpawnRoundEnemies(int count, int round)
    {
        for (int i = 0; i < count; i++)
        {
            EnemyType type = SelectEnemyType(round);
            SpawnEnemyOfType(type);
        }
    }

    EnemyType SelectEnemyType(int round)
    {
        // Determine which enemy types are available this round
        bool canSpawnFast = GameManager.Instance?.ShouldSpawnEnemyType(EnemyType.Fast, round) ?? false;
        bool canSpawnArmored = GameManager.Instance?.ShouldSpawnEnemyType(EnemyType.Armored, round) ?? false;
        bool canSpawnBoss = GameManager.Instance?.ShouldSpawnEnemyType(EnemyType.Boss, round) ?? false;

        // Roll for special enemy types
        float roll = Random.value;

        if (canSpawnBoss && roll < bossEnemyChance)
            return EnemyType.Boss;

        roll = Random.value;
        if (canSpawnArmored && roll < armoredEnemyChance)
            return EnemyType.Armored;

        roll = Random.value;
        if (canSpawnFast && roll < fastEnemyChance)
            return EnemyType.Fast;

        return EnemyType.Regular;
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

        // Initialize enemy to move towards base (center)
        Vector3 targetPos = Vector3.zero;
        var mainBase = BuildingManager.Instance?.GetNearestBuilding(Vector3.zero, BuildingType.Base);
        if (mainBase != null)
        {
            targetPos = mainBase.transform.position;
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

        // Enemy reached the base - deal damage
        var targetBuilding = BuildingManager.Instance?.GetNearestBuilding(enemy.transform.position, BuildingType.Base);
        if (targetBuilding != null && !targetBuilding.IsDead)
        {
            targetBuilding.TakeDamage(enemy.baseDamage);
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
		for(int i = _activeEnemies.Count - 1; i >= 0; i--)
		{
			var e = _activeEnemies[i];
			if(e == null)
			{
				_activeEnemies.RemoveAt(i);
				continue;
			}
			float d2 = (e.transform.position - position).sqrMagnitude;
			if(d2 < bestSqr)
			{
				bestSqr = d2;
				closest = e;
			}
		}
		return closest;
	}
}
