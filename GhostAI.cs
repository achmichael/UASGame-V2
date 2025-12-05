using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GhostAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Animator animator;
    public GridBuilder gridBuilder;
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

    [Header("Pathfinding Settings")]
    [Tooltip("Interval waktu untuk recalculate path (detik)")]
    public float pathUpdateInterval = 0.5f;
    
    [Tooltip("Jarak threshold untuk dianggap sudah sampai node (meter)")]
    public float nodeReachThreshold = 0.5f;

    [Header("States")]
    public bool isChasing = false;
    public bool isAttacking = false;

    // Tracking
    private float lastAttackTime;
    private float lastPathUpdateTime;
    
    // Pathfinding
    private List<Node> currentPath;
    private int currentPathIndex = 0;

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
        
        if (gridBuilder == null)
            gridBuilder = FindObjectOfType<GridBuilder>();

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>(); 

        // Validation
        if (player == null)
            Debug.LogError("Player tidak ditemukan! Pastikan player memiliki tag 'Player'.");

        if (playerHealth == null)
            Debug.LogError("PlayerHealth tidak ditemukan pada player! Pastikan player memiliki script PlayerHealth.");
        
        if (gridBuilder == null)
            Debug.LogError("GridBuilder tidak ditemukan! Pastikan ada GameObject dengan GridBuilder script di scene.");
            
        // Default layer mask jika belum diset (Everything)
        if (obstacleMask == 0) 
            obstacleMask = -1; 
    }

    void Update()
    {
        if (player == null || agent == null || gridBuilder == null) return;

        if (playerHealth != null && playerHealth.IsDead)
        {
            agent.isStopped = true;
            currentPath = null;
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
            currentPath = null;
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
        // Update path secara periodik menggunakan Dijkstra
        if (Time.time - lastPathUpdateTime > pathUpdateInterval)
        {
            CalculatePathUsingDijkstra();
            lastPathUpdateTime = Time.time;
        }

        // Ikuti path yang sudah dihitung
        FollowCalculatedPath();

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
    // PATHFINDING FUNCTIONS (DIJKSTRA ALGORITHM)
    // ------------------------------------------------------------------------

    /// <summary>
    /// Hitung path dari enemy ke player menggunakan algoritma Dijkstra
    /// Menggunakan GridBuilder sebagai state space/graph
    /// </summary>
    void CalculatePathUsingDijkstra()
    {
        if (gridBuilder == null)
        {
            Debug.LogError("[GhostAI] GridBuilder is null!");
            currentPath = null;
            return;
        }

        if (gridBuilder.grid == null)
        {
            Debug.LogError("[GhostAI] GridBuilder.grid is null!");
            currentPath = null;
            return;
        }

        // Ambil start node (posisi enemy saat ini)
        Node startNode = gridBuilder.GetNodeFromWorldPosition(transform.position);
        
        // Ambil goal node (posisi player)
        Node goalNode = gridBuilder.GetNodeFromWorldPosition(player.position);

        // Validasi node dengan debug info
        if (startNode == null)
        {
            Debug.LogWarning($"[GhostAI] Start node NULL at position {transform.position}");
            currentPath = null;
            return;
        }

        if (goalNode == null)
        {
            Debug.LogWarning($"[GhostAI] Goal node NULL at player position {player.position}");
            currentPath = null;
            return;
        }

        if (!startNode.isWalkable)
        {
            Debug.LogWarning("[GhostAI] Start node is not walkable!");
            currentPath = null;
            return;
        }

        if (!goalNode.isWalkable)
        {
            Debug.LogWarning("[GhostAI] Goal node is not walkable!");
            currentPath = null;
            return;
        }

        // Jalankan algoritma Dijkstra
        currentPath = DijkstraPathfinding(startNode, goalNode);
        currentPathIndex = 0;

        if (currentPath != null && currentPath.Count > 0)
        {
            Debug.Log($"[GhostAI] Path calculated: {currentPath.Count} nodes");
        }
        else
        {
            Debug.LogWarning("[GhostAI] No path found!");
        }
    }

    /// <summary>
    /// Implementasi Algoritma Dijkstra untuk pathfinding
    /// State Space: Grid nodes dari GridBuilder
    /// Mengembalikan list node dari start ke goal
    /// </summary>
    List<Node> DijkstraPathfinding(Node startNode, Node goalNode)
    {
        // Dictionary untuk menyimpan jarak terpendek dari start ke setiap node
        Dictionary<Node, float> distances = new Dictionary<Node, float>();
        
        // Dictionary untuk menyimpan node sebelumnya dalam path terpendek
        Dictionary<Node, Node> previousNodes = new Dictionary<Node, Node>();
        
        // List node yang belum dikunjungi
        List<Node> unvisitedNodes = new List<Node>();

        // Inisialisasi semua node di grid
        for (int x = 0; x < gridBuilder.gridWidth; x++)
        {
            for (int z = 0; z < gridBuilder.gridHeight; z++)
            {
                Node node = gridBuilder.grid[x, z];
                
                // Skip node yang null atau unwalkable
                if (node == null || !node.isWalkable)
                    continue;

                // Set jarak awal semua node ke infinity
                distances[node] = float.MaxValue;
                unvisitedNodes.Add(node);
            }
        }

        // Jarak dari start node ke dirinya sendiri = 0
        distances[startNode] = 0;

        // Algoritma Dijkstra - loop sampai semua node dikunjungi atau goal tercapai
        while (unvisitedNodes.Count > 0)
        {
            // Cari node dengan jarak terpendek dari unvisited nodes
            Node currentNode = null;
            float shortestDistance = float.MaxValue;

            foreach (Node node in unvisitedNodes)
            {
                if (distances[node] < shortestDistance)
                {
                    shortestDistance = distances[node];
                    currentNode = node;
                }
            }

            // Jika tidak ada node yang bisa dijangkau atau sudah sampai goal
            if (currentNode == null || currentNode == goalNode)
                break;

            // Tandai node ini sebagai sudah dikunjungi
            unvisitedNodes.Remove(currentNode);

            // Periksa semua neighbor dari current node
            if (currentNode.neighbors != null)
            {
                foreach (Node neighbor in currentNode.neighbors)
                {
                    // Skip jika neighbor tidak ada dalam unvisited list
                    if (!unvisitedNodes.Contains(neighbor))
                        continue;

                    // Hitung jarak dari start -> current -> neighbor
                    float distanceToNeighbor = Vector3.Distance(currentNode.position, neighbor.position);
                    float alternativeDistance = distances[currentNode] + distanceToNeighbor;

                    // Jika jalur ini lebih pendek, update distance dan previous node
                    if (alternativeDistance < distances[neighbor])
                    {
                        distances[neighbor] = alternativeDistance;
                        previousNodes[neighbor] = currentNode;
                    }
                }
            }
        }

        // Rekonstruksi path dari goal ke start menggunakan previousNodes
        List<Node> path = new List<Node>();
        Node current = goalNode;

        // Trace back dari goal ke start
        while (previousNodes.ContainsKey(current))
        {
            path.Add(current);
            current = previousNodes[current];
        }

        // Reverse path agar urutan dari start ke goal
        path.Reverse();

        // Return path jika valid, null jika tidak ada path
        return path.Count > 0 ? path : null;
    }

    /// <summary>
    /// Ikuti path yang sudah dihitung node per node menggunakan NavMeshAgent
    /// NavMeshAgent hanya sebagai executor untuk gerakan 3D (naik tangga, dll)
    /// </summary>
    void FollowCalculatedPath()
    {
        // Jika tidak ada path, stop agent
        if (currentPath == null || currentPath.Count == 0)
        {
            // Fallback: langsung chase player jika tidak ada grid path
            agent.isStopped = false;
            agent.SetDestination(player.position);
            return;
        }

        // Jika sudah sampai akhir path
        if (currentPathIndex >= currentPath.Count)
        {
            // Path selesai, tapi tetap update ke posisi player langsung
            agent.isStopped = false;
            agent.SetDestination(player.position);
            return;
        }

        // Ambil node target saat ini
        Node targetNode = currentPath[currentPathIndex];
        Vector3 targetPosition = targetNode.position;

        // Set NavMeshAgent untuk bergerak ke node target
        // NavMeshAgent akan handle gerakan 3D (naik tangga, slope, dll)
        agent.isStopped = false;
        agent.SetDestination(targetPosition);

        // Cek apakah sudah sampai di node target
        float distanceToNode = Vector3.Distance(transform.position, targetPosition);
        
        if (distanceToNode < nodeReachThreshold)
        {
            // Pindah ke node berikutnya
            currentPathIndex++;
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

        // Visualisasi Dijkstra Path (calculated from GridBuilder)
        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.yellow;
            
            // Draw path dari enemy ke node pertama
            if (currentPath.Count > 0)
            {
                Gizmos.DrawLine(transform.position, currentPath[0].position);
            }
            
            // Draw path antar node
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i].position, currentPath[i + 1].position);
                
                // Draw sphere di setiap node
                Gizmos.DrawSphere(currentPath[i].position, 0.2f);
            }
            
            // Draw sphere di node terakhir
            if (currentPath.Count > 0)
            {
                Gizmos.DrawSphere(currentPath[currentPath.Count - 1].position, 0.2f);
            }
        }

        // NavMesh Path (from NavMeshAgent - for comparison)
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = new Color(0, 1, 1, 0.5f); // Cyan with transparency
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }
}