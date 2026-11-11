using UnityEngine;

/// <summary>
/// Moves the camera with the player. It eases toward the player instead of
/// snapping, looks a little ahead in the direction they're moving so they can
/// see where they're going, and can shake for a moment when they get hit.
/// It goes on the Main Camera.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    /// <summary>The camera in the current scene. PlayerVisuals uses this to shake it.</summary>
    public static CameraFollow Instance { get; private set; }

    // What the camera follows. If it's empty, the camera finds the player in Start.
    [SerializeField] private Transform target;

    // Where the camera sits compared to the player. Two units up shows more of what's above.
    [SerializeField] private Vector2 offset = new Vector2(0f, 2f);

    // About how many seconds the camera takes to catch up. Lower is snappier.
    [SerializeField] private float smoothTime = 0.15f;

    [Tooltip("Seconds of movement to look ahead. 0 turns look-ahead off.")]
    [SerializeField] private float lookAheadTime = 0.25f;

    // The farthest the camera can look ahead, in units.
    [SerializeField] private float maxLookAhead = 3f;

    private Vector3 velocity;
    private Vector3 smoothedPosition;
    private Rigidbody2D targetBody;
    private float shakeTime;
    private float shakeDuration;
    private float shakeStrength;

    private void Awake() => Instance = this;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (target == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) target = player.transform;
        }
        if (target != null) targetBody = target.GetComponent<Rigidbody2D>();
        smoothedPosition = transform.position;
    }

    /// <summary>Shakes the camera, for example when the player gets hit.</summary>
    /// <param name="strength">How far the camera can move, in units.</param>
    /// <param name="duration">How long the shake lasts, in seconds.</param>
    public void Shake(float strength, float duration)
    {
        // If a shake is already going, keep whichever one is stronger.
        shakeStrength = Mathf.Max(shakeStrength, strength);
        shakeDuration = Mathf.Max(duration, 0.01f);
        shakeTime = shakeDuration;
    }

    // LateUpdate runs after the player has moved this frame, so the camera
    // doesn't lag one frame behind.
    private void LateUpdate()
    {
        if (target == null) return;

        // Look ahead in the direction of the player's velocity. Vertical
        // look-ahead is halved so fast falls don't swing the view too much.
        Vector2 lookAhead = Vector2.zero;
        if (targetBody != null && lookAheadTime > 0f)
            lookAhead = Vector2.ClampMagnitude(targetBody.linearVelocity * lookAheadTime, maxLookAhead);

        Vector3 goal = new Vector3(
            target.position.x + offset.x + lookAhead.x,
            target.position.y + offset.y + lookAhead.y * 0.5f,
            transform.position.z);
        smoothedPosition = Vector3.SmoothDamp(smoothedPosition, goal, ref velocity, smoothTime);

        // The shake is added on top of the smoothed position so it doesn't
        // throw off the follow. It fades out over its duration.
        Vector3 shake = Vector3.zero;
        if (shakeTime > 0f)
        {
            shakeTime -= Time.unscaledDeltaTime;
            float fade = Mathf.Clamp01(shakeTime / shakeDuration);
            shake = (Vector3)(Random.insideUnitCircle * shakeStrength * fade);
            if (shakeTime <= 0f) shakeStrength = 0f;
        }

        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, transform.position.z) + shake;
    }
}
