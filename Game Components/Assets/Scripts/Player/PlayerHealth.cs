using System;
using UnityEngine;

/// <summary>
/// Keeps track of the player's health. The player starts each climb with 3
/// hearts, and every hit from a hazard costs one. After a hit, the player
/// can't lose another heart for a short time, so one touch of a spike strip
/// doesn't take all of them at once. When the last heart is gone, the Died
/// event tells GameManager to start the climb over.
///
/// Health only refills when the level restarts. There are no health pickups,
/// which keeps with the SRS's idea of no checkpoints and no easy way back.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    // How many hearts the player starts with.
    [SerializeField] private int maxHealth = 3;

    // Seconds after a hit when the player can't be hurt again.
    [SerializeField] private float invulnerableTime = 1f;

    private float invulnerableTimer;

    /// <summary>The most hearts the player can have.</summary>
    public int MaxHealth => maxHealth;

    /// <summary>How many hearts the player has right now.</summary>
    public int CurrentHealth { get; private set; }

    /// <summary>True once the player has run out of hearts.</summary>
    public bool IsDead => CurrentHealth <= 0;

    /// <summary>True for a short time after a hit, while the player can't lose another heart.</summary>
    public bool IsInvulnerable => invulnerableTimer > 0f;

    /// <summary>Raised whenever the player loses a heart. It passes how many hearts are left. HealthDisplay listens to it.</summary>
    public event Action<int> Changed;

    /// <summary>Raised once when the last heart is lost. GameManager and PlayerVisuals listen to it.</summary>
    public event Action Died;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void Update()
    {
        if (invulnerableTimer > 0f) invulnerableTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Takes away hearts, for example when a hazard hits the player (R07).
    /// It does nothing if the player was just hit or is already out of hearts.
    /// </summary>
    /// <param name="amount">How many hearts to take away.</param>
    /// <returns>True if the player actually lost health.</returns>
    public bool TakeDamage(int amount)
    {
        if (IsDead || IsInvulnerable || amount <= 0) return false;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        invulnerableTimer = invulnerableTime;
        Changed?.Invoke(CurrentHealth);

        if (CurrentHealth == 0) Died?.Invoke();
        return true;
    }
}
