using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shows the frame rate in the top left corner of the screen so we can check
/// NFR2, which says the game has to stay above 60 frames per second on the lab
/// computers. Pressing F8 turns it on and off. It starts hidden, so players
/// never see it unless they press the key.
///
/// It shows two numbers. The first is the average frame rate over the last
/// half second. The second is the lowest frame rate since the counter was
/// turned on, which catches short stutters an average would hide. Both turn
/// green at 60 or above and red below 60. Turning the counter off and back on
/// resets the lowest number.
///
/// Like AudioManager, it creates itself when the game starts and stays loaded
/// between scenes, so it works in every scene and in the Windows build
/// without any setup.
/// </summary>
public class FrameRateCounter : MonoBehaviour
{
    // The frame rate NFR2 asks for.
    private const float TargetFps = 60f;

    // How often the average updates, in seconds.
    private const float SampleTime = 0.5f;

    private bool visible;
    private float timer;
    private int frames;
    private float averageFps;
    private float lowestFps;
    private GUIStyle style;

    // Unity calls this once when the game starts, before the first scene loads.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        var go = new GameObject("FrameRateCounter");
        DontDestroyOnLoad(go);
        go.AddComponent<FrameRateCounter>();
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb != null && kb.f8Key.wasPressedThisFrame)
        {
            visible = !visible;
            ResetCounts();
        }
        if (!visible) return;

        // Unscaled time keeps counting while the game is paused, so the
        // counter still works on the pause screen.
        float dt = Time.unscaledDeltaTime;
        frames++;
        timer += dt;

        // Skip the first few frames after turning it on, since the frame
        // where the key was pressed can be slower than normal.
        if (dt > 0f && frames > 3) lowestFps = Mathf.Min(lowestFps, 1f / dt);

        if (timer >= SampleTime)
        {
            averageFps = frames / timer;
            frames = 0;
            timer = 0f;
        }
    }

    private void ResetCounts()
    {
        timer = 0f;
        frames = 0;
        averageFps = 0f;
        lowestFps = float.MaxValue;
    }

    // OnGUI draws simple on-screen text without needing a Canvas in the scene.
    private void OnGUI()
    {
        if (!visible) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, richText = true };
        }

        string avg = averageFps > 0f ? Colored(averageFps) : "...";
        string low = lowestFps < float.MaxValue ? Colored(lowestFps) : "...";
        string text = $"FPS {avg}   Lowest {low}";

        // A dark box behind the text so it's readable on any background.
        var box = new Rect(10f, 10f, 330f, 36f);
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(20f, 14f, 320f, 30f), text, style);
    }

    // Rounds the number and colors it green if it meets the target or red if it doesn't.
    private static string Colored(float fps)
    {
        string color = fps >= TargetFps ? "#5CFF7A" : "#FF5C5C";
        return $"<color={color}>{Mathf.RoundToInt(fps)}</color>";
    }
}
