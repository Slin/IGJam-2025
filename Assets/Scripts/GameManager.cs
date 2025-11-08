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

    [Header("Difficulty Scaling")] public int baseEnemiesPerRound = 5;
    public float enemyScalingPerRound = 1.2f; // Multiply enemy count by this each round
    public int roundsUntilFastEnemies = 2;
    public int roundsUntilArmoredEnemies = 4;
    public int roundsUntilBossEnemies = 7;

    [Header("Events")] public UnityEvent onGameStarted;
    public UnityEvent onGameOver;
    public UnityEvent<GamePhase> onPhaseChanged;
    public UnityEvent<int> onRoundStarted;
    public UnityEvent<int> onRoundCompleted;

    [Header("UI")]
    public GameObject readyButton; // Optional: assign in Inspector; otherwise found by name "ReadyButton"

    GameState _currentState = GameState.NotStarted;
    GamePhase _currentPhase = GamePhase.Building;
    float _buildPhaseStartTime;

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

        // Initialize all managers
        PlayerStatsManager.Instance?.InitializeNewGame();
        BuildingManager.Instance?.InitializeNewGame();
        SpawnerManager.Instance?.InitializeNewGame();
        DroneManager.Instance?.ClearAllDrones();

        // Play building phase music
        AudioManager.Instance?.PlayBuildingPhaseMusic();

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

        // Start new round
        PlayerStatsManager.Instance?.StartNewRound();
        int currentRound = PlayerStatsManager.Instance?.CurrentRound ?? 1;

        // Play defense music
        AudioManager.Instance?.PlayDefensePhaseMusic();

        try
        {
            onPhaseChanged?.Invoke(_currentPhase);
            onRoundStarted?.Invoke(currentRound);
        }
        catch (Exception)
        {
            /* ignore event exceptions */
        }

        // Update round-dependent UI for this round
        ApplyRoundToUI(currentRound);
        UpdateReadyButtonVisibility();

        // Start spawning enemies
        int enemyCount = CalculateEnemyCount(currentRound);
        SpawnerManager.Instance?.StartRound(enemyCount, currentRound);
    }

    int CalculateEnemyCount(int round)
    {
        return Mathf.RoundToInt(baseEnemiesPerRound * Mathf.Pow(enemyScalingPerRound, round - 1));
    }

    public void OnRoundDefeated()
    {
        if (_currentPhase != GamePhase.Defense) return;

        int currentRound = PlayerStatsManager.Instance?.CurrentRound ?? 0;
        Debug.Log($"GameManager: Round {currentRound} defeated!");

        // Give round completion reward
        PlayerStatsManager.Instance?.CompleteRound();

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

        // Play building music
        AudioManager.Instance?.PlayBuildingPhaseMusic();

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

        // Play game over music
        AudioManager.Instance?.PlayGameOverMusic();

        try
        {
            onGameOver?.Invoke();
        }
        catch (Exception)
        {
            /* ignore event exceptions */
        }
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
