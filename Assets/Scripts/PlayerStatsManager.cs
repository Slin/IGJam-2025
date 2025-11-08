using System;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlayerStatsManager : MonoBehaviour
{
    public static PlayerStatsManager Instance { get; private set; }

    [Header("Starting Values")]
    public int startingTritium = 100;
    public int tritiumPerRound = 50;

    [Header("Current Stats")]
    [SerializeField] private int _tritium;
    [SerializeField] private int _currentRound;
    [SerializeField] private int _enemiesKilled;

    [Header("Events")]
    public UnityEvent<int> onTritiumChanged;
    public UnityEvent<int> onRoundChanged;
    public UnityEvent<int> onEnemyKilled;

    public int Tritium => _tritium;
    public int CurrentRound => _currentRound;
    public int EnemiesKilled => _enemiesKilled;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void InitializeNewGame()
    {
        _tritium = startingTritium;
        // _currentRound = 0;
        _enemiesKilled = 0;

        NotifyTritiumChanged();
        NotifyRoundChanged();
    }

    public void StartNewRound()
    {
        _currentRound++;
        NotifyRoundChanged();
    }

    public void CompleteRound()
    {
        AddTritium(tritiumPerRound);
    }

    public bool CanAfford(int cost)
    {
        return _tritium >= cost;
    }

    public bool TrySpendTritium(int amount)
    {
        if (!CanAfford(amount))
        {
            Debug.LogWarning($"PlayerStatsManager: Not enough tritium. Have: {_tritium}, Need: {amount}");
            return false;
        }

        _tritium -= amount;
        NotifyTritiumChanged();
        return true;
    }

    public void AddTritium(int amount)
    {
        _tritium += Mathf.Max(0, amount);
        NotifyTritiumChanged();
    }

    public void OnEnemyKilled(Enemy enemy)
    {
        if (enemy == null) return;

        _enemiesKilled++;
        AddTritium(enemy.tritiumReward);

        try
        {
            onEnemyKilled?.Invoke(_enemiesKilled);
        }
        catch (Exception) { /* ignore event exceptions */ }
    }

    void NotifyTritiumChanged()
    {
        try
        {
            onTritiumChanged?.Invoke(_tritium);
        }
        catch (Exception) { /* ignore event exceptions */ }
    }

    void NotifyRoundChanged()
    {
        try
        {
            onRoundChanged?.Invoke(_currentRound);
        }
        catch (Exception) { /* ignore event exceptions */ }
    }

    public void ResetStats()
    {
        InitializeNewGame();
    }
}
