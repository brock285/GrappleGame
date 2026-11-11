using UnityEngine;

/// <summary>
/// One small square from a particle burst. Effects.Burst creates these and
/// calls Init. Each one flies out, falls with gravity, shrinks, fades, and
/// then deletes itself when its lifetime runs out.
/// </summary>
public class BurstParticle : MonoBehaviour
{
    // How fast the particle falls, in units per second squared.
    private const float Gravity = 12f;

    private Vector2 velocity;
    private float life;
    private float age;
    private Vector3 startScale;
    private SpriteRenderer sr;
    private Color startColor;

    /// <summary>Sets the particle moving. Effects.Burst calls this right after it creates the particle.</summary>
    /// <param name="startVelocity">Starting speed and direction in units per second.</param>
    /// <param name="lifetime">Seconds before the particle disappears.</param>
    public void Init(Vector2 startVelocity, float lifetime)
    {
        velocity = startVelocity;
        life = lifetime;
        startScale = transform.localScale;
        sr = GetComponent<SpriteRenderer>();
        startColor = sr.color;
    }

    private void Update()
    {
        age += Time.deltaTime;
        if (age >= life)
        {
            Destroy(gameObject);
            return;
        }

        velocity.y -= Gravity * Time.deltaTime;
        transform.position += (Vector3)(velocity * Time.deltaTime);

        // t goes from 0 to 1 over the particle's life. It shrinks to 40% of
        // its size and fades to clear by the end.
        float t = age / life;
        transform.localScale = startScale * (1f - t * 0.6f);
        sr.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));
    }
}
