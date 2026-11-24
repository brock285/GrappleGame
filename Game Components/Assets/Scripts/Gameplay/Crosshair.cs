using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The aiming crosshair that replaces the mouse pointer while the player is
/// climbing. It follows the mouse, and it turns orange when the hook would
/// catch something if the player fired right now, so they don't have to guess.
/// GameManager hides the normal mouse pointer during play, and this
/// crosshair hides itself whenever a menu is open (paused, the intro screen,
/// the victory screen, or out of health), when the normal pointer comes back.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Crosshair : MonoBehaviour
{
    [SerializeField] private GrapplingHook grapple;

    // The color when nothing can be hooked where the player is aiming.
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.85f);

    // The color when firing now would hook a grapple point.
    [SerializeField] private Color readyColor = new Color(1f, 0.72f, 0.25f, 1f);

    private SpriteRenderer sr;
    private Camera cam;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        cam = Camera.main;
        if (grapple == null) grapple = FindAnyObjectByType<GrapplingHook>();
    }

    // LateUpdate runs after the camera moves, so the crosshair stays exactly on the mouse.
    private void LateUpdate()
    {
        Mouse mouse = Mouse.current;
        GameManager gm = GameManager.Instance;
        bool show = mouse != null && cam != null && gm != null && gm.IsPlaying;
        sr.enabled = show;
        if (!show) return;

        Vector2 screen = mouse.position.ReadValue();
        Vector3 world = cam.ScreenToWorldPoint(screen);
        transform.position = new Vector3(world.x, world.y, 0f);

        // Orange when the hook can reach something along this aim and isn't already attached.
        bool ready = grapple != null && !grapple.IsAttached && grapple.TryFindTarget(screen, out _);
        sr.color = ready ? readyColor : idleColor;
    }
}
