using TMPro;
using UnityEngine;

/// <summary>
/// Shows the running completion time at the top of the screen while the
/// player climbs (UC10). It reads the time from CompletionTimer every frame
/// and writes it into a TextMeshPro label, so it stops changing when the timer
/// stops, like when the game is paused.
/// </summary>
public class TimerDisplay : MonoBehaviour
{
    [SerializeField] private CompletionTimer timer;
    [SerializeField] private TMP_Text label;

    private void Awake()
    {
        // Find anything that's missing, for example after the level was rebuilt.
        if (timer == null) timer = FindAnyObjectByType<CompletionTimer>();
        if (label == null) label = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (timer == null || label == null) return;
        label.text = CompletionTimer.Format(timer.ElapsedSeconds);
    }
}
