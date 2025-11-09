using System;
using UnityEngine;

/// <summary>
/// Types of effects that can modify gameplay
/// </summary>
public enum SingularityEffectType
{
    BuildingAttackDamage,    // Modifies attack damage of buildings
    BuildingAttackRange,     // Modifies attack range of buildings
    BuildingDamageTaken,     // Modifies damage buildings receive
    EnemyDamageDealt,        // Modifies damage enemies deal
    EnemyDamageTaken,        // Modifies damage enemies receive
    EnemySpeed,              // Modifies enemy movement speed
    DroneSpeed,              // Modifies drone movement speed
    DroneDamage              // Modifies drone attack damage
}

/// <summary>
/// Represents a single singularity effect that can modify various gameplay parameters
/// </summary>
[Serializable]
public class SingularityEffect
{
    [Header("Effect Definition")]
    public string effectName = "Mystery Effect";
    public SingularityEffectType effectType;

    [Header("Effect Value")]
    [Tooltip("Percentage modifier from -100 to 100. Positive values increase, negative decrease.")]
    [Range(-100f, 100f)]
    public float valuePercent = 0f;

    [Header("Type Restrictions")]
    [Tooltip("Specific enemy type affected. Leave as Regular to affect all enemies.")]
    public EnemyType targetEnemyType = EnemyType.Regular;

    [Tooltip("If true, affects all enemy types. If false, only affects targetEnemyType.")]
    public bool affectsAllEnemies = true;

    [Tooltip("Specific building type affected. Leave as Base to affect all buildings.")]
    public BuildingType targetBuildingType = BuildingType.Base;

    [Tooltip("If true, affects all building types. If false, only affects targetBuildingType.")]
    public bool affectsAllBuildings = true;

    [Header("Round Restrictions")]
    [Tooltip("Minimum round number where this effect can be drawn (inclusive)")]
    public int minRound = 1;

    [Tooltip("Maximum round number where this effect can be drawn (inclusive). Set to -1 for no limit.")]
    public int maxRound = -1;

    /// <summary>
    /// Check if this effect can be drawn in the specified round
    /// </summary>
    public bool CanBeDrawnInRound(int round)
    {
        if (round < minRound) return false;
        if (maxRound >= 0 && round > maxRound) return false;
        return true;
    }

    /// <summary>
    /// Check if this effect applies to a specific enemy type
    /// </summary>
    public bool AppliesTo(EnemyType enemyType)
    {
        if (affectsAllEnemies) return true;
        return enemyType == targetEnemyType;
    }

    /// <summary>
    /// Check if this effect applies to a specific building type
    /// </summary>
    public bool AppliesTo(BuildingType buildingType)
    {
        if (affectsAllBuildings) return true;
        return buildingType == targetBuildingType;
    }

    /// <summary>
    /// Get the multiplier to apply (e.g., 50% = 1.5x, -30% = 0.7x)
    /// </summary>
    public float GetMultiplier()
    {
        return 1f + (valuePercent / 100f);
    }

    /// <summary>
    /// Get a display-friendly description of this effect
    /// </summary>
    public string GetDescription()
    {
        string sign = valuePercent >= 0 ? "+" : "";
        string typeName = effectType switch
        {
            SingularityEffectType.BuildingAttackDamage => "Building Attack Damage",
            SingularityEffectType.BuildingAttackRange => "Building Attack Range",
            SingularityEffectType.BuildingDamageTaken => "Building Damage Taken",
            SingularityEffectType.EnemyDamageDealt => "Enemy Damage Dealt",
            SingularityEffectType.EnemyDamageTaken => "Enemy Damage Taken",
            SingularityEffectType.EnemySpeed => "Enemy Speed",
            SingularityEffectType.DroneSpeed => "Drone Speed",
            SingularityEffectType.DroneDamage => "Drone Damage",
            _ => "Unknown Effect"
        };

        // Add target type info if not affecting all
        string targetInfo = "";
        if (!affectsAllEnemies && (effectType == SingularityEffectType.EnemyDamageDealt ||
                                    effectType == SingularityEffectType.EnemyDamageTaken ||
                                    effectType == SingularityEffectType.EnemySpeed))
        {
            targetInfo = $" ({targetEnemyType})";
        }
        else if (!affectsAllBuildings && (effectType == SingularityEffectType.BuildingAttackDamage ||
                                           effectType == SingularityEffectType.BuildingAttackRange ||
                                           effectType == SingularityEffectType.BuildingDamageTaken))
        {
            targetInfo = $" ({targetBuildingType})";
        }

        return $"{effectName}: {typeName}{targetInfo} {sign}{valuePercent:F0}%";
    }
}
