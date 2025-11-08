using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages all drones in the game.
/// Handles persistence between rounds and provides access to drones for enemy targeting.
/// </summary>
[DisallowMultipleComponent]
public class DroneManager : MonoBehaviour
{
    public static DroneManager Instance { get; private set; }

    private List<Drone> _allDrones = new List<Drone>();
    private List<DroneFactoryAttackBehavior> _factories = new List<DroneFactoryAttackBehavior>();

    public IReadOnlyList<Drone> AllDrones => _allDrones.AsReadOnly();
    public IReadOnlyList<DroneFactoryAttackBehavior> AllFactories => _factories.AsReadOnly();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Registers a drone with the manager
    /// </summary>
    public void RegisterDrone(Drone drone)
    {
        if (drone == null) return;
        if (!_allDrones.Contains(drone))
        {
            _allDrones.Add(drone);
            Debug.Log($"DroneManager: Registered drone. Total drones: {_allDrones.Count}");
        }
    }

    /// <summary>
    /// Unregisters a drone from the manager
    /// </summary>
    public void OnDroneDestroyed(Drone drone)
    {
        _allDrones.Remove(drone);
    }

    /// <summary>
    /// Registers a drone factory with the manager
    /// </summary>
    public void RegisterFactory(DroneFactoryAttackBehavior factory)
    {
        if (factory == null) return;
        if (!_factories.Contains(factory))
        {
            _factories.Add(factory);
        }
    }

    /// <summary>
    /// Unregisters a drone factory from the manager
    /// </summary>
    public void UnregisterFactory(DroneFactoryAttackBehavior factory)
    {
        _factories.Remove(factory);
    }

    /// <summary>
    /// Gets the closest drone to a position within a given range
    /// </summary>
    public Drone GetClosestDrone(Vector3 position, float maxRange = Mathf.Infinity)
    {
        Drone closest = null;
        float bestSqr = maxRange * maxRange;

        for (int i = _allDrones.Count - 1; i >= 0; i--)
        {
            var drone = _allDrones[i];
            if (drone == null)
            {
                _allDrones.RemoveAt(i);
                continue;
            }

            if (drone.IsDead) continue;

            float distSqr = (drone.Position - position).sqrMagnitude;
            if (distSqr < bestSqr)
            {
                bestSqr = distSqr;
                closest = drone;
            }
        }

        if (closest != null)
        {
            Debug.Log($"DroneManager: Found closest drone at distance {Mathf.Sqrt(bestSqr)}");
        }

        return closest;
    }

    /// <summary>
    /// Called when a round ends - drones persist, so nothing to do here
    /// </summary>
    public void OnRoundEnd()
    {
        // Clean up null references
        _allDrones.RemoveAll(d => d == null);
        _factories.RemoveAll(f => f == null);

        Debug.Log($"DroneManager: Round ended. {_allDrones.Count} drones persisting to next round.");
    }

    /// <summary>
    /// Called when starting a new game - clear all drones
    /// </summary>
    public void ClearAllDrones()
    {
        foreach (var drone in _allDrones.ToArray())
        {
            if (drone != null)
            {
                Destroy(drone.gameObject);
            }
        }
        _allDrones.Clear();
        _factories.Clear();

        Debug.Log("DroneManager: All drones cleared.");
    }

    /// <summary>
    /// Gets the total count of active drones across all factories
    /// </summary>
    public int GetTotalDroneCount()
    {
        _allDrones.RemoveAll(d => d == null);
        return _allDrones.Count(d => !d.IsDead);
    }
}
