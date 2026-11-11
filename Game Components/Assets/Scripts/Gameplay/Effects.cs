using UnityEngine;

/// <summary>
/// Makes small particle bursts for things like landing dust, grapple sparks,
/// hazard hits, and the finish. Any script can call Effects.Burst, and it
/// doesn't need anything set up in the scene.
/// </summary>
public static class Effects
{
    // A plain white square shared by every particle. It gets tinted to each burst's color.
    private static Sprite pixel;

    /// <summary>Creates a burst of small squares that fly out, fall, and fade away.</summary>
    /// <param name="position">Where the burst happens, in world space.</param>
    /// <param name="color">The particles' color.</param>
    /// <param name="count">How many particles to make.</param>
    /// <param name="speed">Average launch speed in units per second.</param>
    /// <param name="size">Particle size in units.</param>
    /// <param name="upwardBias">0 sprays in every direction and 1 sprays mostly upward.</param>
    public static void Burst(Vector2 position, Color color, int count = 10, float speed = 4f,
                             float size = 0.12f, float upwardBias = 0f)
    {
        if (pixel == null)
        {
            // Unity has a built-in 4 by 4 white texture, so no art file is needed.
            Texture2D tex = Texture2D.whiteTexture;
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
            sr.sortingOrder = 50; // Draw in front of the level and the player.

            // Pick a random direction, then bend it upward based on upwardBias.
            Vector2 dir = Random.insideUnitCircle.normalized;
            dir = Vector2.Lerp(dir, new Vector2(dir.x * 0.5f, Mathf.Abs(dir.y) + 0.5f), upwardBias).normalized;

            var particle = go.AddComponent<BurstParticle>();
            particle.Init(dir * speed * Random.Range(0.6f, 1.4f), Random.Range(0.35f, 0.7f));
        }
    }
}
