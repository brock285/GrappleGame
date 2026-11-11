using UnityEngine;

/// <summary>
/// Marks a surface as slippery, like ice. While the player stands on it, they
/// speed up and slow down much more slowly, so they slide. It goes on any
/// ground object that has a collider. PlayerController reads the Grip value
/// each physics step.
/// </summary>
public class SlipperySurface : MonoBehaviour
{
    [Tooltip("How much grip the player keeps. 1 is normal ground and 0 is no control at all.")]
    [Range(0f, 1f)]
    [SerializeField] private float grip = 0.05f;

    /// <summary>How much of the normal acceleration the player keeps on this surface, from 0 to 1.</summary>
    public float Grip => grip;
}
