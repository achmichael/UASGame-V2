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

    // State flags
    public bool IsDead { get; private set; }
    public bool IsInvulnerable { get; private set; }
    public float respawnInvulnerabilityTime = 3f; // Durasi kebal setelah respawn

    public DamageEffect damageEffect; // Referensi ke skrip efek layar
    public FadeTransition fadeTransition; // optional for dramatic death
    public AudioClip hurtSound;
    public AudioClip deathSound;
    
    [Header("UI")]
    public HealthIndicator healthIndicator; // Referensi ke health indicator UI (auto-assigned)

    private void Start()
    {
        currentHealth = maxHealth;
        IsDead = false;
        IsInvulnerable = false;
        
        // Auto-assign HealthIndicator jika belum di-set
        if (healthIndicator == null)
        {
            healthIndicator = FindObjectOfType<HealthIndicator>();
            
            if (healthIndicator != null)
            {
                Debug.Log("[PlayerHealth] HealthIndicator auto-assigned successfully!");
            }
            else
            {
                Debug.LogWarning("[PlayerHealth] HealthIndicator tidak ditemukan di scene. Pastikan ada GameObject dengan HealthIndicator script.");
            }
        }
        
        // Update health indicator di awal game
        UpdateHealthUI();
    }

    public void TakeDamage(int amount)
    {
        // Cek kondisi yang mencegah damage: Mati, Kebal, atau sedang Cooldown
        if (IsDead || IsInvulnerable || isDamaged) return; 
        
        isDamaged = true;
        currentHealth -= amount;
        Debug.Log($"Player terkena serangan! Sisa HP: {currentHealth}");

        // Update health UI setelah damage
        UpdateHealthUI();

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
        if (IsDead) return; // Mencegah double death trigger
        IsDead = true;

        if (deathSound != null)
            AudioSource.PlayClipAtPoint(deathSound, transform.position);

        // Opsional: bermain fade out sebelum respawn
        if (fadeTransition != null)
            fadeTransition.FadeOutAndIn();

        Debug.Log("Player Mati. Meminta Respawn ke GameManager.");
        
        // Panggil respawn melalui GameManager
        GameManager.Instance?.RespawnPlayer(gameObject);
    }

    void ResetDamageState()
    {
        isDamaged = false;
    }

    /// <summary>
    /// Dipanggil oleh GameManager setelah posisi player di-reset
    /// </summary>
    public void OnRespawn()
    {
        IsDead = false;
        currentHealth = maxHealth;
        isDamaged = false;
        
        // Mulai invulnerability
        StartCoroutine(InvulnerabilityRoutine());
        
        // Update health UI setelah restore
        UpdateHealthUI();
        Debug.Log("Player Health Reset & Invulnerable started.");
    }

    private System.Collections.IEnumerator InvulnerabilityRoutine()
    {
        IsInvulnerable = true;
        Debug.Log("Player Invulnerable...");
        yield return new WaitForSeconds(respawnInvulnerabilityTime);
        IsInvulnerable = false;
        Debug.Log("Player Vulnerable again.");
    }

    public void RestoreFullHealth()
    {
        currentHealth = maxHealth;
        
        // Update health UI setelah restore
        UpdateHealthUI();
    }

    public int GetCurrentHealth() => currentHealth;
    
    /// <summary>
    /// Update health indicator UI berdasarkan playerLives dari GameManager
    /// </summary>
    private void UpdateHealthUI()
    {
        if (healthIndicator != null && GameManager.Instance != null)
        {
            // Update health icon berdasarkan playerLives dari GameManager
            healthIndicator.UpdateHealth(GameManager.Instance.playerLives);
        }
    }
}
