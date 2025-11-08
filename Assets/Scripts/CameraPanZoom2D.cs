using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraPanZoom2D : MonoBehaviour
{
    [Header("Zoom")]
    [SerializeField] float minOrthographicSize = 2f;
    [SerializeField] float maxOrthographicSize = 20f;
    [SerializeField] float mouseWheelZoomSpeed = 0.1f;     // Units of size per scroll unit
    [SerializeField] float pinchZoomSpeed = 0.01f;         // Units of size per pixel distance change
    [SerializeField] bool invertScroll = false;

    [Header("Pan")]
    [SerializeField] bool rightMouseDragToPan = true;
    [SerializeField] float inertialPanDamp = 0.0f;         // 0 = no inertia; >0 enables simple damping

    [Header("Optional World Bounds (camera view kept inside)")]
    [SerializeField] bool useWorldBounds = false;
    [SerializeField] Vector2 worldMin = new Vector2(-100, -100);
    [SerializeField] Vector2 worldMax = new Vector2(100, 100);

    Camera _camera;
    Vector3 _velocity; // for simple inertial pan

    bool _mousePanning;
    Vector3 _lastMouseScreenPosition;

    void Awake()
    {
        _camera = GetComponent<Camera>();
        if (!_camera.orthographic)
        {
            _camera.orthographic = true;
        }
        ClampZoom();
        ClampToBounds();
    }

    void OnValidate()
    {
        if (minOrthographicSize < 0.01f) minOrthographicSize = 0.01f;
        if (maxOrthographicSize < minOrthographicSize) maxOrthographicSize = minOrthographicSize;
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        HandleMousePanAndZoom();
        HandleTouchPanAndZoom();
        ApplyInertialPanIfAny();
        ClampToBounds();
    }

    void HandleMousePanAndZoom()
    {
        var mouse = Mouse.current;
        if (mouse != null)
        {
            float scrollY = mouse.scroll.ReadValue().y;
            if (scrollY != 0)
            {
                float sign = invertScroll ? -1f : 1f;
                Vector2 screenPos = mouse.position.ReadValue();
                Vector3 worldBefore = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
                _camera.orthographicSize -= scrollY * mouseWheelZoomSpeed * sign;
                ClampZoom();
                Vector3 worldAfter = _camera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
                Vector3 delta = worldBefore - worldAfter;
                transform.position += new Vector3(delta.x, delta.y, 0f);
                _velocity = Vector3.zero;
            }

            if (rightMouseDragToPan && mouse.rightButton.isPressed)
            {
                Vector2 screenPos = mouse.position.ReadValue();
                if (!_mousePanning)
                {
                    _mousePanning = true;
                    _lastMouseScreenPosition = screenPos;
                    _velocity = Vector3.zero;
                }
                else
                {
                    PanByScreenDelta(screenPos - (Vector2)_lastMouseScreenPosition);
                    _lastMouseScreenPosition = screenPos;
                }
            }
            else
            {
                _mousePanning = false;
            }
        }
    }

    void HandleTouchPanAndZoom()
    {
        var activeTouches = ETouch.activeTouches;
        int touchCount = activeTouches.Count;
        if (touchCount == 1)
        {
            // One-finger pan
            var t = activeTouches[0];
            if (t.delta.sqrMagnitude > 0)
            {
                PanByScreenDelta(t.delta);
            }
        }
        else if (touchCount >= 2)
        {
            var t0 = activeTouches[0];
            var t1 = activeTouches[1];

            Vector2 prev0 = t0.screenPosition - t0.delta;
            Vector2 prev1 = t1.screenPosition - t1.delta;

            float prevDist = (prev0 - prev1).magnitude;
            float currDist = (t0.screenPosition - t1.screenPosition).magnitude;
            float distDelta = currDist - prevDist;

            Vector2 prevMid = (prev0 + prev1) * 0.5f;
            Vector2 currMid = (t0.screenPosition + t1.screenPosition) * 0.5f;
            Vector3 worldBefore = _camera.ScreenToWorldPoint(new Vector3(prevMid.x, prevMid.y, 0));

            _camera.orthographicSize -= distDelta * pinchZoomSpeed;
            ClampZoom();

            Vector3 worldAfter = _camera.ScreenToWorldPoint(new Vector3(currMid.x, currMid.y, 0));
            Vector3 delta = worldBefore - worldAfter;
            transform.position += new Vector3(delta.x, delta.y, 0f);
            _velocity = Vector3.zero;
        }
    }

    void PanByScreenDelta(Vector2 screenDelta)
    {
        if (screenDelta.sqrMagnitude == 0) return;

        Vector3 before = _camera.ScreenToWorldPoint(new Vector3(0, 0, 0));
        Vector3 after = _camera.ScreenToWorldPoint(new Vector3(screenDelta.x, screenDelta.y, 0));
        Vector3 worldDelta = before - after;

        transform.position += new Vector3(worldDelta.x, worldDelta.y, 0f);
        _velocity = new Vector3(worldDelta.x, worldDelta.y, 0f) / Time.deltaTime;
    }

    void ApplyInertialPanIfAny()
    {
        if (inertialPanDamp <= 0f) return;
        if (_mousePanning) return; // active input cancels inertia

        if (_velocity.sqrMagnitude > 0.0001f)
        {
            transform.position += _velocity * Time.deltaTime;
            _velocity = Vector3.Lerp(_velocity, Vector3.zero, inertialPanDamp * Time.deltaTime);
        }
    }

    void ClampZoom()
    {
        _camera.orthographicSize = Mathf.Clamp(_camera.orthographicSize, minOrthographicSize, maxOrthographicSize);
    }

    void ClampToBounds()
    {
        if (!useWorldBounds) return;

        float halfHeight = _camera.orthographicSize;
        float halfWidth = halfHeight * _camera.aspect;

        float minX = worldMin.x + halfWidth;
        float maxX = worldMax.x - halfWidth;
        float minY = worldMin.y + halfHeight;
        float maxY = worldMax.y - halfHeight;

        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, minX, maxX);
        p.y = Mathf.Clamp(p.y, minY, maxY);
        transform.position = p;
    }
}


