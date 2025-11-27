// EnemyLogicSementara.cs
// ------------------------------------------------------------
// Skrip AI enemy dasar yang mengejar player menggunakan node-pathfinding
// berbasis Dijkstra (versi sederhana).
//
// Fitur:
// - Enemy mengejar player dengan mencari path terpendek antar node
// - Enemy berhenti mengejar ketika player masuk checkpoint
// - Enemy kembali aktif mengejar ketika checkpoint tidak aktif
//
// Catatan:
// - Script ini menggunakan Node class milik GhostAI.cs
// - Hubungkan node grid melalui inspector
// ------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

public class EnemyLogicSementara : MonoBehaviour
{
    [Header("References")]
    public Transform player;          // Target player
    public Node[,] grid;              // Node grid dipasang di inspector

    [Header("Behavior")]
    public float chaseRange = 10f;    // Jarak untuk mulai mengejar
    public float moveSpeed = 3f;      // Kecepatan enemy
    public float nodeReachThreshold = 0.2f;

    private Vector3 startPosition;     // Posisi awal enemy
    private Node currentNode;          // Node tempat enemy sekarang berada

    // Variabel untuk pathfinding
    private List<Node> path = new List<Node>();
    private int pathIndex = 0;

    // Dipakai oleh CheckpointZone untuk memblokir enemy
    private bool checkpointBlocked = false;

    void Start()
    {
        startPosition = transform.position;

        if (grid != null && grid.Length > 0)
        {
            currentNode = GetClosestNode(transform.position);
        }
    }

    void Update()
    {
        // Enemy tidak melakukan apa-apa jika checkpoint aktif
        if (checkpointBlocked)
            return;

        if (player == null || grid == null) return;

        float distToPlayer = Vector3.Distance(transform.position, player.position);

        // Tentukan target node
        Node targetNode;
        if (distToPlayer <= chaseRange)
        {
            // Mengejar Player
            targetNode = GetClosestNode(player.position);
        }
        else
        {
            // Kembali ke posisi awal
            targetNode = GetClosestNode(startPosition);
        }

        // Hitung ulang path jika node berubah atau path kosong
        Node myNode = GetClosestNode(transform.position);
        if (currentNode == null || myNode != currentNode || path.Count == 0)
        {
            currentNode = myNode;
            path = Dijkstra(currentNode, targetNode);
            pathIndex = 0;
        }

        FollowPath();
    }

    // ---------------------------------------------------------------------
    // Fungsi pencari node terdekat
    // ---------------------------------------------------------------------
    Node GetClosestNode(Vector3 pos)
    {
        Node closest = null;
        float minDist = Mathf.Infinity;

        foreach (Node n in grid)
        {
            if (n == null) continue;

            float d = Vector3.Distance(pos, n.position);
            if (d < minDist)
            {
                closest = n;
                minDist = d;
            }
        }

        return closest;
    }

    // ---------------------------------------------------------------------
    // Algoritma Dijkstra sederhana (untuk pembelajaran)
    // ---------------------------------------------------------------------
    List<Node> Dijkstra(Node start, Node target)
    {
        List<Node> result = new List<Node>();
        if (start == null || target == null) return result;

        Dictionary<Node, float> dist = new Dictionary<Node, float>();
        Dictionary<Node, Node> prev = new Dictionary<Node, Node>();
        List<Node> unvisited = new List<Node>();

        foreach (Node node in grid)
        {
            if (node == null) continue;

            dist[node] = Mathf.Infinity;
            prev[node] = null;
            unvisited.Add(node);
        }

        dist[start] = 0f;

        while (unvisited.Count > 0)
        {
            // Ambil node terdekat
            unvisited.Sort((a, b) => dist[a].CompareTo(dist[b]));
            Node u = unvisited[0];
            unvisited.RemoveAt(0);

            if (u == target) break;
            if (dist[u] == Mathf.Infinity) break;

            // Cek semua neighbor
            foreach (Node neighbor in u.neighbors)
            {
                if (neighbor == null) continue;

                float alt = dist[u] + Vector3.Distance(u.position, neighbor.position);
                if (alt < dist[neighbor])
                {
                    dist[neighbor] = alt;
                    prev[neighbor] = u;
                }
            }
        }

        // Bangun ulang path
        Node curr = target;
        while (curr != null)
        {
            result.Insert(0, curr);
            prev.TryGetValue(curr, out curr);
        }

        return result;
    }

    // ---------------------------------------------------------------------
    // Gerakkan enemy mengikuti path
    // ---------------------------------------------------------------------
    void FollowPath()
    {
        if (path == null || path.Count == 0) return;

        if (pathIndex >= path.Count)
            pathIndex = path.Count - 1;

        int nextIndex = Mathf.Min(pathIndex + 1, path.Count - 1);

        Vector3 targetPos = path[nextIndex].position;

        transform.position = Vector3.MoveTowards(transform.position,
                                                 targetPos,
                                                 moveSpeed * Time.deltaTime);

        // Rotasi menghadap node tujuan
        Vector3 dir = targetPos - transform.position;
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 10f * Time.deltaTime);
        }

        if (Vector3.Distance(transform.position, targetPos) <= nodeReachThreshold)
        {
            currentNode = path[nextIndex];
            pathIndex = nextIndex;
        }
    }

    // ---------------------------------------------------------------------
    // Dipanggil oleh CheckpointZone
    // ---------------------------------------------------------------------
    /// <summary>
    /// Mengatur state enemy ketika player memasuki/keluar checkpoint.
    /// Jika state = true  → Enemy berhenti mengejar (kehilangan jejak).
    /// Jika state = false → Enemy kembali aktif mengejar.
    /// </summary>
    public void SetCheckpointState(bool state)
    {
        checkpointBlocked = state;

        if (checkpointBlocked)
        {
            // Menghapus path agar AI langsung berhenti total
            path.Clear();
            pathIndex = 0;
        }
    }
}
