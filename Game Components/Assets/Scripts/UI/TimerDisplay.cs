using TMPro;
using UnityEngine;

/// <summary>Shows the running completion time on the in-game HUD.</summary>
public class TimerDisplay : MonoBehaviour
{
    [SerializeField] private CompletionTimer timer;
    [SerializeField] private TMP_Text label;

    private void Awake()
    {
        // Find what's missing, for example after the level was rebuilt.
        if (timer == null) timer = FindAnyObjectByType<CompletionTimer>();
        if (label == null) label = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (timer == null || label == null) return;
        label.text = CompletionTimer.Format(timer.ElapsedSeconds);
    }
}
