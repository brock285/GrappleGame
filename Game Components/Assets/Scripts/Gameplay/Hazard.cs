using UnityEngine;

/// <summary>
/// Hurts the player and knocks them back when they touch it (R07). Each hit
/// costs a heart, unless the player was just hit. It goes on any obstacle
/// with a Collider2D, like spikes, and works with both solid colliders and
/// triggers. The push always goes away from the hazard, so the player gets
/// sent back the way they came.
/// </summary>
public class Hazard : MonoBehaviour
{
    [Tooltip("Push direction relative to the hazard. It's mirrored to point away from the player's side.")]
    [SerializeField] private Vector2 knockbackDirection = new Vector2(1f, 1f);

    // How hard the push is. Higher values throw the player farther.
    [SerializeField] private float knockbackStrength = 10f;

    // How many hearts one hit costs.
    [SerializeField] private int damage = 1;

    private void OnCollisionEnter2D(Collision2D collision) => TryHit(collision.collider);

    private void OnTriggerEnter2D(Collider2D other) => TryHit(other);

    private void TryHit(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerController player)) return;

        // Flip the sideways part of the push so it points away from the
        // hazard, whichever side the player came from.
        Vector2 dir = knockbackDirection.normalized;
        bool playerIsLeft = other.transform.position.x < transform.position.x;
        dir.x = Mathf.Abs(dir.x) * (playerIsLeft ? -1f : 1f);

        if (player.TryGetComponent(out PlayerHealth health)) health.TakeDamage(damage);
        player.Knockback(dir * knockbackStrength);
    }
}
