using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public class RoundCounterUI : MonoBehaviour
{
	public TMP_Text tmpText;
	bool _subscribed;
	Coroutine _waitSubscribe;

	void Awake()
	{
		if (tmpText == null) tmpText = GetComponent<TMP_Text>();
		if (tmpText == null)
		{
			var go = GameObject.Find("RoundCounter");
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
			p.onRoundChanged.RemoveListener(UpdateText);
		}
		_subscribed = false;
		if (_waitSubscribe != null)
		{
			StopCoroutine(_waitSubscribe);
			_waitSubscribe = null;
		}
	}

	void UpdateText(int round)
	{
		// Display one higher than the stored round (show upcoming/active round)
		int displayRound = Mathf.Max(0, round) + 1;
		string text = string.Format("{0}", displayRound);
		if (tmpText != null) tmpText.text = text;
		Debug.Log($"RoundCounterUI: Round changed to {round}");
	}

	void TrySubscribeAndRefresh()
	{
		var p = PlayerStatsManager.Instance;
		if (p == null) return;
		if (!_subscribed)
		{
			p.onRoundChanged.AddListener(UpdateText);
			_subscribed = true;
		}
		UpdateText(p.CurrentRound);
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


