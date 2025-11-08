using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIRoundController : MonoBehaviour
{
    [Tooltip("Assign the ScrollRect Content here. If left empty, this object's RectTransform is used.")]
    public RectTransform content;

    RoundUIVisibility[] _items;

    void Awake()
    {
        if (content == null)
        {
            content = GetComponent<RectTransform>();
        }
        RefreshCache();
    }

    public void RefreshCache()
    {
        if (content == null) return;
        _items = content.GetComponentsInChildren<RoundUIVisibility>(true);
    }

    public void ApplyRound(int round)
    {
        if (content == null) return;
        if (_items == null || _items.Length == 0) RefreshCache();

        foreach (var item in _items)
        {
            if (item == null) continue;
            bool visible = item.IsVisibleFor(round);
            if (item.gameObject.activeSelf != visible)
            {
                item.gameObject.SetActive(visible);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }
}


