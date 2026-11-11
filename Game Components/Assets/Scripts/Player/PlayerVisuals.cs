using UnityEngine;

/// <summary>
/// Handles how the player looks. The sprite faces the way the player is
/// moving, leans into movement, squashes on a landing, stretches when the hook
/// catches, and flashes red when hit. It also makes dust when the player lands
/// and sparks where the hook attaches. None of this changes gameplay.
///
/// It goes on the Player object. The sprite has to be on a child object named
/// Body, so squashing and stretching the picture doesn't change the size of
/// the player's collider.
/// </summary>
public class PlayerVisuals : MonoBehaviour
{
    [SerializeField] private Transform body;
    [SerializeField] private SpriteRenderer bodyRenderer;
    // How fast a squash or stretch eases back to normal. Higher is quicker.
    [SerializeField] private float returnSpeed = 12f;

    // The most the sprite tilts while moving, in degrees.
    [SerializeField] private float maxLean = 10f;
    [SerializeField] private Color hitColor = new Color(1f, 0.35f, 0.35f);
    [SerializeField] private Color dustColor = new Color(0.8f, 0.8f, 0.85f, 0.9f);
    [SerializeField] private Color sparkColor = new Color(1f, 0.75f, 0.3f);

    private PlayerController controller;
    private GrapplingHook grapple;
    private Vector3 baseScale;
    private Vector3 squash = Vector3.one; // 1 on each axis is normal size.
    private Color baseColor;
    private float flashTimer;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        grapple = GetComponent<GrapplingHook>();

        if (body == null)
        {
            Transform found = transform.Find("Body");
            body = found != null ? found : transform;
        }
        if (bodyRenderer == null) bodyRenderer = body.GetComponent<SpriteRenderer>();

        baseScale = body.localScale;
        if (bodyRenderer != null) baseColor = bodyRenderer.color;
    }

    // Listen to the player's events while this component is on.
    private void OnEnable()
    {
        if (controller != null)
        {
            controller.Landed += OnLanded;
            controller.Hit += OnHit;
        }
        if (grapple != null) grapple.Attached += OnAttached;
    }

    private void OnDisable()
    {
        if (controller != null)
        {
            controller.Landed -= OnLanded;
            controller.Hit -= OnHit;
        }
        if (grapple != null) grapple.Attached -= OnAttached;
    }

    private void Update()
    {
        if (controller == null) return;

        // Face the direction of movement.
        if (bodyRenderer != null) bodyRenderer.flipX = controller.Facing < 0;

        // Ease squash and stretch back to normal.
        squash = Vector3.Lerp(squash, Vector3.one, returnSpeed * Time.deltaTime);
        if (body != transform)
        {
            body.localScale = Vector3.Scale(baseScale, squash);

            // Lean slightly into horizontal movement.
            float lean = Mathf.Clamp(-controller.Velocity.x * 1.5f, -maxLean, maxLean);
            body.localRotation = Quaternion.Lerp(body.localRotation, Quaternion.Euler(0f, 0f, lean), 10f * Time.deltaTime);
        }

        // Flash between the hit color and the normal color while the flash timer runs.
        if (bodyRenderer != null)
        {
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                bool on = Mathf.Repeat(flashTimer, 0.1f) > 0.05f;
                bodyRenderer.color = on ? hitColor : baseColor;
            }
            else
            {
                bodyRenderer.color = baseColor;
            }
        }
    }

    // Harder landings squash more and kick up more dust.
    private void OnLanded(float impactSpeed)
    {
        float amount = Mathf.Clamp01(impactSpeed / 20f);
        squash = new Vector3(1f + 0.35f * amount, 1f - 0.3f * amount, 1f);

        Vector2 feet = (Vector2)transform.position + Vector2.down * 1f;
        Effects.Burst(feet, dustColor, Mathf.RoundToInt(4 + 8 * amount), 2f + 3f * amount, 0.1f, 0.8f);
    }

    // A quick stretch when the rope catches, plus sparks at the anchor.
    private void OnAttached(Vector2 anchor)
    {
        squash = new Vector3(0.85f, 1.15f, 1f);
        Effects.Burst(anchor, sparkColor, 8, 3f, 0.08f);
    }

    // Flash red, squash, throw out red particles, and shake the camera.
    private void OnHit(Vector2 direction)
    {
        flashTimer = 0.4f;
        squash = new Vector3(1.25f, 0.8f, 1f);
        Effects.Burst(transform.position, hitColor, 14, 5f, 0.12f);
        if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.3f, 0.25f);
    }
}
