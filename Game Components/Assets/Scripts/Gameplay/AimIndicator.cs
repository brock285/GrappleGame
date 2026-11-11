using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shows a spinning marker on the spot where the grappling hook would land if
/// the player fired right now. That way the player can tell when a grapple
/// point is in range and not blocked by a wall. The marker hides while the
/// player is already hooked and whenever nothing can be hooked.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AimIndicator : MonoBehaviour
{
    [SerializeField] private GrapplingHook grapple;

    // Degrees per second the marker turns.
    [SerializeField] private float spinSpeed = 120f;

    // How much the marker grows and shrinks as it pulses (0.15 is 15%).
    [SerializeField] private float pulseAmount = 0.15f;

    private SpriteRenderer marker;

    private void Awake()
    {
        marker = GetComponent<SpriteRenderer>();
        if (grapple == null) grapple = FindAnyObjectByType<GrapplingHook>();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        bool show = false;

        // Ask the hook for its target using the same check it uses when firing,
        // so the marker never shows a spot the hook can't actually reach.
        if (grapple != null && mouse != null && grapple.InputEnabled && !grapple.IsAttached &&
            grapple.TryFindTarget(mouse.position.ReadValue(), out Vector2 point))
        {
            transform.position = new Vector3(point.x, point.y, 0f);
            show = true;
        }

        marker.enabled = show;
        if (!show) return;

        transform.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
        float pulse = 1f + Mathf.Sin(Time.time * 8f) * pulseAmount;
        transform.localScale = Vector3.one * pulse;
    }
}
