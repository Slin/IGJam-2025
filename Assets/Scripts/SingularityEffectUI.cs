using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// UI component that displays the current active singularity effects.
/// Automatically updates when effects change.
/// </summary>
[DisallowMultipleComponent]
public class SingularityEffectUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("TextMeshPro component to display the effect description")]
    public TextMeshProUGUI effectText;

    [Header("Display Settings")]
    [Tooltip("Text to show when no effects are active")]
    public string noEffectText = "No Active Effect";

    [Tooltip("Separator between multiple effects")]
    public string effectSeparator = "\n";

    void Start()
    {
        // Find TextMeshProUGUI component if not assigned
        if (effectText == null)
        {
            effectText = GetComponent<TextMeshProUGUI>();
        }

        // Subscribe to effect changes
        if (SingularityEffectManager.Instance != null)
        {
            SingularityEffectManager.Instance.onEffectsChanged.AddListener(OnEffectsChanged);
            
            // Initialize with current effects
            UpdateEffectDisplay(SingularityEffectManager.Instance.ActiveEffects);
        }
        else
        {
            Debug.LogWarning("SingularityEffectUI: SingularityEffectManager not found");
            UpdateEffectDisplay(new List<SingularityEffect>());
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (SingularityEffectManager.Instance != null)
        {
            SingularityEffectManager.Instance.onEffectsChanged.RemoveListener(OnEffectsChanged);
        }
    }

    /// <summary>
    /// Called when the active effects change
    /// </summary>
    void OnEffectsChanged(List<SingularityEffect> activeEffects)
    {
        UpdateEffectDisplay(activeEffects);
    }

    /// <summary>
    /// Update the text display with current effects
    /// </summary>
    void UpdateEffectDisplay(IReadOnlyList<SingularityEffect> activeEffects)
    {
        if (effectText == null)
        {
            Debug.LogWarning("SingularityEffectUI: effectText is not assigned");
            return;
        }

        if (activeEffects == null || activeEffects.Count == 0)
        {
            effectText.text = noEffectText;
            return;
        }

        // Build description string
        string description = "";
        for (int i = 0; i < activeEffects.Count; i++)
        {
            if (activeEffects[i] != null)
            {
                description += activeEffects[i].GetDescription();
                
                // Add separator if not the last effect
                if (i < activeEffects.Count - 1)
                {
                    description += effectSeparator;
                }
            }
        }

        effectText.text = description;
    }
}
