using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Opens and closes the pause screen when the player presses Esc (R02, UC7,
/// UC8). The pause panel's buttons call Resume, Restart (UC9), ShowControls,
/// and MainMenu (R03). GameManager does the actual pausing, and this script
/// only handles the key and the panels.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject controlsPanel;

    private void Start()
    {
        pausePanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

        // There's nothing to pause on the victory screen.
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.IsFinished) return;

        if (controlsPanel != null && controlsPanel.activeSelf)
        {
            // If the controls screen is open, Esc goes back to the pause screen first.
            HideControls();
        }
        else if (gm.IsPaused)
        {
            Resume();
        }
        else
        {
            gm.Pause();
            pausePanel.SetActive(true);
        }
    }

    /// <summary>Closes the pause screen and goes back to the game (UC8).</summary>
    public void Resume()
    {
        pausePanel.SetActive(false);
        GameManager.Instance.Resume();
    }

    /// <summary>Starts the climb over from the bottom with the timer reset (UC9).</summary>
    public void Restart() => GameManager.Instance.Restart();

    /// <summary>Leaves the climb and goes back to the main menu (R03).</summary>
    public void MainMenu() => GameManager.Instance.GoToMainMenu();

    /// <summary>Swaps the pause screen for the controls screen (UC2).</summary>
    public void ShowControls()
    {
        if (controlsPanel == null) return;
        pausePanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    /// <summary>Swaps the controls screen back to the pause screen.</summary>
    public void HideControls()
    {
        if (controlsPanel == null) return;
        controlsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }
}
