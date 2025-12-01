using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damageAmount = 20;
    public float damageCooldown = 2f;
    
    private float lastDamageTime;

    private GhostAI ghostAI;

    private void Start()
    {
        ghostAI = GetComponent<GhostAI>();
    }

    private void OnCollisionStay(Collision collision)
    {
        // FIX: Jika script GhostAI ada, matikan damage dari collision (touch damage).
        // Biarkan GhostAI yang menangani damage saat state 'Attack' aktif.
        // Ini mencegah HP berkurang saat enemy hanya berjalan menabrak player.
        if (ghostAI != null) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time - lastDamageTime > damageCooldown)
            {
                PlayerHealth ph = collision.gameObject.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    // Jangan damage jika player sudah mati atau invulnerable
                    if (ph.IsDead || ph.IsInvulnerable) return;

                    ph.TakeDamage(damageAmount);
                    lastDamageTime = Time.time;
                    Debug.Log($"Ghost damaged player for {damageAmount} HP!");
                }
                else
                {
                    Debug.LogWarning("PlayerHealth component tidak ditemukan di Player!");
                }
            }
        }
    }
}
