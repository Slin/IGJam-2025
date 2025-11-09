using UnityEngine;

/// <summary>
/// Singleton class that stores game statistics to be displayed on the game over screen.
/// Persists across scene changes using DontDestroyOnLoad.
/// </summary>
[DisallowMultipleComponent]
public class GameStats : MonoBehaviour
{
    public static GameStats Instance { get; private set; }

    [Header("Game Statistics")]
    [SerializeField] private int _finalRound;
    [SerializeField] private int _totalKills;
    [SerializeField] private int _totalBuildingsBuilt;

    public int FinalRound => _finalRound;
    public int TotalKills => _totalKills;
    public int TotalBuildingsBuilt => _totalBuildingsBuilt;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Reset all statistics to zero for a new game
    /// </summary>
    public void ResetStats()
    {
        _finalRound = 0;
        _totalKills = 0;
        _totalBuildingsBuilt = 0;
    }

    /// <summary>
    /// Update final game statistics when the game ends
    /// </summary>
    public void RecordGameOver(int finalRound, int totalKills, int buildingsBuilt)
    {
        _finalRound = finalRound;
        _totalKills = totalKills;
        _totalBuildingsBuilt = buildingsBuilt;
        
        Debug.Log($"GameStats: Recorded game over - Round: {finalRound}, Kills: {totalKills}, Buildings: {buildingsBuilt}");
    }

    /// <summary>
    /// Increment the buildings built counter
    /// </summary>
    public void IncrementBuildingsBuilt()
    {
        _totalBuildingsBuilt++;
    }
}
