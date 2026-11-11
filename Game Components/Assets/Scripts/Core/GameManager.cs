using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runs one climb in the game scene. It starts the timer, pauses and resumes
/// the game, restarts the level, goes back to the main menu, and finishes the
/// run when the player reaches the top. There's one GameManager in the game
/// scene, and other scripts reach it through GameManager.Instance.
/// </summary>
public class GameManager : MonoBehaviour
{
    /// <summary>The GameManager in the current scene, or null on the main menu.</summary>
    public static GameManager Instance { get; private set; }

    [SerializeField] private string mainMenuSceneName = "MainMenu";

    // These can be linked in the Inspector. Any that are left empty get found in Start.
    [SerializeField] private PlayerController player;
    [SerializeField] private GrapplingHook grapple;
    [SerializeField] private CompletionTimer timer;
    [SerializeField] private VictoryScreen victoryScreen;

    /// <summary>True while the pause menu is open.</summary>
    public bool IsPaused { get; private set; }

    /// <summary>True once the player has reached the finish line.</summary>
    public bool IsFinished { get; private set; }

    private void Awake()
    {
        // Only one GameManager can run at a time.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Find anything that wasn't linked in the Inspector, for example after
        // the level builder rebuilt the scene.
        if (timer == null) timer = GetComponent<CompletionTimer>();
        if (timer == null) timer = FindAnyObjectByType<CompletionTimer>();
        if (player == null) player = FindAnyObjectByType<PlayerController>();
        if (grapple == null) grapple = FindAnyObjectByType<GrapplingHook>();
        if (victoryScreen == null) victoryScreen = FindAnyObjectByType<VictoryScreen>();

        // Make sure time runs normally, in case the last scene was left paused.
        Time.timeScale = 1f;
        if (timer != null) timer.StartTimer();
        else Debug.LogWarning("GameManager: assign a CompletionTimer.", this);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Freezes the game (R02, UC7). Setting the time scale to 0 stops physics
    /// and the timer, and player input is turned off. PauseMenu calls this
    /// when the player presses Esc.
    /// </summary>
    public void Pause()
    {
        if (IsFinished || IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;
        SetPlayerInput(false);
    }

    /// <summary>Unfreezes the game and turns player input back on (UC8).</summary>
    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
        SetPlayerInput(true);
    }

    /// <summary>
    /// Starts the climb over (UC9). Reloading the scene puts the player back
    /// at the bottom, and Start resets the timer.
    /// </summary>
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Leaves the climb and loads the main menu (R03). Nothing about the
    /// unfinished run gets saved (R06).
    /// </summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Ends the run when the player reaches the top (UC10). It stops the timer,
    /// turns off input, saves the time (UC11), plays the finish sound, and opens
    /// the victory screen. FinishLine calls this, and it only runs once per climb.
    /// </summary>
    public void CompleteLevel()
    {
        if (IsFinished) return;
        IsFinished = true;

        timer.StopTimer();
        SetPlayerInput(false);

        float time = timer.ElapsedSeconds;
        bool isNewBest = BestTimeManager.RecordTime(time);
        AudioManager.Play(isNewBest ? AudioManager.Sfx.NewBest : AudioManager.Sfx.Finish);

        if (victoryScreen != null)
            victoryScreen.Show(time, isNewBest);
        else
            Debug.Log($"Level complete in {CompletionTimer.Format(time)}" + (isNewBest ? " (new best time)" : ""));
    }

    // Turns movement and the hook on or off. Turning input off also lets go
    // of the hook, so the player doesn't hang there frozen.
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
