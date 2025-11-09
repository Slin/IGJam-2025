using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Displays a semi-transparent circle showing the attack range of buildings.
/// Shows when hovering over a placed building or during placement preview.
/// </summary>
[DisallowMultipleComponent]
public class AttackRangeIndicator : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color rangeColor = new Color(1f, 1f, 1f, 0.5f); // White with 50% opacity
    public int circleSegments = 64; // Number of segments for circle smoothness
    public float lineWidth = 0.05f;

    private BuildingAttackBehavior _attackBehavior;
    private Building _building;
    private Collider2D _collider;
    private GameObject _rangeCircle;
    private LineRenderer _lineRenderer;
    private bool _isHovered;
    private bool _isPreview;

    void Awake()
    {
        _attackBehavior = GetComponent<BuildingAttackBehavior>();
        _building = GetComponent<Building>();

        // Add collider if not present (needed for mouse detection)
        _collider = GetComponent<Collider2D>();
        if (_collider == null)
        {
            // Add a circle collider as fallback
            var circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.radius = 0.5f;
            _collider = circleCollider;
        }
    }

    void Update()
    {
        // Only show for buildings with attack behavior
        if (_attackBehavior == null) return;

        // Check if this is a preview building
        _isPreview = _building != null && _building.IsPreview;

        // For preview buildings, always show the range
        if (_isPreview)
        {
            if (_rangeCircle == null)
            {
                CreateRangeCircle();
            }
            UpdateRangeCircle();
            return;
        }

        // For placed buildings, only show when hovering
        if (_building == null || !_building.IsPlaced || _building.IsDead)
        {
            HideRangeCircle();
            return;
        }

        // Check if mouse is hovering over this building
        CheckMouseHover();

        if (_isHovered)
        {
            if (_rangeCircle == null)
            {
                CreateRangeCircle();
            }
            UpdateRangeCircle();
        }
        else
        {
            HideRangeCircle();
        }
    }

    void CheckMouseHover()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Mathf.Abs(cam.transform.position.z - transform.position.z)));
        Vector2 worldPos2D = new Vector2(worldPos.x, worldPos.y);

        // Check if mouse is over this building's collider
        _isHovered = _collider.OverlapPoint(worldPos2D);
    }

    void CreateRangeCircle()
    {
        if (_rangeCircle != null) return;

        _rangeCircle = new GameObject("AttackRangeCircle");
        _rangeCircle.transform.SetParent(transform);
        _rangeCircle.transform.localPosition = Vector3.zero;

        _lineRenderer = _rangeCircle.AddComponent<LineRenderer>();
        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;
        _lineRenderer.positionCount = circleSegments + 1;
        _lineRenderer.useWorldSpace = false;
        _lineRenderer.loop = true;

        // Set material and color
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = rangeColor;
        _lineRenderer.endColor = rangeColor;

        // Set sorting layer to render above ground but below UI
        _lineRenderer.sortingLayerName = "Default";
        _lineRenderer.sortingOrder = 50;
    }

    void UpdateRangeCircle()
    {
        if (_lineRenderer == null || _attackBehavior == null) return;

        // Use effective attack range which includes singularity effects
        float radius = _attackBehavior.EffectiveAttackRange;

        // Generate circle points
        float angleStep = 360f / circleSegments;
        for (int i = 0; i <= circleSegments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            _lineRenderer.SetPosition(i, new Vector3(x, y, 0));
        }
    }

    void HideRangeCircle()
    {
        if (_rangeCircle != null)
        {
            Destroy(_rangeCircle);
            _rangeCircle = null;
            _lineRenderer = null;
        }
    }

    void OnDestroy()
    {
        HideRangeCircle();
    }
}
