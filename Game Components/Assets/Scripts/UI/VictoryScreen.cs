using TMPro;
using UnityEngine;

/// <summary>
/// The screen that shows up when the player reaches the top (UC10). It shows
/// the finish time, and it adds a "new best time" banner when the run beats
/// the player's old record (R09). GameManager opens it, and its buttons let
/// the player play again or go back to the main menu.
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

    /// <summary>Opens the victory screen.</summary>
    /// <param name="seconds">The finish time.</param>
    /// <param name="isNewBest">True shows the new best time banner.</param>
    public void Show(float seconds, bool isNewBest)
    {
        panel.SetActive(true);
        timeText.text = $"Time: {CompletionTimer.Format(seconds)}";
        if (newBestBanner != null) newBestBanner.SetActive(isNewBest);
    }

    /// <summary>Starts a new climb from the bottom (the Play again button).</summary>
    public void PlayAgain() => GameManager.Instance.Restart();

    /// <summary>Goes back to the title screen (the Main menu button).</summary>
    public void MainMenu() => GameManager.Instance.GoToMainMenu();
}
