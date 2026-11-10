using UnityEngine;

/// <summary>
/// Scales an AudioSource's volume by the player's music or sound effect setting
/// (R05). Add it next to any AudioSource and pick the channel.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioChannel : MonoBehaviour
{
    public enum Channel { Music, SoundEffects }

    [SerializeField] private Channel channel = Channel.SoundEffects;

    private AudioSource source;
    private float baseVolume;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        baseVolume = source.volume; // The volume set in the Inspector is the 100% level.
    }

    private void OnEnable()
    {
        GameSettings.Changed += Apply;
        Apply();
    }

    private void OnDisable() => GameSettings.Changed -= Apply;

    private void Apply()
    {
        float setting = channel == Channel.Music ? GameSettings.MusicVolume : GameSettings.SfxVolume;
        source.volume = baseVolume * setting;
    }
}
