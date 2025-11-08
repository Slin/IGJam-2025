using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("Building Prefabs")]
    public Building basePrefab;
    public Building rocketLauncherPrefab;
    public Building laserTowerPrefab;
    public Building boostBuildingPrefab;
    public Building droneFactoryPrefab;

    [Header("Placement Settings")]
    public int baseRingRadius = 2; // Base occupies 2 rings of hex tiles
    public LayerMask hexTileLayer;

    [Header("Events")]
    public UnityEvent<Building> onBuildingPlaced;
    public UnityEvent<Building> onBuildingDestroyed;
    public UnityEvent<BuildingType> onBuildingPreviewStarted;
    public UnityEvent onBuildingPreviewCancelled;
    public UnityEvent onBaseDestroyed;

    List<Building> _allBuildings = new List<Building>();
    List<Building> _bases = new List<Building>();
    Building _previewBuilding;
    BuildingType _selectedBuildingType;
    bool _isPlacingBuilding;

    public IReadOnlyList<Building> AllBuildings => _allBuildings;
    public IReadOnlyList<Building> Bases => _bases;
    public bool IsPlacingBuilding => _isPlacingBuilding;
    public bool HasBase => _bases.Count > 0 && _bases.Any(b => !b.IsDead);

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
        // Clear existing buildings
        foreach (var building in _allBuildings.ToList())
        {
            if (building != null)
                Destroy(building.gameObject);
        }
        _allBuildings.Clear();
        _bases.Clear();

        // Place initial base at center
        PlaceBaseAtCenter();
    }

    void PlaceBaseAtCenter()
    {
        if (basePrefab == null)
        {
            Debug.LogError("BuildingManager: Base prefab not assigned!");
            return;
        }

        var baseBuilding = Instantiate(basePrefab, Vector3.zero, Quaternion.identity);
        baseBuilding.Initialize(BuildingType.Base, 0, baseBuilding.maxHealth, true);
        
        // For now, place without tile validation (tiles can be added later)
        baseBuilding.Place(new List<HexTile>());
        
        _allBuildings.Add(baseBuilding);
        _bases.Add(baseBuilding);
    }

    public void StartBuildingPlacement(BuildingType type)
    {
        if (_isPlacingBuilding)
        {
            CancelBuildingPlacement();
        }

        Building prefab = GetBuildingPrefab(type);
        if (prefab == null)
        {
            Debug.LogError($"BuildingManager: No prefab found for {type}");
            return;
        }

        // Check if player can afford it
        if (!PlayerStatsManager.Instance.CanAfford(prefab.tritiumCost))
        {
            Debug.LogWarning($"BuildingManager: Cannot afford {type}. Cost: {prefab.tritiumCost}");
            AudioManager.Instance?.PlaySFX("error");
            return;
        }

        _selectedBuildingType = type;
        _isPlacingBuilding = true;
        
        // Create preview building
        _previewBuilding = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        _previewBuilding.SetPreviewMode(true);

        try
        {
            onBuildingPreviewStarted?.Invoke(type);
        }
        catch (Exception) { /* ignore event exceptions */ }

        AudioManager.Instance?.PlaySFX("select");
    }

    public void UpdateBuildingPreview(Vector3 worldPosition)
    {
        if (!_isPlacingBuilding || _previewBuilding == null) return;

        _previewBuilding.transform.position = worldPosition;
    }

    public bool TryPlaceBuilding(Vector3 worldPosition, HexTile tile = null)
    {
        if (!_isPlacingBuilding || _previewBuilding == null) return false;

        // Validate placement (tile availability, etc.)
        if (!IsValidPlacement(worldPosition, tile))
        {
            AudioManager.Instance?.PlaySFX("error");
            return false;
        }

        // Spend tritium
        if (!PlayerStatsManager.Instance.TrySpendTritium(_previewBuilding.tritiumCost))
        {
            AudioManager.Instance?.PlaySFX("error");
            return false;
        }

        // Place the building
        List<HexTile> occupiedTiles = new List<HexTile>();
        if (tile != null) occupiedTiles.Add(tile);

        _previewBuilding.transform.position = worldPosition;
        _previewBuilding.Place(occupiedTiles);
        
        _allBuildings.Add(_previewBuilding);
        
        if (_previewBuilding.buildingType == BuildingType.Base)
        {
            _bases.Add(_previewBuilding);
        }

        AudioManager.Instance?.PlaySFX("build");

        _previewBuilding = null;
        _isPlacingBuilding = false;

        return true;
    }

    public void CancelBuildingPlacement()
    {
        if (_previewBuilding != null)
        {
            Destroy(_previewBuilding.gameObject);
            _previewBuilding = null;
        }

        _isPlacingBuilding = false;

        try
        {
            onBuildingPreviewCancelled?.Invoke();
        }
        catch (Exception) { /* ignore event exceptions */ }
    }

    bool IsValidPlacement(Vector3 position, HexTile tile)
    {
        // TODO: Add more sophisticated validation
        // - Check if tile is occupied
        // - Check if within asteroid bounds
        // - Check minimum distance from other buildings
        
        return true; // Placeholder
    }

    Building GetBuildingPrefab(BuildingType type)
    {
        return type switch
        {
            BuildingType.Base => basePrefab,
            BuildingType.RocketLauncher => rocketLauncherPrefab,
            BuildingType.LaserTower => laserTowerPrefab,
            BuildingType.BoostBuilding => boostBuildingPrefab,
            BuildingType.DroneFactory => droneFactoryPrefab,
            _ => null
        };
    }

    public void OnBuildingPlaced(Building building)
    {
        try
        {
            onBuildingPlaced?.Invoke(building);
        }
        catch (Exception) { /* ignore event exceptions */ }
    }

    public void OnBuildingDestroyed(Building building)
    {
        _allBuildings.Remove(building);
        
        if (building.buildingType == BuildingType.Base)
        {
            _bases.Remove(building);
            
            try
            {
                onBaseDestroyed?.Invoke();
            }
            catch (Exception) { /* ignore event exceptions */ }

            // Check if all bases are destroyed
            if (!HasBase)
            {
                GameManager.Instance?.OnAllBasesDestroyed();
            }
        }

        try
        {
            onBuildingDestroyed?.Invoke(building);
        }
        catch (Exception) { /* ignore event exceptions */ }
    }

    public Building GetNearestBuilding(Vector3 position, BuildingType? filterType = null)
    {
        Building nearest = null;
        float minDistance = float.MaxValue;

        foreach (var building in _allBuildings)
        {
            if (building == null || building.IsDead) continue;
            if (filterType.HasValue && building.buildingType != filterType.Value) continue;

            float distance = Vector3.Distance(position, building.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = building;
            }
        }

        return nearest;
    }

    public List<Building> GetBuildingsOfType(BuildingType type)
    {
        return _allBuildings.Where(b => b != null && !b.IsDead && b.buildingType == type).ToList();
    }
}
