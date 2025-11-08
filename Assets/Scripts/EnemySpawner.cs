using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class EnemySpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public Enemy enemyPrefab;

    [Header("Spawn Settings")]
    public float spawnRadius = 8f;
    public float minSpawnDelay = 0.8f;
    public float maxSpawnDelay = 2.0f;

    public void SetEnemyPrefab(Enemy prefab)
    {
        enemyPrefab = prefab;
    }

    public Enemy SpawnEnemy(Enemy prefabOverride = null, Action<Enemy> onArrived = null)
    {
        var prefab = prefabOverride != null ? prefabOverride : enemyPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("EnemySpawner: No enemy prefab assigned.");
            return null;
        }

        Vector3 position = GetRandomSpawnPosition();
        Enemy enemy = Instantiate(prefab, position, Quaternion.identity);
        enemy.Initialize(Vector3.zero, onArrived);
        return enemy;
    }

    public List<Enemy> SpawnEnemies(int count, Enemy prefabOverride = null, Action<Enemy> onArrived = null)
    {
        List<Enemy> list = new List<Enemy>(Mathf.Max(0, count));
        for (int i = 0; i < count; i++)
        {
            var e = SpawnEnemy(prefabOverride, onArrived);
            if (e != null) list.Add(e);
        }
        return list;
    }

    public Coroutine SpawnEnemiesSequential(int count, Enemy prefabOverride = null, Action<Enemy> onArrived = null, float? minDelayOverride = null, float? maxDelayOverride = null)
    {
        float minD = minDelayOverride.HasValue ? Mathf.Max(0f, minDelayOverride.Value) : Mathf.Max(0f, minSpawnDelay);
        float maxD = maxDelayOverride.HasValue ? Mathf.Max(minD, maxDelayOverride.Value) : Mathf.Max(minD, maxSpawnDelay);
        return StartCoroutine(SpawnEnemiesSequentialCoroutine(count, prefabOverride, onArrived, minD, maxD));
    }

    IEnumerator SpawnEnemiesSequentialCoroutine(int count, Enemy prefabOverride, Action<Enemy> onArrived, float minDelay, float maxDelay)
    {
        int total = Mathf.Max(0, count);
        for (int i = 0; i < total; i++)
        {
            SpawnEnemy(prefabOverride, onArrived);
            if (i < total - 1)
            {
                float delay = UnityEngine.Random.Range(minDelay, maxDelay);
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
                else
                    yield return null;
            }
        }
    }

    public List<Enemy> SpawnEnemiesAtPositions(IReadOnlyList<Vector3> positions, Enemy prefabOverride = null, Action<Enemy> onArrived = null)
    {
        var prefab = prefabOverride != null ? prefabOverride : enemyPrefab;
        List<Enemy> list = new List<Enemy>(positions.Count);
        for (int i = 0; i < positions.Count; i++)
        {
            Enemy enemy = Instantiate(prefab, positions[i], Quaternion.identity);
            enemy.Initialize(Vector3.zero, onArrived);
            list.Add(enemy);
        }
        return list;
    }

    Vector3 GetRandomSpawnPosition()
    {
        Vector2 circle = UnityEngine.Random.insideUnitCircle * spawnRadius;
        return new Vector3(transform.position.x + circle.x, transform.position.y + circle.y, 0f);
    }
}


