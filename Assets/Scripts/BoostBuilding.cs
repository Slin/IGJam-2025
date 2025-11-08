using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boosts the attack damage of adjacent buildings by a factor of 1.5
/// </summary>
[DisallowMultipleComponent]
public class BoostBuilding : MonoBehaviour
{
    [Header("Boost Settings")]
    public float boostRadius = 2f; // How far the boost reaches
    public float damageMultiplier = 1.5f;

    private Building _building;
    private List<BuildingAttackBehavior> _boostedBuildings = new List<BuildingAttackBehavior>();

    void Awake()
    {
        _building = GetComponent<Building>();
    }

    void Start()
    {
        // Initial boost application
        UpdateBoosts();
    }

    void Update()
    {
        // Only boost if building is placed and alive
        if (_building == null || !_building.IsPlaced || _building.IsDead)
        {
            RemoveAllBoosts();
            return;
        }

        // Periodically update boosts (every few frames to avoid overhead)
        if (Time.frameCount % 30 == 0)
        {
            UpdateBoosts();
        }
    }

    void UpdateBoosts()
    {
        // Remove old boosts
        RemoveAllBoosts();

        // Find adjacent buildings with attack behaviors
        var adjacentBuildings = FindAdjacentAttackBuildings();

        // Apply boost to each
        foreach (var attackBehavior in adjacentBuildings)
        {
            if (attackBehavior != null)
            {
                attackBehavior.AddDamageMultiplier(damageMultiplier);
                _boostedBuildings.Add(attackBehavior);
            }
        }
    }

    List<BuildingAttackBehavior> FindAdjacentAttackBuildings()
    {
        var result = new List<BuildingAttackBehavior>();

        if (BuildingManager.Instance == null)
            return result;

        foreach (var building in BuildingManager.Instance.AllBuildings)
        {
            if (building == null || building.IsDead || building == _building)
                continue;

            // Check if within boost radius
            float distance = Vector3.Distance(transform.position, building.transform.position);
            if (distance <= boostRadius)
            {
                // Get attack behavior component
                var attackBehavior = building.GetComponent<BuildingAttackBehavior>();
                if (attackBehavior != null)
                {
                    result.Add(attackBehavior);
                }
            }
        }

        return result;
    }

    void RemoveAllBoosts()
    {
        foreach (var attackBehavior in _boostedBuildings)
        {
            if (attackBehavior != null)
            {
                attackBehavior.RemoveDamageMultiplier(damageMultiplier);
            }
        }
        _boostedBuildings.Clear();
    }

    void OnDestroy()
    {
        // Clean up boosts when destroyed
        RemoveAllBoosts();
    }

    void OnDrawGizmosSelected()
    {
        // Visualize boost radius in editor
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, boostRadius);
    }
}
