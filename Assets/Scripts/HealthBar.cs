using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a health bar above game objects (buildings and enemies)
/// </summary>
public class HealthBar : MonoBehaviour
{
    [Header("Health Bar Settings")]
    public Vector3 offset = new Vector3(0, 1.5f, 0);
    public Vector2 size = new Vector2(1f, 0.15f);

    [Header("Visual Settings")]
    public Color fullHealthColor = Color.green;
    public Color halfHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color missingHealthColor = new Color(0.5f, 0f, 0f, 1f); // Dark red

    [Header("Behavior")]
    public bool hideWhenFull = false;
    public bool alwaysShow = false;

    Canvas _canvas;
    RectTransform _canvasRect;
    Image _backgroundImage;
    Image _fillImage;
    Image _missingHealthImage;
    GameObject _barContainer;

    Transform _target;
    Camera _mainCamera;
    float _currentHealthPercent = 1f;

    void Awake()
    {
        CreateHealthBarUI();
        _mainCamera = Camera.main;
    }

    void CreateHealthBarUI()
    {
        // Create canvas for health bar
        GameObject canvasObj = new GameObject("HealthBarCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = offset;

        _canvas = canvasObj.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        _canvasRect = canvasObj.GetComponent<RectTransform>();
        _canvasRect.sizeDelta = size * 100f; // Higher resolution for canvas
        _canvasRect.localScale = Vector3.one * 0.01f; // Scale to match world units

        // Create background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = Vector3.one;

        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        _backgroundImage = bgObj.AddComponent<Image>();
        _backgroundImage.color = backgroundColor;
        _backgroundImage.sprite = CreateWhiteSprite();

        // Create missing health bar (dark red behind the fill)
        GameObject missingHealthObj = new GameObject("MissingHealth");
        missingHealthObj.transform.SetParent(canvasObj.transform);
        missingHealthObj.transform.localPosition = Vector3.zero;
        missingHealthObj.transform.localScale = Vector3.one;

        RectTransform missingHealthRect = missingHealthObj.AddComponent<RectTransform>();
        missingHealthRect.anchorMin = new Vector2(0, 0);
        missingHealthRect.anchorMax = new Vector2(1, 1);
        missingHealthRect.sizeDelta = new Vector2(-4, -4); // Padding
        missingHealthRect.anchoredPosition = Vector2.zero;

        _missingHealthImage = missingHealthObj.AddComponent<Image>();
        _missingHealthImage.color = missingHealthColor;
        _missingHealthImage.sprite = CreateWhiteSprite();

        // Create fill (health bar) - this goes on top
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(canvasObj.transform);
        fillObj.transform.localPosition = Vector3.zero;
        fillObj.transform.localScale = Vector3.one;

        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.sizeDelta = new Vector2(-4, -4); // Padding
        fillRect.anchoredPosition = Vector2.zero;

        _fillImage = fillObj.AddComponent<Image>();
        _fillImage.color = fullHealthColor;
        _fillImage.sprite = CreateWhiteSprite();
        _fillImage.type = Image.Type.Filled;
        _fillImage.fillMethod = Image.FillMethod.Horizontal;
        _fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;

        _barContainer = canvasObj;

        // Start hidden if hideWhenFull is enabled
        if (hideWhenFull && !alwaysShow)
        {
            _barContainer.SetActive(false);
        }
    }

    void LateUpdate()
    {
        // Make health bar face camera
        if (_mainCamera != null && _canvas != null)
        {
            _canvas.transform.rotation = _mainCamera.transform.rotation;
        }
    }

    /// <summary>
    /// Updates the health bar display
    /// </summary>
    /// <param name="currentHealth">Current health value</param>
    /// <param name="maxHealth">Maximum health value</param>
    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (maxHealth <= 0) return;

        _currentHealthPercent = Mathf.Clamp01(currentHealth / maxHealth);

        if (_fillImage != null)
        {
            _fillImage.fillAmount = _currentHealthPercent;
            Debug.Log($"Set fillAmount to {_currentHealthPercent}");

            // Update color based on health percentage
            if (_currentHealthPercent > 0.5f)
            {
                _fillImage.color = Color.Lerp(halfHealthColor, fullHealthColor, (_currentHealthPercent - 0.5f) * 2f);
            }
            else
            {
                _fillImage.color = Color.Lerp(lowHealthColor, halfHealthColor, _currentHealthPercent * 2f);
            }
        }

        // Show/hide based on settings
        if (_barContainer != null && !alwaysShow)
        {
            bool shouldShow = !hideWhenFull || _currentHealthPercent < 1f;
            _barContainer.SetActive(shouldShow);
        }
        else if (_barContainer != null && alwaysShow)
        {
            _barContainer.SetActive(true);
        }
    }

    /// <summary>
    /// Sets whether the health bar should always be visible
    /// </summary>
    public void SetAlwaysShow(bool show)
    {
        alwaysShow = show;
        if (_barContainer != null)
        {
            if (alwaysShow)
            {
                _barContainer.SetActive(true);
            }
            else
            {
                _barContainer.SetActive(!hideWhenFull || _currentHealthPercent < 1f);
            }
        }
    }

    /// <summary>
    /// Sets the offset of the health bar from the parent object
    /// </summary>
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
        if (_canvas != null)
        {
            _canvas.transform.localPosition = offset;
        }
    }

    /// <summary>
    /// Sets the size of the health bar
    /// </summary>
    public void SetSize(Vector2 newSize)
    {
        size = newSize;
        if (_canvasRect != null)
        {
            _canvasRect.sizeDelta = size * 100f;
        }
    }

    Sprite CreateWhiteSprite()
    {
        // Create a simple 1x1 white texture
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }
}
