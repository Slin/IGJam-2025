using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SpawnEdgeIndicators : MonoBehaviour
{
	[Header("References")]
	public Camera targetCamera; // if null, will use Camera.main
	public Transform indicatorPrefab3D; // A world-space arrow object (Sprite/Quad)
	public Transform indicatorsParent; // Optional parent (defaults to camera transform)

	[Header("Behavior")]
	public float screenCircleRadiusPx = 200f; // radius from screen center in pixels
	public float indicatorDepthFromCamera = 5f; // world units in front of camera
	public float hideInsideMargin = 0.02f; // viewport margin to consider as visible
	public bool onlyShowDuringBuilding = true;
	public bool faceCamera = true; // billboard towards camera

	readonly List<Transform> _indicators = new List<Transform>();

	void Awake()
	{
		if (targetCamera == null) targetCamera = Camera.main;
	}

	void LateUpdate()
	{
		if (targetCamera == null)
		{
			targetCamera = Camera.main;
			if (targetCamera == null) return;
		}

		// Optional visibility based on phase
		if (onlyShowDuringBuilding && GameManager.Instance != null && GameManager.Instance.CurrentPhase != GamePhase.Building)
		{
			SetAllIndicatorsActive(false);
			return;
		}

		var sm = SpawnerManager.Instance;
		if (sm == null || indicatorPrefab3D == null) { SetAllIndicatorsActive(false); return; }

		var positions = sm.GetCurrentSpawnPositions();
		int count = positions != null ? positions.Count : 0;
		EnsureIndicatorCount(count);

		Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

		for (int i = 0; i < _indicators.Count; i++)
		{
			Transform ind = _indicators[i];
			if (i >= count || ind == null)
			{
				if (ind != null) ind.gameObject.SetActive(false);
				continue;
			}

			Vector3 spawnWorld = positions[i];
			Vector3 vp3 = targetCamera.WorldToViewportPoint(spawnWorld);
			Vector2 vp = new Vector2(vp3.x, vp3.y);

			// On-screen check (with margin) and in front of camera
			bool visible = vp3.z > 0f &&
			               vp.x >= 0f + hideInsideMargin && vp.x <= 1f - hideInsideMargin &&
			               vp.y >= 0f + hideInsideMargin && vp.y <= 1f - hideInsideMargin;

			if (visible)
			{
				ind.gameObject.SetActive(false);
				continue;
			}

			// Direction from center in viewport space
			Vector2 dvp = new Vector2(vp3.x - 0.5f, vp3.y - 0.5f);
			float ang = Mathf.Atan2(dvp.y, dvp.x);
			if (float.IsNaN(ang)) ang = 0f;

			// Ellipse radii in pixels that respect aspect ratio (auto: 0.7 * screen height)
			float radiusPx = 0.7f * Screen.height;
			float rx = radiusPx;
			float ry = radiusPx * ((float)Screen.height / Mathf.Max(1, Screen.width));

			// Point on ellipse in screen space
			Vector2 indicatorScreen = screenCenter + new Vector2(Mathf.Cos(ang) * rx, Mathf.Sin(ang) * ry);

			// Convert to world at a fixed depth from camera
			float depth = Mathf.Max(0.01f, indicatorDepthFromCamera);
			Vector3 world = targetCamera.ScreenToWorldPoint(new Vector3(indicatorScreen.x, indicatorScreen.y, depth));
			ind.position = world;
			ind.gameObject.SetActive(true);

			// Rotate arrow to point towards the offscreen target direction
			if (faceCamera)
			{
				ind.rotation = targetCamera.transform.rotation;
			}
			// Additional roll to align with direction
			float angleDeg = ang * Mathf.Rad2Deg;
			ind.Rotate(0f, 0f, angleDeg, Space.Self);
		}
	}

	void EnsureIndicatorCount(int count)
	{
		Transform parent = indicatorsParent != null ? indicatorsParent : (targetCamera != null ? targetCamera.transform : transform);
		while (_indicators.Count < count)
		{
			var inst = Instantiate(indicatorPrefab3D, parent);
			inst.gameObject.SetActive(false);
			_indicators.Add(inst);
		}
		for (int i = count; i < _indicators.Count; i++)
		{
			if (_indicators[i] != null) _indicators[i].gameObject.SetActive(false);
		}
	}

	void SetAllIndicatorsActive(bool active)
	{
		for (int i = 0; i < _indicators.Count; i++)
		{
			if (_indicators[i] != null) _indicators[i].gameObject.SetActive(active);
		}
	}
}


