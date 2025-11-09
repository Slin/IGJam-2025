using UnityEngine;

[DisallowMultipleComponent]
public class FollowCameraWorld : MonoBehaviour
{
	[Header("Target")]
	public Camera targetCamera; // If null, uses Camera.main
	public Vector3 worldOffset = Vector3.zero;
	public bool followRotation = false;

	[Header("Scaling With Zoom (Orthographic)")]
	[Tooltip("If true, scales so on-screen size stays constant while zooming.")]
	public bool keepConstantScreenSize = false;
	[Tooltip("Reference orthographic size captured at start if <= 0.")]
	public float referenceOrthoSize = 0f;

	[Header("Auto Rotation")]
	public bool autoRotate = true;
	[Tooltip("Degrees per second around Z axis.")]
	public float rotationSpeedDegPerSec = 10f;
	int _rotationDirection = 1; // 1 or -1

	Vector3 _initialLocalScale;

	void Awake()
	{
		if (targetCamera == null) targetCamera = Camera.main;
		_initialLocalScale = transform.localScale;
		if (keepConstantScreenSize && targetCamera != null && targetCamera.orthographic && referenceOrthoSize <= 0f)
		{
			referenceOrthoSize = targetCamera.orthographicSize;
		}
	}

	void LateUpdate()
	{
		if (targetCamera == null)
		{
			targetCamera = Camera.main;
			if (targetCamera == null) return;
		}

		// Follow camera position with an optional world-space offset; keep this object's own Z by default
		Vector3 camPos = targetCamera.transform.position;
		Vector3 newPos = camPos + worldOffset;
		newPos.z = transform.position.z;
		transform.position = newPos;

		if (followRotation)
		{
			transform.rotation = targetCamera.transform.rotation;
		}

		// Optionally counter-scale to keep constant screen size when zooming (orthographic only)
		if (keepConstantScreenSize && targetCamera.orthographic && referenceOrthoSize > 0f)
		{
			float factor = targetCamera.orthographicSize / referenceOrthoSize;
			transform.localScale = _initialLocalScale * factor;
		}

		// Apply slow rotation
		if (autoRotate && Mathf.Abs(rotationSpeedDegPerSec) > 0.0001f)
		{
			float deltaZ = rotationSpeedDegPerSec * _rotationDirection * Time.unscaledDeltaTime;
			transform.Rotate(0f, 0f, deltaZ, Space.Self);
		}
	}

	// Public controls
	public void SwitchRotationDirection()
	{
		_rotationDirection = -_rotationDirection;
	}

	public void SetRotationDirection(int direction)
	{
		_rotationDirection = direction >= 0 ? 1 : -1;
	}

	public void SetRotationSpeed(float degPerSec)
	{
		rotationSpeedDegPerSec = Mathf.Abs(degPerSec);
	}
}


