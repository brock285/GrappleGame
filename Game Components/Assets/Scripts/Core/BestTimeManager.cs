using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Saves and loads completion times on the player's machine (UC11, UC12).
/// Only finished runs are stored. Mid-run progress is never saved (R06).
/// </summary>
public static class BestTimeManager
{
    private const string SaveKey = "Grappler_BestTimes";
    private const int MaxStoredTimes = 10;

    [System.Serializable]
    private class TimeList
    {
        public List<float> times = new List<float>();
    }

    /// <summary>All stored times, fastest first.</summary>
    public static List<float> GetTimes()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json)) return new List<float>();

        TimeList data = JsonUtility.FromJson<TimeList>(json);
        return data?.times ?? new List<float>();
    }

    /// <summary>The fastest stored time, or null if there are none yet.</summary>
    public static float? GetBestTime()
    {
        List<float> times = GetTimes();
        return times.Count > 0 ? times[0] : (float?)null;
    }

    /// <summary>
    /// Records a finished run. Returns true if it beats the previous best (R09).
    /// </summary>
    public static bool RecordTime(float seconds)
    {
        List<float> times = GetTimes();
        bool isNewBest = times.Count == 0 || seconds < times[0];

        times.Add(seconds);
        times.Sort();
        if (times.Count > MaxStoredTimes) times.RemoveRange(MaxStoredTimes, times.Count - MaxStoredTimes);

        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(new TimeList { times = times }));
        PlayerPrefs.Save();
        return isNewBest;
    }

    /// <summary>Deletes all stored times. Useful for testing.</summary>
    public static void ClearTimes()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();
    }
}
