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
    public float hexOuterRadius = 0.866025404f; // Distance between hex centers
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
    public BuildingType? SelectedBuildingType => _isPlacingBuilding ? _selectedBuildingType : (BuildingType?)null;
    public bool IsPlacingBuildingOfType(BuildingType type) => _isPlacingBuilding && _selectedBuildingType == type;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        // Allow canceling building placement with ESC key
        if (_isPlacingBuilding)
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                CancelBuildingPlacement();
            }
        }
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
        // Base has size 2 (center + 1 ring)
        baseBuilding.Initialize(BuildingType.Base, 0, baseBuilding.maxHealth, 2);

        // Mark tiles occupied by this building
        MarkTilesOccupiedByBuilding(baseBuilding, Vector3.zero);

        // Place without tile validation
        baseBuilding.Place(new List<HexTile>());

        _allBuildings.Add(baseBuilding);
        _bases.Add(baseBuilding);
    }

    void MarkTilesOccupiedByBuilding(Building building, Vector3 position)
    {
        // Find all tiles within the building's occupied radius
        var allTiles = FindObjectsByType<HexTile>(FindObjectsSortMode.None);
        float occupiedRadius = building.GetOccupiedRadius();

        foreach (var tile in allTiles)
        {
            float distance = Vector3.Distance(tile.transform.position, position);
            if (distance <= occupiedRadius)
            {
                tile.SetOccupied(true);
            }
        }
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
        CreatePreviewForType(_selectedBuildingType);

        try
        {
            onBuildingPreviewStarted?.Invoke(type);
        }
        catch (Exception) { /* ignore event exceptions */ }

        AudioManager.Instance?.PlaySFX("select");
    }

    void CreatePreviewForType(BuildingType type)
    {
        var prefab = GetBuildingPrefab(type);
        if (prefab == null) return;
        _previewBuilding = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        _previewBuilding.SetPreviewMode(true);
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

        // Mark tiles as occupied based on building size
        MarkTilesOccupiedByBuilding(_previewBuilding, worldPosition);

        AudioManager.Instance?.PlaySFX("build");

        // Keep the same building type selected so player can place multiple
        CreatePreviewForType(_selectedBuildingType);
        // Start the preview at the last placed position; it will snap on hover
        _previewBuilding.transform.position = worldPosition;
        _isPlacingBuilding = true;

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
        // Basic validation: require a tile
        if (tile == null) return false;
        if (_previewBuilding == null) return false;

        // Get the radius this building will occupy
        float buildingRadius = _previewBuilding.GetOccupiedRadius();

        // Check collision with existing buildings
        foreach (var existingBuilding in _allBuildings)
        {
            if (existingBuilding == null || existingBuilding.IsDead) continue;

            float existingRadius = existingBuilding.GetOccupiedRadius();
            float distance = Vector3.Distance(position, existingBuilding.transform.position);

            // Buildings overlap if distance is less than sum of their radii
            // Add small epsilon to account for floating point precision errors
            float minDistance = buildingRadius + existingRadius;
            const float epsilon = 0.001f;
            if (distance < minDistance - epsilon)
            {
                return false;
            }
        }

        // Check if the center tile is occupied (for size 1 buildings)
        if (_previewBuilding.size <= 1 && tile.IsOccupied)
        {
            return false;
        }

        return true;
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
