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

    /// <summary>
    /// Current slow percentage (0.5 = 50% slow)
    /// </summary>
    public float SlowPercentage => _slowPercentage;

    /// <summary>
    /// Whether the slow effect is currently active
    /// </summary>
    public bool IsActive => _remainingDuration > 0;

    void Update()
    {
        if (_remainingDuration > 0)
        {
            _remainingDuration -= Time.deltaTime;
            
            if (_remainingDuration <= 0)
            {
                _remainingDuration = 0;
                // Effect has expired - can destroy this component
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
    }
}
