using UnityEngine;

/// <summary>
/// Tracks how long the current run has taken (UC10, UC11).
/// Uses scaled time, so it stops automatically while the game is paused (UC7, UC8).
/// </summary>
public class CompletionTimer : MonoBehaviour
{
    public float ElapsedSeconds { get; private set; }
    public bool IsRunning { get; private set; }

    private void Update()
    {
        if (IsRunning) ElapsedSeconds += Time.deltaTime;
    }

    public void StartTimer()
    {
        ElapsedSeconds = 0f;
        IsRunning = true;
    }

    public void StopTimer() => IsRunning = false;

    /// <summary>Formats seconds as m:ss.hh, for example 3:07.42.</summary>
    public static string Format(float seconds)
    {
        int minutes = (int)(seconds / 60f);
        float rest = seconds - minutes * 60f;
        return $"{minutes}:{rest:00.00}";
    }
}
