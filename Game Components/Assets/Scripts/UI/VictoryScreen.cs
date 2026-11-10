using TMPro;
using UnityEngine;

/// <summary>
/// Shows the finish time when the player reaches the top, with a notice when
/// it's a new personal best (R09, UC10).
/// </summary>
public class VictoryScreen : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private GameObject newBestBanner;

    private void Start()
    {
        panel.SetActive(false);
    }

    public void Show(float seconds, bool isNewBest)
    {
        panel.SetActive(true);
        timeText.text = $"Time: {CompletionTimer.Format(seconds)}";
        if (newBestBanner != null) newBestBanner.SetActive(isNewBest);
    }

    // Button handlers
    public void PlayAgain() => GameManager.Instance.Restart();
    public void MainMenu() => GameManager.Instance.GoToMainMenu();
}
