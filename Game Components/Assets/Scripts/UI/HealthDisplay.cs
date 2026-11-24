using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the player's hearts in the top left corner of the screen. A full
/// heart is red and a lost heart turns dark. When the player runs out of
/// hearts, it shows an "Out of health" message while GameManager waits to
/// restart the climb. UIBuilder creates the heart images and links them here.
/// </summary>
public class HealthDisplay : MonoBehaviour
{
    // One image per heart, in order from left to right.
    [SerializeField] private Image[] hearts = new Image[0];

    // The message that shows up when the player runs out of hearts.
    [SerializeField] private TMP_Text outOfHealthText;

    [SerializeField] private Color fullColor = new Color(0.95f, 0.25f, 0.3f);
    [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.25f, 0.8f);

    private PlayerHealth health;

    private void Start()
    {
        if (outOfHealthText != null) outOfHealthText.gameObject.SetActive(false);
        TryConnect();
    }

    private void Update()
    {
        // In case the player's health wasn't ready when Start ran.
        if (health == null) TryConnect();
    }

    private void OnDestroy()
    {
        if (health == null) return;
        health.Changed -= Refresh;
        health.Died -= ShowOutOfHealth;
    }

    private void TryConnect()
    {
        health = FindAnyObjectByType<PlayerHealth>();
        if (health == null) return;
        health.Changed += Refresh;
        health.Died += ShowOutOfHealth;
        Refresh(health.CurrentHealth);
    }

    // Colors each heart full or empty based on how many the player has left.
    private void Refresh(int current)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null) continue;
            hearts[i].color = i < current ? fullColor : emptyColor;

            // Hide any extra images if the player has fewer hearts than there are images.
            hearts[i].enabled = health == null || i < health.MaxHealth;
        }
    }

    private void ShowOutOfHealth()
    {
        if (outOfHealthText != null) outOfHealthText.gameObject.SetActive(true);
    }
}
