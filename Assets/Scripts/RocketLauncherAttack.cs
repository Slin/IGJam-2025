using UnityEngine;

/// <summary>
/// Rocket launcher attack - spawns a seeking projectile
/// </summary>
public class RocketLauncherAttack : BuildingAttackBehavior
{
    [Header("Rocket Settings")]
    public GameObject rocketPrefab;
    public float rocketSpeed = 15f;
    public float rocketTurnSpeed = 200f;
    public Vector3 launchOffset = new Vector3(0, 0.5f, 0);

    protected override void PerformAttack(Enemy target)
    {
        if (target == null || target.IsDead) return;

        // Create rocket projectile
        if (rocketPrefab != null)
        {
            Debug.Log("RocketLauncherAttack: Creating rocket projectile");
            Vector3 spawnPos = transform.position + launchOffset;
            GameObject rocketObj = Instantiate(rocketPrefab, spawnPos, Quaternion.identity);

            SeekingRocket rocket = rocketObj.GetComponent<SeekingRocket>();
            if (rocket == null)
            {
                rocket = rocketObj.AddComponent<SeekingRocket>();
            }

            rocket.Initialize(target, EffectiveAttackDamage, rocketSpeed, rocketTurnSpeed);
			// Launch with random initial rotation; rocket will home afterwards
			float randomAngle = Random.Range(0f, 360f);
			rocket.transform.rotation = Quaternion.Euler(0f, 0f, randomAngle);
        }
        else
        {
            // Fallback: instant damage if no prefab
            target.TakeDamage(EffectiveAttackDamage);
        }

        // Play attack sound
        AudioManager.Instance?.PlaySFX("rocket_launch");
    }
}
