using UnityEngine;
using UnityEngine.InputSystem;

public class HexTile : MonoBehaviour
{
    public InputActionReference mousePosition;
    private float hexRadius = 0.5f / 1.1f; // circumradius (center to vertex) in local units
    private int highlightOrderOffset = 100;

    public Color _baseColor;
    public Color _highlightColor;

   private Renderer _renderer;
   private int _baseSortingLayerId;
   private int _baseSortingOrder;
   private bool _isHighlighted;
   private bool _isOccupied = false;

    public bool IsOccupied => _isOccupied;

    public void SetOccupied(bool occupied)
    {
        _isOccupied = occupied;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mousePosition.action.Enable();
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _baseSortingLayerId = _renderer.sortingLayerID;
            _baseSortingOrder = _renderer.sortingOrder;
        }

        _isHighlighted = true;
        SetHighlight(false);
    }

    bool IsPointInHexagon(Vector2 point)
    {
        // Convert world point to local (XY plane)
        Vector3 localP3 = transform.InverseTransformPoint(new Vector3(point.x, point.y, transform.position.z));
        Vector2 p = new Vector2(localP3.x, localP3.y);

        // SDF for regular hexagon (adapted from GridTileSDF.hlsl 3-10)
        Vector3 k = new Vector3(-0.866025404f, 0.5f, 0.577350269f);
        float r = hexRadius * 0.866025404f; // convert circumradius to apothem

        p = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y));
        float proj = Mathf.Min(Vector2.Dot(new Vector2(k.x, k.y), p), 0f);
        p -= 2.0f * proj * new Vector2(k.x, k.y);
        p -= new Vector2(Mathf.Clamp(p.x, -k.z * r, k.z * r), r);
        float distance = p.magnitude * Mathf.Sign(p.y);

        return distance <= 0f;
    }

    // Update is called once per frame
    void Update()
    {
        //Check if mouse coursor is inside the hexagon of this tile
        Vector2 mousePos = mousePosition.action.ReadValue<Vector2>();
        var cam = Camera.main;
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Mathf.Abs(cam.transform.position.z - transform.position.z)));
        bool inside = IsPointInHexagon(new Vector2(world.x, world.y));
		SetHighlight(inside);

		// Building placement preview + placement
		var bm = BuildingManager.Instance;
		if (bm != null && bm.IsPlacingBuilding)
		{
			if (inside)
			{
				// Snap preview to this tile's center
				bm.UpdateBuildingPreview(transform.position);

				// Place on click
				var mouse = Mouse.current;
				if (mouse != null && mouse.leftButton.wasPressedThisFrame)
				{
					bm.TryPlaceBuilding(transform.position, this);
				}
			}
		}
    }

    void SetHighlight(bool on)
    {
        if(_isHighlighted == on) return;
        _isHighlighted = on;
        if(_renderer == null) return;
        if(on)
        {
            _renderer.sortingLayerID = _baseSortingLayerId;
            _renderer.sortingOrder = _baseSortingOrder + highlightOrderOffset;
            if (_renderer.material.color != _highlightColor)
                _renderer.material.color = _highlightColor;
        }
        else
        {
            _renderer.sortingLayerID = _baseSortingLayerId;
            _renderer.sortingOrder = _baseSortingOrder;
            if (_renderer.material.color != _baseColor)
                _renderer.material.color = _baseColor;
        }
    }
}
