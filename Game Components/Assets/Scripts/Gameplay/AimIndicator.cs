using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shows a marker where the grappling hook will land if fired now, so players
/// can see when a target is in range and in sight. Hidden while hooked and
/// when nothing can be hooked.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class AimIndicator : MonoBehaviour
{
    [SerializeField] private GrapplingHook grapple;
    [SerializeField] private float spinSpeed = 120f;
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
