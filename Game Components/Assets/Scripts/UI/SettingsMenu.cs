using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings screen: music volume, sound effect volume (R05), and brightness
/// (UC13). Values are stored by GameSettings and persist between sessions.
/// Any slider left empty is ignored.
///
/// Put this on an object that stays active (such as the Canvas), not on the
/// settings panel itself.
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    [SerializeField] private Slider musicSlider;      // Range 0 to 1
    [SerializeField] private Slider sfxSlider;        // Range 0 to 1
    [SerializeField] private Slider brightnessSlider; // Range set automatically

    private void Start()
    {
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
