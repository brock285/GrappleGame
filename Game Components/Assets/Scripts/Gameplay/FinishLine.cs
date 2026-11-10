using UnityEngine;

/// <summary>
/// Ends the run when the player enters it (UC10). Needs a Collider2D with
/// "Is Trigger" turned on.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FinishLine : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() == null || GameManager.Instance == null) return;
        if (GameManager.Instance.IsFinished) return;

        // Celebrate with bursts in a few colors.
        Vector2 p = other.transform.position;
        Effects.Burst(p, new Color(0.3f, 1f, 0.45f), 20, 7f, 0.15f, 0.7f);
        Effects.Burst(p, new Color(1f, 0.85f, 0.3f), 16, 6f, 0.12f, 0.7f);
        Effects.Burst(p, Color.white, 12, 5f, 0.1f, 0.7f);

        GameManager.Instance.CompleteLevel();
    }
}
