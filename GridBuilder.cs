// GridBuilder.cs - RAYCAST-BASED FLOOR DETECTION VERSION
// 🚀 Generates grid ONLY inside labyrinth shape using downward raycasts
//
// KEY FEATURES:
// ✓ Raycast from +5 units above each cell to detect "Floor" tagged colliders
// ✓ ONLY creates grid cells that hit actual floor surfaces
// ✓ Skips generating cells that don't hit floor - NO unwalkable cells stored
// ✓ Stores ONLY valid cells in List<Vector3> validCells
// ✓ GetRandomValidCell() for guaranteed valid spawn points
// ✓ Grid exactly follows labyrinth floor silhouette
// ✓ Visual debug showing ONLY valid cells (green dots)
//
// SETUP REQUIREMENTS:
// 1. Tag all labyrinth floor colliders as "Floor"
// 2. Set gridWidth/gridHeight large enough to cover entire labyrinth area
// 3. Set nodeSpacing to desired resolution (smaller = more accurate, 1.0 recommended)
// 4. Raycasts start from +5 units above each cell position

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
    [Tooltip("Height above each cell to start raycast (recommend 5-10 units)")]
    public float raycastStartOffset = 5f;
    
    [Tooltip("Maximum distance to raycast downward")]
    public float raycastMaxDistance = 50f;
    
    [Tooltip("Use adaptive raycast height (recommended for stairs/slopes)")]
    public bool useAdaptiveHeight = true;
    
    [Tooltip("Additional height for adaptive raycast (for stairs)")]
    public float adaptiveHeightBonus = 10f;
    
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
    public Node[,] grid;  // 2D grid (contains ONLY valid floor nodes, others are null)
    
    [HideInInspector]
    public List<Vector3> validCells = new List<Vector3>();  // 🚀 List of ONLY valid floor positions
    
    [HideInInspector]
    public List<Vector3> walkableCells = new List<Vector3>();  // Alias for compatibility (same as validCells)
    
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
        
        LogDebug($"Auto-detected grid: Origin={gridOrigin}, Size={gridWidth}x{gridHeight}");
    }
    
    /// <summary>
    /// 🚀 MAIN GRID CREATION - Uses raycasts to detect floor surfaces
    /// ONLY creates nodes where valid floor exists, skips all others
    /// </summary>
    void CreateGrid()
    {
        grid = new Node[gridWidth, gridHeight];
        validCells.Clear();
        walkableCells.Clear();
        
        int totalScanned = gridWidth * gridHeight;
        int validCount = 0;
        int skipCount = 0;
        
        LogDebug($"Starting grid generation: Scanning {totalScanned} potential cells...");
        
        // Scan each grid position with downward raycast
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                // Calculate cell center position at ground level
                Vector3 cellCenter = gridOrigin + new Vector3(
                    x * nodeSpacing + (nodeSpacing * 0.5f),
                    0f,
                    z * nodeSpacing + (nodeSpacing * 0.5f)
                );
                
                // Calculate raycast start height (adaptive for stairs)
                float actualStartHeight = raycastStartOffset;
                
                if (useAdaptiveHeight && autoDetectBounds && labyrinthParent != null)
                {
                    // Use higher start point to catch stairs/elevated surfaces
                    actualStartHeight = raycastStartOffset + adaptiveHeightBonus;
                }
                
                // Raycast from calculated height above this cell position
                Vector3 raycastStart = cellCenter + Vector3.up * actualStartHeight;
                
                // Perform downward raycast to detect floor
                RaycastHit hit;
                bool hitSomething = Physics.Raycast(raycastStart, Vector3.down, out hit, raycastMaxDistance);
                
                // Debug sample raycasts
                if ((x == 0 && z == 0) || (x == gridWidth/2 && z == gridHeight/2))
                {
                    if (hitSomething)
                    {
                        Debug.Log($"[GridBuilder] Sample raycast [{x},{z}]: HIT {hit.collider.name} at {hit.point}, Tag={hit.collider.tag}");
                    }
                    else
                    {
                        Debug.Log($"[GridBuilder] Sample raycast [{x},{z}]: NO HIT from {raycastStart}");
                    }
                }
                
                // Check if hit is valid floor
                if (hitSomething && IsValidFloor(hit))
                {
                    // ✓ Valid floor found! Create node and add to valid cells
                    Vector3 floorPosition = hit.point;
                    
                    grid[x, z] = new Node(floorPosition);
                    grid[x, z].isWalkable = true;
                    
                    validCells.Add(floorPosition);
                    walkableCells.Add(floorPosition); // Alias for compatibility
                    validCount++;
                    
                    // Debug first valid cell
                    if (validCount == 1)
                    {
                        Debug.Log($"[GridBuilder] ✓ First valid cell at [{x},{z}]: {hit.collider.name}, Position={floorPosition}");
                    }
                }
                else
                {
                    // ✗ No valid floor - leave grid cell as NULL (don't create node)
                    grid[x, z] = null;
                    skipCount++;
                }
            }
        }
        
        // Connect neighbors for pathfinding (4-directional)
        ConnectNeighbors();
        
        // Log results
        float coverageRate = (validCount / (float)totalScanned) * 100f;
        LogDebug($"✓ Grid generation complete:");
        LogDebug($"  - Scanned: {totalScanned} positions ({gridWidth}x{gridHeight})");
        LogDebug($"  - Valid cells: {validCount} ({coverageRate:F1}%)");
        LogDebug($"  - Skipped: {skipCount} (no floor detected)");
        
        if (validCount == 0)
        {
            Debug.LogError("[GridBuilder] ⚠️⚠️⚠️ NO VALID CELLS FOUND!");
            Debug.LogError($"[GridBuilder] Detection method: {detectionMethod}");
            if (detectionMethod == DetectionMethod.UseTag || detectionMethod == DetectionMethod.UseBoth)
                Debug.LogError($"[GridBuilder] Floor Tag: '{floorTag}' - Make sure floor objects have this tag!");
            if (detectionMethod == DetectionMethod.UseLayerMask || detectionMethod == DetectionMethod.UseBoth)
                Debug.LogError($"[GridBuilder] Floor Layer: {floorLayer.value} - Make sure floor objects are on this layer!");
            Debug.LogError($"[GridBuilder] Raycast: Start offset={raycastStartOffset}, MaxDistance={raycastMaxDistance}");
            Debug.LogError($"[GridBuilder] Grid: Origin={gridOrigin}, Size={gridWidth}x{gridHeight}, Spacing={nodeSpacing}");
        }
        else if (validCount < 10)
        {
            Debug.LogWarning($"[GridBuilder] ⚠️ Only {validCount} valid cells found - might be too few for spawning!");
        }
    }
    
    /// <summary>
    /// Check if raycast hit is a valid floor based on detection method
    /// </summary>
    bool IsValidFloor(RaycastHit hit)
    {
        switch (detectionMethod)
        {
            case DetectionMethod.UseTag:
                return hit.collider.CompareTag(floorTag);
                
            case DetectionMethod.UseLayerMask:
                return ((1 << hit.collider.gameObject.layer) & floorLayer) != 0;
                
            case DetectionMethod.UseBoth:
                bool hasTag = hit.collider.CompareTag(floorTag);
                bool onLayer = ((1 << hit.collider.gameObject.layer) & floorLayer) != 0;
                return hasTag || onLayer;
                
            default:
                return false;
        }
    }
    
    /// <summary>
    /// Connect valid nodes to their valid neighbors (4-directional)
    /// Only connects nodes that exist (non-null)
    /// </summary>
    void ConnectNeighbors()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                // Skip if no valid node exists at this position
                if (grid[x, z] == null)
                    continue;
                
                List<Node> neighborsList = new List<Node>();
                
                // Right
                if (x + 1 < gridWidth && grid[x + 1, z] != null)
                    neighborsList.Add(grid[x + 1, z]);
                
                // Left
                if (x - 1 >= 0 && grid[x - 1, z] != null)
                    neighborsList.Add(grid[x - 1, z]);
                
                // Up (Z+)
                if (z + 1 < gridHeight && grid[x, z + 1] != null)
                    neighborsList.Add(grid[x, z + 1]);
                
                // Down (Z-)
                if (z - 1 >= 0 && grid[x, z - 1] != null)
                    neighborsList.Add(grid[x, z - 1]);
                
                grid[x, z].neighbors = neighborsList;
            }
        }
    }
    
    // ==================== PUBLIC API ====================
    
    /// <summary>
    /// 🚀 Get random valid cell for spawning objects
    /// Returns a random position from ONLY valid floor cells
    /// </summary>
    /// <param name="heightOffset">Y-axis offset above floor (e.g., 0.5f for items, 1.0f for player)</param>
    /// <returns>Valid spawn position, or Vector3.zero if no valid cells exist</returns>
    public Vector3 GetRandomValidCell(float heightOffset = 0f)
    {
        if (validCells.Count == 0)
        {
            Debug.LogError("[GridBuilder] No valid cells available! Cannot get random position.");
            return Vector3.zero;
        }
        
        Vector3 randomCell = validCells[Random.Range(0, validCells.Count)];
        return randomCell + Vector3.up * heightOffset;
    }
    
    /// <summary>
    /// Alias for compatibility - same as GetRandomValidCell
    /// </summary>
    public Vector3 GetRandomWalkablePosition(float heightOffset = 0f)
    {
        return GetRandomValidCell(heightOffset);
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
    /// Get all valid cell positions
    /// </summary>
    public List<Vector3> GetAllValidCells()
    {
        return new List<Vector3>(validCells);
    }
    
    /// <summary>
    /// Alias for compatibility
    /// </summary>
    public List<Vector3> GetAllWalkablePositions()
    {
        return GetAllValidCells();
    }
    
    /// <summary>
    /// Get count of valid cells
    /// </summary>
    public int GetValidCellCount()
    {
        return validCells.Count;
    }
    
    /// <summary>
    /// Alias for compatibility
    /// </summary>
    public int GetWalkableCellCount()
    {
        return GetValidCellCount();
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
        if (!showGizmos) return;
        
        // Draw ONLY valid cells (green dots)
        if (validCells != null && validCells.Count > 0)
        {
            Gizmos.color = walkableColor;
            foreach (Vector3 cell in validCells)
            {
                Gizmos.DrawSphere(cell + Vector3.up * 0.1f, gizmoSize);
            }
        }
        
        // Draw neighbor connections if grid exists
        if (grid != null && !showOnlyWalkable)
        {
            Gizmos.color = new Color(0, 1, 1, 0.2f);
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridHeight; z++)
                {
                    if (grid[x, z] != null && grid[x, z].neighbors != null)
                    {
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
        }
        
        // Draw scan area bounds (yellow wireframe)
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Vector3 center = gridOrigin + new Vector3(gridWidth * nodeSpacing * 0.5f, 0, gridHeight * nodeSpacing * 0.5f);
        Vector3 size = new Vector3(gridWidth * nodeSpacing, 0.1f, gridHeight * nodeSpacing);
        Gizmos.DrawWireCube(center, size);
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        // Draw sample raycast lines when selected
        Gizmos.color = new Color(1, 1, 0, 0.15f);
        
        int step = Mathf.Max(1, gridWidth / 10);
        for (int x = 0; x < gridWidth; x += step)
        {
            for (int z = 0; z < gridHeight; z += step)
            {
                Vector3 cellCenter = gridOrigin + new Vector3(
                    x * nodeSpacing + (nodeSpacing * 0.5f),
                    0f,
                    z * nodeSpacing + (nodeSpacing * 0.5f)
                );
                Vector3 rayStart = cellCenter + Vector3.up * raycastStartOffset;
                Vector3 rayEnd = rayStart + Vector3.down * raycastMaxDistance;
                Gizmos.DrawLine(rayStart, rayEnd);
            }
        }
    }
}
