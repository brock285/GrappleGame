using UnityEngine;

/// <summary>
/// Keeps track of how long the current run has taken (UC10, UC11). It counts
/// with Unity's scaled time, and pausing sets the time scale to 0, so the
/// timer stops on its own while the game is paused and picks back up on
/// resume (UC7, UC8). GameManager starts it and stops it.
/// </summary>
public class CompletionTimer : MonoBehaviour
{
    /// <summary>Seconds since the run started.</summary>
    public float ElapsedSeconds { get; private set; }

    /// <summary>True while the timer is counting.</summary>
    public bool IsRunning { get; private set; }

    private void Update()
    {
        if (IsRunning) ElapsedSeconds += Time.deltaTime;
    }

    /// <summary>Resets the time to 0 and starts counting.</summary>
    public void StartTimer()
    {
        ElapsedSeconds = 0f;
        IsRunning = true;
    }

    /// <summary>Stops counting and keeps the current time.</summary>
    public void StopTimer() => IsRunning = false;

    /// <summary>
    /// Turns seconds into minutes, seconds, and hundredths, for example 3:07.42.
    /// The HUD, the victory screen, and the best times list all use this.
    /// </summary>
    public static string Format(float seconds)
    {
        int minutes = (int)(seconds / 60f);
        float rest = seconds - minutes * 60f;
        return $"{minutes}:{rest:00.00}";
    }
}
