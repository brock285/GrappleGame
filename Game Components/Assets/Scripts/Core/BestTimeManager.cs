using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Saves and loads the player's completion times on their own PC (UC11, UC12).
/// It keeps the 10 fastest finished runs in Unity's PlayerPrefs. The game
/// never saves a run that's still in progress, so closing the game in the
/// middle of a climb means starting over (R06).
///
/// The times are only written by RecordTime when a run finishes, and nothing
/// in the game lets the player type in or change a time (NFR8).
/// </summary>
public static class BestTimeManager
{
    private const string SaveKey = "Grappler_BestTimes";
    private const int MaxStoredTimes = 10;

    // PlayerPrefs can't store a list directly, so the times get wrapped in this
    // class and saved as one JSON string.
    [System.Serializable]
    private class TimeList
    {
        public List<float> times = new List<float>();
    }

    /// <summary>Returns every stored time in seconds, fastest first. The list is empty if there are none.</summary>
    public static List<float> GetTimes()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return new List<float>();

        TimeList data = JsonUtility.FromJson<TimeList>(json);
        return data?.times ?? new List<float>();
    }

    /// <summary>Returns the fastest stored time in seconds, or null if the player hasn't finished yet.</summary>
    public static float? GetBestTime()
    {
        List<float> times = GetTimes();
        return times.Count > 0 ? times[0] : (float?)null;
    }

    /// <summary>
    /// Saves a finished run's time and keeps only the 10 fastest. Returns true
    /// if it's a new best, which tells the victory screen to show the new best
    /// banner (R09).
    /// </summary>
    /// <param name="seconds">How long the run took.</param>
    public static bool RecordTime(float seconds)
    {
        List<float> times = GetTimes();

        // The very first finish always counts as a new best.
        bool isNewBest = times.Count == 0 || seconds < times[0];

        times.Add(seconds);
        times.Sort();
        if (times.Count > MaxStoredTimes) times.RemoveRange(MaxStoredTimes, times.Count - MaxStoredTimes);

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new TimeList { times = times }));
        PlayerPrefs.Save();
        return isNewBest;
    }

    /// <summary>
    /// Deletes every stored time. Only the Editor menu (Tools, Grappler, Clear
    /// Best Times) calls this, for testing. Players can't reach it in the game.
    /// </summary>
    public static void ClearTimes()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}
