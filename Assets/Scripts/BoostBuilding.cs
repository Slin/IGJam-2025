using UnityEngine;

/// <summary>
/// Provides damage boost to adjacent buildings
/// Towers will check for nearby BoostBuildings when calculating their damage
/// </summary>
[DisallowMultipleComponent]
public class BoostBuilding : MonoBehaviour
{
    [Header("Boost Settings")]
    public float boostRadius = 1f; // How far the boost reaches
    public float damageMultiplier = 1.5f; // 1.5 = +50% damage bonus

    private Building _building;

    void Awake()
    {
        _building = GetComponent<Building>();
    }

    /// <summary>
    /// Checks if this boost building is active and can provide boosts
    /// </summary>
    public bool IsActive()
    {
        return _building != null && _building.IsPlaced && !_building.IsDead;
    }

    /// <summary>
    /// Checks if a position is within boost range
    /// </summary>
    public bool IsInRange(Vector3 position)
    {
        if (!IsActive()) return false;
        float distance = Vector3.Distance(transform.position, position);
        return distance <= boostRadius;
    }

    void OnDrawGizmosSelected()
    {
        // Visualize boost radius in editor
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, boostRadius);
    }
}
