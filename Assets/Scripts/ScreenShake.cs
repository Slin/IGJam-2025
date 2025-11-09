using UnityEngine;

[DisallowMultipleComponent]
public class ScreenShake : MonoBehaviour
{
	public static ScreenShake Instance { get; private set; }

	[Header("Target")]
	[SerializeField] Transform target; // Defaults to main camera transform

	[Header("Shake Settings")]
	[SerializeField] float maxPositionOffset = 0.35f; // world units
	[SerializeField] float traumaDecayPerSecond = 0.6f; // how fast shake fades (lower = longer shake)
	[SerializeField] float frequency = 35f; // noise frequency

	float _trauma;
	Vector3 _lastOffset;
	float _noiseSeedX;
	float _noiseSeedY;

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		_noiseSeedX = Random.value * 1000f;
		_noiseSeedY = Random.value * 2000f;
	}

	void LateUpdate()
	{
		if (target == null)
		{
			var cam = Camera.main;
			if (cam == null)
			{
				// Fallback: pick first active camera
				var cams = Camera.allCamerasCount > 0 ? Camera.allCameras : null;
				if (cams != null && cams.Length > 0) cam = cams[0];
			}
			if (cam != null) target = cam.transform;
			if (target == null) return;
		}

		// Base (no-shake) position is current minus last applied offset
		Vector3 basePos = target.localPosition - _lastOffset;

		// Decay trauma
		if (_trauma > 0f)
		{
			_trauma = Mathf.Max(0f, _trauma - traumaDecayPerSecond * Time.deltaTime);
		}

		// Compute new offset from Perlin noise scaled by trauma^2
		float t = Time.unscaledTime * frequency;
		float amount = _trauma * _trauma;

		float nx = Mathf.PerlinNoise(_noiseSeedX, t) * 2f - 1f;
		float ny = Mathf.PerlinNoise(_noiseSeedY, t) * 2f - 1f;

		Vector3 newOffset = new Vector3(nx, ny, 0f) * (maxPositionOffset * amount);
		_lastOffset = newOffset;
		target.localPosition = basePos + newOffset;
	}

	public void AddTrauma(float amount)
	{
		_trauma = Mathf.Clamp01(_trauma + Mathf.Max(0f, amount));
	}

	public static void Shake(float intensity)
	{
		if (Instance == null)
		{
			// Create an instance on the main camera if possible
			var cam = Camera.main;
			if (cam == null)
			{
				var cams = Camera.allCamerasCount > 0 ? Camera.allCameras : null;
				if (cams != null && cams.Length > 0) cam = cams[0];
			}
			if (cam != null)
			{
				var ss = cam.gameObject.GetComponent<ScreenShake>();
				if (ss == null) ss = cam.gameObject.AddComponent<ScreenShake>();
				ss.target = cam.transform;
				Instance = ss;
			}
		}
		Instance?.AddTrauma(intensity);
	}
}


