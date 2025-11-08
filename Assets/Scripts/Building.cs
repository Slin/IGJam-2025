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
    
    [Header("Placement")]
    public bool isMultiTile = false; // true for Base which occupies 2 rings
    public List<HexTile> occupiedTiles = new List<HexTile>();

    [Header("Events")]
    public UnityEvent onDestroyed;
    public UnityEvent<float> onDamaged;

    float _currentHealth;
    bool _isPlaced = false;
    bool _isPreview = false;

    public float CurrentHealth => _currentHealth;
    public bool IsDead => _currentHealth <= 0;
    public bool IsPlaced => _isPlaced;
    public bool IsPreview => _isPreview;

    void Awake()
    {
        _currentHealth = maxHealth;
    }

    public void Initialize(BuildingType type, int cost, float health, bool multiTile = false)
    {
        buildingType = type;
        tritiumCost = cost;
        maxHealth = health;
        _currentHealth = health;
        isMultiTile = multiTile;
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
        
        // Register with BuildingManager
        BuildingManager.Instance?.OnBuildingPlaced(this);
    }

    public void TakeDamage(float damage)
    {
        if (IsDead || !_isPlaced) return;

        _currentHealth -= damage;
        _currentHealth = Mathf.Max(0, _currentHealth);

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

        // Clear occupied tiles
        occupiedTiles.Clear();

        Destroy(gameObject);
    }

    public void Repair(float amount)
    {
        if (IsDead || !_isPlaced) return;
        
        _currentHealth = Mathf.Min(_currentHealth + amount, maxHealth);
    }

    public float GetHealthPercentage()
    {
        if (maxHealth <= 0) return 0;
        return _currentHealth / maxHealth;
    }
}
