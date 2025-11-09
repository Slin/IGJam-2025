using UnityEngine;

[DisallowMultipleComponent]
public class Rotator : MonoBehaviour
{
	[Header("Rotation")]
	public Vector3 eulerSpeedDegPerSec = new Vector3(0f, 0f, 90f);
	public Space rotationSpace = Space.Self;
	public bool useUnscaledTime = false;

	void Update()
	{
		float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
		if (dt <= 0f) return;
		transform.Rotate(eulerSpeedDegPerSec * dt, rotationSpace);
	}
}


