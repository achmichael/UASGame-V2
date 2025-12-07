using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimator : MonoBehaviour
{
    private GhostAI ghostAI;

    void Awake()
    {
        ghostAI = GetComponentInParent<GhostAI>();
        if (ghostAI == null)
            Debug.LogError("GhostAI tidak ditemukan di parent!");
    }

    // Nama fungsi ini yang kamu panggil dari Animation Event
    public void DealDamageEvent()
    {
        if (ghostAI != null)
            ghostAI.DealDamageToPlayer(); // fungsi asli di parent
    }
}
