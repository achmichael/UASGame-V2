// GridBuilder.cs - RAYCAST-BASED FLOOR DETECTION VERSION
// 🚀 Generates grid ONLY inside labyrinth shape using downward raycasts
//
// KEY FEATURES:
// ✓ Raycast from above to detect "Floor" tagged colliders
// ✓ Only creates grid cells that hit actual floor surfaces
// ✓ Stores walkable world positions in List<Vector3> for fast spawning
// ✓ GetRandomWalkablePosition() for guaranteed valid spawn points
// ✓ Perfect labyrinth shape following (no bounding box approximation)
// ✓ Visual debug showing only walkable tiles
//
// SETUP REQUIREMENTS:
// 1. Tag all labyrinth floor colliders as "Floor"
// 2. Set gridWidth/gridHeight large enough to cover entire labyrinth area
// 3. Set nodeSpacing to desired resolution (smaller = more accurate)
// 4. Set raycastHeight high enough to be above all floor surfaces
// 5. Adjust raycastMaxDistance if floors are very deep

using System.Collections.Generic;
using UnityEngine;

public class GridBuilder : MonoBehaviour
{
    [Header("Grid Scan Settings")]
    [Tooltip("Width of scan area (number of cells in X axis)")]
    public int gridWidth = 50;
    
    [Tooltip("Height of scan area (number of cells in Z axis)")]
    public int gridHeight = 50;
    
    [Tooltip("Distance between raycast sample points")]
    public float nodeSpacing = 1f;
    
    [Tooltip("Grid origin (bottom-left corner of scan area)")]
    public Vector3 gridOrigin = Vector3.zero;
    
    [Header("Raycast Detection")]
    [Tooltip("Height above grid origin to start raycasts from")]
    public float raycastHeight = 50f;
    
    [Tooltip("Maximum distance to raycast downward")]
    public float raycastMaxDistance = 100f;
    
    [Tooltip("Radius for node representation")]
    public float nodeRadius = 0.4f;
    
    // Enum for floor detection method
    public enum DetectionMethod { UseTag, UseLayerMask, UseBoth }
    
    [Header("Floor Detection Method")]
    [Tooltip("Use Tag or LayerMask for floor detection")]
    public DetectionMethod detectionMethod = DetectionMethod.UseBoth;
    
    [Tooltip("Floor objects tag (if using Tag method)")]
    public string floorTag = "Floor";
    
    [Tooltip("Floor layer mask (if using LayerMask method)")]
    public LayerMask floorLayer = -1;
    
    [Header("Auto Detection")]
    [Tooltip("Auto-detect grid bounds from labyrinth parent")]
    public bool autoDetectBounds = false;
    
    [Tooltip("Parent object containing labyrinth for auto-detect")]
    public Transform labyrinthParent;
    
    [Tooltip("Extra padding around auto-detected bounds")]
    public float autoDetectPadding = 2f;
    
    [Header("Visualization")]
    public bool showGizmos = true;
    public bool showOnlyWalkable = true;
    public Color walkableColor = new Color(0, 1, 0, 0.5f);
    public Color unwalkableColor = new Color(1, 0, 0, 0.3f);
    public float gizmoSize = 0.3f;
    
    [Header("Debug Info")]
    public bool showDebugLogs = true;
    
    // ==================== PUBLIC DATA ====================
    
    [HideInInspector]
    public Node[,] grid;  // Full 2D grid (includes non-walkable cells as null or marked unwalkable)
    
    [HideInInspector]
    public List<Vector3> walkableCells = new List<Vector3>();  // 🚀 NEW: Fast lookup for spawning
    
    // ==================== INITIALIZATION ====================
    
    void Awake()
    {
        if (autoDetectBounds && labyrinthParent != null)
        {
            AutoDetectGridSettings();
        }
        
        CreateGrid();
    }
    
