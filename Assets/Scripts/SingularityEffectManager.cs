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

        // Define effect types to generate
        var effectTypes = new[]
        {
            SingularityEffectType.BuildingAttackDamage,
            SingularityEffectType.BuildingAttackRange,
            SingularityEffectType.BuildingDamageTaken,
            SingularityEffectType.EnemyDamageDealt,
            SingularityEffectType.EnemyDamageTaken,
            SingularityEffectType.EnemySpeed,
            SingularityEffectType.DroneSpeed,
            SingularityEffectType.DroneDamage
        };

        foreach (var effectType in effectTypes)
        {
            // ±25% effects (rounds 1-7)
            effectPool.Add(CreateEffect(effectType, 25f, 1, 7));
            effectPool.Add(CreateEffect(effectType, -25f, 1, 7));

            // ±50% effects (rounds 8-15)
            effectPool.Add(CreateEffect(effectType, 50f, 8, 15));
            effectPool.Add(CreateEffect(effectType, -50f, 8, 15));

            // ±100% effects (rounds 16+)
            effectPool.Add(CreateEffect(effectType, 100f, 16, -1));
            effectPool.Add(CreateEffect(effectType, -100f, 16, -1));
        }

        Debug.Log($"SingularityEffectManager: Populated with {effectPool.Count} effects");
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
            25f => isPositive ? "Enhanced" : "Weakened",
            50f => isPositive ? "Superior" : "Inferior",
            100f => isPositive ? "Maximum" : "Minimum",
            _ => isPositive ? "Boosted" : "Reduced"
        };

        return intensity;
    }
}
