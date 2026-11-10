using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Darkens the screen based on the brightness setting (UC13). Put it on a
/// full-screen black Image that's the first child of the Canvas, so it darkens
/// the game but not the menus drawn on top of it.
/// </summary>
[RequireComponent(typeof(Image))]
public class BrightnessOverlay : MonoBehaviour
{
    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
        image.raycastTarget = false; // Never block clicks on buttons.
    }

    private void OnEnable()
    {
        GameSettings.Changed += Apply;
        Apply();
    }

    private void OnDisable() => GameSettings.Changed -= Apply;

    private void Apply()
    {
        image.color = new Color(0f, 0f, 0f, 1f - GameSettings.Brightness);
    }
}