    /// <summary>
    /// Auto-detect grid settings from labyrinth colliders
    /// </summary>
    void AutoDetectGridSettings()
    {
        Collider[] colliders = labyrinthParent.GetComponentsInChildren<Collider>();
        
        if (colliders.Length == 0)
        {
            Debug.LogError("[GridBuilder] Auto-detect failed: No colliders found in labyrinthParent!");
            return;
        }
        
        Bounds bounds = colliders[0].bounds;
        foreach (Collider col in colliders)
        {
            bounds.Encapsulate(col.bounds);
        }
        
        // Set grid origin to bottom-left corner with padding
        gridOrigin = new Vector3(
            bounds.min.x - autoDetectPadding,
            bounds.min.y,
            bounds.min.z - autoDetectPadding
        );
        
        // Calculate grid dimensions with padding
        float totalWidth = bounds.size.x + (autoDetectPadding * 2);
        float totalHeight = bounds.size.z + (autoDetectPadding * 2);
        
        gridWidth = Mathf.Max(10, Mathf.CeilToInt(totalWidth / nodeSpacing));
        gridHeight = Mathf.Max(10, Mathf.CeilToInt(totalHeight / nodeSpacing));
        
        // Set raycast height above highest point
        raycastHeight = bounds.max.y + 10f;
        
        LogDebug($"Auto-detected grid: Origin={gridOrigin}, Size={gridWidth}x{gridHeight}, RaycastHeight={raycastHeight}");
    }
    
    /// <summary>
    /// 🚀 MAIN GRID CREATION - Uses raycasts to detect floor surfaces
    /// Only creates walkable nodes where floor exists
    /// </summary>
    void CreateGrid()
    {
        grid = new Node[gridWidth, gridHeight];
        walkableCells.Clear();
        
        int totalCells = gridWidth * gridHeight;
        int walkableCount = 0;
        int floorHitCount = 0;
        
        LogDebug($"Starting grid generation: Scanning {totalCells} cells...");
        
        // Scan each grid cell with downward raycast
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                // Calculate world position for this grid cell (at raycast start height)
                Vector3 raycastStart = gridOrigin + new Vector3(
                    x * nodeSpacing + (nodeSpacing * 0.5f),  // Center of cell
                    raycastHeight,
                    z * nodeSpacing + (nodeSpacing * 0.5f)   // Center of cell
                );
                
                // Perform downward raycast to detect floor
                RaycastHit hit;
                bool hitSomething = Physics.Raycast(raycastStart, Vector3.down, out hit, raycastMaxDistance);
                
                // Debug untuk beberapa cell pertama
                if ((x == 0 && z == 0) || (x == gridWidth/2 && z == gridHeight/2))
                {
                    if (hitSomething)
                    {
                        Debug.Log($"[GridBuilder] Sample raycast [{x},{z}]: HIT {hit.collider.name} at {hit.point}, Tag={hit.collider.tag}, Layer={LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                    }
                    else
                    {
                        Debug.Log($"[GridBuilder] Sample raycast [{x},{z}]: NO HIT from {raycastStart}");
                    }
                }
                
                if (hitSomething)
                {
                    // Check if this is a valid floor based on detection method
                    bool isValidFloor = false;
                    
                    switch (detectionMethod)
                    {
                        case DetectionMethod.UseTag:
                            isValidFloor = hit.collider.CompareTag(floorTag);
                            break;
                            
                        case DetectionMethod.UseLayerMask:
                            isValidFloor = ((1 << hit.collider.gameObject.layer) & floorLayer) != 0;
                            break;
                            
                        case DetectionMethod.UseBoth:
                            bool hasTag = hit.collider.CompareTag(floorTag);
                            bool onLayer = ((1 << hit.collider.gameObject.layer) & floorLayer) != 0;
                            isValidFloor = hasTag || onLayer;
                            break;
                    }
                    
                    if (isValidFloor)
                    {
                        // Valid floor found! Create walkable node at floor surface
                        Vector3 floorPosition = hit.point;
                        
                        grid[x, z] = new Node(floorPosition);
                        grid[x, z].isWalkable = true;
                        
                        walkableCells.Add(floorPosition);
                        walkableCount++;
                        floorHitCount++;
                        
                        // Debug first valid floor
                        if (walkableCount == 1)
                        {
                            Debug.Log($"[GridBuilder] ✓ First valid floor found at [{x},{z}]: {hit.collider.name}, Position={floorPosition}");
                        }
                    }
                    else
                    {
                        // Hit something but not a valid floor
                        grid[x, z] = new Node(hit.point);
                        grid[x, z].isWalkable = false;
                        
                        // Debug first non-floor hit
                        if (walkableCount == 0 && floorHitCount == 0)
                        {
                            Debug.LogWarning($"[GridBuilder] ⚠️ Hit non-floor object '{hit.collider.name}' (Tag={hit.collider.tag}, Layer={LayerMask.LayerToName(hit.collider.gameObject.layer)})");
                        }
                    }
                }
                else
                {
                    // No hit at all - create unwalkable node
                    Vector3 defaultPos = gridOrigin + new Vector3(x * nodeSpacing, 0, z * nodeSpacing);
                    grid[x, z] = new Node(defaultPos);
                    grid[x, z].isWalkable = false;
                }
            }
        }
        
        // Connect neighbors for pathfinding (4-directional)
        ConnectNeighbors();
        
        // Log results
        float successRate = (walkableCount / (float)totalCells) * 100f;
        LogDebug($"✓ Grid generation complete:");
        LogDebug($"  - Scanned: {totalCells} cells ({gridWidth}x{gridHeight})");
        LogDebug($"  - Walkable: {walkableCount} cells ({successRate:F1}%)");
        LogDebug($"  - Floor hits: {floorHitCount}");
        LogDebug($"  - Non-floor: {totalCells - walkableCount}");
        
        if (walkableCount == 0)
        {
            Debug.LogError("[GridBuilder] ⚠️⚠️⚠️ NO WALKABLE CELLS FOUND!");
            Debug.LogError($"[GridBuilder] Detection method: {detectionMethod}");
            if (detectionMethod == DetectionMethod.UseTag || detectionMethod == DetectionMethod.UseBoth)
                Debug.LogError($"[GridBuilder] Floor Tag: '{floorTag}' - Make sure floor objects have this tag!");
            if (detectionMethod == DetectionMethod.UseLayerMask || detectionMethod == DetectionMethod.UseBoth)
                Debug.LogError($"[GridBuilder] Floor Layer: {floorLayer.value} - Make sure floor objects are on this layer!");
            Debug.LogError($"[GridBuilder] Raycast: Start Y={raycastHeight}, MaxDistance={raycastMaxDistance}");
            Debug.LogError($"[GridBuilder] Grid: Origin={gridOrigin}, Size={gridWidth}x{gridHeight}, Spacing={nodeSpacing}");
            Debug.LogError($"[GridBuilder] Total raycasts performed: {totalCells}");
        }
        else if (walkableCount < 10)
        {
            Debug.LogWarning($"[GridBuilder] ⚠️ Only {walkableCount} walkable cells found - might be too few for spawning!");
        }
    }
    
