using UnityEngine;

/// <summary>
/// Changes an AudioSource's volume to match the player's music or sound effect
/// setting (R05). Put it on the same object as an AudioSource and pick which
/// channel it belongs to. AudioManager handles the game's own sounds, so this
/// is only needed for extra audio sources placed in a scene.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioChannel : MonoBehaviour
{
    /// <summary>Which volume setting this source follows.</summary>
    public enum Channel { Music, SoundEffects }

    [SerializeField] private Channel channel = Channel.SoundEffects;

    private AudioSource source;

    // The volume set in the Inspector counts as 100%. The setting scales it down from there.
    private float baseVolume;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        baseVolume = source.volume;
    }

    private void OnEnable()
    {
        GameSettings.Changed += Apply;
        Apply();
    }

    private void OnDisable() => GameSettings.Changed -= Apply;

    // Runs whenever a setting changes, so moving a slider updates the volume right away.
    private void Apply()
    {
        float setting = channel == Channel.Music ? GameSettings.MusicVolume : GameSettings.SfxVolume;
        source.volume = baseVolume * setting;
    }
}
