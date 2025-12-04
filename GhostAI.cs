using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GridBuilder gridBuilder;
    public Animator animator; 
    private PlayerHealth playerHealth; 

    [Header("AI Settings")]
    public float chaseRange = 12f;
    public float attackRange = 1.5f; 
    public float moveSpeed = 4f;
    public float nodeReachThreshold = 0.1f;
    public float pathUpdateInterval = 0.5f;
    public float attackCooldown = 1f; 
    public int attackDamage = 10; 
    public float attackMoveSpeed = 2f; 
    [Tooltip("Jarak di mana enemy akan selalu memutar badan menghadap player.")]
    public float facePlayerDistance = 5.0f;

    [Header("Advanced Attack Conditions")]
    [Tooltip("Sudut maksimal (0-180) agar enemy dianggap menghadap player.")]
    public float frontAngleThreshold = 60f;

    [Tooltip("Beda ketinggian maksimal yang diperbolehkan untuk menyerang.")]
    public float maxVerticalOffset = 1.0f;

    [Tooltip("Offset tinggi (Y) dari posisi enemy untuk memulai Raycast Line of Sight.")]
    public float raycastHeightOffset = 1.0f;

    [Tooltip("Layer apa saja yang dianggap penghalang pandangan (misal: Default, Ground, Wall).")]
    public LayerMask obstacleMask;

    [Header("States")]
    public bool isChasing = false;
    public bool isAttacking = false;

    // Pathfinding
    private List<Node> currentPath;
    private int currentPathIndex = 0;
    private float lastPathUpdateTime;
    private float lastAttackTime;

    void Start()
    {
        int difficulty = PlayerPrefs.GetInt("Difficulty", 0);

        switch (difficulty)
        {
            case 0: moveSpeed = 3f; chaseRange = 8f; break;  // Easy
            case 1: moveSpeed = 4f; chaseRange = 12f; break; // Normal
            case 2: moveSpeed = 5f; chaseRange = 15f; break; // Hard
        }

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (gridBuilder == null)
            gridBuilder = FindObjectOfType<GridBuilder>();

        if (animator == null)
            animator = GetComponent<Animator>(); 

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>(); 

        if (gridBuilder == null)
            Debug.LogError("GridBuilder tidak ditemukan! Pastikan ada GameObject dengan GridBuilder script di scene.");

        if (playerHealth == null)
            Debug.LogError("PlayerHealth tidak ditemukan pada player! Pastikan player memiliki script PlayerHealth.");
            
        // Default layer mask jika belum diset (Everything)
        if (obstacleMask == 0) 
            obstacleMask = -1; 
    }

    void Update()
    {
        if (player == null || gridBuilder == null || gridBuilder.grid == null) return;

        if (playerHealth != null && playerHealth.IsDead)
        {
            SetAnimationState(true, false, false, false); 
            return;
        }

        // --- LOGIKA STATE MACHINE ---

        // Cek apakah bisa menyerang (semua kondisi terpenuhi)
        if (CanAttackPlayer())
        {
            // --- STATE: ATTACK ---
            isAttacking = true;
            isChasing = false;
            
            PerformAttackBehavior();
        }
        else
        {
            // Cek apakah masuk range chase (menggunakan jarak horizontal saja)
            Vector3 playerPosFlat = new Vector3(player.position.x, 0, player.position.z);
            Vector3 enemyPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
            float distanceToPlayerFlat = Vector3.Distance(enemyPosFlat, playerPosFlat);

            if (distanceToPlayerFlat <= chaseRange)
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
                currentPath = null;

                SetAnimationState(true, false, false, false);
            }
        }
    }

    // ------------------------------------------------------------------------
    // BEHAVIOR FUNCTIONS
    // ------------------------------------------------------------------------

    void PerformAttackBehavior()
    {
        Vector3 playerPosFlat = new Vector3(player.position.x, 0, player.position.z);
        Vector3 enemyPosFlat = new Vector3(transform.position.x, 0, transform.position.z);

        // Jaga jarak attackRange dari player (sedikit mundur/maju biar pas)
        Vector3 directionToPlayer = (playerPosFlat - enemyPosFlat).normalized;
        
        // Target posisi sedikit di depan player
        Vector3 targetPosition = player.position - directionToPlayer * (attackRange * 0.5f); 
        targetPosition.y = transform.position.y; // Jaga tinggi tetap sesuai enemy
        
        // Gerak perlahan saat attack (adjust positioning)
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, attackMoveSpeed * Time.deltaTime);
        
        // Rotasi paksa menghadap player agar attack selalu valid
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
        
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
        SetAnimationState(false, false, false, true);
    }

    void PerformChaseBehavior()
    {
        // Update path secara periodik
        if (Time.time - lastPathUpdateTime > pathUpdateInterval)
        {
            FindPathToPlayer();
            lastPathUpdateTime = Time.time;
        }

        FollowPath();

        // Update Animasi ke Run
        SetAnimationState(false, false, true, false);
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

        // 1. Cek Jarak Horizontal
        Vector3 playerPosFlat = new Vector3(player.position.x, 0, player.position.z);
        Vector3 enemyPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
        float distance = Vector3.Distance(enemyPosFlat, playerPosFlat);

        if (distance > attackRange) return false;

        // 2. Cek Kondisi Lainnya
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
    // ANIMATION & PATHFINDING
    // ------------------------------------------------------------------------

    void SetAnimationState(bool idle, bool walk, bool run, bool attack)
    {
        if (animator == null) return;

        animator.SetBool("Idle", idle);
        animator.SetBool("Walk", walk);
        animator.SetBool("Run", run);
        animator.SetBool("Attack", attack);
    }

    void FindPathToPlayer()
    {
        Node startNode = gridBuilder.GetNodeFromWorldPosition(transform.position);
        Node targetNode = gridBuilder.GetNodeFromWorldPosition(player.position);

        if (startNode == null || targetNode == null || !targetNode.isWalkable)
        {
            currentPath = null;
            return;
        }

        currentPath = Dijkstra(startNode, targetNode);
        currentPathIndex = 0;
    }

    List<Node> Dijkstra(Node start, Node target)
    {
        Dictionary<Node, float> distances = new Dictionary<Node, float>();
        Dictionary<Node, Node> previous = new Dictionary<Node, Node>();
        List<Node> unvisited = new List<Node>();

        foreach (Node node in gridBuilder.grid)
        {
            if (node == null) continue;

            distances[node] = float.MaxValue;
            unvisited.Add(node);
        }
        distances[start] = 0;

        while (unvisited.Count > 0)
        {
            Node current = null;
            float smallestDistance = float.MaxValue;
            foreach (Node node in unvisited)
            {
                if (distances[node] < smallestDistance)
                {
                    smallestDistance = distances[node];
                    current = node;
                }
            }

            if (current == null || current == target)
                break;

            unvisited.Remove(current);

            if (current.neighbors != null)
            {
                foreach (Node neighbor in current.neighbors)
                {
                    if (!unvisited.Contains(neighbor)) continue;

                    float distance = Vector3.Distance(current.position, neighbor.position);
                    float alt = distances[current] + distance;

                    if (alt < distances[neighbor])
                    {
                        distances[neighbor] = alt;
                        previous[neighbor] = current;
                    }
                }
            }
        }

        List<Node> path = new List<Node>();
        Node temp = target;

        while (previous.ContainsKey(temp))
        {
            path.Add(temp);
            temp = previous[temp];
        }

        path.Reverse();
        return path.Count > 0 ? path : null;
    }

    void FollowPath()
    {
        if (currentPath == null || currentPath.Count == 0) return;

        if (currentPathIndex >= currentPath.Count)
        {
            currentPath = null;
            return;
        }

        Node targetNode = currentPath[currentPathIndex];
        Vector3 targetPosition = new Vector3(targetNode.position.x, transform.position.y, targetNode.position.z);

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // --- ROTATION LOGIC ---
        Vector3 lookDirection;
        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // Jika dalam jarak tertentu, selalu menghadap player (agar tidak membelakangi)
        if (distToPlayer <= facePlayerDistance)
        {
            lookDirection = (player.position - transform.position).normalized;
        }
        else
        {
            // Jika jauh, hadap ke arah node tujuan
            lookDirection = (targetPosition - transform.position).normalized;
        }

        lookDirection.y = 0; // Pastikan rotasi hanya pada sumbu Y (horizontal)

        if (lookDirection != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        if (Vector3.Distance(transform.position, targetPosition) < nodeReachThreshold)
        {
            currentPathIndex++;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i].position, currentPath[i + 1].position);
            }
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, facePlayerDistance);

        // Visualisasi Attack Angle
        Vector3 leftRay = Quaternion.Euler(0, -frontAngleThreshold, 0) * transform.forward;
        Vector3 rightRay = Quaternion.Euler(0, frontAngleThreshold, 0) * transform.forward;
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, leftRay * attackRange);
        Gizmos.DrawRay(transform.position, rightRay * attackRange);
    }
}