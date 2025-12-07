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

    [Header("Wandering Settings")]
    [Tooltip("Kecepatan berjalan saat wandering (lebih lambat dari chase)")]
    public float wanderSpeed = 2f;

    [Tooltip("Waktu menunggu di lokasi sebelum mencari target baru (detik) - dikurangi agar tidak diam terlalu lama")]
    public float wanderWaitTime = 0.5f; // Dikurangi dari 2f ke 0.5f

    [Tooltip("Jarak minimum dari posisi sekarang untuk memilih wander target")]
    public float minWanderDistance = 3f; // Dikurangi dari 5f ke 3f agar lebih mudah menemukan target

    [Tooltip("Jarak maksimum untuk wander target")]
    public float maxWanderDistance = 15f;

    [Tooltip("Waktu maksimal stuck di satu posisi sebelum pindah target (detik)")]
    public float maxStuckTime = 3f;

    [Header("Audio Settings")]
    public AudioClip ghostChaseClip;
    [Range(0f, 1f)] public float chaseVolume = 0.6f;
    public AudioClip attackClip;
    [Range(0f, 1f)] public float attackVolume = 1.0f;
    private AudioSource ghostAudioSource;

    // Global tracking untuk heartbeat player
    private static int chasingCount = 0;
    private bool wasChasing = false;

    [Header("States")]
    public bool isChasing = false;
    public bool isAttacking = false;
    public bool isWandering = false;

    // Tracking
    private float lastAttackTime;
    private float lastPathUpdateTime;
    private Vector3 wanderTarget;
    private bool hasWanderTarget = false;
    private float wanderArrivalTime;
    private bool isWaitingAtWanderTarget = false;

    // BARU: Tracking untuk mencegah stuck
    private Vector3 lastPosition;
    private float lastMoveTime;
    private float stuckCheckInterval = 1f;
    private float lastStuckCheckTime;

    // Pathfinding
    private List<Node> currentPath;
    private int currentPathIndex = 0;

    // FIX: Flag untuk mencegah multiple damage dalam satu attack animation
    private bool hasDealtDamageThisAttack = false;

    void Start()
    {
        // Setup Audio
        ghostAudioSource = GetComponent<AudioSource>();
        if (ghostAudioSource == null)
        {
            ghostAudioSource = gameObject.AddComponent<AudioSource>();
        }
        ghostAudioSource.loop = true;
        ghostAudioSource.spatialBlend = 1.0f; // 3D Sound
        ghostAudioSource.minDistance = 2f;
        ghostAudioSource.maxDistance = chaseRange + 5f;
        ghostAudioSource.rolloffMode = AudioRolloffMode.Linear;

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
                moveSpeed = 2f;
                chaseRange = 8f;
                wanderSpeed = 1.5f;
                break;
            case 1: // Normal
                moveSpeed = 3f;
                chaseRange = 12f;
                wanderSpeed = 2f;
                break;
            case 2: // Hard
                moveSpeed = 4f;
                chaseRange = 15f;
                wanderSpeed = 2.5f;
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

        // Mulai dengan wander state
        isWandering = true;
        lastPosition = transform.position;
        lastMoveTime = Time.time;
        lastStuckCheckTime = Time.time;

        // Delay sedikit sebelum mulai wander untuk memastikan grid sudah ready
        StartCoroutine(DelayedFirstWander());
    }

    IEnumerator DelayedFirstWander()
    {
        yield return new WaitForSeconds(0.5f);

        if (gridBuilder != null && gridBuilder.grid != null)
        {
            PickRandomWanderTarget();
            Debug.Log($"[GhostAI] {gameObject.name} memulai wandering");
        }
        else
        {
            Debug.LogWarning($"[GhostAI] {gameObject.name} - GridBuilder belum ready, mencoba lagi...");
            yield return new WaitForSeconds(1f);
            PickRandomWanderTarget();
        }
    }

    void Update()
    {
        // FIX: Jangan return jika gridBuilder null, karena kita punya fallback NavMesh
        if (player == null || agent == null) return;

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
            isWaitingAtWanderTarget = false;

            PerformAttackBehavior();
        }
        else if (distanceToPlayer <= chaseRange && !isPlayerSafe)
        {
            // --- STATE: CHASE ---
            isAttacking = false;
            isChasing = true;
            isWandering = false;
            isWaitingAtWanderTarget = false;

            // Reset damage flag ketika keluar dari attack state
            hasDealtDamageThisAttack = false;

            // Set kecepatan normal untuk chase
            agent.speed = moveSpeed;

            PerformChaseBehavior();
        }
        else
        {
            // --- STATE: WANDER ---
            isAttacking = false;
            isChasing = false;
            isWandering = true;

            // Reset damage flag ketika keluar dari attack state
            hasDealtDamageThisAttack = false;

            // Set kecepatan lebih lambat untuk wander
            agent.speed = wanderSpeed;

            PerformWanderBehavior();
        }

        // Rotasi hanya saat tidak wandering
        if (!isWandering)
        {
            HandleRotation();
        }

        // Update Audio State
        UpdateChaseAudio();
    }

    void OnDisable()
    {
        if (wasChasing)
        {
            chasingCount--;
            if (chasingCount <= 0)
            {
                chasingCount = 0;
                if (AudioManager.Instance != null)
                    AudioManager.Instance.StopHeartbeat();
            }
            wasChasing = false;
        }
    }

    void UpdateChaseAudio()
    {
        // Jika state berubah
        if (isChasing != wasChasing)
        {
            if (isChasing)
            {
                // Start Chasing
                chasingCount++;

                // Play Ghost Sound
                if (ghostAudioSource != null && ghostChaseClip != null)
                {
                    ghostAudioSource.clip = ghostChaseClip;
                    ghostAudioSource.volume = chaseVolume;
                    ghostAudioSource.Play();
                }

                // Trigger Player Heartbeat (jika ini ghost pertama yang chase)
                if (chasingCount == 1 && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayHeartbeat();
                }
            }
            else
            {
                // Stop Chasing
                chasingCount--;

                // Stop Ghost Sound
                if (ghostAudioSource != null)
                {
                    ghostAudioSource.Stop();
                }

                // Stop Player Heartbeat (jika tidak ada lagi ghost yang chase)
                if (chasingCount <= 0)
                {
                    chasingCount = 0; // Safety clamp
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.StopHeartbeat();
                    }
                }
            }

            wasChasing = isChasing;
        }
    }

    // ------------------------------------------------------------------------
    // BEHAVIOR FUNCTIONS
    // ------------------------------------------------------------------------

    void PerformWanderBehavior()
    {
        // CEK STUCK: Apakah enemy stuck di satu tempat terlalu lama?
        CheckIfStuck();

        // PENTING: Pastikan agent tidak stopped
        if (agent.isStopped)
        {
            agent.isStopped = false;
        }

        // Jika sedang menunggu di target, cek apakah sudah waktunya bergerak lagi
        if (isWaitingAtWanderTarget)
        {
            if (Time.time - wanderArrivalTime >= wanderWaitTime)
            {
                isWaitingAtWanderTarget = false;
                hasWanderTarget = false;
                // Langsung pilih target baru tanpa delay
                PickRandomWanderTarget();
            }
            else
            {
                // Menunggu sebentar
                SetAnimationState(false, false, false);
                return;
            }
        }

        // Cek apakah sudah sampai target (gunakan pathPending untuk akurasi)
        // Kita gunakan remainingDistance dari NavMeshAgent langsung
        bool reachedDestination = !agent.pathPending &&
                                  (agent.remainingDistance <= agent.stoppingDistance + 0.5f);

        if (!hasWanderTarget || reachedDestination)
        {
            if (hasWanderTarget) // Berarti baru sampai
            {
                isWaitingAtWanderTarget = true;
                wanderArrivalTime = Time.time;
                return;
            }
            else // Tidak punya target, pilih baru
            {
                PickRandomWanderTarget();
            }
        }

        // Bergerak ke target wander
        if (hasWanderTarget)
        {
            // SIMPLIFIKASI: Langsung gunakan NavMeshAgent, abaikan custom pathfinding untuk wander
            // Ini jauh lebih reliable untuk random movement
            if (Vector3.Distance(agent.destination, wanderTarget) > 1.0f)
            {
                agent.SetDestination(wanderTarget);
            }

            // Cek apakah agent sedang bergerak
            bool isMoving = agent.velocity.sqrMagnitude > 0.1f;

            // Set animasi walk (bukan run) saat wandering
            SetAnimationState(isMoving, false, false);

            // Rotasi smooth ke arah tujuan saat wandering
            if (isMoving)
            {
                Vector3 direction = agent.velocity.normalized;
                direction.y = 0;

                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 5f * Time.deltaTime);
                }

                // Update posisi terakhir bergerak
                lastPosition = transform.position;
                lastMoveTime = Time.time;
            }
        }
    }

    /// <summary>
    /// Fallback method: gunakan NavMesh langsung untuk mencari random point
    /// </summary>
    void TryDirectNavMeshWander()
    {
        // Cari random point di sekitar enemy menggunakan NavMesh
        Vector3 randomDirection = Random.insideUnitSphere * maxWanderDistance;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, maxWanderDistance, NavMesh.AllAreas))
        {
            wanderTarget = hit.position;
            hasWanderTarget = true;
            agent.SetDestination(wanderTarget);
            Debug.Log($"[GhostAI] Using NavMesh direct wander to {wanderTarget}");
        }
    }

    /// <summary>
    /// Cek apakah enemy stuck dan paksa pilih target baru jika iya
    /// </summary>
    void CheckIfStuck()
    {
        // Cek setiap interval tertentu
        if (Time.time - lastStuckCheckTime < stuckCheckInterval)
            return;

        lastStuckCheckTime = Time.time;

        // Hitung jarak dari posisi terakhir
        float movedDistance = Vector3.Distance(transform.position, lastPosition);

        // Jika tidak bergerak signifikan dalam waktu lama
        if (movedDistance < 0.3f && Time.time - lastMoveTime > maxStuckTime)
        {
            Debug.Log($"[GhostAI] {gameObject.name} stuck! Mencari target wander baru...");

            // Reset state dan pilih target baru
            hasWanderTarget = false;
            isWaitingAtWanderTarget = false;
            currentPath = null;
            agent.ResetPath();

            // Coba NavMesh direct wander dulu (lebih reliable)
            TryDirectNavMeshWander();

            // Jika masih gagal, coba grid-based
            if (!hasWanderTarget)
            {
                PickRandomWanderTarget();
            }

            // Reset tracking
            lastPosition = transform.position;
            lastMoveTime = Time.time;
        }

        // Update last position untuk tracking berikutnya
        if (movedDistance > 0.3f)
        {
            lastPosition = transform.position;
        }
    }

    void PickRandomWanderTarget()
    {
        // Metode 1: Gunakan validCells dari GridBuilder (lebih reliable)
        if (gridBuilder != null && gridBuilder.validCells != null && gridBuilder.validCells.Count > 0)
        {
            List<Vector3> validPositions = new List<Vector3>();
            List<Vector3> nearbyPositions = new List<Vector3>();

            foreach (Vector3 cellPos in gridBuilder.validCells)
            {
                float distanceToCell = Vector3.Distance(transform.position, cellPos);

                if (distanceToCell >= minWanderDistance && distanceToCell <= maxWanderDistance)
                {
                    validPositions.Add(cellPos);
                }
                else if (distanceToCell >= 1f && distanceToCell < minWanderDistance)
                {
                    nearbyPositions.Add(cellPos);
                }
            }

            // Prioritas: posisi dengan jarak ideal
            if (validPositions.Count > 0)
            {
                Vector3 selectedPos = validPositions[Random.Range(0, validPositions.Count)];
                SetWanderTarget(selectedPos);
                Debug.Log($"[GhostAI] {gameObject.name} picked wander target from validCells");
                return;
            }

            // Fallback: posisi lebih dekat
            if (nearbyPositions.Count > 0)
            {
                Vector3 selectedPos = nearbyPositions[Random.Range(0, nearbyPositions.Count)];
                SetWanderTarget(selectedPos);
                Debug.Log($"[GhostAI] {gameObject.name} using nearby position as fallback");
                return;
            }

            // Last resort dari validCells: random cell
            if (gridBuilder.validCells.Count > 0)
            {
                Vector3 randomCell = gridBuilder.validCells[Random.Range(0, gridBuilder.validCells.Count)];
                SetWanderTarget(randomCell);
                Debug.Log($"[GhostAI] {gameObject.name} using random validCell as last resort");
                return;
            }
        }

        // Metode 2: Gunakan grid jika validCells tidak tersedia
        if (gridBuilder != null && gridBuilder.grid != null)
        {
            List<Node> validNodes = new List<Node>();

            for (int x = 0; x < gridBuilder.gridWidth; x++)
            {
                for (int z = 0; z < gridBuilder.gridHeight; z++)
                {
                    Node node = gridBuilder.grid[x, z];
                    if (node != null && node.isWalkable)
                    {
                        float distanceToNode = Vector3.Distance(transform.position, node.position);
                        if (distanceToNode >= minWanderDistance && distanceToNode <= maxWanderDistance)
                        {
                            validNodes.Add(node);
                        }
                    }
                }
            }

            if (validNodes.Count > 0)
            {
                Node selectedNode = validNodes[Random.Range(0, validNodes.Count)];
                SetWanderTarget(selectedNode.position);
                Debug.Log($"[GhostAI] {gameObject.name} picked wander target from grid");
                return;
            }
        }

        // Metode 3: NavMesh fallback (selalu berhasil jika ada NavMesh)
        Debug.LogWarning($"[GhostAI] {gameObject.name} - Grid tidak tersedia, menggunakan NavMesh fallback");
        TryDirectNavMeshWander();
    }

    /// <summary>
    /// Set wander target dan reset path
    /// </summary>
    void SetWanderTarget(Vector3 targetPosition)
    {
        wanderTarget = targetPosition;
        hasWanderTarget = true;
        currentPath = null; // Reset path agar dihitung ulang
        agent.isStopped = false;

        float distanceToTarget = Vector3.Distance(transform.position, wanderTarget);
        Debug.Log($"[GhostAI] Enemy memilih wander target baru di {wanderTarget}, jarak: {distanceToTarget:F2}m");
    }

    /// <summary>
    /// Dipanggil saat player masuk checkpoint (safe zone) - enemy langsung wander
    /// </summary>
    public void OnPlayerEnteredSafeZone()
    {
        if (isChasing || isAttacking)
        {
            Debug.Log($"[GhostAI] Player masuk safe zone! Enemy berhenti mengejar dan mulai wander.");

            // Force switch ke wander state
            isChasing = false;
            isAttacking = false;
            isWandering = true;

            // Reset path dan pilih target wander baru
            currentPath = null;
            hasWanderTarget = false;
            agent.ResetPath();
            agent.speed = wanderSpeed;

            PickRandomWanderTarget();
        }
    }

    /// <summary>
    /// Dipanggil saat player keluar checkpoint - enemy bisa mengejar lagi jika dalam range
    /// </summary>
    public void OnPlayerExitedSafeZone()
    {
        Debug.Log($"[GhostAI] Player keluar safe zone! Enemy siap mengejar jika dalam range.");
        // State akan otomatis berubah di Update() berdasarkan jarak
    }

    void PerformAttackBehavior()
    {
        agent.isStopped = true;

        // Hanya trigger animasi dan cek cooldown, damage akan diberikan via Animation Event
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

    // Method yang akan dipanggil dari Animation Event
    public void DealDamageToPlayer()
    {
        // Cek apakah sudah pernah deal damage dalam attack ini
        if (hasDealtDamageThisAttack)
        {
            Debug.Log("Damage sudah diberikan untuk attack ini, skip.");
            return;
        }

        Debug.Log("Ghost mencoba memberikan damage ke player...");

        if (player == null) return;

        Debug.Log("Memeriksa jarak ke player untuk memberikan damage...");

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= attackRange && CanAttackPlayer())
        {
            if (playerHealth != null)
            {
                if (ghostAudioSource != null && attackClip != null)
                {
                    ghostAudioSource.PlayOneShot(attackClip, attackVolume);
                }
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

            // Untuk wander, gunakan wander target; untuk chase, gunakan player position
            if (isWandering && hasWanderTarget)
            {
                agent.SetDestination(wanderTarget);
            }
            else if (isChasing && player != null)
            {
                agent.SetDestination(player.position);
            }
            return;
        }

        if (currentPathIndex >= currentPath.Count)
        {
            agent.isStopped = false;

            if (isWandering && hasWanderTarget)
            {
                agent.SetDestination(wanderTarget);
            }
            else if (isChasing && player != null)
            {
                agent.SetDestination(player.position);
            }
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

        // Visualisasi wander target
        if (hasWanderTarget)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(wanderTarget, 0.5f);
            Gizmos.DrawLine(transform.position, wanderTarget);
        }

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