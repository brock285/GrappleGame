using UnityEngine;

/// <summary>
/// Spikes that pop up and go back down on a repeating cycle. First they're
/// hidden (only the tips show), then they blink as a warning, then they stick
/// up and hurt the player. The object needs a SpriteRenderer set to Tiled draw
/// mode, a trigger Collider2D, and a Hazard component. The Hazard does the
/// knockback, and this script only turns its collider on while the spikes are up.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class TimedSpikes : MonoBehaviour
{
    // Seconds spent in each part of the cycle.
    [SerializeField] private float hiddenTime = 1.6f;
    [SerializeField] private float warningTime = 0.6f;
    [SerializeField] private float activeTime = 1.2f;

    [Tooltip("Seconds to shift this spike's cycle, so spikes next to each other take turns.")]
    [SerializeField] private float phaseOffset;

    // How tall the spikes are when they're fully up.
    [SerializeField] private float height = 0.5f;

    private SpriteRenderer sr;
    private Collider2D col;
    private Color baseColor;
    private float width;
    private float baseY;

    private float CycleLength => hiddenTime + warningTime + activeTime;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        // A trigger, so the player can walk over the spot while the spikes are down.
        col.isTrigger = true;
        baseColor = sr.color;
        width = sr.size.x;

        // The ground level under the spikes. The spikes grow up from here.
        baseY = transform.position.y - height / 2f;
    }

    private void Update()
    {
        // Where we are in the cycle right now, from 0 up to CycleLength.
        float t = Mathf.Repeat(Time.time + phaseOffset, CycleLength);

        if (t < hiddenTime)
        {
            // Just the tips show, so players know spikes are there.
            SetSpikes(0.14f, 0.6f, false);
        }
        else if (t < hiddenTime + warningTime)
        {
            // Blink a few times as a warning before they come up.
            bool blink = Mathf.Repeat(t, 0.15f) < 0.075f;
            SetSpikes(0.18f, blink ? 1f : 0.5f, false);
        }
        else
        {
            SetSpikes(height, 1f, true);
        }
    }

    private void SetSpikes(float visibleHeight, float alpha, bool dangerous)
    {
        sr.size = new Vector2(width, visibleHeight);

        // Move the object up by half the height so the bottom of the spikes
        // stays on the ground while they grow.
        Vector3 p = transform.position;
        transform.position = new Vector3(p.x, baseY + visibleHeight / 2f, p.z);
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha);

        // The collider is only on while the spikes are up, so they can only hurt the player then.
        if (col.enabled != dangerous) col.enabled = dangerous;
    }
}
