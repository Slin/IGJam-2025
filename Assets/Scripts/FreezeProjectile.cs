using UnityEngine;

/// <summary>
/// Projectile for the freeze tower that applies a slow effect on hit
/// </summary>
public class FreezeProjectile : MonoBehaviour
{
    Enemy _target;
    float _slowPercentage;
    float _slowDuration;
    float _speed;
    float _lifetime = 10f; // Auto-destroy after this time
    bool _outbound; // when true, fly straight until offscreen then disappear

    public void Initialize(Enemy target, float slowPercentage, float slowDuration, float speed)
    {
        _target = target;
        _slowPercentage = slowPercentage;
        _slowDuration = slowDuration;
        _speed = speed;
    }

    void Update()
    {
        _lifetime -= Time.deltaTime;

        // Destroy if lifetime expired
        if (_lifetime <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // If we lost target, try to retarget once
        if (!_outbound && (_target == null || _target.IsDead))
        {
            var next = SpawnerManager.Instance != null
                ? SpawnerManager.Instance.GetClosestEnemy(transform.position, Mathf.Infinity)
                : null;
            if (next != null && !next.IsDead)
            {
                _target = next;
            }
            else
            {
                _outbound = true;
            }
        }

        // Outbound mode: fly straight until offscreen then destroy
        if (_outbound)
        {
            transform.position += transform.right * _speed * Time.deltaTime;
            var cam = Camera.main;
            if (cam != null)
            {
                var vp = cam.WorldToViewportPoint(transform.position);
                if (vp.z > 0 && (vp.x < -0.1f || vp.x > 1.1f || vp.y < -0.1f || vp.y > 1.1f))
                {
                    Destroy(gameObject);
                    return;
                }
            }
            return;
        }

        // Move towards target
        Vector3 direction = (_target.transform.position - transform.position).normalized;

        // Rotate towards target
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 360f * Time.deltaTime);
        }

        // Move forward
        transform.position += transform.right * _speed * Time.deltaTime;

        // Check for collision (simple distance check)
        float distanceToTarget = Vector3.Distance(transform.position, _target.transform.position);
        if (distanceToTarget < 0.3f)
        {
            // Hit the target - apply slow effect
            ApplySlowEffect(_target);

            // Play impact effect/sound
            AudioManager.Instance?.PlaySFX("freeze_impact");

            // Destroy projectile
            Destroy(gameObject);
        }
    }

    void ApplySlowEffect(Enemy target)
    {
        if (target == null || target.IsDead) return;

        // Get or add SlowEffect component
        SlowEffect slowEffect = target.GetComponent<SlowEffect>();
        if (slowEffect == null)
        {
            slowEffect = target.gameObject.AddComponent<SlowEffect>();
        }

        // Apply the slow (will refresh if already slowed)
        slowEffect.ApplySlow(_slowPercentage, _slowDuration);
    }
}
