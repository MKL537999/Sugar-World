using System;
using UnityEngine;
using UnityEngine.Events;

public enum GameState
{
    Playing,
    Paused,
    LevelUp,
    Shopping,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameConfig config;

    public GameConfig Config => config;
    public GameState CurrentState { get; private set; } = GameState.Playing;

    public float SurviveTime { get; private set; }
    public int CurrentLevel { get; private set; } = 1;
    public int CurrentXp { get; private set; }
    public int TotalKills { get; private set; }
    public int TotalCoins { get; private set; }
    public int XpToNextLevel => config.XpRequiredForLevel(CurrentLevel);

    public UnityEvent<int> OnXpChanged = new UnityEvent<int>();
    public UnityEvent<int> OnLevelUp = new UnityEvent<int>();
    public UnityEvent<int> OnCoinsChanged = new UnityEvent<int>();
    public UnityEvent OnGameOver = new UnityEvent();

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (CurrentState != GameState.GameOver)
        {
            SurviveTime += Time.deltaTime;
        }
    }

    public void AddXp(int amount)
    {
        CurrentXp += amount;
        OnXpChanged.Invoke(CurrentXp);

        while (CurrentXp >= XpToNextLevel && CurrentState == GameState.Playing)
        {
            CurrentXp -= XpToNextLevel;
            CurrentLevel++;
            OnXpChanged.Invoke(CurrentXp);
            OnLevelUp.Invoke(CurrentLevel);
            SetState(GameState.LevelUp);
        }
    }

    public void ProcessPendingLevelUps()
    {
        if (CurrentState == GameState.Playing && CurrentXp >= XpToNextLevel)
        {
            AddXp(0);
        }
    }

    public void AddCoins(int amount)
    {
        TotalCoins += amount;
        OnCoinsChanged.Invoke(TotalCoins);
    }

    public void AddKill()
    {
        TotalKills++;
    }

    public void SetState(GameState state)
    {
        CurrentState = state;
        Time.timeScale = state == GameState.Playing ? 1f : 0f;
    }

    public void GameOver()
    {
        SetState(GameState.GameOver);
        OnGameOver.Invoke();
    }
}
