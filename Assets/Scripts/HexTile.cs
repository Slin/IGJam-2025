using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class HexTile : MonoBehaviour
{
    public InputActionReference mousePosition;
    private float hexRadius = 0.5f / 1.1f; // circumradius (center to vertex) in local units

    public Color _baseColor;
    public Color _highlightColor;
    public Color _neutralMouseoverColor = Color.cyan;
    public Color _validPlacementColor = Color.green;
    public Color _invalidPlacementColor = Color.red;

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

		// Building placement preview + placement
		var bm = BuildingManager.Instance;
		if (bm != null && bm.IsPlacingBuilding)
		{
			if (inside)
			{
				// Always snap preview to this tile's center when hovering
				bm.UpdateBuildingPreview(transform.position);

				// Place on click (ignore clicks over UI)
				var mouse = Mouse.current;
				bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
				if (mouse != null && mouse.leftButton.wasPressedThisFrame && !overUI)
				{
					bm.TryPlaceBuilding(transform.position, this);
				}
			}

			// Determine which tiles are affected by the building placement
			bool isAffectedByBuilding = bm.IsTileAffectedByPlacement(transform.position);
			
			if (isAffectedByBuilding)
			{
				// This tile would be occupied by the building
				// Check validity using the tile that's being hovered (which updates the preview position)
				bool canPlace = bm.IsCurrentPlacementValid();
				SetHighlight(true, canPlace ? _validPlacementColor : _invalidPlacementColor);
			}
			else if (_isOccupied)
			{
				// Not affected by building but this tile is blocked
				SetHighlight(true, _invalidPlacementColor);
			}
			else
			{
				SetHighlight(false);
			}
		}
		else
		{
			// Not placing a building - use neutral cyan color on mouseover
			if (inside)
			{
				SetHighlight(true, _neutralMouseoverColor);
			}
			else
			{
				SetHighlight(false);
			}
		}
    }

    void SetHighlight(bool on, Color? customColor = null)
    {
        Color targetColor = on ? (customColor ?? _highlightColor) : _baseColor;
        
        if (_renderer == null) return;
        
        if (_renderer.material.color != targetColor)
            _renderer.material.color = targetColor;
        
        _isHighlighted = on;
    }
}
