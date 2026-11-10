using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Title screen: start the game (UC1), view controls (UC2), view best times
/// (UC12), open settings (UC13), and quit. Each screen is a separate panel;
/// only one is visible at a time.
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
    [SerializeField] private TMP_Text bestTimeLabel;   // Shown on the main panel.
    [SerializeField] private TMP_Text bestTimesList;   // Shown on the best times panel.

    private void Start()
    {
        Time.timeScale = 1f;
        ShowPanel(mainPanel);
        RefreshBestTime();
    }

    public void StartGame() => SceneManager.LoadScene(gameSceneName);

    public void ShowControls() => ShowPanel(controlsPanel);
    public void ShowSettings() => ShowPanel(settingsPanel);
    public void BackToMain() => ShowPanel(mainPanel);

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
                // The top three are bold so they stand out from the rest (UC12).
                string line = $"{i + 1}. {CompletionTimer.Format(times[i])}";
                sb.AppendLine(i < 3 ? $"<b>{line}</b>" : line);
            }
        }

        bestTimesList.text = sb.ToString();
        ShowPanel(bestTimesPanel);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RefreshBestTime()
    {
        if (bestTimeLabel == null) return;
        float? best = BestTimeManager.GetBestTime();
        bestTimeLabel.text = best.HasValue ? $"Best: {CompletionTimer.Format(best.Value)}" : "Best: --";
    }

    private void ShowPanel(GameObject panel)
    {
        foreach (GameObject p in new[] { mainPanel, controlsPanel, bestTimesPanel, settingsPanel })
        {
            if (p != null) p.SetActive(p == panel);
        }
    }
}
