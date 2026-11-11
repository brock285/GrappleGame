using UnityEngine;

/// <summary>
/// Moves a platform back and forth between two points, stopping for a moment
/// at each end. A player standing on it rides along. The platform needs a
/// Rigidbody2D, and this script sets it to Kinematic so gravity and
/// collisions don't push it around.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    // The two ends of the path. They can be empty child objects of the platform.
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    // Units per second.
    [SerializeField] private float speed = 2f;

    // Seconds the platform waits at each end before turning around.
    [SerializeField] private float pauseAtEnds = 0.5f;

    private Rigidbody2D rb;
    private Vector2 a, b, target;
    private float waitTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Save the end points now. If they're children of the platform, they'd
        // move with it, and the platform would never reach them.
        a = pointA.position;
        b = pointB.position;
        target = b;
    }

    // Movement happens in FixedUpdate, in step with physics, so the player
    // standing on top moves smoothly with it.
    private void FixedUpdate()
    {
        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector2 next = Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime);
        rb.MovePosition(next);

        // When it reaches an end, wait, then head for the other end.
        if (Vector2.Distance(next, target) < 0.01f)
        {
            target = target == a ? b : a;
            waitTimer = pauseAtEnds;
        }
    }

    // While the player is on the platform, they become its child, so they
    // move with it. They're let go as soon as they step or jump off.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<PlayerController>() != null)
            collision.transform.SetParent(transform);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // Unity doesn't allow changing parents while the scene is unloading,
        // which is when the platform is inactive, so that case is skipped.
        if (collision.collider.GetComponent<PlayerController>() != null && gameObject.activeInHierarchy)
            collision.transform.SetParent(null);
    }

    // Draws the platform's path in the Scene view so it's easy to see while building levels.
    private void OnDrawGizmos()
    {
        if (pointA == null || pointB == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pointA.position, pointB.position);
    }
}
