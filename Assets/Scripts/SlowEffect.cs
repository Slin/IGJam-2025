using UnityEngine;

/// <summary>
/// Component that tracks a slow effect on an enemy.
/// Slows down the enemy's movement speed by a percentage for a duration.
/// Does not stack - refreshes duration instead.
/// </summary>
public class SlowEffect : MonoBehaviour
{
    float _slowPercentage;
    float _remainingDuration;
    SpriteRenderer[] _spriteRenderers;
    Color[] _originalColors;
    Color _slowTint = new Color(0.5f, 0.5f, 1f, 1f); // Blue tint

    /// <summary>
    /// Current slow percentage (0.5 = 50% slow)
    /// </summary>
    public float SlowPercentage => _slowPercentage;

    /// <summary>
    /// Whether the slow effect is currently active
    /// </summary>
    public bool IsActive => _remainingDuration > 0;

    void Awake()
    {
        // Cache sprite renderers and their original colors
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        _originalColors = new Color[_spriteRenderers.Length];
        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            if (_spriteRenderers[i] != null)
            {
                _originalColors[i] = _spriteRenderers[i].color;
            }
        }
    }

    void Update()
    {
        if (_remainingDuration > 0)
        {
            _remainingDuration -= Time.deltaTime;

            if (_remainingDuration <= 0)
            {
                _remainingDuration = 0;
                // Effect has expired - restore colors and destroy component
                RestoreOriginalColors();
                Destroy(this);
            }
        }
    }

    /// <summary>
    /// Apply or refresh the slow effect
    /// </summary>
    /// <param name="slowPercentage">Percentage to slow (0.5 = 50% slower)</param>
    /// <param name="duration">Duration in seconds</param>
    public void ApplySlow(float slowPercentage, float duration)
    {
        _slowPercentage = slowPercentage;
        _remainingDuration = duration;
        ApplySlowTint();
    }

    void ApplySlowTint()
    {
        if (_spriteRenderers == null) return;

        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            if (_spriteRenderers[i] != null)
            {
                _spriteRenderers[i].color = _slowTint;
            }
        }
    }

    void RestoreOriginalColors()
    {
        if (_spriteRenderers == null || _originalColors == null) return;

        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            if (_spriteRenderers[i] != null && i < _originalColors.Length)
            {
                _spriteRenderers[i].color = _originalColors[i];
            }
        }
    }

    void OnDestroy()
    {
        // Ensure colors are restored when component is destroyed
        RestoreOriginalColors();
    }
}
