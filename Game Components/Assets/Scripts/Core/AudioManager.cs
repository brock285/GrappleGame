using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Plays the background music and every sound effect in the game (R05, UC13).
/// The music follows the music volume setting and the effects follow the
/// sound effect volume setting.
///
/// It creates itself when the game starts and stays loaded between scenes, so
/// nobody has to add it to a scene. The sound files live in
/// Assets/Resources/Audio. We made them with a script, so they don't need a
/// license.
///
/// Other scripts can play a sound with AudioManager.Play. Most sounds don't
/// need that, though. When a scene loads, this class finds the player and the
/// grappling hook and listens to their events, and it adds a click to every
/// button.
/// </summary>
public class AudioManager : MonoBehaviour
{
    /// <summary>
    /// The sound effects the game can play. The order has to match the file
    /// names list in Awake.
    /// </summary>
    public enum Sfx { GrappleAttach, GrappleRelease, Land, Hit, Click, Finish, NewBest }

    // Music plays a little quieter than the effects so it doesn't drown them out.
    private const float MusicBaseVolume = 0.55f;

    // How loud the music is while paused, compared to normal.
    private const float PausedMusicLevel = 0.4f;

    // A landing at this fall speed or faster plays the thud at full volume.
    private const float HardLandingSpeed = 18f;

    /// <summary>The one AudioManager in the game, or null before it's created.</summary>
    public static AudioManager Instance { get; private set; }

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioClip[] clips;

    // 1 at normal volume, drops toward PausedMusicLevel while the game is paused.
    private float musicDuck = 1f;

    // The player objects in the current scene, if there are any (the main menu has none).
    private PlayerController player;
    private GrapplingHook grapple;

    // Unity calls this once when the game starts, before the first scene loads.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        go.AddComponent<AudioManager>();
    }

    /// <summary>Plays a sound effect at the player's sound effect volume.</summary>
    /// <param name="sfx">Which sound to play.</param>
    /// <param name="volume">Extra volume scale from 0 to 1, on top of the setting.</param>
    public static void Play(Sfx sfx, float volume = 1f)
    {
        if (Instance != null) Instance.PlayOneShot(sfx, volume);
    }

    private void Awake()
    {
        Instance = this;

        // One source loops the music and a second one plays the effects, so
        // effects never cut the music off.
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.clip = Resources.Load<AudioClip>("Audio/music_loop");

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        // These names are in the same order as the Sfx enum.
        string[] names = { "grapple_attach", "grapple_release", "land", "hit", "click", "finish", "new_best" };
        clips = new AudioClip[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            clips[i] = Resources.Load<AudioClip>("Audio/" + names[i]);
            if (clips[i] == null) Debug.LogWarning($"AudioManager: missing Resources/Audio/{names[i]}");
        }
    }

    private void OnEnable()
    {
        GameSettings.Changed += ApplyMusicVolume;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        GameSettings.Changed -= ApplyMusicVolume;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Unsubscribe();
    }

    private void Start()
    {
        ApplyMusicVolume();
        if (musicSource.clip != null) musicSource.Play();
    }

    private void Update()
    {
        // Fade the music down while the game is paused and back up after.
        // Pausing sets the time scale to 0, so this uses unscaled time.
        bool paused = GameManager.Instance != null && GameManager.Instance.IsPaused;
        float target = paused ? PausedMusicLevel : 1f;
        if (!Mathf.Approximately(musicDuck, target))
        {
            musicDuck = Mathf.MoveTowards(musicDuck, target, Time.unscaledDeltaTime * 3f);
            ApplyMusicVolume();
        }
    }

    private void ApplyMusicVolume()
    {
        musicSource.volume = MusicBaseVolume * GameSettings.MusicVolume * musicDuck;
    }

    private void PlayOneShot(Sfx sfx, float volume)
    {
        AudioClip clip = clips[(int)sfx];
        if (clip == null) return;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume) * GameSettings.SfxVolume);
    }

    // Runs after every scene load. It hooks up the new scene's player and
    // buttons, since the old ones were destroyed with the last scene.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Unsubscribe();

        player = FindAnyObjectByType<PlayerController>();
        grapple = FindAnyObjectByType<GrapplingHook>();
        if (player != null)
        {
            player.Landed += OnLanded;
            player.Hit += OnHit;
        }
        if (grapple != null)
        {
            grapple.Attached += OnAttached;
            grapple.Detached += OnDetached;
        }

        // Every button clicks, including ones on hidden panels, so the menus
        // don't need any extra setup.
        foreach (Button button in FindObjectsByType<Button>(FindObjectsInactive.Include))
            button.onClick.AddListener(OnButtonClick);
    }

    private void Unsubscribe()
    {
        if (player != null)
        {
            player.Landed -= OnLanded;
            player.Hit -= OnHit;
        }
        if (grapple != null)
        {
            grapple.Attached -= OnAttached;
            grapple.Detached -= OnDetached;
        }
        player = null;
        grapple = null;
    }

    // Harder landings are louder. A small hop plays at about a third of full volume.
    private void OnLanded(float fallSpeed) => PlayOneShot(Sfx.Land, Mathf.Lerp(0.35f, 1f, fallSpeed / HardLandingSpeed));
    private void OnHit(Vector2 direction) => PlayOneShot(Sfx.Hit, 1f);
    private void OnAttached(Vector2 point) => PlayOneShot(Sfx.GrappleAttach, 0.8f);
    private void OnDetached() => PlayOneShot(Sfx.GrappleRelease, 0.6f);
    private void OnButtonClick() => PlayOneShot(Sfx.Click, 0.7f);
}