    /// <summary>
    /// Connect walkable nodes to their walkable neighbors (4-directional)
    /// </summary>
    void ConnectNeighbors()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (grid[x, z] == null || !grid[x, z].isWalkable)
                    continue;
                
                List<Node> neighborsList = new List<Node>();
                
                // Right
                if (x + 1 < gridWidth && grid[x + 1, z] != null && grid[x + 1, z].isWalkable)
                    neighborsList.Add(grid[x + 1, z]);
                
                // Left
                if (x - 1 >= 0 && grid[x - 1, z] != null && grid[x - 1, z].isWalkable)
                    neighborsList.Add(grid[x - 1, z]);
                
                // Up (Z+)
                if (z + 1 < gridHeight && grid[x, z + 1] != null && grid[x, z + 1].isWalkable)
                    neighborsList.Add(grid[x, z + 1]);
                
                // Down (Z-)
                if (z - 1 >= 0 && grid[x, z - 1] != null && grid[x, z - 1].isWalkable)
                    neighborsList.Add(grid[x, z - 1]);
                
                grid[x, z].neighbors = neighborsList.ToArray();
            }
        }
    }
    
    // ==================== PUBLIC API ====================
    
    /// <summary>
    /// 🚀 Get random walkable position for spawning objects
    /// Guarantees position is on actual floor surface inside labyrinth
    /// </summary>
    /// <param name="heightOffset">Y-axis offset above floor (e.g., 0.5f for items, 1.0f for player)</param>
    /// <returns>Valid spawn position, or Vector3.zero if no walkable cells exist</returns>
    public Vector3 GetRandomWalkablePosition(float heightOffset = 0f)
    {
        if (walkableCells.Count == 0)
        {
            Debug.LogError("[GridBuilder] No walkable cells available! Cannot get random position.");
            return Vector3.zero;
        }
        
        Vector3 randomCell = walkableCells[Random.Range(0, walkableCells.Count)];
        return randomCell + Vector3.up * heightOffset;
    }
    
    /// <summary>
    /// Get node from world position (nearest node lookup)
    /// </summary>
    public Node GetNodeFromWorldPosition(Vector3 worldPosition)
    {
        // Convert world position to grid coordinates
        float percentX = (worldPosition.x - gridOrigin.x) / (gridWidth * nodeSpacing);
        float percentZ = (worldPosition.z - gridOrigin.z) / (gridHeight * nodeSpacing);
        
        percentX = Mathf.Clamp01(percentX);
        percentZ = Mathf.Clamp01(percentZ);
        
        int x = Mathf.RoundToInt((gridWidth - 1) * percentX);
        int z = Mathf.RoundToInt((gridHeight - 1) * percentZ);
        
        x = Mathf.Clamp(x, 0, gridWidth - 1);
        z = Mathf.Clamp(z, 0, gridHeight - 1);
        
        return grid[x, z];
    }
    
    /// <summary>
    /// Check if a world position is on walkable floor
    /// </summary>
    public bool IsPositionWalkable(Vector3 worldPosition)
    {
        Node node = GetNodeFromWorldPosition(worldPosition);
        return node != null && node.isWalkable;
    }
    
    /// <summary>
    /// Get all walkable positions (useful for debugging or advanced spawning)
    /// </summary>
    public List<Vector3> GetAllWalkablePositions()
    {
        return new List<Vector3>(walkableCells);
    }
    
    /// <summary>
    /// Get count of walkable cells
    /// </summary>
    public int GetWalkableCellCount()
    {
        return walkableCells.Count;
    }
    
    /// <summary>
    /// Force refresh grid (call if labyrinth is modified at runtime)
    /// </summary>
    public void RefreshGrid()
    {
        CreateGrid();
    }
    
    // ==================== UTILITIES ====================
    
    void LogDebug(string message)
    {
        if (showDebugLogs)
            Debug.Log($"[GridBuilder] {message}");
    }
    
    // ==================== VISUALIZATION ====================
    
    void OnDrawGizmos()
    {
        if (!showGizmos || grid == null) return;
        
        // Draw all grid cells
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                if (grid[x, z] == null) continue;
                
                bool isWalkable = grid[x, z].isWalkable;
                
                // Skip unwalkable if only showing walkable
                if (!isWalkable && showOnlyWalkable) continue;
                
                Gizmos.color = isWalkable ? walkableColor : unwalkableColor;
                Gizmos.DrawCube(grid[x, z].position + Vector3.up * 0.1f, Vector3.one * gizmoSize);
                
                // Draw neighbor connections for walkable nodes
                if (isWalkable && grid[x, z].neighbors != null)
                {
                    Gizmos.color = new Color(0, 1, 1, 0.2f);
                    foreach (Node neighbor in grid[x, z].neighbors)
                    {
                        if (neighbor != null)
                        {
                            Gizmos.DrawLine(grid[x, z].position, neighbor.position);
                        }
                    }
                }
            }
        }
        
        // Draw grid bounds
        Gizmos.color = Color.yellow;
        Vector3 center = gridOrigin + new Vector3(gridWidth * nodeSpacing * 0.5f, 0, gridHeight * nodeSpacing * 0.5f);
        Vector3 size = new Vector3(gridWidth * nodeSpacing, 0.1f, gridHeight * nodeSpacing);
        Gizmos.DrawWireCube(center, size);
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        // Draw raycast visualization when selected
        Gizmos.color = new Color(1, 1, 0, 0.1f);
        
        // Show a few sample raycasts
        for (int x = 0; x < gridWidth; x += Mathf.Max(1, gridWidth / 10))
        {
            for (int z = 0; z < gridHeight; z += Mathf.Max(1, gridHeight / 10))
            {
                Vector3 rayStart = gridOrigin + new Vector3(x * nodeSpacing, raycastHeight, z * nodeSpacing);
                Vector3 rayEnd = rayStart + Vector3.down * raycastMaxDistance;
                Gizmos.DrawLine(rayStart, rayEnd);
            }
        }
    }
}
