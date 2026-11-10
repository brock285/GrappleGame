using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles horizontal player movement (R01, UC3) and swing control while
/// attached to the grappling hook (UC5).
/// Requires a Rigidbody2D and a child transform used as a ground check.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Ground movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float groundAcceleration = 60f;

    [Header("Air and swing control")]
    [SerializeField] private float airAcceleration = 18f;
    [Tooltip("Push along the swing arc while hooked. Higher values build momentum faster.")]
    [SerializeField] private float swingForce = 30f;
    [Tooltip("Fastest the player can fall, so long drops stay controllable.")]
    [SerializeField] private float maxFallSpeed = 22f;

    [Tooltip("Seconds without control after a hazard hit, so the knockback carries the player back.")]
    [SerializeField] private float hitStunTime = 0.4f;

    [Header("Ground check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private GrapplingHook grapple;
    private float horizontalInput;
    private float lastFallSpeed;
    private float groundGrip = 1f;
    private float stunTimer;

    public bool IsGrounded { get; private set; }

    /// <summary>1 when facing right, -1 when facing left.</summary>
    public int Facing { get; private set; } = 1;

    public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;

    /// <summary>When false, the player ignores input (used by pause and victory screens).</summary>
    public bool InputEnabled { get; set; } = true;

    /// <summary>Raised when the player lands. The argument is the downward speed at impact.</summary>
    public event Action<float> Landed;

    /// <summary>Raised when a hazard knocks the player back. The argument is the push direction.</summary>
    public event Action<Vector2> Hit;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        grapple = GetComponent<GrapplingHook>();
        AutoAssignReferences();
    }

    // Runs in the Editor when the component is added or reset.
    private void Reset() => AutoAssignReferences();

    /// <summary>
    /// Fills in empty fields: a child named "GroundCheck" and the "Ground" layer.
    /// Values set in the Inspector are left alone.
    /// </summary>
    [ContextMenu("Auto-assign references")]
    private void AutoAssignReferences()
    {
        if (groundCheck == null) groundCheck = transform.Find("GroundCheck");
        if (groundLayer.value == 0) groundLayer = LayerMask.GetMask("Ground");

        // Only warn in Play mode: in the Editor, Reset can run before the child is added.
        if (groundCheck == null && Application.isPlaying)
            Debug.LogWarning($"{name}: add a child object named \"GroundCheck\" at the player's feet.", this);
    }

    private void Update()
    {
        if (stunTimer > 0f) stunTimer -= Time.deltaTime;
        horizontalInput = InputEnabled && stunTimer <= 0f ? ReadHorizontal() : 0f;
        if (horizontalInput != 0f) Facing = horizontalInput > 0f ? 1 : -1;
    }

    /// <summary>Returns -1 for left (A or left arrow), 1 for right (D or right arrow), or 0.</summary>
    private static float ReadHorizontal()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return 0f;

        float value = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) value -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) value += 1f;
        return value;
    }

    private void FixedUpdate()
    {
        bool wasGrounded = IsGrounded;
        Collider2D ground = groundCheck != null
            ? Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer)
            : null;
        IsGrounded = ground != null;

        // Ice and other slippery surfaces reduce grip.
        groundGrip = ground != null && ground.TryGetComponent(out SlipperySurface slippery) ? slippery.Grip : 1f;

        if (IsGrounded && !wasGrounded && lastFallSpeed > 2f) Landed?.Invoke(lastFallSpeed);

        if (grapple != null && grapple.IsAttached)
            ApplySwing();
        else
            ApplyRunAndAirControl();

        // Cap fall speed.
        Vector2 v = rb.linearVelocity;
        if (v.y < -maxFallSpeed) rb.linearVelocity = new Vector2(v.x, -maxFallSpeed);

        lastFallSpeed = Mathf.Max(0f, -rb.linearVelocity.y);
    }

    private void ApplySwing()
    {
        if (horizontalInput == 0f) return;

        // Push along the arc (perpendicular to the rope), so input builds swing
        // momentum instead of fighting the rope.
        Vector2 toAnchor = grapple.AnchorPoint - rb.position;
        Vector2 tangent = new Vector2(toAnchor.y, -toAnchor.x).normalized;
        if (tangent.x < 0f) tangent = -tangent; // Positive input always pushes right.

        rb.AddForce(tangent * horizontalInput * swingForce);
    }

    private void ApplyRunAndAirControl()
    {
        float vx = rb.linearVelocity.x;
        float targetSpeed = horizontalInput * moveSpeed;

        if (!IsGrounded)
        {
            // In the air, keep momentum from swings: no input means no braking,
            // and holding the same direction never slows a fast fling down.
            if (horizontalInput == 0f) return;
            if (Mathf.Sign(vx) == Mathf.Sign(horizontalInput) && Mathf.Abs(vx) > moveSpeed) return;
        }

        float accel = IsGrounded ? groundAcceleration * groundGrip : airAcceleration;
        float newX = Mathf.MoveTowards(vx, targetSpeed, accel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    /// <summary>Applies an instant push, for example from a hazard (R07).</summary>
    public void Knockback(Vector2 force)
    {
        if (grapple != null) grapple.Detach();
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(force, ForceMode2D.Impulse);
        stunTimer = hitStunTime;
        Hit?.Invoke(force.normalized);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
