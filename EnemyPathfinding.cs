using UnityEngine;
using System.Collections.Generic;

public class EnemyPathfinding : MonoBehaviour
{
    [Header("References")]
    public NodeGenerator nodeGenerator; // Referensi ke generator graph
    public Transform target;            // Player atau target lain

    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 10f;
    public float reachThreshold = 0.5f;
    public float repathInterval = 0.5f; // Hitung ulang path setiap x detik

    private List<Node> currentPath = new List<Node>();
    private int targetIndex = 0;
    private float lastRepathTime;

    void Start()
    {
        if (nodeGenerator == null)
            nodeGenerator = FindObjectOfType<NodeGenerator>();
    }

    void Update()
    {
        if (target == null || nodeGenerator == null) return;

        // Repath secara berkala
        if (Time.time - lastRepathTime > repathInterval)
        {
            CalculatePath();
            lastRepathTime = Time.time;
        }

        MoveAlongPath();
    }

    void CalculatePath()
    {
        Node startNode = nodeGenerator.GetClosestNode(transform.position);
        Node endNode = nodeGenerator.GetClosestNode(target.position);

        if (startNode != null && endNode != null)
        {
            currentPath = Dijkstra(startNode, endNode);
            
            // Reset index jika path baru ditemukan
            // Kita cari node terdekat di path baru yang ada di depan kita
            targetIndex = 0;
            if (currentPath.Count > 1)
            {
                targetIndex = 1; // Mulai dari node kedua (node pertama adalah posisi start)
            }
        }
    }

    void MoveAlongPath()
    {
        if (currentPath == null || currentPath.Count == 0) return;
        if (targetIndex >= currentPath.Count) return;

        Node targetNode = currentPath[targetIndex];
        Vector3 destination = targetNode.position;

        // Gerak ke tujuan
        Vector3 direction = (destination - transform.position).normalized;
        
        // Abaikan sumbu Y jika tidak sedang di tangga (opsional, biar smooth di lantai datar)
        if (targetNode.type != NodeType.Stair)
        {
            direction.y = 0;
        }

        if (direction != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, rotationSpeed * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(transform.position, destination, moveSpeed * Time.deltaTime);

        // Cek apakah sudah sampai node
        if (Vector3.Distance(transform.position, destination) < reachThreshold)
        {
            targetIndex++;
        }
    }

    // Algoritma Dijkstra
    List<Node> Dijkstra(Node start, Node end)
    {
        Dictionary<Node, float> distances = new Dictionary<Node, float>();
        Dictionary<Node, Node> previous = new Dictionary<Node, Node>();
        List<Node> unvisited = new List<Node>();

        // Inisialisasi
        foreach (Node node in nodeGenerator.allNodes)
        {
            distances[node] = Mathf.Infinity;
            previous[node] = null;
            unvisited.Add(node);
        }

        distances[start] = 0;

        while (unvisited.Count > 0)
        {
            // Sort manual atau pakai Priority Queue (disini manual sort list sederhana)
            unvisited.Sort((a, b) => distances[a].CompareTo(distances[b]));
            Node current = unvisited[0];
            unvisited.RemoveAt(0);

            if (current == end) break;
            if (distances[current] == Mathf.Infinity) break; // Sisa node tidak terjangkau

            foreach (Node neighbor in current.neighbors)
            {
                if (!unvisited.Contains(neighbor)) continue;

                float dist = Vector3.Distance(current.position, neighbor.position);
                
                // Tambahkan cost extra untuk tipe tertentu jika perlu
                // misal: tangga lebih berat cost-nya biar gak dipake kalau gak perlu
                // if (neighbor.type == NodeType.Stair) dist += 5.0f;

                float newDist = distances[current] + dist;

                if (newDist < distances[neighbor])
                {
                    distances[neighbor] = newDist;
                    previous[neighbor] = current;
                }
            }
        }

        // Reconstruct Path
        List<Node> path = new List<Node>();
        Node curr = end;
        while (curr != null)
        {
            path.Add(curr);
            curr = previous[curr];
        }
        path.Reverse();
        return path;
    }
    
    void OnDrawGizmos()
    {
        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i].position, currentPath[i+1].position);
            }
        }
    }
}
