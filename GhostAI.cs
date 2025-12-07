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
    private MovementLogic playerMovement;
    private NavMeshAgent agent;

    [Header("AI Settings")]
    public float chaseRange = 12f;
    public float attackRange = 2.5f;
    public float stoppingDistance = 2.0f;
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
    public bool isWandering = false;

    // Tracking
    private float lastAttackTime;
    private float lastPathUpdateTime;
    private Vector3 wanderTarget;
    private bool hasWanderTarget = false;
    
    // Pathfinding
    private List<Node> currentPath;
    private int currentPathIndex = 0;

    // FIX: Flag untuk mencegah multiple damage dalam satu attack animation
    private bool hasDealtDamageThisAttack = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null) 
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
            Debug.LogWarning("NavMeshAgent tidak ditemukan, otomatis ditambahkan ke " + gameObject.name);
        }

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

        agent.speed = moveSpeed;
        agent.angularSpeed = angularSpeed;
        agent.acceleration = acceleration;
        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = false;
        agent.baseOffset = baseOffset;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (animator == null)
            animator = GetComponent<Animator>();
        
        if (gridBuilder == null)
            gridBuilder = FindObjectOfType<GridBuilder>();

        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>(); 
            playerMovement = player.GetComponent<MovementLogic>();
        }

        if (player == null)
            Debug.LogError("Player tidak ditemukan! Pastikan player memiliki tag 'Player'.");

        if (playerHealth == null)
            Debug.LogError("PlayerHealth tidak ditemukan pada player! Pastikan player memiliki script PlayerHealth.");
        
        if (gridBuilder == null)
            Debug.LogError("GridBuilder tidak ditemukan! Pastikan ada GameObject dengan GridBuilder script di scene.");
            
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
        bool isPlayerSafe = (playerMovement != null && playerMovement.isSafe);

        // --- LOGIKA STATE MACHINE ---

        if (distanceToPlayer <= attackRange && CanAttackPlayer() && !isPlayerSafe)
        {
            // --- STATE: ATTACK ---
            isAttacking = true;
            isChasing = false;
            isWandering = false;
            
            PerformAttackBehavior();
        }
        else if (distanceToPlayer <= chaseRange && !isPlayerSafe)
        {
            // --- STATE: CHASE ---
            isAttacking = false;
            isChasing = true;
            isWandering = false;

            // FIX: Reset damage flag ketika keluar dari attack state
            hasDealtDamageThisAttack = false;

            PerformChaseBehavior();
        }
        else
        {
            // --- STATE: WANDER ---
            isAttacking = false;
            isChasing = false;
            isWandering = true;

            // FIX: Reset damage flag ketika keluar dari attack state
            hasDealtDamageThisAttack = false;
            
            PerformWanderBehavior();
        }

        if (!isWandering)
        {
            HandleRotation();
        }
    }

    // ------------------------------------------------------------------------
    // BEHAVIOR FUNCTIONS
    // ------------------------------------------------------------------------

    void PerformWanderBehavior()
    {
        if (!hasWanderTarget || Vector3.Distance(transform.position, wanderTarget) < nodeReachThreshold)
        {
            PickRandomWanderTarget();
        }

        if (hasWanderTarget)
        {
             if (Time.time - lastPathUpdateTime > pathUpdateInterval)
             {
                 CalculatePathToTarget(wanderTarget);
                 lastPathUpdateTime = Time.time;
             }
             FollowCalculatedPath();
             
             bool isMoving = agent.velocity.sqrMagnitude > 0.1f;
             SetAnimationState(false, isMoving, false);
        }
    }

    void PickRandomWanderTarget()
    {
        if (gridBuilder != null && gridBuilder.grid != null)
        {
             for(int i=0; i<10; i++)
             {
                 int x = Random.Range(0, gridBuilder.gridWidth);
                 int z = Random.Range(0, gridBuilder.gridHeight);
                 Node node = gridBuilder.grid[x,z];
                 if (node != null && node.isWalkable)
                 {
                     wanderTarget = node.position;
                     hasWanderTarget = true;
                     return;
                 }
             }
        }
    }

    void PerformAttackBehavior()
    {
        agent.isStopped = true;
        
        // FIX: Hanya trigger animasi dan cek cooldown, damage akan diberikan via Animation Event
        if (Time.time - lastAttackTime > attackCooldown)
        {
            // Trigger attack animation
            SetAnimationState(false, false, true);
            
            // Update last attack time
            lastAttackTime = Time.time;
            
            // Reset damage flag untuk attack baru
            hasDealtDamageThisAttack = false;
            
            Debug.Log("Ghost memulai animasi serangan!");
        }
        else
        {
            // Tetap dalam attack state tapi tidak trigger animasi baru
            SetAnimationState(false, false, true);
        }
    }

    // FIX: Method baru yang akan dipanggil dari Animation Event
    // Tambahkan Animation Event pada frame tengah animasi attack Anda
    public void DealDamageToPlayer()
    {
        // Cek apakah sudah pernah deal damage dalam attack ini
        if (hasDealtDamageThisAttack)
        {
            Debug.Log("Damage sudah diberikan untuk attack ini, skip.");
            return;
        }
        Debug.Log("Ghost mencoba memberikan damage ke player...");
        // Double check apakah player masih dalam range dan kondisi valid
        if (player == null) return;

        Debug.Log("Memeriksa jarak ke player untuk memberikan damage...");
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        Debug.Log($"Value of animator {animator}");
        if (distanceToPlayer <= attackRange && CanAttackPlayer())
        {
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
                Debug.Log($"Ghost memberikan damage {attackDamage} ke player!");
                
                // Set flag bahwa damage sudah diberikan
                hasDealtDamageThisAttack = true;
            }
        }
        else
        {
            Debug.Log("Player keluar dari range saat attack animation, damage dibatalkan.");
        }
    }

    void PerformChaseBehavior()
    {
        if (Time.time - lastPathUpdateTime > pathUpdateInterval)
        {
            CalculatePathToTarget(player.position);
            lastPathUpdateTime = Time.time;
        }

        FollowCalculatedPath();

        bool isMoving = agent.velocity.sqrMagnitude > 0.1f;
        SetAnimationState(false, isMoving, false);
    }

    void HandleRotation()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 10f * Time.deltaTime);
        }
    }

    // ------------------------------------------------------------------------
    // PATHFINDING FUNCTIONS (DIJKSTRA ALGORITHM)
    // ------------------------------------------------------------------------

    void CalculatePathToTarget(Vector3 targetPosition)
    {
        if (gridBuilder == null || gridBuilder.grid == null)
        {
            currentPath = null;
            return;
        }

        Node startNode = gridBuilder.GetNodeFromWorldPosition(transform.position);
        Node goalNode = gridBuilder.GetNodeFromWorldPosition(targetPosition);

        if (startNode == null || goalNode == null || !startNode.isWalkable || !goalNode.isWalkable)
        {
            currentPath = null;
            return;
        }

        currentPath = DijkstraPathfinding(startNode, goalNode);
        currentPathIndex = 0;
    }

    List<Node> DijkstraPathfinding(Node startNode, Node goalNode)
    {
        Dictionary<Node, float> distances = new Dictionary<Node, float>();
        Dictionary<Node, Node> previousNodes = new Dictionary<Node, Node>();
        List<Node> unvisitedNodes = new List<Node>();

        for (int x = 0; x < gridBuilder.gridWidth; x++)
        {
            for (int z = 0; z < gridBuilder.gridHeight; z++)
            {
                Node node = gridBuilder.grid[x, z];
                
                if (node == null || !node.isWalkable)
                    continue;

                distances[node] = float.MaxValue;
                unvisitedNodes.Add(node);
            }
        }

        distances[startNode] = 0;

        while (unvisitedNodes.Count > 0)
        {
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

            if (currentNode == null || currentNode == goalNode)
                break;

            unvisitedNodes.Remove(currentNode);

            if (currentNode.neighbors != null)
            {
                foreach (Node neighbor in currentNode.neighbors)
                {
                    if (!unvisitedNodes.Contains(neighbor))
                        continue;

                    float distanceToNeighbor = Vector3.Distance(currentNode.position, neighbor.position);
                    float alternativeDistance = distances[currentNode] + distanceToNeighbor;

                    if (alternativeDistance < distances[neighbor])
                    {
                        distances[neighbor] = alternativeDistance;
                        previousNodes[neighbor] = currentNode;
                    }
                }
            }
        }

        List<Node> path = new List<Node>();
        Node current = goalNode;

        while (previousNodes.ContainsKey(current))
        {
            path.Add(current);
            current = previousNodes[current];
        }

        path.Reverse();

        return path.Count > 0 ? path : null;
    }

    void FollowCalculatedPath()
    {
        if (currentPath == null || currentPath.Count == 0)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            return;
        }

        if (currentPathIndex >= currentPath.Count)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            return;
        }

        Node targetNode = currentPath[currentPathIndex];
        Vector3 targetPosition = targetNode.position;

        agent.isStopped = false;
        agent.SetDestination(targetPosition);

        float distanceToNode = Vector3.Distance(transform.position, targetPosition);
        
        if (distanceToNode < nodeReachThreshold)
        {
            currentPathIndex++;
        }
    }

    // ------------------------------------------------------------------------
    // HELPER FUNCTIONS (VALIDATION)
    // ------------------------------------------------------------------------

    public bool CanAttackPlayer()
    {
        if (player == null) return false;
        return IsPlayerInFront() && IsVerticalPositionValid() && HasLineOfSight();
    }

    public bool IsPlayerInFront()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        dirToPlayer.y = 0;
        Vector3 forwardFlat = transform.forward;
        forwardFlat.y = 0;

        if (dirToPlayer == Vector3.zero) return false;

        float angle = Vector3.Angle(forwardFlat, dirToPlayer);
        return angle <= frontAngleThreshold;
    }

    public bool IsVerticalPositionValid()
    {
        float verticalDiff = Mathf.Abs(transform.position.y - player.position.y);
        return verticalDiff <= maxVerticalOffset;
    }

    public bool HasLineOfSight()
    {
        Vector3 origin = transform.position + Vector3.up * raycastHeightOffset;
        Vector3 target = player.position + Vector3.up * raycastHeightOffset;
        Vector3 dir = target - origin;
        float dist = dir.magnitude;

        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dist, obstacleMask))
        {
            if (hit.transform == player || hit.transform.IsChildOf(player))
            {
                return true;
            }
            return false;
        }

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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 leftRay = Quaternion.Euler(0, -frontAngleThreshold, 0) * transform.forward;
        Vector3 rightRay = Quaternion.Euler(0, frontAngleThreshold, 0) * transform.forward;
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, leftRay * attackRange);
        Gizmos.DrawRay(transform.position, rightRay * attackRange);

        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.yellow;
            
            if (currentPath.Count > 0)
            {
                Gizmos.DrawLine(transform.position, currentPath[0].position);
            }
            
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i].position, currentPath[i + 1].position);
                Gizmos.DrawSphere(currentPath[i].position, 0.2f);
            }
            
            if (currentPath.Count > 0)
            {
                Gizmos.DrawSphere(currentPath[currentPath.Count - 1].position, 0.2f);
            }
        }

        if (agent != null && agent.hasPath)
        {
            Gizmos.color = new Color(0, 1, 1, 0.5f);
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }
}