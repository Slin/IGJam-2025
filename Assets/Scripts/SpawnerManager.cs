using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class SpawnerManager : MonoBehaviour
{
    public static SpawnerManager Instance { get; private set; }

    [Header("Prefabs")]
    public EnemySpawner spawnerPrefab;

    [Header("Spawner Movement")]
    public float newSpawnerCircleRadius = 18.0f;

    [Header("Rounds")]
    public int enemiesPerRound = 5;

    EnemySpawner _activeSpawner;
    int _currentRound;
    int _remainingInRound;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EndRound();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame)
        {
            if (_remainingInRound <= 0)
            {
                StartRound(enemiesPerRound);
            }
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

    public void StartRound(int enemyCount)
    {
        if (_activeSpawner == null)
        {
            _activeSpawner = InstantiateSpawner(Vector3.zero);
            if (_activeSpawner == null)
            {
                Debug.LogWarning("SpawnerManager: Failed to create spawner; cannot start round.");
                return;
            }
        }

        _currentRound++;
        _remainingInRound = Mathf.Max(0, enemyCount);
        _activeSpawner.SpawnEnemiesSequential(_remainingInRound, null, OnEnemyArrived);
    }

    public void OnEnemyArrived(Enemy enemy)
    {
        if (enemy != null)
        {
            Destroy(enemy.gameObject);
        }
        _remainingInRound = Mathf.Max(0, _remainingInRound - 1);

        if (_remainingInRound == 0)
        {
            // Round complete. Hook your next-round logic here if desired.
            // Debug.Log($"Round {_currentRound} complete.");
        }
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
        Vector3 position = new Vector3(Mathf.Cos(angle) * newSpawnerCircleRadius, Mathf.Sin(angle) * newSpawnerCircleRadius, 0f);
        _activeSpawner = InstantiateSpawner(position);
    }
}


