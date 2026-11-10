using UnityEngine;

/// <summary>
/// Marks a surface as slippery, like ice. While standing on it, the player
/// speeds up and slows down much more gradually, so they slide.
/// Add it to any ground object with a collider.
/// </summary>
public class SlipperySurface : MonoBehaviour
{
    [Tooltip("How much grip the player keeps: 1 is normal ground, 0 is no control at all.")]
    [Range(0f, 1f)]
    [SerializeField] private float grip = 0.05f;

    public float Grip => grip;
}
