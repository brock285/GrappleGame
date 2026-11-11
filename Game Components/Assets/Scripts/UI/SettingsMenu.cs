using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Connects the settings screen's sliders to GameSettings. There's one slider
/// each for music volume, sound effect volume (R05), and brightness (UC13).
/// Moving a slider saves the new value right away, and it stays saved after
/// the game closes. A slider that's left empty in the Inspector is skipped.
///
/// This script has to go on an object that stays active, like the Canvas,
/// and not on the settings panel itself. Start only runs on active objects,
/// and the panel starts out hidden.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;      // Range 0 to 1
    [SerializeField] private Slider sfxSlider;        // Range 0 to 1
    [SerializeField] private Slider brightnessSlider; // Range is set in Start

    private void Start()
    {
        // Each slider starts at the saved value. SetValueWithoutNotify moves
        // the slider without triggering a save.
        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
            musicSlider.onValueChanged.AddListener(v => GameSettings.MusicVolume = v);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
            sfxSlider.onValueChanged.AddListener(v => GameSettings.SfxVolume = v);
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = GameSettings.MinBrightness;
            brightnessSlider.maxValue = 1f;
            brightnessSlider.SetValueWithoutNotify(GameSettings.Brightness);
            brightnessSlider.onValueChanged.AddListener(v => GameSettings.Brightness = v);
        }
    }
}
