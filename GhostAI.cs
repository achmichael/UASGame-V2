using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GhostAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator animator; 
    private PlayerHealth playerHealth;
    private NavMeshAgent agent;

    [Header("AI Settings")]
    public float chaseRange = 12f;
    public float attackRange = 1.5f; 
    public float moveSpeed = 4f;
    public float attackCooldown = 1f; 
    public int attackDamage = 10;
    
    [Tooltip("Sudut maksimal (0-180) agar enemy dianggap menghadap player.")]
    public float frontAngleThreshold = 60f;

    [Tooltip("Beda ketinggian maksimal yang diperbolehkan untuk menyerang.")]
    public float maxVerticalOffset = 1.0f;

    [Tooltip("Offset tinggi (Y) dari posisi enemy untuk memulai Raycast Line of Sight.")]
    public float raycastHeightOffset = 1.0f;

    [Tooltip("Layer apa saja yang dianggap penghalang pandangan (misal: Default, Ground, Wall).")]
    public LayerMask obstacleMask;

    [Header("NavMesh Settings")]
    [Tooltip("Kecepatan angular untuk rotasi NavMeshAgent")]
    public float angularSpeed = 720f;
    
    [Tooltip("Akselerasi NavMeshAgent")]
    public float acceleration = 8f;

    [Tooltip("Base offset untuk menyesuaikan tinggi enemy dari NavMesh (0 = default, positif = naik, negatif = turun)")]
    public float baseOffset = 0f;

    [Header("States")]
    public bool isChasing = false;
    public bool isAttacking = false;

    // Tracking
    private float lastAttackTime;

    void Start()
    {
        // Setup NavMeshAgent
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) 
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
            Debug.LogWarning("NavMeshAgent tidak ditemukan, otomatis ditambahkan ke " + gameObject.name);
        }

        // Adjust difficulty settings
        int difficulty = PlayerPrefs.GetInt("Difficulty", 0);

        switch (difficulty)
        {
            case 0: // Easy
                moveSpeed = 3f; 
                chaseRange = 8f; 
                break;
            case 1: // Normal
                moveSpeed = 4f; 
                chaseRange = 12f; 
                break;
            case 2: // Hard
                moveSpeed = 5f; 
                chaseRange = 15f; 
                break;
        }

        // Setup NavMeshAgent parameters
        agent.speed = moveSpeed;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;
        agent.stoppingDistance = attackRange;
        agent.updateRotation = false; // Kita handle rotasi manual untuk kontrol lebih halus
        agent.baseOffset = baseOffset; // Sesuaikan tinggi enemy dari NavMesh

        // Find player reference
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Get components
        if (animator == null)
            animator = GetComponent<Animator>(); 

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>(); 

        // Validation
        if (player == null)
            Debug.LogError("Player tidak ditemukan! Pastikan player memiliki tag 'Player'.");

        if (playerHealth == null)
            Debug.LogError("PlayerHealth tidak ditemukan pada player! Pastikan player memiliki script PlayerHealth.");
            
        // Default layer mask jika belum diset (Everything)
        if (obstacleMask == 0) 
            obstacleMask = -1; 
    }

    void Update()
    {
        if (player == null || agent == null) return;

        if (playerHealth != null && playerHealth.IsDead)
        {
            agent.isStopped = true;
            SetAnimationState(false, false, false); 
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // --- LOGIKA STATE MACHINE ---

        // Cek apakah bisa menyerang (semua kondisi terpenuhi)
        if (distanceToPlayer <= attackRange && CanAttackPlayer())
        {
            // --- STATE: ATTACK ---
            isAttacking = true;
            isChasing = false;
            
            PerformAttackBehavior();
        }
        else if (distanceToPlayer <= chaseRange)
        {
            // --- STATE: CHASE ---
            isAttacking = false;
            isChasing = true;

            PerformChaseBehavior();
        }
        else
        {
            // --- STATE: IDLE ---
            isAttacking = false;
            isChasing = false;
            
            agent.isStopped = true;
            SetAnimationState(false, false, false);
        }

        // Manual rotation for smoother look at player
        HandleRotation();
    }

    // ------------------------------------------------------------------------
    // BEHAVIOR FUNCTIONS
    // ------------------------------------------------------------------------

    void PerformAttackBehavior()
    {
        // Stop NavMeshAgent saat menyerang
        agent.isStopped = true;
        
        // Eksekusi serangan jika cooldown habis
        if (Time.time - lastAttackTime > attackCooldown)
        {
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log("Ghost menyerang player!");
            }
            lastAttackTime = Time.time;
        }
        
        // Update Animasi ke Attack
        SetAnimationState(false, false, true);
    }

    void PerformChaseBehavior()
    {
        // Set destination NavMeshAgent ke player
        agent.isStopped = false;
        agent.SetDestination(player.position);

        // Tentukan animasi berdasarkan kecepatan agent
        bool isMoving = agent.velocity.sqrMagnitude > 0.1f;
        
        // Update Animasi ke Run saat bergerak
        SetAnimationState(false, isMoving, false);
    }

    void HandleRotation()
    {
        if (player == null) return;

        // Rotasi manual untuk menghadap player (hanya sumbu Y)
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // Pastikan hanya rotasi horizontal

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 10f * Time.deltaTime);
        }
    }

    // ------------------------------------------------------------------------
    // HELPER FUNCTIONS (VALIDATION)
    // ------------------------------------------------------------------------

    /// <summary>
    /// Mengecek apakah semua syarat untuk menyerang terpenuhi.
    /// </summary>
    public bool CanAttackPlayer()
    {
        if (player == null) return false;

        // Cek kondisi untuk menyerang
        return IsPlayerInFront() && IsVerticalPositionValid() && HasLineOfSight();
    }

    /// <summary>
    /// Mengembalikan true jika player ada di depan enemy berdasarkan angle horizontal.
    /// </summary>
    public bool IsPlayerInFront()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        
        // Abaikan Y axis agar beda tinggi tidak mempengaruhi arah hadap horizontal
        dirToPlayer.y = 0;
        Vector3 forwardFlat = transform.forward;
        forwardFlat.y = 0;

        // Jika posisi sama persis, anggap true atau false (tergantung preferensi, di sini false)
        if (dirToPlayer == Vector3.zero) return false;

        float angle = Vector3.Angle(forwardFlat, dirToPlayer);
        return angle <= frontAngleThreshold;
    }

    /// <summary>
    /// Mengembalikan true jika perbedaan tinggi enemy dan player masih dalam batas toleransi.
    /// </summary>
    public bool IsVerticalPositionValid()
    {
        float verticalDiff = Mathf.Abs(transform.position.y - player.position.y);
        return verticalDiff <= maxVerticalOffset;
    }

    /// <summary>
    /// Raycast dari enemy ke player untuk memastikan tidak ada tembok/obstacle.
    /// </summary>
    public bool HasLineOfSight()
    {
        // Titik asal raycast (sedikit di atas kaki enemy)
        Vector3 origin = transform.position + Vector3.up * raycastHeightOffset;
        
        // Titik target (sedikit di atas kaki player, asumsi pivot player di bawah)
        // Kita tembak ke arah 'center' player kira-kira setinggi raycastHeightOffset juga
        Vector3 target = player.position + Vector3.up * raycastHeightOffset;
        
        Vector3 dir = target - origin;
        float dist = dir.magnitude;

        // Raycast
        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist, obstacleMask))
        {
            // Jika kena sesuatu sebelum sampai player
            // Cek apakah yang kena itu player (atau anak objeknya)
            if (hit.transform == player || hit.transform.IsChildOf(player))
            {
                return true;
            }
            
            // Jika kena obstacle lain
            return false;
        }

        // Jika raycast tidak kena apa-apa sampai jarak 'dist', berarti clear
        return true;
    }

    // ------------------------------------------------------------------------
    // ANIMATION
    // ------------------------------------------------------------------------

    void SetAnimationState(bool walk, bool run, bool attack)
    {
        if (animator == null) return;

        animator.SetBool("Walk", walk);
        animator.SetBool("Run", run);
        animator.SetBool("Attack", attack);
    }

    // ------------------------------------------------------------------------
    // GIZMOS FOR DEBUGGING
    // ------------------------------------------------------------------------

    void OnDrawGizmosSelected()
    {
        // Chase Range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // Attack Range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Visualisasi Attack Angle
        Vector3 leftRay = Quaternion.Euler(0, -frontAngleThreshold, 0) * transform.forward;
        Vector3 rightRay = Quaternion.Euler(0, frontAngleThreshold, 0) * transform.forward;
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, leftRay * attackRange);
        Gizmos.DrawRay(transform.position, rightRay * attackRange);

        // NavMesh Path (if available)
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.yellow;
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }
}