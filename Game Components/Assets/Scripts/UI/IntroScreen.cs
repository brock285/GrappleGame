using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The "How to play" screen that shows up before a climb starts. It lists the
/// controls and explains what happens in the game, like hearts, rope cutting,
/// and the timer. The player closes it with the Start climbing button or by
/// pressing Enter. GameManager decides when to show it. It shows when the
/// player comes from the main menu, but not after a restart, since they've
/// already read it.
/// </summary>
public class IntroScreen : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    /// <summary>True while the screen is open.</summary>
    public bool IsOpen => panel != null && panel.activeSelf;

    /// <summary>Opens the screen. GameManager calls this at the start of the scene.</summary>
    public void Show()
    {
        if (panel != null) panel.SetActive(true);
    }

    /// <summary>Closes the screen without starting anything.</summary>
    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    /// <summary>The Start climbing button. Closes the screen and lets the player move.</summary>
    public void StartClimbing()
    {
        Hide();
        if (GameManager.Instance != null) GameManager.Instance.BeginRun();
    }

    private void Update()
    {
        if (!IsOpen) return;

        // Enter works too, so the player doesn't have to click.
        Keyboard kb = Keyboard.current;
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            StartClimbing();
    }
}
