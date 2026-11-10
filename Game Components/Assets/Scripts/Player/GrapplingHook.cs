using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Fires a grappling hook toward the mouse cursor (R04, UC4), lets the player
/// swing from the anchor (UC5), and reels the player in or out (UC6).
///
/// Controls:
///   Left mouse button (press)   - fire the hook
///   Left mouse button (release) - let go
///   W / Space (hold)            - reel in
///   S (hold)                    - let out rope
///
/// Only surfaces on the "grappleable" layers can be hooked, and walls on the
/// "Ground" layer block the line of sight. A small aim assist forgives near misses.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(DistanceJoint2D))]
public class GrapplingHook : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private float maxRange = 12f;
    [SerializeField] private LayerMask grappleableLayers;
    [Tooltip("Layers that block the hook, such as walls and floors.")]
    [SerializeField] private LayerMask blockingLayers;
    [Tooltip("How far a near miss can be from a grapple point and still hook it.")]
    [SerializeField] private float aimAssistRadius = 0.6f;

    [Header("Rope")]
    [SerializeField] private float reelSpeed = 9f;
    [SerializeField] private float minRopeLength = 1f;
    [SerializeField] private LineRenderer rope;

    private DistanceJoint2D joint;
    private Camera cam;

    public bool IsAttached => joint != null && joint.enabled;

    /// <summary>World position the rope is attached to.</summary>
    public Vector2 AnchorPoint { get; private set; }

    public float MaxRange => maxRange;

    /// <summary>When false, the hook ignores input (used by pause and victory screens).</summary>
    public bool InputEnabled { get; set; } = true;

    /// <summary>Raised when the hook attaches. The argument is the anchor point.</summary>
    public event Action<Vector2> Attached;

    /// <summary>Raised when the hook lets go.</summary>
    public event Action Detached;

    private void Awake()
    {
        joint = GetComponent<DistanceJoint2D>();
        joint.enableCollision = true;
        joint.autoConfigureDistance = false;
        joint.maxDistanceOnly = true; // Rope can go slack but never stretch.
        joint.enabled = false;

        cam = Camera.main;
        AutoAssignReferences();

        if (rope != null)
        {
            rope.positionCount = 2;
            rope.enabled = false;
        }
    }

    // Runs in the Editor when the component is added or reset.
    private void Reset() => AutoAssignReferences();

    /// <summary>
    /// Fills in empty fields: the first child LineRenderer, the "Grappleable"
    /// layer, and the "Ground" layer as a blocker. Values set in the Inspector are left alone.
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

    private void LateUpdate()
    {
        if (rope == null || !IsAttached) return;
        rope.SetPosition(0, transform.position);
        rope.SetPosition(1, AnchorPoint);
    }

    /// <summary>
    /// Finds where the hook would land if fired toward a screen position.
    /// Returns false if nothing grappleable is in range and in sight.
    /// </summary>
    public bool TryFindTarget(Vector2 screenPosition, out Vector2 point)
    {
        point = default;
        if (cam == null) cam = Camera.main;
        if (cam == null) return false;

        Vector2 origin = transform.position;
        Vector2 mouseWorld = cam.ScreenToWorldPoint(screenPosition);
        Vector2 direction = (mouseWorld - origin).normalized;
        int mask = grappleableLayers | blockingLayers;

        // Exact aim first, then a wider cast for near misses. Either way the
        // first thing hit must be grappleable, so walls block the hook.
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, maxRange, mask);
        if (!IsGrappleable(hit) && aimAssistRadius > 0f)
            hit = Physics2D.CircleCast(origin, aimAssistRadius, direction, maxRange, mask);

        if (!IsGrappleable(hit)) return false;
        point = hit.point;
        return true;
    }

    private bool IsGrappleable(RaycastHit2D hit) =>
        hit.collider != null && (grappleableLayers.value & (1 << hit.collider.gameObject.layer)) != 0;

    private void TryFire(Vector2 mouseScreenPosition)
    {
        if (!TryFindTarget(mouseScreenPosition, out Vector2 point)) return;

        AnchorPoint = point;
        joint.connectedAnchor = point; // No connected body, so this is a world position.
        joint.distance = Vector2.Distance(transform.position, point);
        joint.enabled = true;

        if (rope != null) rope.enabled = true;
        Attached?.Invoke(point);
    }

    private void HandleReel()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        float change = 0f;
        if (kb.wKey.isPressed || kb.spaceKey.isPressed) change -= reelSpeed;
        if (kb.sKey.isPressed) change += reelSpeed;

        joint.distance = Mathf.Clamp(joint.distance + change * Time.deltaTime, minRopeLength, maxRange);
    }

    /// <summary>Releases the hook. Safe to call when not attached.</summary>
    public void Detach()
    {
        if (!IsAttached) return;
        joint.enabled = false;
        if (rope != null) rope.enabled = false;
        Detached?.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxRange);
    }
}
