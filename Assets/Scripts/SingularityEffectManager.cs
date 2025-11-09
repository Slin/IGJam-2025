using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages the singularity effect system - selects random effects at the start of each building phase
/// and tracks active effects during the defense phase
/// </summary>
[DisallowMultipleComponent]
public class SingularityEffectManager : MonoBehaviour
{
    public static SingularityEffectManager Instance { get; private set; }

    [Header("Effect Pool")]
    [Tooltip("List of all possible effects that can be drawn")]
    public List<SingularityEffect> effectPool = new List<SingularityEffect>();

    [Header("Effect Limits")]
    [Tooltip("Maximum number of effects that can be active simultaneously")]
    [Range(1, 5)]
    public int maxActiveEffects = 1;

    [Header("Events")]
    public UnityEvent<List<SingularityEffect>> onEffectsChanged;

    private List<SingularityEffect> _activeEffects = new List<SingularityEffect>();

    public IReadOnlyList<SingularityEffect> ActiveEffects => _activeEffects.AsReadOnly();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Always populate effects on code side
        PopulateEffects();
    }

    void Start()
    {
        // Subscribe to game events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onPhaseChanged.AddListener(OnPhaseChanged);
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.onPhaseChanged.RemoveListener(OnPhaseChanged);
        }
    }

    /// <summary>
    /// Called when game phase changes
    /// </summary>
    void OnPhaseChanged(GamePhase newPhase)
    {
        if (newPhase == GamePhase.Building)
        {
            // Select new effects at the start of the building phase
            SelectRandomEffects();
        }
    }

    /// <summary>
    /// Select random effects for the upcoming defense round
    /// </summary>
    public void SelectRandomEffects()
    {
        _activeEffects.Clear();

        int currentRound = PlayerStatsManager.Instance?.CurrentRound ?? 1;
        int upcomingRound = currentRound + 1; // Building phase prepares for the next round

        // Filter effects that can be drawn in this round
        var availableEffects = effectPool.Where(e => e != null && e.CanBeDrawnInRound(upcomingRound)).ToList();

        if (availableEffects.Count == 0)
        {
            Debug.LogWarning($"SingularityEffectManager: No effects available for round {upcomingRound}");
            NotifyEffectsChanged();
            return;
        }

        // Select up to maxActiveEffects random effects
        int effectCount = Mathf.Min(maxActiveEffects, availableEffects.Count);

        for (int i = 0; i < effectCount; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableEffects.Count);
            SingularityEffect selectedEffect = availableEffects[randomIndex];
            _activeEffects.Add(selectedEffect);

            // Remove from available pool to avoid duplicates
            availableEffects.RemoveAt(randomIndex);
        }

        Debug.Log($"SingularityEffectManager: Selected {_activeEffects.Count} effect(s) for round {upcomingRound}:");
        foreach (var effect in _activeEffects)
        {
            Debug.Log($"  - {effect.GetDescription()}");
        }

        NotifyEffectsChanged();
    }

    /// <summary>
    /// Get the multiplier for a specific effect type based on all active effects
    /// </summary>
    public float GetEffectMultiplier(SingularityEffectType effectType)
    {
        float multiplier = 1f;

        foreach (var effect in _activeEffects)
        {
            if (effect != null && effect.effectType == effectType)
            {
                // Stack effects multiplicatively
                multiplier *= effect.GetMultiplier();
            }
        }

        return multiplier;
    }

    /// <summary>
    /// Get the multiplier for a specific effect type and enemy type
    /// </summary>
    public float GetEffectMultiplier(SingularityEffectType effectType, EnemyType enemyType)
    {
        float multiplier = 1f;

        foreach (var effect in _activeEffects)
        {
            if (effect != null && effect.effectType == effectType && effect.AppliesTo(enemyType))
            {
                multiplier *= effect.GetMultiplier();
            }
        }

        return multiplier;
    }

    /// <summary>
    /// Get the multiplier for a specific effect type and building type
    /// </summary>
    public float GetEffectMultiplier(SingularityEffectType effectType, BuildingType buildingType)
    {
        float multiplier = 1f;

        foreach (var effect in _activeEffects)
        {
            if (effect != null && effect.effectType == effectType && effect.AppliesTo(buildingType))
            {
                multiplier *= effect.GetMultiplier();
            }
        }

        return multiplier;
    }    /// <summary>
         /// Check if a specific effect type is currently active
         /// </summary>
    public bool HasActiveEffect(SingularityEffectType effectType)
    {
        return _activeEffects.Any(e => e != null && e.effectType == effectType);
    }

    /// <summary>
    /// Clear all active effects (useful for game restart)
    /// </summary>
    public void ClearActiveEffects()
    {
        _activeEffects.Clear();
        NotifyEffectsChanged();
    }

    /// <summary>
    /// Manually set active effects (useful for testing)
    /// </summary>
    public void SetActiveEffects(List<SingularityEffect> effects)
    {
        _activeEffects.Clear();
        if (effects != null)
        {
            _activeEffects.AddRange(effects.Where(e => e != null).Take(maxActiveEffects));
        }
        NotifyEffectsChanged();
    }

    void NotifyEffectsChanged()
    {
        try
        {
            onEffectsChanged?.Invoke(_activeEffects);
        }
        catch (Exception ex)
        {
            Debug.LogError($"SingularityEffectManager: Error invoking effects changed event: {ex.Message}");
        }
    }

    /// <summary>
    /// Populate all effects with systematic ±25%, ±50%, ±100% variations across all effect types
    /// </summary>
    public void PopulateEffects()
    {
        effectPool = new List<SingularityEffect>();

        // Define effect types to generate (non-drone)
        var effectTypes = new[]
        {
            SingularityEffectType.BuildingAttackDamage,
            SingularityEffectType.BuildingAttackRange,
            SingularityEffectType.BuildingDamageTaken,
            SingularityEffectType.EnemyDamageDealt,
            SingularityEffectType.EnemyDamageTaken,
            SingularityEffectType.EnemySpeed
        };

        // Add general effects (affect all types)
        foreach (var effectType in effectTypes)
        {
            // ±10% effects (rounds 1-7)
            effectPool.Add(CreateEffect(effectType, 10f, 1, 7));
            effectPool.Add(CreateEffect(effectType, -10f, 1, 7));

            // ±20% effects (rounds 8-15)
            effectPool.Add(CreateEffect(effectType, 20f, 8, 15));
            effectPool.Add(CreateEffect(effectType, -20f, 8, 15));

            // ±30% effects (rounds 16+)
            effectPool.Add(CreateEffect(effectType, 30f, 16, -1));
            effectPool.Add(CreateEffect(effectType, -30f, 16, -1));
        }

        // Add drone effects based on DroneFactory unlock round
        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            int droneUnlock = gameManager.roundsUntilDroneFactory;

            // DroneSpeed effects
            effectPool.Add(CreateEffect(SingularityEffectType.DroneSpeed, 25f, droneUnlock, droneUnlock + 6));
            effectPool.Add(CreateEffect(SingularityEffectType.DroneSpeed, -25f, droneUnlock, droneUnlock + 6));
            effectPool.Add(CreateEffect(SingularityEffectType.DroneSpeed, 50f, droneUnlock + 7, droneUnlock + 14));
            effectPool.Add(CreateEffect(SingularityEffectType.DroneSpeed, -50f, droneUnlock + 7, droneUnlock + 14));
            effectPool.Add(CreateEffect(SingularityEffectType.DroneSpeed, 100f, droneUnlock + 15, -1));
            effectPool.Add(CreateEffect(SingularityEffectType.DroneSpeed, -75f, droneUnlock + 15, -1));

            // DroneDamage effects
            effectPool.Add(CreateEffect(SingularityEffectType.DroneDamage, 25f, droneUnlock, droneUnlock + 6));
            effectPool.Add(CreateEffect(SingularityEffectType.DroneDamage, -25f, droneUnlock, droneUnlock + 6));
            effectPool.Add(CreateEffect(SingularityEffectType.DroneDamage, 50f, droneUnlock + 7, droneUnlock + 14));
            effectPool.Add(CreateEffect(SingularityEffectType.DroneDamage, -50f, droneUnlock + 7, droneUnlock + 14));
            effectPool.Add(CreateEffect(SingularityEffectType.DroneDamage, 100f, droneUnlock + 15, -1));
            effectPool.Add(CreateEffect(SingularityEffectType.DroneDamage, -75f, droneUnlock + 15, -1));
        }

        // Add type-specific enemy effects
        AddEnemyTypeSpecificEffects();

        // Add type-specific building effects
        AddBuildingTypeSpecificEffects();

        Debug.Log($"SingularityEffectManager: Populated with {effectPool.Count} effects");
    }

    /// <summary>
    /// Add effects that target specific enemy types
    /// </summary>
    private void AddEnemyTypeSpecificEffects()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null) return;

        // Fast enemies - available when they start appearing
        int fastUnlock = gameManager.roundsUntilFastEnemies;
        AddSpecificEnemyEffect(EnemyType.Fast, SingularityEffectType.EnemySpeed, 25f, fastUnlock, fastUnlock + 6);
        AddSpecificEnemyEffect(EnemyType.Fast, SingularityEffectType.EnemySpeed, -25f, fastUnlock, fastUnlock + 6);
        AddSpecificEnemyEffect(EnemyType.Fast, SingularityEffectType.EnemyDamageTaken, 15f, fastUnlock, fastUnlock + 6);
        AddSpecificEnemyEffect(EnemyType.Fast, SingularityEffectType.EnemySpeed, 50f, fastUnlock + 7, fastUnlock + 14);
        AddSpecificEnemyEffect(EnemyType.Fast, SingularityEffectType.EnemySpeed, -50f, fastUnlock + 7, fastUnlock + 14);
        AddSpecificEnemyEffect(EnemyType.Fast, SingularityEffectType.EnemyDamageTaken, 20f, fastUnlock + 7, fastUnlock + 14);
        AddSpecificEnemyEffect(EnemyType.Fast, SingularityEffectType.EnemySpeed, 100f, fastUnlock + 15, -1);
        AddSpecificEnemyEffect(EnemyType.Fast, SingularityEffectType.EnemySpeed, -75f, fastUnlock + 15, -1);
        AddSpecificEnemyEffect(EnemyType.Fast, SingularityEffectType.EnemyDamageTaken, 25f, fastUnlock + 15, -1);

        // Armored enemies - available when they start appearing
        int armoredUnlock = gameManager.roundsUntilArmoredEnemies;
        AddSpecificEnemyEffect(EnemyType.Armored, SingularityEffectType.EnemyDamageTaken, -15f, armoredUnlock, armoredUnlock + 6);
        AddSpecificEnemyEffect(EnemyType.Armored, SingularityEffectType.EnemySpeed, -25f, armoredUnlock, armoredUnlock + 6);
        AddSpecificEnemyEffect(EnemyType.Armored, SingularityEffectType.EnemyDamageTaken, 15f, armoredUnlock, armoredUnlock + 6);
        AddSpecificEnemyEffect(EnemyType.Armored, SingularityEffectType.EnemyDamageTaken, -20f, armoredUnlock + 7, armoredUnlock + 14);
        AddSpecificEnemyEffect(EnemyType.Armored, SingularityEffectType.EnemySpeed, -50f, armoredUnlock + 7, armoredUnlock + 14);
        AddSpecificEnemyEffect(EnemyType.Armored, SingularityEffectType.EnemyDamageTaken, 20f, armoredUnlock + 7, armoredUnlock + 14);
        AddSpecificEnemyEffect(EnemyType.Armored, SingularityEffectType.EnemyDamageTaken, -25f, armoredUnlock + 15, -1);
        AddSpecificEnemyEffect(EnemyType.Armored, SingularityEffectType.EnemySpeed, -75f, armoredUnlock + 15, -1);
        AddSpecificEnemyEffect(EnemyType.Armored, SingularityEffectType.EnemyDamageTaken, 25f, armoredUnlock + 15, -1);

        // Boss enemies - available when they start appearing
        int bossUnlock = gameManager.roundsUntilBossEnemies;
        AddSpecificEnemyEffect(EnemyType.Boss, SingularityEffectType.EnemyDamageDealt, 25f, bossUnlock, bossUnlock + 6);
        AddSpecificEnemyEffect(EnemyType.Boss, SingularityEffectType.EnemyDamageTaken, -15f, bossUnlock, bossUnlock + 6);
        AddSpecificEnemyEffect(EnemyType.Boss, SingularityEffectType.EnemyDamageTaken, 15f, bossUnlock, bossUnlock + 6);
        AddSpecificEnemyEffect(EnemyType.Boss, SingularityEffectType.EnemyDamageDealt, 50f, bossUnlock + 7, bossUnlock + 14);
        AddSpecificEnemyEffect(EnemyType.Boss, SingularityEffectType.EnemyDamageTaken, -20f, bossUnlock + 7, bossUnlock + 14);
        AddSpecificEnemyEffect(EnemyType.Boss, SingularityEffectType.EnemyDamageTaken, 20f, bossUnlock + 7, bossUnlock + 14);
        AddSpecificEnemyEffect(EnemyType.Boss, SingularityEffectType.EnemyDamageDealt, 100f, bossUnlock + 15, -1);
        AddSpecificEnemyEffect(EnemyType.Boss, SingularityEffectType.EnemyDamageTaken, -25f, bossUnlock + 15, -1);
        AddSpecificEnemyEffect(EnemyType.Boss, SingularityEffectType.EnemyDamageTaken, 25f, bossUnlock + 15, -1);

        // Attack enemies - available when they start appearing
        int attackUnlock = gameManager.roundsUntilAttackEnemies;
        AddSpecificEnemyEffect(EnemyType.Attack, SingularityEffectType.EnemyDamageDealt, 25f, attackUnlock, attackUnlock + 6);
        AddSpecificEnemyEffect(EnemyType.Attack, SingularityEffectType.EnemyDamageDealt, -25f, attackUnlock, attackUnlock + 6);
        AddSpecificEnemyEffect(EnemyType.Attack, SingularityEffectType.EnemyDamageDealt, 50f, attackUnlock + 7, attackUnlock + 14);
        AddSpecificEnemyEffect(EnemyType.Attack, SingularityEffectType.EnemyDamageDealt, -50f, attackUnlock + 7, attackUnlock + 14);
        AddSpecificEnemyEffect(EnemyType.Attack, SingularityEffectType.EnemyDamageDealt, 100f, attackUnlock + 15, -1);
        AddSpecificEnemyEffect(EnemyType.Attack, SingularityEffectType.EnemyDamageDealt, -75f, attackUnlock + 15, -1);

        // Teleporter enemies - available when they start appearing
        int teleporterUnlock = gameManager.roundsUntilTeleporterEnemies;
        AddSpecificEnemyEffect(EnemyType.Teleporter, SingularityEffectType.EnemySpeed, 25f, teleporterUnlock, teleporterUnlock + 6);
        AddSpecificEnemyEffect(EnemyType.Teleporter, SingularityEffectType.EnemyDamageTaken, 15f, teleporterUnlock, teleporterUnlock + 6);
        AddSpecificEnemyEffect(EnemyType.Teleporter, SingularityEffectType.EnemySpeed, 50f, teleporterUnlock + 7, teleporterUnlock + 14);
        AddSpecificEnemyEffect(EnemyType.Teleporter, SingularityEffectType.EnemyDamageTaken, 20f, teleporterUnlock + 7, teleporterUnlock + 14);
        AddSpecificEnemyEffect(EnemyType.Teleporter, SingularityEffectType.EnemySpeed, 100f, teleporterUnlock + 15, -1);
        AddSpecificEnemyEffect(EnemyType.Teleporter, SingularityEffectType.EnemyDamageTaken, 25f, teleporterUnlock + 15, -1);

        // Exploder enemies - available when they start appearing
        int exploderUnlock = gameManager.roundsUntilExploderEnemies;
        AddSpecificEnemyEffect(EnemyType.Exploder, SingularityEffectType.EnemyDamageDealt, 25f, exploderUnlock, exploderUnlock + 6);
        AddSpecificEnemyEffect(EnemyType.Exploder, SingularityEffectType.EnemySpeed, -25f, exploderUnlock, exploderUnlock + 6);
        AddSpecificEnemyEffect(EnemyType.Exploder, SingularityEffectType.EnemyDamageDealt, 50f, exploderUnlock + 7, exploderUnlock + 14);
        AddSpecificEnemyEffect(EnemyType.Exploder, SingularityEffectType.EnemySpeed, -50f, exploderUnlock + 7, exploderUnlock + 14);
        AddSpecificEnemyEffect(EnemyType.Exploder, SingularityEffectType.EnemyDamageDealt, 100f, exploderUnlock + 15, -1);
        AddSpecificEnemyEffect(EnemyType.Exploder, SingularityEffectType.EnemySpeed, -75f, exploderUnlock + 15, -1);
    }

    /// <summary>
    /// Add effects that target specific building types
    /// </summary>
    private void AddBuildingTypeSpecificEffects()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null) return;

        // Rocket Launcher - available when unlocked
        int rocketUnlock = gameManager.roundsUntilRocketLauncher;
        AddSpecificBuildingEffect(BuildingType.RocketLauncher, SingularityEffectType.BuildingAttackDamage, 25f, rocketUnlock, rocketUnlock + 6);
        AddSpecificBuildingEffect(BuildingType.RocketLauncher, SingularityEffectType.BuildingAttackDamage, -25f, rocketUnlock, rocketUnlock + 6);
        AddSpecificBuildingEffect(BuildingType.RocketLauncher, SingularityEffectType.BuildingAttackRange, 25f, rocketUnlock, rocketUnlock + 6);
        AddSpecificBuildingEffect(BuildingType.RocketLauncher, SingularityEffectType.BuildingAttackRange, -25f, rocketUnlock, rocketUnlock + 6);
        AddSpecificBuildingEffect(BuildingType.RocketLauncher, SingularityEffectType.BuildingAttackDamage, 50f, rocketUnlock + 7, rocketUnlock + 14);
        AddSpecificBuildingEffect(BuildingType.RocketLauncher, SingularityEffectType.BuildingAttackDamage, -50f, rocketUnlock + 7, rocketUnlock + 14);
        AddSpecificBuildingEffect(BuildingType.RocketLauncher, SingularityEffectType.BuildingAttackRange, 50f, rocketUnlock + 7, rocketUnlock + 14);
        AddSpecificBuildingEffect(BuildingType.RocketLauncher, SingularityEffectType.BuildingAttackRange, -50f, rocketUnlock + 7, rocketUnlock + 14);
        AddSpecificBuildingEffect(BuildingType.RocketLauncher, SingularityEffectType.BuildingAttackDamage, 100f, rocketUnlock + 15, -1);
        AddSpecificBuildingEffect(BuildingType.RocketLauncher, SingularityEffectType.BuildingAttackDamage, -75f, rocketUnlock + 15, -1);
        AddSpecificBuildingEffect(BuildingType.RocketLauncher, SingularityEffectType.BuildingAttackRange, 100f, rocketUnlock + 15, -1);
        AddSpecificBuildingEffect(BuildingType.RocketLauncher, SingularityEffectType.BuildingAttackRange, -75f, rocketUnlock + 15, -1);

        // Laser Tower - available when unlocked
        int laserUnlock = gameManager.roundsUntilLaserTower;
        AddSpecificBuildingEffect(BuildingType.LaserTower, SingularityEffectType.BuildingAttackDamage, 25f, laserUnlock, laserUnlock + 4);
        AddSpecificBuildingEffect(BuildingType.LaserTower, SingularityEffectType.BuildingAttackDamage, -25f, laserUnlock, laserUnlock + 4);
        AddSpecificBuildingEffect(BuildingType.LaserTower, SingularityEffectType.BuildingAttackRange, 25f, laserUnlock, laserUnlock + 4);
        AddSpecificBuildingEffect(BuildingType.LaserTower, SingularityEffectType.BuildingAttackRange, -25f, laserUnlock, laserUnlock + 4);
        AddSpecificBuildingEffect(BuildingType.LaserTower, SingularityEffectType.BuildingAttackDamage, 50f, laserUnlock + 5, laserUnlock + 12);
        AddSpecificBuildingEffect(BuildingType.LaserTower, SingularityEffectType.BuildingAttackDamage, -50f, laserUnlock + 5, laserUnlock + 12);
        AddSpecificBuildingEffect(BuildingType.LaserTower, SingularityEffectType.BuildingAttackRange, 50f, laserUnlock + 5, laserUnlock + 12);
        AddSpecificBuildingEffect(BuildingType.LaserTower, SingularityEffectType.BuildingAttackRange, -50f, laserUnlock + 5, laserUnlock + 12);
        AddSpecificBuildingEffect(BuildingType.LaserTower, SingularityEffectType.BuildingAttackDamage, 100f, laserUnlock + 13, -1);
        AddSpecificBuildingEffect(BuildingType.LaserTower, SingularityEffectType.BuildingAttackDamage, -75f, laserUnlock + 13, -1);
        AddSpecificBuildingEffect(BuildingType.LaserTower, SingularityEffectType.BuildingAttackRange, 100f, laserUnlock + 13, -1);
        AddSpecificBuildingEffect(BuildingType.LaserTower, SingularityEffectType.BuildingAttackRange, -75f, laserUnlock + 13, -1);

        // Freeze Tower - available when unlocked
        int freezeUnlock = gameManager.roundsUntilFreezeTower;
        AddSpecificBuildingEffect(BuildingType.FreezeTower, SingularityEffectType.BuildingAttackRange, 25f, freezeUnlock, freezeUnlock + 6);
        AddSpecificBuildingEffect(BuildingType.FreezeTower, SingularityEffectType.BuildingAttackRange, -25f, freezeUnlock, freezeUnlock + 6);
        AddSpecificBuildingEffect(BuildingType.FreezeTower, SingularityEffectType.BuildingAttackDamage, 25f, freezeUnlock, freezeUnlock + 6);
        AddSpecificBuildingEffect(BuildingType.FreezeTower, SingularityEffectType.BuildingAttackDamage, -25f, freezeUnlock, freezeUnlock + 6);
        AddSpecificBuildingEffect(BuildingType.FreezeTower, SingularityEffectType.BuildingAttackRange, 50f, freezeUnlock + 7, freezeUnlock + 14);
        AddSpecificBuildingEffect(BuildingType.FreezeTower, SingularityEffectType.BuildingAttackRange, -50f, freezeUnlock + 7, freezeUnlock + 14);
        AddSpecificBuildingEffect(BuildingType.FreezeTower, SingularityEffectType.BuildingAttackDamage, 50f, freezeUnlock + 7, freezeUnlock + 14);
        AddSpecificBuildingEffect(BuildingType.FreezeTower, SingularityEffectType.BuildingAttackDamage, -50f, freezeUnlock + 7, freezeUnlock + 14);
        AddSpecificBuildingEffect(BuildingType.FreezeTower, SingularityEffectType.BuildingAttackRange, 100f, freezeUnlock + 15, -1);
        AddSpecificBuildingEffect(BuildingType.FreezeTower, SingularityEffectType.BuildingAttackRange, -75f, freezeUnlock + 15, -1);
        AddSpecificBuildingEffect(BuildingType.FreezeTower, SingularityEffectType.BuildingAttackDamage, 100f, freezeUnlock + 15, -1);
        AddSpecificBuildingEffect(BuildingType.FreezeTower, SingularityEffectType.BuildingAttackDamage, -75f, freezeUnlock + 15, -1);

        // Base - available when unlocked, affects damage taken
        int baseUnlock = gameManager.roundsUntilBase;
        AddSpecificBuildingEffect(BuildingType.Base, SingularityEffectType.BuildingDamageTaken, -25f, baseUnlock, baseUnlock + 6);
        AddSpecificBuildingEffect(BuildingType.Base, SingularityEffectType.BuildingDamageTaken, 25f, baseUnlock, baseUnlock + 6);
        AddSpecificBuildingEffect(BuildingType.Base, SingularityEffectType.BuildingDamageTaken, -50f, baseUnlock + 7, baseUnlock + 14);
        AddSpecificBuildingEffect(BuildingType.Base, SingularityEffectType.BuildingDamageTaken, 50f, baseUnlock + 7, baseUnlock + 14);
        AddSpecificBuildingEffect(BuildingType.Base, SingularityEffectType.BuildingDamageTaken, -75f, baseUnlock + 15, -1);
        AddSpecificBuildingEffect(BuildingType.Base, SingularityEffectType.BuildingDamageTaken, 100f, baseUnlock + 15, -1);

        // Boost Building - available when unlocked
        int boostUnlock = gameManager.roundsUntilBoostBuilding;
        AddSpecificBuildingEffect(BuildingType.BoostBuilding, SingularityEffectType.BuildingAttackRange, 25f, boostUnlock, boostUnlock + 6);
        AddSpecificBuildingEffect(BuildingType.BoostBuilding, SingularityEffectType.BuildingAttackRange, -25f, boostUnlock, boostUnlock + 6);
        AddSpecificBuildingEffect(BuildingType.BoostBuilding, SingularityEffectType.BuildingAttackRange, 50f, boostUnlock + 7, boostUnlock + 14);
        AddSpecificBuildingEffect(BuildingType.BoostBuilding, SingularityEffectType.BuildingAttackRange, -50f, boostUnlock + 7, boostUnlock + 14);
        AddSpecificBuildingEffect(BuildingType.BoostBuilding, SingularityEffectType.BuildingAttackRange, 100f, boostUnlock + 15, -1);
        AddSpecificBuildingEffect(BuildingType.BoostBuilding, SingularityEffectType.BuildingAttackRange, -75f, boostUnlock + 15, -1);

        // Radar Jammer - available when unlocked
        int jammerUnlock = gameManager.roundsUntilRadarJammer;
        AddSpecificBuildingEffect(BuildingType.RadarJammer, SingularityEffectType.BuildingAttackRange, 25f, jammerUnlock, jammerUnlock + 6);
        AddSpecificBuildingEffect(BuildingType.RadarJammer, SingularityEffectType.BuildingAttackRange, -25f, jammerUnlock, jammerUnlock + 6);
        AddSpecificBuildingEffect(BuildingType.RadarJammer, SingularityEffectType.BuildingAttackRange, 50f, jammerUnlock + 7, jammerUnlock + 14);
        AddSpecificBuildingEffect(BuildingType.RadarJammer, SingularityEffectType.BuildingAttackRange, -50f, jammerUnlock + 7, jammerUnlock + 14);
        AddSpecificBuildingEffect(BuildingType.RadarJammer, SingularityEffectType.BuildingAttackRange, 100f, jammerUnlock + 15, -1);
        AddSpecificBuildingEffect(BuildingType.RadarJammer, SingularityEffectType.BuildingAttackRange, -75f, jammerUnlock + 15, -1);
    }

    /// <summary>
    /// Create and add a type-specific enemy effect
    /// </summary>
    private void AddSpecificEnemyEffect(EnemyType enemyType, SingularityEffectType effectType, float valuePercent, int minRound, int maxRound)
    {
        string baseName = GetEnemyTypeName(enemyType);
        string effectName = GetEffectBaseName(effectType);
        string modifier = GetModifierName(effectType, valuePercent);

        effectPool.Add(new SingularityEffect
        {
            effectName = $"{modifier} {baseName} {effectName}",
            effectType = effectType,
            valuePercent = valuePercent,
            minRound = minRound,
            maxRound = maxRound,
            affectsAllEnemies = false,
            targetEnemyType = enemyType
        });
    }

    /// <summary>
    /// Create and add a type-specific building effect
    /// </summary>
    private void AddSpecificBuildingEffect(BuildingType buildingType, SingularityEffectType effectType, float valuePercent, int minRound, int maxRound)
    {
        string baseName = GetBuildingTypeName(buildingType);
        string effectName = GetEffectBaseName(effectType);
        string modifier = GetModifierName(effectType, valuePercent);

        effectPool.Add(new SingularityEffect
        {
            effectName = $"{modifier} {baseName} {effectName}",
            effectType = effectType,
            valuePercent = valuePercent,
            minRound = minRound,
            maxRound = maxRound,
            affectsAllBuildings = false,
            targetBuildingType = buildingType
        });
    }

    /// <summary>
    /// Get display name for enemy type
    /// </summary>
    private string GetEnemyTypeName(EnemyType enemyType)
    {
        return enemyType switch
        {
            EnemyType.Regular => "Regular",
            EnemyType.Fast => "Fast",
            EnemyType.Armored => "Armored",
            EnemyType.Boss => "Boss",
            EnemyType.Attack => "Attack",
            EnemyType.Teleporter => "Teleporter",
            EnemyType.Exploder => "Exploder",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get display name for building type
    /// </summary>
    private string GetBuildingTypeName(BuildingType buildingType)
    {
        return buildingType switch
        {
            BuildingType.Base => "Base",
            BuildingType.RocketLauncher => "Rocket",
            BuildingType.LaserTower => "Laser",
            BuildingType.BoostBuilding => "Boost",
            BuildingType.DroneFactory => "Drone",
            BuildingType.FreezeTower => "Freeze",
            BuildingType.RadarJammer => "Jammer",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Create a singularity effect with appropriate naming
    /// </summary>
    private SingularityEffect CreateEffect(SingularityEffectType effectType, float valuePercent, int minRound, int maxRound)
    {
        string baseName = GetEffectBaseName(effectType);
        string modifier = GetModifierName(effectType, valuePercent);

        return new SingularityEffect
        {
            effectName = $"{modifier} {baseName}",
            effectType = effectType,
            valuePercent = valuePercent,
            minRound = minRound,
            maxRound = maxRound,
            affectsAllEnemies = true,
            affectsAllBuildings = true
        };
    }

    /// <summary>
    /// Get base name for effect type
    /// </summary>
    private string GetEffectBaseName(SingularityEffectType effectType)
    {
        return effectType switch
        {
            SingularityEffectType.BuildingAttackDamage => "Tower Damage",
            SingularityEffectType.BuildingAttackRange => "Tower Range",
            SingularityEffectType.BuildingDamageTaken => "Tower Armor",
            SingularityEffectType.EnemyDamageDealt => "Enemy Strength",
            SingularityEffectType.EnemyDamageTaken => "Enemy Armor",
            SingularityEffectType.EnemySpeed => "Enemy Speed",
            SingularityEffectType.DroneSpeed => "Drone Speed",
            SingularityEffectType.DroneDamage => "Drone Damage",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get modifier name based on effect type and value
    /// </summary>
    private string GetModifierName(SingularityEffectType effectType, float valuePercent)
    {
        bool isPositive = valuePercent > 0;
        float absValue = Mathf.Abs(valuePercent);

        // For "damage taken" and "armor" effects, reverse the naming logic
        bool reverseLogic = effectType == SingularityEffectType.BuildingDamageTaken ||
                           effectType == SingularityEffectType.EnemyDamageTaken;

        if (reverseLogic)
        {
            isPositive = !isPositive;
        }

        string intensity = absValue switch
        {
            10f => isPositive ? "Enhanced" : "Weakened",
            20f => isPositive ? "Superior" : "Inferior",
            25f => isPositive ? "Enhanced" : "Weakened",
            30f => isPositive ? "Maximum" : "Minimum",
            50f => isPositive ? "Superior" : "Inferior",
            75f => isPositive ? "Maximum" : "Minimum",
            100f => isPositive ? "Maximum" : "Minimum",
            _ => isPositive ? "Boosted" : "Reduced"
        };

        return intensity;
    }
}
