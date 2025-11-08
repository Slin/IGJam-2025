using UnityEngine;

[DisallowMultipleComponent]
public class RoundUIVisibility : MonoBehaviour
{
    [Tooltip("Inclusive minimum round index. Use -1 for no minimum.")]
    public int minRound = -1;

    [Tooltip("Inclusive maximum round index. Use -1 for no maximum.")]
    public int maxRound = -1;

    public bool IsVisibleFor(int round)
    {
        if (minRound >= 0 && round < minRound) return false;
        if (maxRound >= 0 && round > maxRound) return false;
        return true;
    }
}


