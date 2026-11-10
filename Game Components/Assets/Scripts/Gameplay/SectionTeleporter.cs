using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Testing shortcut: F1-F4 move the player to the start of each tower section.
/// Only works in the Unity Editor and development builds, never in a release build.
/// Useful for playtesting one section at a time and for the class demo.
/// </summary>
public class SectionTeleporter : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Vector2[] sectionStarts = new Vector2[0];

    private void Awake()
    {
        if (!Application.isEditor && !Debug.isDebugBuild) enabled = false;
        if (player == null)
        {
            PlayerController found = FindAnyObjectByType<PlayerController>();
            if (found != null) player = found.transform;
        }
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null || player == null) return;

        Key[] keys = { Key.F1, Key.F2, Key.F3, Key.F4 };
        for (int i = 0; i < keys.Length && i < sectionStarts.Length; i++)
        {
            if (kb[keys[i]].wasPressedThisFrame) MoveTo(sectionStarts[i]);
        }
    }

    private void MoveTo(Vector2 point)
    {
        if (player.TryGetComponent(out GrapplingHook hook)) hook.Detach();
        if (player.TryGetComponent(out Rigidbody2D rb))
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = point;
        }
        player.position = point;
        Debug.Log($"Testing shortcut: moved the player to {point}.");
    }
}
