using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damageAmount = 20;
    public float damageCooldown = 2f;
    
    private float lastDamageTime;

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time - lastDamageTime > damageCooldown)
            {
                PlayerHealth ph = collision.gameObject.GetComponent<PlayerHealth>();
                if (ph != null)
                {
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
