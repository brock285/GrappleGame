using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves the player. On the ground, A and D (or the arrow keys) walk left and
/// right (R01, UC3). While hooked, the same keys push the player along the
/// swing so they can build momentum (UC5). In the air, the player keeps the
/// speed they got from a swing instead of slowing down right away.
///
/// Space jumps while the player is on the ground. A tap gives a short hop and
/// holding Space gives a full jump. Two small helpers make jumping feel fair.
/// The player can still jump for a split second after walking off a ledge,
/// and a press just before landing still counts once they touch down.
///
/// It also caps how fast the player can fall, makes ice slippery, and handles
/// getting knocked back by hazards (R07). The player needs a Rigidbody2D and
/// a child object named GroundCheck at their feet.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Ground movement")]
    // Top walking speed in units per second.
    [SerializeField] private float moveSpeed = 6f;

    // How fast the player gets up to speed or stops on normal ground.
    [SerializeField] private float groundAcceleration = 60f;

    [Header("Air and swing control")]
    // Steering in the air is weaker than on the ground, so swings feel committed.
    [SerializeField] private float airAcceleration = 18f;
    [Tooltip("Push along the swing arc while hooked. Higher values build momentum faster.")]
    [SerializeField] private float swingForce = 30f;
    [Tooltip("Fastest the player can fall, so long drops stay controllable.")]
    [SerializeField] private float maxFallSpeed = 22f;

    [Header("Jumping")]
    // How high a full jump goes, in units. The player is 2 units tall.
    [SerializeField] private float jumpHeight = 2.2f;

    [Tooltip("Seconds after walking off a ledge when a jump still works.")]
    [SerializeField] private float coyoteTime = 0.1f;

    [Tooltip("Seconds before landing when a jump press still counts.")]
    [SerializeField] private float jumpBufferTime = 0.12f;

    [Tooltip("How much upward speed is kept when Space is let go early. Lower makes taps shorter.")]
    [Range(0f, 1f)]
    [SerializeField] private float jumpCutMultiplier = 0.5f;

    [Tooltip("Seconds without control after a hazard hit, so the knockback carries the player back.")]
    [SerializeField] private float hitStunTime = 0.4f;

    [Header("Ground check")]
    // A small circle at the player's feet. If it overlaps the ground layer, the player is grounded.
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private GrapplingHook grapple;
    private float horizontalInput;  // -1, 0, or 1 from the keyboard this frame.
    private float lastFallSpeed;    // Used to tell how hard the player landed.
    private float groundGrip = 1f;  // 1 on normal ground, lower on ice.
    private float stunTimer;        // Seconds left before input works again after a hit.
    private float coyoteTimer;      // Seconds left when a jump still works after leaving the ground.
    private float jumpBufferTimer;  // Seconds left when a recent Space press can still turn into a jump.
    private float jumpLockTimer;    // Stops the ground check from cancelling a jump right after takeoff.
    private bool jumpCutRequested;  // True when Space was let go.
    private bool isJumping;         // True from a jump until the player lands, so only real jumps get cut short.

    /// <summary>True while the player is standing on the ground.</summary>
    public bool IsGrounded { get; private set; }

    /// <summary>1 when facing right, -1 when facing left.</summary>
    public int Facing { get; private set; } = 1;

    /// <summary>The player's current speed and direction. PlayerVisuals uses it to lean the sprite.</summary>
    public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;

    /// <summary>When false, the player ignores the keyboard. GameManager turns this off while paused and after the finish.</summary>
    public bool InputEnabled { get; set; } = true;

    /// <summary>Raised when the player lands after falling. It passes how fast they were falling.</summary>
    public event Action<float> Landed;

    /// <summary>Raised when a hazard knocks the player back. It passes the direction of the push.</summary>
    public event Action<Vector2> Hit;

    /// <summary>Raised when the player jumps. PlayerVisuals and AudioManager listen to it.</summary>
    public event Action Jumped;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        grapple = GetComponent<GrapplingHook>();
        AutoAssignReferences();
    }

    // Runs in the Editor when the component is added or reset.
    private void Reset() => AutoAssignReferences();

    /// <summary>
    /// Fills in any empty fields. It looks for a child named GroundCheck and
    /// uses the Ground layer. Anything already set in the Inspector stays the same.
    /// </summary>
    [ContextMenu("Auto-assign references")]
    private void AutoAssignReferences()
    {
        if (groundCheck == null) groundCheck = transform.Find("GroundCheck");
        if (groundLayer.value == 0) groundLayer = LayerMask.GetMask("Ground");

        // Only warn in Play mode. In the Editor, Reset can run before the child exists yet.
        if (groundCheck == null && Application.isPlaying)
            Debug.LogWarning($"{name}: add a child object named \"GroundCheck\" at the player's feet.", this);
    }

    // Input is read in Update, which runs every frame, so no key presses get
    // missed. The physics changes happen in FixedUpdate.
    private void Update()
    {
        if (stunTimer > 0f) stunTimer -= Time.deltaTime;
        bool canControl = InputEnabled && stunTimer <= 0f;
        horizontalInput = canControl ? ReadHorizontal() : 0f;
        if (horizontalInput != 0f) Facing = horizontalInput > 0f ? 1 : -1;

        // Remember a Space press for a moment, so pressing just before landing still jumps.
        Keyboard kb = Keyboard.current;
        if (jumpBufferTimer > 0f) jumpBufferTimer -= Time.deltaTime;
        if (canControl && kb != null && kb.spaceKey.wasPressedThisFrame) jumpBufferTimer = jumpBufferTime;
        if (kb != null && kb.spaceKey.wasReleasedThisFrame) jumpCutRequested = true;
    }

    /// <summary>Returns -1 for left (A or left arrow), 1 for right (D or right arrow), or 0 for neither or both.</summary>
    private static float ReadHorizontal()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return 0f;

        float value = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) value -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) value += 1f;
        return value;
    }

    // Runs once per physics step.
    private void FixedUpdate()
    {
        bool wasGrounded = IsGrounded;
        Collider2D ground = groundCheck != null
            ? Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer)
            : null;
        IsGrounded = ground != null;

        // Ice and other slippery surfaces lower the grip.
        groundGrip = ground != null && ground.TryGetComponent(out SlipperySurface slippery) ? slippery.Grip : 1f;

        if (IsGrounded && !wasGrounded && lastFallSpeed > 2f) Landed?.Invoke(lastFallSpeed);

        HandleJump();

        if (grapple != null && grapple.IsAttached)
            ApplySwing();
        else
            ApplyRunAndAirControl();

        // Cap the fall speed so long drops stay controllable. The player can still fall a long way (R08).
        Vector2 v = rb.linearVelocity;
        if (v.y < -maxFallSpeed) rb.linearVelocity = new Vector2(v.x, -maxFallSpeed);

        lastFallSpeed = Mathf.Max(0f, -rb.linearVelocity.y);
    }

    // Jumps if Space was pressed recently and the player is on the ground, or
    // just left it. Letting go of Space early cuts the jump short.
    private void HandleJump()
    {
        if (jumpLockTimer > 0f) jumpLockTimer -= Time.fixedDeltaTime;
        if (IsGrounded && jumpLockTimer <= 0f)
        {
            coyoteTimer = coyoteTime;
            isJumping = false;
        }
        else
        {
            coyoteTimer -= Time.fixedDeltaTime;
        }

        bool hooked = grapple != null && grapple.IsAttached;
        if (jumpBufferTimer > 0f && coyoteTimer > 0f && !hooked)
        {
            // The launch speed needed to reach jumpHeight under this body's gravity.
            float gravity = -Physics2D.gravity.y * rb.gravityScale;
            float launchSpeed = Mathf.Sqrt(2f * gravity * jumpHeight);
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, launchSpeed);

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            jumpLockTimer = 0.15f;
            jumpCutRequested = false;
            isJumping = true;
            Jumped?.Invoke();
        }
        else if (jumpCutRequested)
        {
            jumpCutRequested = false;
            Vector2 v = rb.linearVelocity;
            // Only cut a real jump, so letting go of Space never slows down a fling from a swing.
            if (isJumping && v.y > 0f && !hooked)
                rb.linearVelocity = new Vector2(v.x, v.y * jumpCutMultiplier);
            isJumping = false;
        }
    }

    private void ApplySwing()
    {
        if (horizontalInput == 0f) return;

        // Push along the arc, at a right angle to the rope. That builds swing
        // momentum, since pushing straight sideways would just pull against the rope.
        Vector2 toAnchor = grapple.AnchorPoint - rb.position;
        Vector2 tangent = new Vector2(toAnchor.y, -toAnchor.x).normalized;
        if (tangent.x < 0f) tangent = -tangent; // Pressing D always pushes to the right.

        rb.AddForce(tangent * horizontalInput * swingForce);
    }

    // Walking on the ground and steering in the air.
    private void ApplyRunAndAirControl()
    {
        float vx = rb.linearVelocity.x;
        float targetSpeed = horizontalInput * moveSpeed;

        if (!IsGrounded)
        {
            // In the air, keep the momentum from swings. Letting go of the keys
            // doesn't brake, and holding the same direction never slows down a
            // fling that's faster than walking speed.
            if (horizontalInput == 0f) return;
            if (Mathf.Sign(vx) == Mathf.Sign(horizontalInput) && Mathf.Abs(vx) > moveSpeed) return;
        }

        float accel = IsGrounded ? groundAcceleration * groundGrip : airAcceleration;
        float newX = Mathf.MoveTowards(vx, targetSpeed, accel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    /// <summary>
    /// Knocks the player back, for example when they touch spikes (R07). It
    /// lets go of the hook, replaces the player's speed with the push, and
    /// ignores input for a short stun so the player can't just walk through.
    /// </summary>
    /// <param name="force">The direction and strength of the push.</param>
    public void Knockback(Vector2 force)
    {
        if (grapple != null) grapple.Detach();
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(force, ForceMode2D.Impulse);
        stunTimer = hitStunTime;
        Hit?.Invoke(force.normalized);
    }

    // Shows the ground check circle in the Scene view when the player is selected.
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
