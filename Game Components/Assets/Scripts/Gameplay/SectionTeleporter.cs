using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A testing shortcut. Pressing F1, F2, F3, F4, or F5 moves the player to the
/// start of that section of the tower, so we can test one section without
/// climbing the whole thing first. It only works in the Unity Editor and in
/// development builds. In a normal release build it turns itself off, so
/// players can't skip ahead.
/// </summary>
public class SectionTeleporter : MonoBehaviour
{
    [SerializeField] private Transform player;

    // The start point of each section. TowerLevelBuilder fills these in.
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

        Key[] keys = { Key.F1, Key.F2, Key.F3, Key.F4, Key.F5 };
        for (int i = 0; i < keys.Length && i < sectionStarts.Length; i++)
        {
            if (kb[keys[i]].wasPressedThisFrame) MoveTo(sectionStarts[i]);
        }
    }

    private void MoveTo(Vector2 point)
    {
        // Let go of the hook and stop all movement first, or the player would
        // get yanked back toward the old anchor or keep their old speed.
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
