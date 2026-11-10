using UnityEngine;

/// <summary>
/// Moves a platform back and forth between two points. The player rides along
/// while standing on it. Give the platform a Rigidbody2D set to Kinematic.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float pauseAtEnds = 0.5f;

    private Rigidbody2D rb;
    private Vector2 a, b, target;
    private float waitTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        // Cache the end points so they stay fixed even if they're children of the platform.
        a = pointA.position;
        b = pointB.position;
        target = b;
    }

    private void FixedUpdate()
    {
        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector2 next = Vector2.MoveTowards(rb.position, target, speed * Time.fixedDeltaTime);
        rb.MovePosition(next);

        if (Vector2.Distance(next, target) < 0.01f)
        {
            target = target == a ? b : a;
            waitTimer = pauseAtEnds;
        }
    }

    // Parent the player to the platform so it carries them.
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<PlayerController>() != null)
            collision.transform.SetParent(transform);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<PlayerController>() != null && gameObject.activeInHierarchy)
            collision.transform.SetParent(null);
    }

    private void OnDrawGizmos()
    {
        if (pointA == null || pointB == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pointA.position, pointB.position);
    }
}
