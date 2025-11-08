using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class Building : MonoBehaviour
{
    [Header("Building Properties")]
    public BuildingType buildingType = BuildingType.RocketLauncher;
    public float maxHealth = 200f;
    public int tritiumCost = 50;
    public int costIncrement = 10; // Cost increase per purchase

    [Header("Placement")]
    public int size = 1; // 1 = single tile, 2 = center + 1 ring, 3 = center + 2 rings, etc.
    public List<HexTile> occupiedTiles = new List<HexTile>();

    [Header("Events")]
    public UnityEvent onDestroyed;
    public UnityEvent<float> onDamaged;

    [Header("Health Bar")]
    public bool showHealthBar = true;
    public Vector3 healthBarOffset = new Vector3(0, 1.5f, 0);
    public Vector2 healthBarSize = new Vector2(1.2f, 0.15f);

    float _currentHealth;
    bool _isPlaced = false;
    bool _isPreview = false;
    HealthBar _healthBar;

    public float CurrentHealth => _currentHealth;
    public bool IsDead => _currentHealth <= 0;
    public bool IsPlaced => _isPlaced;
    public bool IsPreview => _isPreview;

    /// <summary>
    /// Get the current cost for this building type based on how many have been purchased
    /// </summary>
    public int GetCurrentCost(int purchaseCount)
    {
        return tritiumCost + (costIncrement * purchaseCount);
    }

    void Awake()
    {
        _currentHealth = maxHealth;
        InitializeHealthBar();
    }

    public void Initialize(BuildingType type, int cost, float health, int buildingSize = 1)
    {
        buildingType = type;
        tritiumCost = cost;
        maxHealth = health;
        _currentHealth = health;
        size = buildingSize;
        UpdateHealthBar();
    }

    public void SetPreviewMode(bool preview)
    {
        _isPreview = preview;

        // Optional: Visual feedback for preview mode
        // Can be implemented with material changes, transparency, etc.
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (preview)
            {
                // Make semi-transparent or change color
                var color = renderer.material.color;
                color.a = 0.5f;
                renderer.material.color = color;
            }
            else
            {
                // Restore full opacity
                var color = renderer.material.color;
                color.a = 1f;
                renderer.material.color = color;
            }
        }
    }

    public void Place(List<HexTile> tiles)
    {
        _isPlaced = true;
        _isPreview = false;
        occupiedTiles = new List<HexTile>(tiles);

        // Restore visuals to normal
        SetPreviewMode(false);

        // Show health bar when placed
        UpdateHealthBar();

        // Register with BuildingManager
        BuildingManager.Instance?.OnBuildingPlaced(this);
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || !_isPlaced) return;

        _currentHealth -= damage;
        _currentHealth = Mathf.Max(0, _currentHealth);

        UpdateHealthBar();

        try
        {
            onDamaged?.Invoke(_currentHealth);
        }
        catch (Exception) { /* ignore event exceptions */ }

        if (IsDead)
        {
            HandleDestruction();
        }
    }

    void HandleDestruction()
    {
        try
        {
            onDestroyed?.Invoke();
        }
        catch (Exception) { /* ignore event exceptions */ }

        try
        {
            BuildingManager.Instance?.OnBuildingDestroyed(this);
        }
        catch (Exception) { /* ignore if manager not available */ }

        // Free up occupied tiles based on building size
        FreeTilesOccupiedByBuilding();

        Destroy(gameObject);
    }

    void FreeTilesOccupiedByBuilding()
    {
        // Find all tiles within the building's occupied radius and free them
        var allTiles = FindObjectsByType<HexTile>(FindObjectsSortMode.None);
        float occupiedRadius = GetOccupiedRadius();

        foreach (var tile in allTiles)
        {
            float distance = Vector3.Distance(tile.transform.position, transform.position);
            if (distance <= occupiedRadius)
            {
                tile.SetOccupied(false);
            }
        }

        occupiedTiles.Clear();
    }

    public void Repair(float amount)
    {
        if (IsDead || !_isPlaced) return;

        _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);
        UpdateHealthBar();
    }

    public float GetHealthPercentage()
    {
        if (maxHealth <= 0) return 0;
        return _currentHealth / maxHealth;
    }

    /// <summary>
    /// Get the radius occupied by this building based on its size
    /// Size 1 = single hex (radius ~0.5)
    /// Size 2 = center + 1 ring (radius ~0.95, just past first ring)
    /// Size 3 = center + 2 rings (radius ~1.8, just past second ring)
    /// </summary>
    public float GetOccupiedRadius()
    {
        // From CreateTiles: outerRadius = 0.866
        // The first ring of hexes is at distance 0.866 from center
        // The second ring is at distance ~1.732 from center
        // Size 1: just center hex
        // Size 2: center + first ring (6 hexes at 0.866 distance)
        // Size 3: center + first + second ring (12 hexes at ~1.732 distance)

        const float hexOuterRadius = 0.866025404f;

        if (size <= 1)
        {
            // Single hex: small radius to just cover the center
            return hexOuterRadius * 0.5f;
        }

        // For size N, we want to reach just past ring (N-1)
        // Ring 1 is at distance 0.866, ring 2 at 1.732 (0.866 * 2), etc.
        // Add a small margin (0.1) to ensure we capture all tiles in that ring
        return (size - 1) * hexOuterRadius + 0.1f;
    }

    void InitializeHealthBar()
    {
        if (!showHealthBar) return;

        _healthBar = gameObject.AddComponent<HealthBar>();
        _healthBar.SetOffset(healthBarOffset);
        _healthBar.SetSize(healthBarSize);
        _healthBar.hideWhenFull = true;
        UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (_healthBar != null && showHealthBar)
        {
            _healthBar.UpdateHealth(_currentHealth, maxHealth);
        }
    }
}
