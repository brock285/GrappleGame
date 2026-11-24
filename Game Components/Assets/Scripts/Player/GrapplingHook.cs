using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The grappling hook, which is the game's main way to climb. The player
/// presses the left mouse button to fire the hook toward the cursor (R04, UC4)
/// and lets go of the button to release it. While hooked, they can swing
/// with A and D (UC5, handled in PlayerController) and hold W to reel in or
/// S to let out more rope (UC6). Space is the jump button, so it doesn't reel.
///
/// Only objects on the Grappleable layer can be hooked, so the hook doesn't
/// work on every wall (UC4). Objects on the Ground layer block the hook, so
/// the player can't grab a point through a wall. A small aim assist lets near
/// misses still count.
///
/// The hook has a max distance of 12 units, for both firing and letting out
/// rope. The player can also only hang for a limited time (5 seconds by
/// default). The rope turns red and blinks near the end of that time, then it
/// gives out and the player falls, so nobody can rest on one grapple point forever.
///
/// The rope breaks if a piece of the level, like a wall, a ledge, or a moving
/// platform, ends up between the player and the anchor. That stops the player
/// from swinging the rope through solid ground, and it means a moving
/// platform can cut the rope.
///
/// The rope is a DistanceJoint2D. It keeps the player from getting farther
/// than the rope length from the anchor, but it can go slack, so it acts like
/// a real rope instead of a stiff pole.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(DistanceJoint2D))]
public class GrapplingHook : MonoBehaviour
{
    [Header("Targeting")]
    [Tooltip("The max distance of the hook, in units. It's the farthest the hook can reach and the longest the rope can get.")]
    [SerializeField] private float maxRange = 12f;
    [SerializeField] private LayerMask grappleableLayers;
    [Tooltip("Layers that block the hook, such as walls and floors.")]
    [SerializeField] private LayerMask blockingLayers;
    [Tooltip("How far a near miss can be from a grapple point and still hook it.")]
    [SerializeField] private float aimAssistRadius = 0.6f;

    [Header("Rope")]
    // How fast W and S change the rope length, in units per second.
    [SerializeField] private float reelSpeed = 9f;

    // Reeling in stops at this length so the player doesn't end up inside the grapple point.
    [SerializeField] private float minRopeLength = 1f;

    // Draws the rope as a line from the player to the anchor.
    [SerializeField] private LineRenderer rope;

    [Tooltip("Seconds something has to stay in the way before the rope breaks. A tiny delay stops it from breaking on a single glitchy physics step.")]
    [SerializeField] private float breakDelay = 0.04f;

    // Seconds the rope has been blocked so far.
    private float blockedTime;

    [Header("Hang limit")]
    [Tooltip("Seconds the player can stay hooked before the rope gives out.")]
    [SerializeField] private float hangLimit = 5f;

    [Tooltip("Seconds before the limit when the rope starts turning red and blinking as a warning.")]
    [SerializeField] private float hangWarningTime = 1.5f;

    [SerializeField] private Color hangWarningColor = new Color(1f, 0.25f, 0.2f);

    // Seconds the player has been hooked this time.
    private float hangTimer;

    // The rope's normal colors, saved so they can come back after the warning.
    private Color ropeStartColor, ropeEndColor;

    private DistanceJoint2D joint;
    private Camera cam;

    /// <summary>True while the hook is attached to something.</summary>
    public bool IsAttached => joint != null && joint.enabled;

    /// <summary>The point in the world the rope is attached to.</summary>
    public Vector2 AnchorPoint { get; private set; }

    /// <summary>Seconds left before the rope gives out, or 0 when not hooked.</summary>
    public float HangTimeLeft => IsAttached ? Mathf.Max(0f, hangLimit - hangTimer) : 0f;

    /// <summary>The farthest the hook can reach, in units.</summary>
    public float MaxRange => maxRange;

    /// <summary>When false, the hook ignores the mouse and keys. GameManager turns this off while paused and after the finish.</summary>
    public bool InputEnabled { get; set; } = true;

    /// <summary>Raised when the hook attaches. It passes the anchor point. PlayerVisuals and AudioManager listen to it.</summary>
    public event Action<Vector2> Attached;

    /// <summary>Raised when something in the level cuts the rope. It passes the point where it broke.</summary>
    public event Action<Vector2> RopeBroken;

    /// <summary>Raised when the hook lets go, for any reason.</summary>
    public event Action Detached;

    private void Awake()
    {
        joint = GetComponent<DistanceJoint2D>();
        joint.enableCollision = true;
        joint.autoConfigureDistance = false;
        joint.maxDistanceOnly = true; // The rope can go slack but never stretch.
        joint.enabled = false;

        cam = Camera.main;
        AutoAssignReferences();

        if (rope != null)
        {
            rope.positionCount = 2;
            rope.enabled = false;
            ropeStartColor = rope.startColor;
            ropeEndColor = rope.endColor;
        }
    }

    // Runs in the Editor when the component is added or reset.
    private void Reset() => AutoAssignReferences();

    /// <summary>
    /// Fills in any empty fields. It uses the first LineRenderer under the
    /// player for the rope, the Grappleable layer for targets, and the Ground
    /// layer for walls. Anything already set in the Inspector stays the same.
    /// </summary>
    [ContextMenu("Auto-assign references")]
    private void AutoAssignReferences()
    {
        if (rope == null) rope = GetComponentInChildren<LineRenderer>();
        if (grappleableLayers.value == 0) grappleableLayers = LayerMask.GetMask("Grappleable");
        if (blockingLayers.value == 0) blockingLayers = LayerMask.GetMask("Ground");

        if (grappleableLayers.value == 0 && Application.isPlaying)
            Debug.LogWarning($"{name}: add a layer named \"Grappleable\" in Tags and Layers.", this);
    }

