using UnityEngine;

/// <summary>
/// Renders a laser beam between two points using a generated sprite
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class LaserBeam : MonoBehaviour
{
    [Header("Laser Settings")]
    public Color laserColor = Color.red;
    public float width = 0.1f;
    public float duration = 0.2f;

    SpriteRenderer _spriteRenderer;
    float _timeRemaining;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Create a simple red sprite
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
        _spriteRenderer.sprite = sprite;
        _spriteRenderer.color = laserColor;
    }

    void Update()
    {
        _timeRemaining -= Time.deltaTime;
        if (_timeRemaining <= 0)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Sets up the laser beam between two points
    /// </summary>
    /// <param name="start">Start position of the laser</param>
    /// <param name="end">End position of the laser</param>
    public void Setup(Vector3 start, Vector3 end)
    {
        _timeRemaining = duration;

        // Position at midpoint
        Vector3 midpoint = (start + end) / 2f;
        transform.position = midpoint;

        // Calculate length and rotation
        Vector3 direction = end - start;
        float length = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Scale and rotate
        transform.localScale = new Vector3(length, width, 1f);
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// Creates and returns a new laser beam between two points
    /// </summary>
    public static LaserBeam Create(Vector3 start, Vector3 end, Color? color = null, float width = 0.1f, float duration = 0.2f)
    {
        GameObject obj = new GameObject("LaserBeam");
        LaserBeam laser = obj.AddComponent<LaserBeam>();
        
        if (color.HasValue)
        {
            laser.laserColor = color.Value;
        }
        laser.width = width;
        laser.duration = duration;
        
        laser.Setup(start, end);
        
        return laser;
    }
}
