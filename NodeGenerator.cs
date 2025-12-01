using UnityEngine;
using System.Collections.Generic;

public class NodeGenerator : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector3 gridOrigin = Vector3.zero;
    public int gridWidth = 50;
    public int gridHeight = 50;
    public float nodeSpacing = 1f;

    [Header("Raycast Settings")]
    public LayerMask floorLayer;
    public LayerMask obstacleLayer; // Layer untuk dinding/halangan
    public float raycastHeight = 10f;

    [Header("Special Nodes")]
    public bool detectStairs = true;
    public bool detectDoors = true;
    public string doorTag = "Door";

    [Header("Debug")]
    public bool showGizmos = true;
    public float nodeSize = 0.3f;

    // Grid penyimpanan sementara untuk akses cepat
    private Node[,] nodeGrid;
    
    // List final semua node dalam graph
    public List<Node> allNodes = new List<Node>();

    void Awake()
    {
        GenerateGraph();
    }

    [ContextMenu("Generate Graph")]
    public void GenerateGraph()
    {
        allNodes.Clear();
        nodeGrid = new Node[gridWidth, gridHeight];

        // 1. Generate Base Grid (Walkable Nodes)
        GenerateGridNodes();

        // 2. Connect Grid Nodes (with Wall Check)
        ConnectGridNodes();

        // 3. Integrate Stairs (Stair Nodes)
        if (detectStairs)
            IntegrateStairs();

        // 4. Mark Special Nodes (Doors, Corners)
        // (Optional: Logic untuk mendeteksi corner/junction berdasarkan jumlah neighbor)
        
        Debug.Log($"[NodeGenerator] Graph generated with {allNodes.Count} nodes.");
    }

    void GenerateGridNodes()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 pos = gridOrigin + new Vector3(x * nodeSpacing, 0, z * nodeSpacing);
                
                // Raycast ke bawah untuk cari lantai
                if (Physics.Raycast(pos + Vector3.up * raycastHeight, Vector3.down, out RaycastHit hit, raycastHeight * 2, floorLayer))
                {
                    // Cek apakah posisi ini tertutup obstacle (misal di dalam dinding)
                    if (!Physics.CheckSphere(hit.point + Vector3.up * 0.5f, 0.2f, obstacleLayer))
                    {
                        Node newNode = new Node(hit.point, NodeType.Walkable);
                        
                        // Cek Door
                        if (detectDoors)
                        {
                            // Cek radius kecil untuk object bertag Door
                            Collider[] hitColliders = Physics.OverlapSphere(hit.point, nodeSpacing / 2);
                            foreach (var col in hitColliders)
                            {
                                if (col.CompareTag(doorTag))
                                {
                                    newNode.type = NodeType.Door;
                                    break;
                                }
                            }
                        }

                        nodeGrid[x, z] = newNode;
                        allNodes.Add(newNode);
                    }
                }
            }
        }
    }

    void ConnectGridNodes()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Node currentNode = nodeGrid[x, z];
                if (currentNode == null) continue;

                // Cek 4 arah (Atas, Bawah, Kiri, Kanan)
                CheckAndConnect(currentNode, x + 1, z); // Kanan
                CheckAndConnect(currentNode, x - 1, z); // Kiri
                CheckAndConnect(currentNode, x, z + 1); // Atas
                CheckAndConnect(currentNode, x, z - 1); // Bawah
            }
        }
    }

    void CheckAndConnect(Node nodeA, int x, int z)
    {
        if (x < 0 || x >= gridWidth || z < 0 || z >= gridHeight) return;

        Node nodeB = nodeGrid[x, z];
        if (nodeB == null) return;

        // Cek Obstacle antara Node A dan Node B
        Vector3 dir = nodeB.position - nodeA.position;
        float dist = dir.magnitude;
        
        // Raycast/Linecast untuk memastikan tidak tembus dinding
        // Kita angkat sedikit raycastnya agar tidak kena lantai
        Vector3 start = nodeA.position + Vector3.up * 0.5f;
        Vector3 end = nodeB.position + Vector3.up * 0.5f;

        if (!Physics.Linecast(start, end, obstacleLayer))
        {
            // Koneksi valid
            if (!nodeA.neighbors.Contains(nodeB))
                nodeA.neighbors.Add(nodeB);
            
            if (!nodeB.neighbors.Contains(nodeA))
                nodeB.neighbors.Add(nodeA);
        }
    }

    void IntegrateStairs()
    {
        StairTrigger[] stairs = FindObjectsOfType<StairTrigger>();
        foreach (var stair in stairs)
        {
            Vector3 bottomPos = stair.GetBottomPoint();
            Vector3 topPos = stair.GetTopPoint();

            // Cari node terdekat di grid untuk bottom dan top
            Node bottomNode = GetClosestNode(bottomPos, 1.5f); // Radius toleransi
            Node topNode = GetClosestNode(topPos, 1.5f);

            if (bottomNode != null && topNode != null)
            {
                // Buat koneksi langsung antara node bawah dan atas
                // Ini memungkinkan AI "melompat" atau "memanjat" via pathfinding
                
                // Opsional: Buat node baru khusus tangga jika node grid terlalu jauh
                // Tapi menghubungkan node grid terdekat biasanya cukup
                
                if (!bottomNode.neighbors.Contains(topNode))
                {
                    bottomNode.neighbors.Add(topNode);
                    bottomNode.type = NodeType.Stair; // Mark as stair access
                }

                if (!topNode.neighbors.Contains(bottomNode))
                {
                    topNode.neighbors.Add(bottomNode);
                    topNode.type = NodeType.Stair;
                }
                
                Debug.Log($"[NodeGenerator] Connected Stair: {stair.name}");
            }
        }
    }

    public Node GetClosestNode(Vector3 pos, float maxDist = Mathf.Infinity)
    {
        Node closest = null;
        float minDist = maxDist;

        foreach (var node in allNodes)
        {
            float d = Vector3.Distance(pos, node.position);
            if (d < minDist)
            {
                minDist = d;
                closest = node;
            }
        }
        return closest;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || allNodes == null) return;

        foreach (var node in allNodes)
        {
            // Warna berdasarkan tipe
            switch (node.type)
            {
                case NodeType.Walkable: Gizmos.color = Color.green; break;
                case NodeType.Door: Gizmos.color = Color.blue; break;
                case NodeType.Stair: Gizmos.color = Color.yellow; break;
                default: Gizmos.color = Color.white; break;
            }

            Gizmos.DrawSphere(node.position, nodeSize);

            // Gambar koneksi
            Gizmos.color = Color.white;
            foreach (var neighbor in node.neighbors)
            {
                if (neighbor != null)
                    Gizmos.DrawLine(node.position, neighbor.position);
            }
        }
    }
}
