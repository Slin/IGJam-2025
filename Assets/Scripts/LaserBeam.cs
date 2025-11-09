using UnityEngine;

/// <summary>
/// Renders a laser beam between two points using a prefabbed mesh (MeshRenderer)
/// </summary>
public class LaserBeam : MonoBehaviour
{
    [Header("Laser Settings")]
    public Color laserColor = Color.red;
    public float duration = 0.2f;

    MeshRenderer[] _renderers;
    float _timeRemaining;
    Vector3 _startWorld;
    Enemy _endEnemy;
    bool _followTarget;

    void Awake()
    {
        _renderers = GetComponentsInChildren<MeshRenderer>(true);
    }

    void Update()
    {
        _timeRemaining -= Time.deltaTime;
        if (_timeRemaining <= 0)
        {
            Destroy(gameObject);
        }

        if (_followTarget)
        {
            if (_endEnemy == null || _endEnemy.IsDead)
            {
                Destroy(gameObject);
                return;
            }

            UpdateTransform(_startWorld, _endEnemy.transform.position);
        }
    }

    /// <summary>
    /// Sets up the laser beam between two points
    /// </summary>
    /// <param name="start">Start position of the laser</param>
    /// <param name="end">End position of the laser</param>
    public void Setup(Vector3 start, Vector3 end)
    {
        ApplyColorToRenderers();

        _timeRemaining = duration;

        _followTarget = false;
        _startWorld = start;
        UpdateTransform(start, end);
    }

    /// <summary>
    /// Sets up the laser to follow a live target from a fixed start.
    /// The beam stops when the target dies or duration elapses.
    /// </summary>
    public void SetupFollow(Vector3 start, Enemy target)
    {
        ApplyColorToRenderers();

        _timeRemaining = duration;
        _followTarget = true;
        _startWorld = start;
        _endEnemy = target;
        var end = target != null ? target.transform.position : start;
        UpdateTransform(start, end);
    }

    void ApplyColorToRenderers()
    {
        if (_renderers == null || _renderers.Length == 0) return;
        for (int i = 0; i < _renderers.Length; i++)
        {
            var r = _renderers[i];
            if (r == null) continue;
            var mats = r.materials; // instantiates materials
            for (int m = 0; m < mats.Length; m++)
            {
                var mat = mats[m];
                if (mat == null) continue;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", laserColor);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", laserColor);
            }
            r.materials = mats;
        }
    }
    void UpdateTransform(Vector3 start, Vector3 end)
    {
        // Position at start (prefab offset stretches away from start)
        transform.position = start;

        // Calculate length and rotation
        Vector3 direction = end - start;
        float length = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Scale and rotate (prefab is offset to extend from start; keep prefab width)
        var s = transform.localScale;
        transform.localScale = new Vector3(length, s.y, s.z);
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// Creates and returns a new laser beam between two points using a prefab
    /// </summary>
    public static LaserBeam Create(Vector3 start, Vector3 end, Color? color = null, float width = 0.1f, float duration = 0.2f)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.laserBeamPrefab == null)
        {
            Debug.LogError("LaserBeam: GameManager.laserBeamPrefab is not assigned.");
            return null;
        }

        LaserBeam laser = Object.Instantiate(gm.laserBeamPrefab);
        if (color.HasValue) laser.laserColor = color.Value;
        laser.duration = duration;
        laser.Setup(start, end);
        return laser;
    }

    /// <summary>
    /// Creates a laser that follows the target while alive from a fixed start.
    /// </summary>
    public static LaserBeam CreateTracking(Vector3 start, Enemy target, Color? color = null, float width = 0.1f, float duration = 0.2f)
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.laserBeamPrefab == null)
        {
            Debug.LogError("LaserBeam: GameManager.laserBeamPrefab is not assigned.");
            return null;
        }
        LaserBeam laser = Object.Instantiate(gm.laserBeamPrefab);
        if (color.HasValue) laser.laserColor = color.Value;
        laser.duration = duration;
        laser.SetupFollow(start, target);
        return laser;
    }
}
