using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Death Settings")]
    public float destroyDelay = 2f;
    public GameObject deathEffect; // Optional: Particle effect saat mati

    private Animator anim;
    private GhostAI ghostAI;
    private Collider ghostCollider;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        ghostAI = GetComponent<GhostAI>();
        ghostCollider = GetComponent<Collider>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"Ghost took {damage} damage. Current Health: {currentHealth}");

        // Trigger hit animation if available
        if (anim != null)
        {
            anim.SetTrigger("Hit");
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Ghost died!");

        // Disable AI
        if (ghostAI != null)
        {
            ghostAI.enabled = false;
            UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.isStopped = true;
        }

        // Disable Collider
        if (ghostCollider != null)
        {
            ghostCollider.enabled = false;
        }

        // Play death animation
        if (anim != null)
        {
            anim.SetTrigger("Die");
            anim.SetBool("IsDead", true);
        }

        // Spawn death effect
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // Destroy object after delay
        Destroy(gameObject, destroyDelay);
    }
}
