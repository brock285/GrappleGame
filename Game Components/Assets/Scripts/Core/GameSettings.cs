using System;
using UnityEngine;

/// <summary>
/// Stores the player's settings, which are music volume and sound effect volume (R05)
/// and brightness (UC13). They're saved in PlayerPrefs, so they stay the same
/// the next time the game opens. AudioManager, AudioChannel, and
/// BrightnessOverlay listen to the Changed event and update themselves when
/// a setting moves.
/// </summary>
public static class GameSettings
{
    private const string MusicKey = "Grappler_MusicVolume";
    private const string SfxKey = "Grappler_SFXVolume";
    private const string BrightnessKey = "Grappler_Brightness";

    /// <summary>
    /// The darkest the brightness setting can go. It stops above 0 so the
    /// screen can't turn completely black.
    /// </summary>
    public const float MinBrightness = 0.3f;

    /// <summary>Raised every time any setting changes.</summary>
    public static event Action Changed;

    /// <summary>Music volume from 0 (off) to 1 (full). Starts at 0.8.</summary>
    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicKey, 0.8f);
        set => Save(MusicKey, Mathf.Clamp01(value));
    }

    /// <summary>Sound effect volume from 0 (off) to 1 (full). Starts at 0.8.</summary>
    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxKey, 0.8f);
        set => Save(SfxKey, Mathf.Clamp01(value));
    }

    /// <summary>Brightness from MinBrightness (darkest) to 1 (normal). Starts at 1.</summary>
    public static float Brightness
    {
        get => PlayerPrefs.GetFloat(BrightnessKey, 1f);
        set => Save(BrightnessKey, Mathf.Clamp(value, MinBrightness, 1f));
    }

    // Writes the value to disk right away and tells listeners about the change.
    private static void Save(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
