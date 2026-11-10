using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the flow of a run in the game scene: start, pause, resume,
/// restart, finish, and return to the main menu. Put one in the game scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private PlayerController player;
    [SerializeField] private GrapplingHook grapple;
    [SerializeField] private CompletionTimer timer;
    [SerializeField] private VictoryScreen victoryScreen;

    public bool IsPaused { get; private set; }
    public bool IsFinished { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Find anything that wasn't linked in the Inspector.
        if (timer == null) timer = GetComponent<CompletionTimer>();
        if (timer == null) timer = FindAnyObjectByType<CompletionTimer>();
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (grapple == null) grapple = FindAnyObjectByType<GrapplingHook>();
        if (victoryScreen == null) victoryScreen = FindAnyObjectByType<VictoryScreen>();

        Time.timeScale = 1f;
        if (timer != null) timer.StartTimer();
        else Debug.LogWarning("GameManager: assign a CompletionTimer.", this);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Pause()
    {
        if (IsFinished || IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;
        SetPlayerInput(false);
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
        SetPlayerInput(true);
    }

    /// <summary>Reloads the scene and resets the timer (UC9).</summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Leaves the run without saving anything (R03, R06).</summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>Called by the finish line when the player reaches the top (UC10).</summary>
    public void CompleteLevel()
    {
        if (IsFinished) return;
        IsFinished = true;

        timer.StopTimer();
        SetPlayerInput(false);

        float time = timer.ElapsedSeconds;
        bool isNewBest = BestTimeManager.RecordTime(time);

        if (victoryScreen != null)
            victoryScreen.Show(time, isNewBest);
        else
            Debug.Log($"Level complete in {CompletionTimer.Format(time)}" + (isNewBest ? " (new best time)" : ""));
    }

    private void SetPlayerInput(bool enabled)
    {
        if (player != null) player.InputEnabled = enabled;
        if (grapple != null)
        {
            grapple.InputEnabled = enabled;
            if (!enabled) grapple.Detach();
        }
    }
}
