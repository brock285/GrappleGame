using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Runs one climb in the game scene. It shows the "How to play" screen first,
/// starts the timer the first time the player moves, pauses and resumes the
/// game, restarts the level, goes back to the main menu, and finishes the run
/// when the player reaches the top. If the player runs out of hearts, it waits
/// a moment and then starts the climb over.
///
/// It also hides the mouse pointer while the player is climbing, since the
/// Crosshair takes its place, and shows the pointer again whenever a menu is
/// open. There's one GameManager in the game scene, and other scripts reach
/// it through GameManager.Instance.
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
    [SerializeField] private IntroScreen introScreen;

    // Seconds to wait after the player runs out of hearts before the climb starts over.
    [SerializeField] private float restartDelay = 2f;

    private PlayerHealth health;

    // True after the intro closes and before the player's first move. The timer starts on that move.
    private bool waitingForFirstMove;

    // Set when the climb restarts, so the intro screen doesn't show again.
    // It's static, so it survives the scene reloading.
    private static bool skipIntroNextLoad;

    /// <summary>True while the pause menu is open.</summary>
    public bool IsPaused { get; private set; }

    /// <summary>True once the player has reached the finish line.</summary>
    public bool IsFinished { get; private set; }

    /// <summary>True once the player has run out of hearts, while the game waits to restart.</summary>
    public bool IsGameOver { get; private set; }

    /// <summary>True while the "How to play" screen is open before the climb.</summary>
    public bool IsIntroOpen { get; private set; }

    /// <summary>True while the player is actually climbing, with no menu open.</summary>
    public bool IsPlaying => !IsPaused && !IsFinished && !IsGameOver && !IsIntroOpen;

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
        if (introScreen == null) introScreen = FindAnyObjectByType<IntroScreen>();

        // Listen for the player running out of hearts. If the player was built
        // before health was added, give them a PlayerHealth now.
        if (player != null)
        {
            health = player.GetComponent<PlayerHealth>();
            if (health == null) health = player.gameObject.AddComponent<PlayerHealth>();
            health.Died += OnPlayerDied;
        }

        // Make sure time runs normally, in case the last scene was left paused.
        Time.timeScale = 1f;
        if (timer == null) Debug.LogWarning("GameManager: assign a CompletionTimer.", this);

        // Show the intro when the player comes from the main menu, but skip it after a restart.
        if (introScreen != null && !skipIntroNextLoad)
        {
            IsIntroOpen = true;
            SetPlayerInput(false);
            introScreen.Show();
        }
        else
        {
            BeginRun();
        }
        skipIntroNextLoad = false;
    }

    private void Update()
    {
        // The timer doesn't start until the player's first move, so reading the
        // intro or waiting at the bottom doesn't count against their time.
        if (waitingForFirstMove && !IsPaused && PlayerPressedMove())
        {
            waitingForFirstMove = false;
            if (timer != null) timer.StartTimer();
        }

        // Hide the mouse pointer while climbing. The Crosshair shows where the player is aiming instead.
        Cursor.visible = !IsPlaying;
    }

    /// <summary>
    /// Lets the player start climbing. IntroScreen calls this when the player
    /// closes it, and Start calls it right away after a restart. The timer
    /// waits for the first move.
    /// </summary>
    public void BeginRun()
    {
        IsIntroOpen = false;
        SetPlayerInput(true);
        waitingForFirstMove = true;
    }

    // True on the frame the player first uses any movement control: walking,
    // reeling, or firing the hook.
    private static bool PlayerPressedMove()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool key = kb != null && (kb.aKey.isPressed || kb.dKey.isPressed || kb.leftArrowKey.isPressed ||
                                  kb.rightArrowKey.isPressed || kb.wKey.isPressed || kb.sKey.isPressed ||
                                  kb.spaceKey.isPressed);
        bool click = mouse != null && mouse.leftButton.wasPressedThisFrame;
        return key || click;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (health != null) health.Died -= OnPlayerDied;
    }

    /// <summary>
    /// Freezes the game (R02, UC7). Setting the time scale to 0 stops physics
    /// and the timer, and player input is turned off. PauseMenu calls this
    /// when the player presses Esc.
    /// </summary>
    public void Pause()
    {
        if (IsFinished || IsPaused || IsGameOver || IsIntroOpen) return;
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
    /// at the bottom and resets the timer, which starts again on the first
    /// move. The intro screen doesn't show again.
    /// </summary>
    public void Restart()
    {
        skipIntroNextLoad = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Leaves the climb and loads the main menu (R03). Nothing about the
    /// unfinished run gets saved (R06).
    /// </summary>
    public void GoToMainMenu()
    {
        skipIntroNextLoad = false;
        Cursor.visible = true;
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
        if (IsFinished || IsGameOver) return;
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

    // Runs when the player loses their last heart. It stops the timer and
    // input, plays the game over sound, and restarts the climb after a short
    // wait so the player can see what happened. The time isn't saved.
    private void OnPlayerDied()
    {
        if (IsFinished || IsGameOver) return;
        IsGameOver = true;

        timer.StopTimer();
        SetPlayerInput(false);
        AudioManager.Play(AudioManager.Sfx.GameOver);
        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        // Real time, so the wait still works if the time scale changes.
        yield return new WaitForSecondsRealtime(restartDelay);
        Restart();
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
