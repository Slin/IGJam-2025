using UnityEngine;

/// <summary>
/// A seeking projectile that homes in on its target
/// </summary>
public class SeekingRocket : MonoBehaviour
{
    Enemy _target;
    float _damage;
    float _speed;
    float _turnSpeed;
    float _lifetime = 10f; // Auto-destroy after this time

    public void Initialize(Enemy target, float damage, float speed, float turnSpeed)
    {
        _target = target;
        _damage = damage;
        _speed = speed;
        _turnSpeed = turnSpeed;
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

        // Check if target is valid
        if (_target == null || _target.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        // Move towards target
        Vector3 direction = (_target.transform.position - transform.position).normalized;
        
        // Rotate towards target
        if (direction != Vector3.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnSpeed * Time.deltaTime);
        }

        // Move forward
        transform.position += transform.right * _speed * Time.deltaTime;

        // Check for collision (simple distance check)
        float distanceToTarget = Vector3.Distance(transform.position, _target.transform.position);
        if (distanceToTarget < 0.3f)
        {
            // Hit the target
            _target.TakeDamage(_damage);
            
            // Play impact effect/sound
            AudioManager.Instance?.PlaySFX("explosion");
            
            // Destroy rocket
            Destroy(gameObject);
        }
    }
}
