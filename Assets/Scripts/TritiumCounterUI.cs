using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class TritiumCounterUI : MonoBehaviour
{
	public TMP_Text tmpText;
	bool _subscribed;
	Coroutine _waitSubscribe;

	void Awake()
	{
		// Auto-detect on this GameObject if nothing assigned
		if (tmpText == null) tmpText = GetComponent<TMP_Text>();

		// As a fallback, try to find a GameObject named "TritiumCounter"
		if (tmpText == null)
		{
			var go = GameObject.Find("TritiumCounter");
			if (go != null)
			{
				tmpText = go.GetComponent<TMP_Text>();
			}
		}
	}

	void OnEnable()
	{
		TrySubscribeAndRefresh();
		if (!_subscribed)
		{
			_waitSubscribe = StartCoroutine(WaitAndSubscribe());
		}
	}

	void OnDisable()
	{
		var p = PlayerStatsManager.Instance;
		if (p != null)
		{
			p.onTritiumChanged.RemoveListener(UpdateText);
		}
		_subscribed = false;
		if (_waitSubscribe != null)
		{
			StopCoroutine(_waitSubscribe);
			_waitSubscribe = null;
		}
	}

	void UpdateText(int amount)
	{
		string text = string.Format("{0}", amount);
		if(tmpText != null) tmpText.text = text;

		Debug.Log($"TritiumCounterUI: Tritium changed to {amount}");
	}

	void TrySubscribeAndRefresh()
	{
		var p = PlayerStatsManager.Instance;
		if (p == null) return;
		if (!_subscribed)
		{
			p.onTritiumChanged.AddListener(UpdateText);
			_subscribed = true;
		}
		UpdateText(p.Tritium);
	}

	System.Collections.IEnumerator WaitAndSubscribe()
	{
		while (PlayerStatsManager.Instance == null)
		{
			yield return null;
		}
		_waitSubscribe = null;
		TrySubscribeAndRefresh();
	}
}


