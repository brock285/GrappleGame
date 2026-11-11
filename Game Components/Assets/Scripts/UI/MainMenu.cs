using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The title screen. From here the player can start the game (UC1), read the
/// controls (UC2), look at their best times (UC12), change settings (UC13),
/// or quit. Each of those screens is its own panel, and only one panel shows
/// at a time. The buttons on the panels call the public methods below.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Game";

    [Header("Panels")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject bestTimesPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Best times")]
    [SerializeField] private TMP_Text bestTimeLabel;   // The single best time on the main panel (UC11).
    [SerializeField] private TMP_Text bestTimesList;   // The full list on the best times panel.

    private void Start()
    {
        // Make sure time runs normally, in case the player came here from the pause menu.
        Time.timeScale = 1f;
        ShowPanel(mainPanel);
        RefreshBestTime();
    }

    /// <summary>Loads the game scene and starts a new climb (UC1).</summary>
    public void StartGame() => SceneManager.LoadScene(gameSceneName);

    /// <summary>Shows the controls screen (UC2).</summary>
    public void ShowControls() => ShowPanel(controlsPanel);

    /// <summary>Shows the settings screen (UC13).</summary>
    public void ShowSettings() => ShowPanel(settingsPanel);

    /// <summary>Goes back to the main title panel.</summary>
    public void BackToMain() => ShowPanel(mainPanel);

    /// <summary>
    /// Fills in the list of saved times and shows the best times screen (UC12).
    /// The top three are bold so they stand out from the rest.
    /// </summary>
    public void ShowBestTimes()
    {
        List<float> times = BestTimeManager.GetTimes();
        var sb = new StringBuilder();

        if (times.Count == 0)
        {
            sb.Append("No times yet. Reach the top to set one.");
        }
        else
        {
            for (int i = 0; i < times.Count; i++)
            {
                // TextMeshPro reads <b> tags as bold.
                string line = $"{i + 1}. {CompletionTimer.Format(times[i])}";
                sb.AppendLine(i < 3 ? $"<b>{line}</b>" : line);
            }
        }

        bestTimesList.text = sb.ToString();
        ShowPanel(bestTimesPanel);
    }

    /// <summary>
    /// Closes the game. In the Unity Editor it stops Play mode instead, since
    /// Application.Quit doesn't do anything there.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Shows the player's best time on the title screen, or "--" if they haven't finished yet.
    private void RefreshBestTime()
    {
        if (bestTimeLabel == null) return;
        float? best = BestTimeManager.GetBestTime();
        bestTimeLabel.text = best.HasValue ? $"Best: {CompletionTimer.Format(best.Value)}" : "Best: --";
    }

    // Turns on the given panel and turns off every other one.
    private void ShowPanel(GameObject panel)
    {
        foreach (GameObject p in new[] { mainPanel, controlsPanel, bestTimesPanel, settingsPanel })
        {
            if (p != null) p.SetActive(p == panel);
        }
    }
}
