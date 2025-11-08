using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Displays a tooltip when hovering over buildings with attack behaviors
/// Shows boost information and other stats
/// </summary>
[DisallowMultipleComponent]
public class BuildingTooltip : MonoBehaviour
{
    [Header("Settings")]
    public Vector3 tooltipOffset = new Vector3(0, 2.0f, 0);
    public Vector2 tooltipSize = new Vector2(3.0f, 1.0f);
    public Color backgroundColor = new Color(0, 0, 0, 0.85f);
    public Color textColor = Color.white;
    public float fontSize = 0.22f;
    public Vector2 textPadding = new Vector2(0.15f, 0.1f);

    private BuildingAttackBehavior _attackBehavior;
    private Building _building;
    private GameObject _tooltipObject;
    private Canvas _canvas;
    private TMP_Text _text;
    private bool _isHovered;
    private Collider2D _collider;

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
        // Only show tooltip if building is placed and alive
        if (_building == null || !_building.IsPlaced || _building.IsDead)
        {
            HideTooltip();
            return;
        }

        // Check if mouse is hovering over this building
        CheckMouseHover();

        // Update tooltip content if visible
        if (_isHovered && _tooltipObject != null)
        {
            UpdateTooltipContent();
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
        bool wasHovered = _isHovered;
        _isHovered = _collider.OverlapPoint(worldPos2D);

        if (_isHovered && !wasHovered)
        {
            ShowTooltip();
        }
        else if (!_isHovered && wasHovered)
        {
            HideTooltip();
        }
    }

    void ShowTooltip()
    {
        if (_tooltipObject != null) return;
        if (_building == null) return; // Need a building to show tooltip

        CreateTooltip();
        UpdateTooltipContent();
    }

    void HideTooltip()
    {
        if (_tooltipObject != null)
        {
            Destroy(_tooltipObject);
            _tooltipObject = null;
            _canvas = null;
            _text = null;
        }
    }

    void CreateTooltip()
    {
        // Create canvas for tooltip
        _tooltipObject = new GameObject("BuildingTooltip");
        _tooltipObject.transform.SetParent(transform);
        _tooltipObject.transform.localPosition = tooltipOffset;

        _canvas = _tooltipObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;
        _canvas.sortingOrder = 2000; // Higher than health bars

        CanvasScaler scaler = _tooltipObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        RectTransform canvasRect = _tooltipObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = tooltipSize * 100f;
        canvasRect.localScale = Vector3.one * 0.01f;

        // Create background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(_tooltipObject.transform);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = backgroundColor;

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.localPosition = Vector3.zero;
        bgRect.localScale = Vector3.one;

        // Create text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(_tooltipObject.transform);
        _text = textObj.AddComponent<TextMeshProUGUI>();
        _text.color = textColor;
        _text.fontSize = fontSize * 100f;
        _text.alignment = TextAlignmentOptions.Center;
        _text.textWrappingMode = TMPro.TextWrappingModes.Normal;
        _text.overflowMode = TMPro.TextOverflowModes.Overflow;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-textPadding.x * 200f, -textPadding.y * 200f); // Apply padding
        textRect.localPosition = Vector3.zero;
        textRect.localScale = Vector3.one;
    }

    void UpdateTooltipContent()
    {
        if (_text == null) return;

        string tooltip = "";

        // Show health for all buildings
        if (_building != null)
        {
            float currentHealth = _building.CurrentHealth;
            float maxHealth = _building.maxHealth;
            tooltip = $"HP: {currentHealth:F0}/{maxHealth:F0}";
        }

        // Add damage info for attack buildings
        if (_attackBehavior != null)
        {
            // Special handling for DroneFactory
            var droneFactory = _attackBehavior as DroneFactoryAttackBehavior;
            if (droneFactory != null)
            {
                int currentDrones = droneFactory.GetActiveDroneCount();
                int maxDrones = droneFactory.EffectiveDroneLimit;
                int boostCount = droneFactory.GetActiveBoostCount();
                
                tooltip += $"\nDrones: {currentDrones}/{maxDrones}";
                if (boostCount > 0)
                {
                    tooltip += $"\n[{boostCount} Booster{(boostCount > 1 ? "s" : "")}]";
                }
            }
            else
            {
                // Regular attack buildings show damage
                float baseDamage = _attackBehavior.attackDamage;
                float effectiveDamage = _attackBehavior.EffectiveAttackDamage;
                float boostPercent = ((effectiveDamage / baseDamage) - 1f) * 100f;

                if (boostPercent > 0.01f)
                {
                    int boostCount = _attackBehavior.GetActiveBoostCount();
                    tooltip += $"\nDamage: {effectiveDamage:F1} (+{boostPercent:F0}%)";
                    if (boostCount > 0)
                    {
                        tooltip += $"\n[{boostCount} Booster{(boostCount > 1 ? "s" : "")}]";
                    }
                }
                else
                {
                    tooltip += $"\nDamage: {effectiveDamage:F1}";
                }
            }
        }

        _text.text = tooltip;
    }

    void OnDestroy()
    {
        HideTooltip();
    }
}
