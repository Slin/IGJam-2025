using UnityEngine;
using TMPro;

/// <summary>
/// Controls the Game Over screen, populating the statistics text with actual game data
/// </summary>
[DisallowMultipleComponent]
public class GameOverScreen : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The TextMeshProUGUI component displaying the game over message")]
    public TextMeshProUGUI statsText;

    void Start()
    {
        UpdateStatsDisplay();
        PlayGameOverMelody();
    }

    /// <summary>
    /// Plays the game over melody when the screen is displayed
    /// </summary>
    void PlayGameOverMelody()
    {
        AudioManager.Instance?.PlaySFX("game_over_melody");
    }

    /// <summary>
    /// Updates the stats text with actual game statistics from GameStats
    /// </summary>
    void UpdateStatsDisplay()
    {
        if (statsText == null)
        {
            Debug.LogError("GameOverScreen: statsText reference is not assigned!");
            return;
        }

        if (GameStats.Instance == null)
        {
            Debug.LogWarning("GameOverScreen: GameStats instance not found, using default values");
            statsText.text = "Game Over!\n\nYou made it to round 0.\nYou killed 0 aliens while defending your base.\nYou built 0 units.";
            return;
        }

        int round = GameStats.Instance.FinalRound;
        int kills = GameStats.Instance.TotalKills;
        int buildings = GameStats.Instance.TotalBuildingsBuilt;

        // Format the text with actual statistics
        statsText.text = $"Game Over!\n\nYou made it to round {round}.\nYou killed {kills} aliens while defending your base.\nYou built {buildings} units.";

        Debug.Log($"GameOverScreen: Displaying stats - Round: {round}, Kills: {kills}, Buildings: {buildings}");
    }
}
