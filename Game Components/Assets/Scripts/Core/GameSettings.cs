using System;
using UnityEngine;

/// <summary>
/// Player settings that persist between sessions: music volume, sound effect
/// volume (R05), and brightness (UC13). Components such as AudioChannel and
/// BrightnessOverlay listen to Changed and update themselves.
/// </summary>
public static class GameSettings
{
    private const string MusicKey = "Grappler_MusicVolume";
    private const string SfxKey = "Grappler_SFXVolume";
    private const string BrightnessKey = "Grappler_Brightness";

    public const float MinBrightness = 0.3f;

    /// <summary>Raised whenever any setting changes.</summary>
    public static event Action Changed;

    /// <summary>Music volume from 0 to 1.</summary>
    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicKey, 0.8f);
        set => Save(MusicKey, Mathf.Clamp01(value));
    }

    /// <summary>Sound effect volume from 0 to 1.</summary>
    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxKey, 0.8f);
        set => Save(SfxKey, Mathf.Clamp01(value));
    }

    /// <summary>Brightness from MinBrightness (darkest) to 1 (normal).</summary>
    public static float Brightness
    {
        get => PlayerPrefs.GetFloat(BrightnessKey, 1f);
        set => Save(BrightnessKey, Mathf.Clamp(value, MinBrightness, 1f));
    }

    private static void Save(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
