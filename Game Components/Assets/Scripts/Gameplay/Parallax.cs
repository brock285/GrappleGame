using UnityEngine;

/// <summary>
/// Moves a background layer with the camera by a fraction of the camera's
/// movement, so distant layers seem far away. 1 moves fully with the camera
/// (infinitely far, like the sky); 0 doesn't move (part of the level).
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

    private void LateUpdate()
    {
        if (cam == null) return;
        Vector3 delta = cam.position - cameraStart;
        transform.position = new Vector3(
            startPosition.x + delta.x * followFactor,
            startPosition.y + delta.y * followFactor,
            startPosition.z);
    }
}
