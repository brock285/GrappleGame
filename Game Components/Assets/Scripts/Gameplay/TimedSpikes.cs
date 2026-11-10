using UnityEngine;

/// <summary>
/// Spikes that pop up and retract on a repeating cycle: hidden, then a short
/// warning blink, then dangerous. Needs a SpriteRenderer (drawn in Tiled mode),
/// a trigger Collider2D, and a Hazard on the same object.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class TimedSpikes : MonoBehaviour
{
    [SerializeField] private float hiddenTime = 1.6f;
    [SerializeField] private float warningTime = 0.6f;
    [SerializeField] private float activeTime = 1.2f;
    [Tooltip("Seconds to shift this spike's cycle, so neighboring spikes take turns.")]
    [SerializeField] private float phaseOffset;
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
        col.isTrigger = true; // Players can stand where the spikes hide.
        baseColor = sr.color;
        width = sr.size.x;
        baseY = transform.position.y - height / 2f; // Ground level under the spikes
    }

    private void Update()
    {
        float t = Mathf.Repeat(Time.time + phaseOffset, CycleLength);

        if (t < hiddenTime)
        {
            SetSpikes(0.14f, 0.6f, false); // Just the tips show, so players know spikes are here.
        }
        else if (t < hiddenTime + warningTime)
        {
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
        // Keep the base of the spikes on the ground while they grow.
        Vector3 p = transform.position;
        transform.position = new Vector3(p.x, baseY + visibleHeight / 2f, p.z);
        sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, baseColor.a * alpha);
        if (col.enabled != dangerous) col.enabled = dangerous;
    }
}
