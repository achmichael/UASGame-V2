using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostAI : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public GridBuilder gridBuilder;
    public Animator animator; // Referensi ke Animator
    private PlayerHealth playerHealth; // Referensi ke PlayerHealth untuk serangan

    [Header("AI Settings")]
    public float chaseRange = 12f;
    public float attackRange = 1.5f; // Jarak untuk memicu serangan
    public float moveSpeed = 4f;
    public float nodeReachThreshold = 0.1f;
    public float pathUpdateInterval = 0.5f;
    public float attackCooldown = 1f; // Cooldown antara serangan
    public int attackDamage = 10; // Damage per serangan
    public float attackMoveSpeed = 2f; // Kecepatan gerak saat attack untuk menjaga jarak

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

        // Auto-find references jika belum diassign
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (gridBuilder == null)
            gridBuilder = FindObjectOfType<GridBuilder>();

        if (animator == null)
            animator = GetComponent<Animator>(); // Mencoba ambil dari object sendiri

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>(); // Ambil PlayerHealth dari player

        if (gridBuilder == null)
            Debug.LogError("GridBuilder tidak ditemukan! Pastikan ada GameObject dengan GridBuilder script di scene.");

        if (playerHealth == null)
            Debug.LogError("PlayerHealth tidak ditemukan pada player! Pastikan player memiliki script PlayerHealth.");
    }

    void Update()
    {
        if (player == null || gridBuilder == null || gridBuilder.grid == null) return;

        // Cek jika player sudah mati, hentikan AI
        if (playerHealth != null && playerHealth.IsDead)
        {
            SetAnimationState(true, false, false, false); // Set Idle
            return;
        }

        // Gunakan jarak horizontal agar tidak terpengaruh perbedaan ketinggian (Y-axis)
        Vector3 playerPosFlat = new Vector3(player.position.x, 0, player.position.z);
        Vector3 enemyPosFlat = new Vector3(transform.position.x, 0, transform.position.z);
        float distanceToPlayer = Vector3.Distance(enemyPosFlat, playerPosFlat);

        // --- LOGIKA STATE ANIMASI & AI ---

        // 1. Cek Attack (Prioritas Tertinggi)
        if (distanceToPlayer <= attackRange)
        {
            isAttacking = true;
            isChasing = false;
            
            // Jaga jarak attackRange dari player
            Vector3 directionToPlayer = (playerPosFlat - enemyPosFlat).normalized;
            Vector3 targetPosition = player.position - directionToPlayer; // Target 1 unit dari player
            targetPosition.y = transform.position.y; // Jaga tinggi tetap sesuai enemy
            
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, attackMoveSpeed * Time.deltaTime);
            
            // Rotasi menghadap player
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
            
            // Serang player jika cooldown habis
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
        // 2. Cek Chase/Run (Jika dalam jangkauan tapi belum cukup dekat untuk attack)
        else if (distanceToPlayer <= chaseRange)
        {
            isAttacking = false;
            isChasing = true;

            // Update path secara periodik
            if (Time.time - lastPathUpdateTime > pathUpdateInterval)
            {
                FindPathToPlayer();
                lastPathUpdateTime = Time.time;
            }

            FollowPath();

            // Update Animasi ke Run (karena sedang mengejar)
            // Jika Anda ingin Walk saat speed pelan, bisa ubah logika ini
            SetAnimationState(false, false, true, false);
        }
        // 3. Idle (Di luar jangkauan)
        else
        {
            isAttacking = false;
            isChasing = false;
            currentPath = null;

            // Update Animasi ke Idle
            SetAnimationState(true, false, false, false);
        }
    }

    // Fungsi Helper untuk mengatur Animator Boolean agar rapi dan tidak tumpang tindih
    void SetAnimationState(bool idle, bool walk, bool run, bool attack)
    {
        Debug.Log("Value of animator: " + animator);
        if (animator == null) return;

        animator.SetBool("Idle", idle);
        animator.SetBool("Walk", walk);
        animator.SetBool("Run", run);
        animator.SetBool("Attack", attack);

        // Debug.Log($"[GhostAI] Animation State - Idle: {idle}, Walk: {walk}, Run: {run}, Attack: {attack}");
        Debug.Log("Value of each state from animator: " + animator.GetBool("Idle") + animator.GetBool("Walk") + animator.GetBool("Run") + animator.GetBool("Attack"));
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

        Vector3 direction = (targetPosition - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
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

        // Visualisasi Attack Range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}