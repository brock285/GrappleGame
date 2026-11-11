using UnityEngine;

/// <summary>
/// The goal at the top of the tower. When the player touches it, it plays a
/// celebration burst and tells GameManager the level is complete (UC10). Its
/// Collider2D has to have "Is Trigger" turned on so the player can pass into it.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FinishLine : MonoBehaviour
{
    // Runs in the Editor when the component is added, and turns on the trigger setting.
    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only the player can finish, and only once. Without the IsFinished
        // check, touching the line twice would save the time twice.
        if (other.GetComponent<PlayerController>() == null || GameManager.Instance == null) return;
        if (GameManager.Instance.IsFinished) return;

        // Celebrate with bursts in green, gold, and white.
        Vector2 p = other.transform.position;
        Effects.Burst(p, new Color(0.3f, 1f, 0.45f), 20, 7f, 0.15f, 0.7f);
        Effects.Burst(p, new Color(1f, 0.85f, 0.3f), 16, 6f, 0.12f, 0.7f);
        Effects.Burst(p, Color.white, 12, 5f, 0.1f, 0.7f);

        GameManager.Instance.CompleteLevel();
    }
}