    private void Update()
    {
        if (!InputEnabled) return;

        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame) TryFire(mouse.position.ReadValue());
        if (mouse.leftButton.wasReleasedThisFrame) Detach();

        if (IsAttached) HandleReel();
    }

    // Checks every physics step whether a piece of the level is in the way of
    // the rope, and breaks the rope if it stays there for breakDelay seconds.
    private void FixedUpdate()
    {
        if (!IsAttached)
        {
            blockedTime = 0f;
            return;
        }

        // The hang limit. Physics time stops while paused, so pausing doesn't use it up.
        hangTimer += Time.fixedDeltaTime;
        if (hangTimer >= hangLimit)
        {
            BreakRope(AnchorPoint);
            return;
        }

        Vector2 from = transform.position;
        Vector2 toAnchor = AnchorPoint - from;
        float distance = toAnchor.magnitude;
        if (distance < 0.3f) return;

        // Stop just short of the anchor, so whatever the hook is stuck to doesn't count.
        RaycastHit2D hit = Physics2D.Raycast(from, toAnchor / distance, distance - 0.15f, blockingLayers);
        if (hit.collider == null)
        {
            blockedTime = 0f;
            return;
        }

        blockedTime += Time.fixedDeltaTime;
        if (blockedTime >= breakDelay) BreakRope(hit.point);
    }

    // Lets go of the hook because something cut the rope.
    private void BreakRope(Vector2 point)
    {
        blockedTime = 0f;
        Detach();
        RopeBroken?.Invoke(point);
    }

    // Moves the ends of the rope line after the player has moved this frame.
    private void LateUpdate()
    {
        if (rope == null || !IsAttached) return;
        rope.SetPosition(0, transform.position);
        rope.SetPosition(1, AnchorPoint);

        // Warn the player as the hang limit gets close. The rope fades to red
        // and blinks faster and faster until it gives out.
        float left = hangLimit - hangTimer;
        if (left < hangWarningTime)
        {
            float t = 1f - left / hangWarningTime;
            bool blinkOn = Mathf.Repeat(Time.time * (4f + 8f * t), 1f) < 0.5f;
            Color warn = blinkOn ? hangWarningColor : Color.Lerp(ropeStartColor, hangWarningColor, t);
            rope.startColor = warn;
            rope.endColor = warn;
        }
        else
        {
            rope.startColor = ropeStartColor;
            rope.endColor = ropeEndColor;
        }
    }

    /// <summary>
    /// Works out where the hook would land if it were fired toward a spot on
    /// the screen. Firing uses this, and so does AimIndicator to place its marker.
    /// </summary>
    /// <param name="screenPosition">The mouse position in screen pixels.</param>
    /// <param name="point">Where the hook would attach, if it can.</param>
    /// <returns>False if no grapple point is in range and in sight.</returns>
    public bool TryFindTarget(Vector2 screenPosition, out Vector2 point)
    {
        point = default;
        if (cam == null) cam = Camera.main;
        if (cam == null) return false;

        Vector2 origin = transform.position;
        Vector2 mouseWorld = cam.ScreenToWorldPoint(screenPosition);
        Vector2 direction = (mouseWorld - origin).normalized;
        int mask = grappleableLayers | blockingLayers;

        // Try a thin ray along the exact aim first. If that misses, try a
        // wider circle so near misses still count. Either way, the first thing
        // hit has to be a grapple point, so a wall in front of one blocks it.
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, maxRange, mask);
        if (!IsGrappleable(hit) && aimAssistRadius > 0f)
            hit = Physics2D.CircleCast(origin, aimAssistRadius, direction, maxRange, mask);

        if (!IsGrappleable(hit)) return false;
        point = hit.point;
        return true;
    }

    // True if the ray hit something on one of the grappleable layers.
    private bool IsGrappleable(RaycastHit2D hit) =>
        hit.collider != null && (grappleableLayers.value & (1 << hit.collider.gameObject.layer)) != 0;

    // Attaches the rope if there's a target. Nothing happens on a miss (the
    // alternate flow in the UC4 template).
    private void TryFire(Vector2 mouseScreenPosition)
    {
        if (!TryFindTarget(mouseScreenPosition, out Vector2 point)) return;

        AnchorPoint = point;
        joint.connectedAnchor = point; // No connected body, so this is a world position.
        joint.distance = Vector2.Distance(transform.position, point); // Start with the rope exactly as long as the gap.
        hangTimer = 0f; // Each new grab gets the full hang time.
        joint.enabled = true;

        if (rope != null) rope.enabled = true;
        Attached?.Invoke(point);
    }

    // W shortens the rope and S lengthens it, up to the hook's range (UC6).
    private void HandleReel()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        float change = 0f;
        if (kb.wKey.isPressed) change -= reelSpeed;
        if (kb.sKey.isPressed) change += reelSpeed;

        joint.distance = Mathf.Clamp(joint.distance + change * Time.deltaTime, minRopeLength, maxRange);
    }

    /// <summary>
    /// Lets go of the hook. It runs when the player releases the mouse button,
    /// gets hit, pauses, or finishes. Calling it when the hook isn't attached
    /// does nothing.
    /// </summary>
    public void Detach()
    {
        if (!IsAttached) return;
        joint.enabled = false;
        if (rope != null) rope.enabled = false;
        Detached?.Invoke();
    }

    // Shows the hook's range as a circle in the Scene view when the player is selected.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxRange);
    }
}
