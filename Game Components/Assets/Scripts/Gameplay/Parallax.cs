using UnityEngine;

/// <summary>
/// Moves a background layer along with the camera, but only part of the way,
/// so layers that move less look closer and layers that move more look farther
/// away. A follow factor of 1 moves with the camera completely, like a sky that
/// never changes. A factor of 0 doesn't move at all, like part of the level.
/// </summary>
public class Parallax : MonoBehaviour
{
    [Range(0f, 1f)]
    [SerializeField] private float followFactor = 0.8f;

    private Transform cam;
    private Vector3 startPosition;
    private Vector3 cameraStart;

    private void Start()
    {
        if (Camera.main == null) return;
        cam = Camera.main.transform;
        startPosition = transform.position;
        cameraStart = cam.position;
    }

    // Runs after the camera moves, so the background doesn't jitter.
    private void LateUpdate()
    {
        if (cam == null) return;

        // Move the layer by a fraction of however far the camera has moved since the start.
        Vector3 delta = cam.position - cameraStart;
        transform.position = new Vector3(
            startPosition.x + delta.x * followFactor,
            startPosition.y + delta.y * followFactor,
            startPosition.z);
    }
}
