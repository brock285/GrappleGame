using UnityEngine;

/// <summary>
/// Lightweight particle bursts for impacts: landing dust, grapple sparks,
/// hazard hits, and the finish. Call Effects.Burst from anywhere; no setup needed.
/// </summary>
public static class Effects
{
    private static Sprite pixel;

    /// <summary>Spawns a burst of small squares that fly out, fall, and fade.</summary>
    /// <param name="position">World position of the burst.</param>
    /// <param name="color">Particle color.</param>
    /// <param name="count">Number of particles.</param>
    /// <param name="speed">Average launch speed in units per second.</param>
    /// <param name="size">Particle size in units.</param>
    /// <param name="upwardBias">0 sprays in all directions; 1 sprays mostly upward.</param>
    public static void Burst(Vector2 position, Color color, int count = 10, float speed = 4f,
                             float size = 0.12f, float upwardBias = 0f)
    {
        if (pixel == null)
        {
            Texture2D tex = Texture2D.whiteTexture; // Built-in 4x4 white texture.
            pixel = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Particle");
            go.transform.position = position;
            go.transform.localScale = Vector3.one * size * Random.Range(0.7f, 1.3f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = pixel;
            sr.color = color;
            sr.sortingOrder = 50;

            Vector2 dir = Random.insideUnitCircle.normalized;
            dir = Vector2.Lerp(dir, new Vector2(dir.x * 0.5f, Mathf.Abs(dir.y) + 0.5f), upwardBias).normalized;

            var particle = go.AddComponent<BurstParticle>();
            particle.Init(dir * speed * Random.Range(0.6f, 1.4f), Random.Range(0.35f, 0.7f));
        }
    }
}
