using UnityEngine;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState
    {
        Tutorial,
        Playing,
        Paused,
        LevelComplete,
        LevelFail
    }

    public GameState CurrentState { get; private set; }

    public event Action<GameState> OnGameStateChanged;

    [Header("UI References")]
    [SerializeField] private GameObject tutorialPanel;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        SetState(GameState.Tutorial);
        tutorialPanel.SetActive(true);
    }

    private void Update()
    {
        if (CurrentState == GameState.Tutorial && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseTutorial();
        }
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;

        HandleTimeScale(newState);

        OnGameStateChanged?.Invoke(newState);
    }

    private void HandleTimeScale(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                break;

            case GameState.Tutorial:
            case GameState.Paused:
            case GameState.LevelComplete:
            case GameState.LevelFail:
                Time.timeScale = 0f;
                break;
        }
    }

    // ---------------- Tutorial ----------------

    private void ShowTutorial()
    {
        tutorialPanel?.SetActive(true);
    }

    public void CloseTutorial()
    {
        tutorialPanel?.SetActive(false);
        SetState(GameState.Playing);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}