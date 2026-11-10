using UnityEngine;

/// <summary>
/// Smoothly follows the player, looks ahead in the direction they're moving,
/// and can shake briefly for impacts. Attach to the Main Camera.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = new Vector2(0f, 2f);
    [SerializeField] private float smoothTime = 0.15f;
    [Tooltip("Seconds of movement to look ahead. 0 turns look-ahead off.")]
    [SerializeField] private float lookAheadTime = 0.25f;
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
    public void Shake(float strength, float duration)
    {
        shakeStrength = Mathf.Max(shakeStrength, strength);
        shakeDuration = Mathf.Max(duration, 0.01f);
        shakeTime = shakeDuration;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector2 lookAhead = Vector2.zero;
        if (targetBody != null && lookAheadTime > 0f)
            lookAhead = Vector2.ClampMagnitude(targetBody.linearVelocity * lookAheadTime, maxLookAhead);

        Vector3 goal = new Vector3(
            target.position.x + offset.x + lookAhead.x,
            target.position.y + offset.y + lookAhead.y * 0.5f,
            transform.position.z);
        smoothedPosition = Vector3.SmoothDamp(smoothedPosition, goal, ref velocity, smoothTime);

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
