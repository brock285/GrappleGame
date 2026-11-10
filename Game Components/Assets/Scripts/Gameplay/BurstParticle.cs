using UnityEngine;

/// <summary>One particle from Effects.Burst. Moves, falls, shrinks, and fades out.</summary>
public class BurstParticle : MonoBehaviour
{
    private const float Gravity = 12f;

    private Vector2 velocity;
    private float life;
    private float age;
    private Vector3 startScale;
    private SpriteRenderer sr;
    private Color startColor;

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

        float t = age / life;
        transform.localScale = startScale * (1f - t * 0.6f);
        sr.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - t));
    }
}
