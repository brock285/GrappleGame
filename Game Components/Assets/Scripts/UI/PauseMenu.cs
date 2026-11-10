using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Opens and closes the pause screen with the Esc key (R02, UC7, UC8).
/// Hook the panel's buttons up to Resume, Restart, ShowControls, and MainMenu (R03).
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

        GameManager gm = GameManager.Instance;
        if (gm == null || gm.IsFinished) return;

        if (controlsPanel != null && controlsPanel.activeSelf)
        {
            HideControls(); // Esc backs out of the controls screen first.
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

    public void Resume()
    {
        pausePanel.SetActive(false);
        GameManager.Instance.Resume();
    }

    public void Restart() => GameManager.Instance.Restart();

    public void MainMenu() => GameManager.Instance.GoToMainMenu();

    public void ShowControls()
    {
        if (controlsPanel == null) return;
        pausePanel.SetActive(false);
        controlsPanel.SetActive(true);
    }

    public void HideControls()
    {
        if (controlsPanel == null) return;
        controlsPanel.SetActive(false);
        pausePanel.SetActive(true);
    }
}
