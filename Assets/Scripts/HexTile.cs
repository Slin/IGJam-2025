using UnityEngine;
using UnityEngine.InputSystem;

public class HexTile : MonoBehaviour
{
    public InputActionReference mousePosition;
    private float hexRadius = 0.5f / 1.1f; // circumradius (center to vertex) in local units
    private int highlightOrderOffset = 100;

   private Renderer _renderer;
   private int _baseSortingLayerId;
   private int _baseSortingOrder;
   private bool _isHighlighted;

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
        if(inside)
        {
            if (_renderer != null)
            {
                if (_renderer.material.color != Color.red)
                    _renderer.material.color = Color.red;
            }
            if (!_isHighlighted) { SetHighlighted(true); }
        }
        else
        {
            if (_renderer != null)
            {
                if (_renderer.material.color != Color.white)
                    _renderer.material.color = Color.white;
            }
            if (_isHighlighted) { SetHighlighted(false); }
        }
    }

    void SetHighlighted(bool on)
    {
        _isHighlighted = on;
        if (_renderer == null) return;
        if (on)
        {
            _renderer.sortingLayerID = _baseSortingLayerId;
            _renderer.sortingOrder = _baseSortingOrder + highlightOrderOffset;
        }
        else
        {
            _renderer.sortingLayerID = _baseSortingLayerId;
            _renderer.sortingOrder = _baseSortingOrder;
        }
    }
}
