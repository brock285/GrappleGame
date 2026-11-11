using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Darkens the screen to match the brightness setting (UC13). It goes on a
/// black Image that covers the whole screen. The Image has to be the first
/// child of the Canvas, so it's drawn under the menus. That way it darkens
/// the game but the menu text stays easy to read.
/// </summary>
[RequireComponent(typeof(Image))]
public class BrightnessOverlay : MonoBehaviour
{
    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();

        // The overlay covers the whole screen, so it must never catch clicks
        // meant for the buttons.
        image.raycastTarget = false;
    }

    private void OnEnable()
    {
        GameSettings.Changed += Apply;
        Apply();
    }

    private void OnDisable() => GameSettings.Changed -= Apply;

    // Full brightness (1) makes the overlay clear. Lower settings make it more solid, so the game looks darker.
    private void Apply()
    {
        image.color = new Color(0f, 0f, 0f, 1f - GameSettings.Brightness);
    }
}
