using UnityEngine;

/// <summary>
/// Knocks the player back when touched (R07). Attach to any obstacle with a
/// Collider2D. Works with both solid colliders and triggers.
/// </summary>
public class Hazard : MonoBehaviour
{
    [Tooltip("Push direction relative to the hazard. It is mirrored to point away from the player's side.")]
    [SerializeField] private Vector2 knockbackDirection = new Vector2(1f, 1f);
    [SerializeField] private float knockbackStrength = 10f;

    private void OnCollisionEnter2D(Collision2D collision) => TryHit(collision.collider);

    private void OnTriggerEnter2D(Collider2D other) => TryHit(other);

    private void TryHit(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerController player)) return;

        Vector2 dir = knockbackDirection.normalized;
        bool playerIsLeft = other.transform.position.x < transform.position.x;
        dir.x = Mathf.Abs(dir.x) * (playerIsLeft ? -1f : 1f);

        player.Knockback(dir * knockbackStrength);
    }
}
