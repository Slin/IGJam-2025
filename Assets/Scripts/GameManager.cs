using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public float buildPhaseMinDuration = 5f; // Minimum time before allowing defense phase start

    public float defensePhaseEndDelay = 2f; // Delay after all enemies defeated before returning to build phase

    [Header("Difficulty Scaling")]
    public int initialRoundMoneyValue = 50;
    public int roundMoneyIncrement = 20;
    public int roundsUntilFastEnemies = 2;
    public int roundsUntilArmoredEnemies = 4;
    public int roundsUntilBossEnemies = 7;
    public int roundsUntilAttackEnemies = 1;
    public int roundsUntilTeleporterEnemies = 5;
    public int roundsUntilExploderEnemies = 3;
    public int maxEnemiesPerRound = 50;

    [Header("Events")] public UnityEvent onGameStarted;
    public UnityEvent onGameOver;
    public UnityEvent<GamePhase> onPhaseChanged;
    public UnityEvent<int> onRoundStarted;
    public UnityEvent<int> onRoundCompleted;

    [Header("UI")]
    public GameObject readyButton; // Optional: assign in Inspector; otherwise found by name "ReadyButton"

    [Header("VFX Prefabs")]
    public LaserBeam laserBeamPrefab;
	public GameObject buildingDeathEffectPrefab;
	public GameObject enemyDeathEffectPrefab;

    GameState _currentState = GameState.NotStarted;
    GamePhase _currentPhase = GamePhase.Building;
    float _buildPhaseStartTime;
    int _lastRoundMoneyValue = 0; // Track the previous round's money value for formula

    public GameState CurrentState => _currentState;
    public GamePhase CurrentPhase => _currentPhase;
    public bool CanStartDefensePhase => Time.time - _buildPhaseStartTime >= buildPhaseMinDuration;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        // Ensure DroneManager exists
        if (DroneManager.Instance == null)
        {
            GameObject droneManagerObj = new GameObject("DroneManager");
            droneManagerObj.AddComponent<DroneManager>();
            Debug.Log("GameManager: Created DroneManager");
        }

        // Auto-start the game when the scene loads
        StartNewGame();
    }

    public void StartNewGame()
    {
        if (_currentState == GameState.Playing)
        {
            Debug.LogWarning("GameManager: Game already in progress");
            return;
        }

        Debug.Log("GameManager: Starting new game");
        _currentState = GameState.Playing;
        _currentPhase = GamePhase.Building;
        _buildPhaseStartTime = Time.time;
        _lastRoundMoneyValue = 0; // Reset money value for new game

        // Initialize all managers
        PlayerStatsManager.Instance?.InitializeNewGame();
        BuildingManager.Instance?.InitializeNewGame();
        SpawnerManager.Instance?.InitializeNewGame();
        DroneManager.Instance?.ClearAllDrones();

        // Play soundtrack
        AudioManager.Instance?.PlaySoundtrack(true);

        try
        {
            onGameStarted?.Invoke();
            onPhaseChanged?.Invoke(_currentPhase);
        }
        catch (Exception)
        {
            /* ignore event exceptions */
        }

        // Initialize UI to the current (or first) round
        ApplyRoundToUI(PlayerStatsManager.Instance?.CurrentRound ?? 1);
        UpdateReadyButtonVisibility();
    }

    public void RequestStartDefensePhase()
    {
        if (_currentState != GameState.Playing)
        {
            Debug.LogWarning("GameManager: Cannot start defense phase - game not playing");
            return;
        }

        if (_currentPhase != GamePhase.Building)
        {
            Debug.LogWarning("GameManager: Cannot start defense phase - not in building phase");
            return;
        }

        if (!CanStartDefensePhase)
        {
            float remaining = buildPhaseMinDuration - (Time.time - _buildPhaseStartTime);
            Debug.LogWarning($"GameManager: Must wait {remaining:F1} more seconds before starting defense phase");
            AudioManager.Instance?.PlaySFX("error");
            return;
        }

        StartDefensePhase();
    }

    void StartDefensePhase()
    {
        Debug.Log("GameManager: Starting defense phase");
        _currentPhase = GamePhase.Defense;

        // Cancel any building placement in progress
        BuildingManager.Instance?.CancelBuildingPlacement();

        // Determine active round number (increment only after defending)
        int currentCompleted = PlayerStatsManager.Instance?.CurrentRound ?? 0;
        int activeRound = currentCompleted + 1;

        try
        {
            onPhaseChanged?.Invoke(_currentPhase);
            onRoundStarted?.Invoke(activeRound);
        }
        catch (Exception)
        {
            /* ignore event exceptions */
        }

        // Update round-dependent UI for this round
        ApplyRoundToUI(activeRound);
        UpdateReadyButtonVisibility();

        // Calculate round money value and start spawning enemies
        int roundMoneyValue = CalculateRoundMoneyValue(activeRound);
        SpawnerManager.Instance?.StartRound(roundMoneyValue, activeRound);
    }

    /// <summary>
    /// Calculate the money value for a round based on quadratic formula: initialRoundMoneyValue + (roundMoneyIncrement * round² / 2)
    /// This provides smooth exponential-like scaling that accelerates over time.
    /// </summary>
    int CalculateRoundMoneyValue(int round)
    {
        int currentValue = initialRoundMoneyValue + (roundMoneyIncrement * round * round / 2);
        _lastRoundMoneyValue = currentValue;
        return currentValue;
    }

    public int GetBossCountForRound(int round)
    {
        if (round % 5 != 0 || round < roundsUntilBossEnemies)
            return 0;
        return round / 5;
    }

    /// <summary>
    /// Calculate the maximum number of enemies for a round based on formula: (round / 20) * starting max but never less than 50
    /// </summary>
    public int GetMaxEnemiesForRound(int round)
    {
        int scaledMax = Mathf.RoundToInt((round / 20f) * maxEnemiesPerRound);
        return Mathf.Max(50, scaledMax);
    }

    int CalculateEnemyCount(int round)
    {
        // This method is now deprecated in favor of money-based spawning
        // Keeping for compatibility if needed
        return 0;
    }

    public void OnRoundDefeated()
    {
        if (_currentPhase != GamePhase.Defense) return;

        int currentRound = PlayerStatsManager.Instance?.CurrentRound ?? 0;
        Debug.Log($"GameManager: Round {currentRound} defeated!");

        // Give round completion reward
        PlayerStatsManager.Instance?.CompleteRound();
        // Increment round counter AFTER defending
        PlayerStatsManager.Instance?.StartNewRound();

        try
        {
            onRoundCompleted?.Invoke(currentRound);
        }
        catch (Exception)
        {
            /* ignore event exceptions */
        }

        // Return to building phase after delay
        StartCoroutine(ReturnToBuildingPhaseAfterDelay());
    }

    IEnumerator ReturnToBuildingPhaseAfterDelay()
    {
        yield return new WaitForSeconds(defensePhaseEndDelay);

        if (_currentState != GameState.Playing) yield break;

        Debug.Log("GameManager: Returning to building phase");
        _currentPhase = GamePhase.Building;
        _buildPhaseStartTime = Time.time;

        // Prepare next spawner so players can see where enemies will come from
        SpawnerManager.Instance?.EndRound();

        // Notify DroneManager that round ended (drones persist)
        DroneManager.Instance?.OnRoundEnd();

        try
        {
            onPhaseChanged?.Invoke(_currentPhase);
        }
        catch (Exception)
        {
            /* ignore event exceptions */
        }

        // Reflect the current round in UI after returning to build phase
        ApplyRoundToUI(PlayerStatsManager.Instance?.CurrentRound ?? 1);
        UpdateReadyButtonVisibility();
    }

    public void OnAllBasesDestroyed()
    {
        if (_currentState != GameState.Playing) return;

        Debug.Log("GameManager: All bases destroyed - Game Over!");
        EndGame();
    }

    void EndGame()
    {
        _currentState = GameState.GameOver;

        // Stop all spawning
        SpawnerManager.Instance?.StopAllCoroutines();

        // Stop music on game over
        AudioManager.Instance?.StopMusic(true);

        try
        {
            onGameOver?.Invoke();
        }
        catch (Exception)
        {
            /* ignore event exceptions */
        }

        // Load game over scene after a short delay
        StartCoroutine(LoadGameOverSceneAfterDelay());
    }

    IEnumerator LoadGameOverSceneAfterDelay()
    {
        yield return new WaitForSeconds(2f); // Wait 2 seconds before transitioning
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameOverScene");
    }

    public void RestartGame()
    {
        // Start fresh
        StartNewGame();
    }

    public void QuitGame()
    {
        Debug.Log("GameManager: Quitting game");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public bool ShouldSpawnEnemyType(EnemyType type, int round)
    {
        return type switch
        {
            EnemyType.Regular => true,
            EnemyType.Fast => round >= roundsUntilFastEnemies,
            EnemyType.Armored => round >= roundsUntilArmoredEnemies,
            EnemyType.Boss => round >= roundsUntilBossEnemies,
            EnemyType.Attack => round >= roundsUntilAttackEnemies,
            EnemyType.Teleporter => round >= roundsUntilTeleporterEnemies,
            EnemyType.Exploder => round >= roundsUntilExploderEnemies,
            _ => false
        };
    }

    void ApplyRoundToUI(int round)
    {
        var controllers = FindObjectsOfType<UIRoundController>(true);
        for (int i = 0; i < controllers.Length; i++)
        {
            var ctrl = controllers[i];
            if (ctrl != null)
            {
                ctrl.ApplyRound(round);
            }
        }
    }

    void UpdateReadyButtonVisibility()
    {
        // Ensure we have a reference
        if (readyButton == null)
        {
            var found = GameObject.Find("ReadyButton");
            if (found != null) readyButton = found;
        }

        if (readyButton == null) return;

        // Visible during Building, hidden during Defense
        bool isVisible = _currentPhase == GamePhase.Building;
        readyButton.SetActive(isVisible);
    }
}
