// PlayerHealth.cs
// Mengatur sistem kesehatan dan kematian player (Saipul)
// - Menangani damage cooldown, efek damage, suara, dan respawn lewat GameManager

using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxHealth = 100;
    private int currentHealth;
    public float damageCooldown = 1.5f;
    private bool isDamaged = false;

    public DamageEffect damageEffect; // Referensi ke skrip efek layar
    public FadeTransition fadeTransition; // optional for dramatic death
    public AudioClip hurtSound;
    public AudioClip deathSound;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (isDamaged) return; // mencegah spam damage
        isDamaged = true;

        currentHealth -= amount;
        Debug.Log($"Player terkena serangan! Sisa HP: {currentHealth}");

        if (damageEffect != null)
            damageEffect.ShowDamageEffect();

        if (hurtSound != null)
            AudioSource.PlayClipAtPoint(hurtSound, transform.position);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            Invoke(nameof(ResetDamageState), damageCooldown);
        }
    }

    void Die()
    {
        if (deathSound != null)
            AudioSource.PlayClipAtPoint(deathSound, transform.position);

        // Opsional: bermain fade out sebelum respawn
        if (fadeTransition != null)
            fadeTransition.FadeOutAndIn();

        // Panggil respawn melalui GameManager
        GameManager.Instance?.RespawnPlayer(gameObject);

        // Reset HP
        currentHealth = maxHealth;
        isDamaged = false;
        Debug.Log("Player respawn di checkpoint terakhir");
    }

    void ResetDamageState()
    {
        isDamaged = false;
    }

    public void RestoreFullHealth()
    {
        currentHealth = maxHealth;
    }

    public int GetCurrentHealth() => currentHealth;
}
