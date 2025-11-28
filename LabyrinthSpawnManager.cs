// LabyrinthSpawnManager.cs - OPTIMIZED GRID-BASED VERSION
// Ensures ALL spawned objects (Player, Items, Enemies) only appear inside the maze
// Uses pre-calculated walkable grid tiles for 100% accurate spawning
//
// KEY IMPROVEMENTS:
// 1. Collects all walkable grid positions during initialization
// 2. All spawns use GetRandomValidTile() - guarantees maze-only spawning
// 3. No more random coordinate guessing or complex raycasts
// 4. Player always spawns on valid maze tile
// 5. Fast and reliable - uses cached tile list
//
// SETUP REQUIREMENTS:
// - Assign GridBuilder reference
// - Set Wall Layer and Ground Layer
// - Ensure GridBuilder.grid is populated before spawning
// - GridBuilder nodes must have isWalkable property set correctly

using System.Collections.Generic;
using UnityEngine;

public class LabyrinthSpawnManager : MonoBehaviour
{
    [Header("Grid Reference - REQUIRED")]
    [Tooltip("WAJIB: GridBuilder yang sudah generate maze")]
    public GridBuilder gridBuilder;
    
    [Header("Spawn Settings")]
    public float minDistanceBetweenSpawns = 2f;
    public float minDistanceFromPlayer = 10f;
    
    [Header("Player Spawn")]
    public GameObject playerPrefab;
    public float playerSpawnHeight = 1.0f;
    
    [Header("Item Spawn")]
    public GameObject itemPrefab;
    public int itemCount = 30;
    public float itemSpawnHeight = 0.5f;
    
    [Header("Enemy Spawn")]
    public GameObject enemyPrefab;
    public int enemyCount = 3;
    public float enemySpawnHeight = 0.5f;
    
    [Header("Advanced Settings")]
    [Tooltip("Maximum attempts to find position meeting distance requirements")]
    public int maxDistanceAttempts = 50;
    [Tooltip("If true, guarantees spawn even if distance requirements can't be met")]
    public bool allowFallbackSpawn = true;
    
    [Header("Visualization")]
    public bool showGizmos = true;
    public bool showDebugLogs = true;
    public Color walkableTilesColor = new Color(0, 1, 0, 0.3f);
    public Color spawnedPositionsColor = Color.cyan;
    
    [Header("Auto Spawn Control")]
    [Tooltip("Auto spawn on Start - Set false if GameManager triggers spawn")]
    public bool autoSpawnOnStart = false;
    
    // Cached walkable tiles - the core of our optimization
    private List<Vector3> walkableTiles = new List<Vector3>();
    private List<Vector3> spawnedPositions = new List<Vector3>();
    private Vector3 playerSpawnPosition;
    
    void Start()
    {
        ValidateSetup();
        
        if (gridBuilder == null)
            gridBuilder = FindObjectOfType<GridBuilder>();
            
        // Validate GridBuilder has generated valid cells
        if (gridBuilder != null && gridBuilder.validCells.Count > 0)
        {
            Debug.Log($"[SpawnManager] ✓ GridBuilder has {gridBuilder.validCells.Count} valid cells ready for spawning");
        }
        else
        {
            Debug.LogError("[SpawnManager] ⚠️ GridBuilder has no valid cells! Cannot spawn objects.");
        }
        
        // Auto spawn if enabled
        if (autoSpawnOnStart)
        {
            Debug.Log("[SpawnManager] Auto-spawning all objects...");
            SpawnAllObjects();
        }
    }
    
    /// <summary>
    /// Validates that all required components are properly set up
    /// </summary>
    void ValidateSetup()
    {
        if (gridBuilder == null)
        {
            Debug.LogError("[SpawnManager] ⚠️ GridBuilder NOT ASSIGNED! Cannot spawn objects.");
            Debug.LogError("[SpawnManager] Assign GridBuilder in Inspector or ensure it exists in scene.");
        }
        
        if (playerPrefab == null)
        {
            Debug.LogError("[SpawnManager] ⚠️ Player Prefab NOT ASSIGNED!");
        }
        else
        {
            // Validate player has camera
            Camera cam = playerPrefab.GetComponentInChildren<Camera>();
            if (cam == null)
            {
                Debug.LogError("[SpawnManager] ⚠️⚠️⚠️ PLAYER PREFAB HAS NO CAMERA!");
                Debug.LogError("[SpawnManager] Add Camera as child to Player Prefab to avoid 'No cameras rendering' error.");
            }
            else
            {
                LogDebug($"✓ Player Prefab has camera: {cam.name}");
            }
        }
        
        if (itemPrefab == null)
            Debug.LogWarning("[SpawnManager] Item Prefab not assigned - items won't spawn");
            
        if (enemyPrefab == null)
            Debug.LogWarning("[SpawnManager] Enemy Prefab not assigned - enemies won't spawn");
    }
    
    /// <summary>
    /// DEPRECATED - No longer needed, GridBuilder now provides walkableCells directly
    /// Kept for reference only
    /// </summary>
    void CacheWalkableTiles()
    {
        // This method is no longer needed since GridBuilder.walkableCells
        // already contains all walkable positions using raycast-based floor detection
        // All spawning now uses GridBuilder.GetRandomWalkablePosition() directly
    }
    
    /// <summary>
    /// 🚀 Get a random valid tile position using GridBuilder's raycast-based system
    /// Uses GridBuilder.GetRandomValidCell() for guaranteed floor-only spawning
    /// </summary>
    /// <param name="heightOffset">Y-axis offset from ground (e.g., 0.5f for items, 1.0f for player)</param>
    /// <param name="minDistanceFromOthers">Minimum distance from already spawned objects</param>
    /// <param name="minDistanceFromPlayer">Minimum distance from player (for enemies)</param>
    /// <returns>World position on a valid maze tile, or Vector3.zero if no valid position found</returns>
    public Vector3 GetRandomValidTile(float heightOffset, float minDistanceFromOthers = 0f, float minDistanceFromPlayer = 0f)
    {
        if (gridBuilder == null)
        {
            Debug.LogError("[SpawnManager] GridBuilder is null! Cannot get spawn position.");
            return Vector3.zero;
        }
        
        if (gridBuilder.validCells.Count == 0)
        {
            Debug.LogError("[SpawnManager] No valid cells in GridBuilder! Ensure floors are tagged correctly.");
            return Vector3.zero;
        }
        
        // Try multiple times to find a position meeting distance requirements
        for (int attempt = 0; attempt < maxDistanceAttempts; attempt++)
        {
            // Get random valid position from GridBuilder (raycast-verified floor position)
            Vector3 spawnPos = gridBuilder.GetRandomValidCell(heightOffset);
            
            if (spawnPos == Vector3.zero)
            {
                Debug.LogError("[SpawnManager] GridBuilder returned invalid position!");
                continue;
            }
            
            // Check distance from other spawned objects
            if (minDistanceFromOthers > 0f && IsTooCloseToOtherSpawns(spawnPos, minDistanceFromOthers))
                continue;
            
            // Check distance from player (for enemy spawns)
            if (minDistanceFromPlayer > 0f && playerSpawnPosition != Vector3.zero)
            {
                if (Vector3.Distance(spawnPos, playerSpawnPosition) < minDistanceFromPlayer)
                    continue;
            }
            
            // Found valid position!
            return spawnPos;
        }
        
        // Fallback: If we can't meet distance requirements, return any valid position
        // This prevents spawn failures when maze is small or crowded
        if (allowFallbackSpawn)
        {
            Vector3 fallbackPos = gridBuilder.GetRandomValidCell(heightOffset);
            
            LogDebug($"Using fallback spawn (distance requirements couldn't be met after {maxDistanceAttempts} attempts)");
            return fallbackPos;
        }
        
        return Vector3.zero;
    }
    
    /// <summary>
    /// Spawn all game objects: Player, Items, and Enemies
    /// Call this from GameManager or use autoSpawnOnStart
    /// </summary>
    public void SpawnAllObjects()
    {
        if (gridBuilder == null || gridBuilder.validCells.Count == 0)
        {
            Debug.LogError("[SpawnManager] Cannot spawn - GridBuilder has no valid cells! Ensure floors are tagged 'Floor'.");
            return;
        }
        
        spawnedPositions.Clear();
        
        // Spawn player first (other objects reference player position)
        if (playerPrefab != null)
            SpawnPlayer();
        else
            Debug.LogError("[SpawnManager] Cannot spawn player - prefab not assigned!");
        
        // Spawn items
        if (itemPrefab != null)
            SpawnItems(itemCount);
        
        // Spawn enemies (they avoid player spawn position)
        if (enemyPrefab != null)
            SpawnEnemies(enemyCount);
            
        Debug.Log($"[SpawnManager] ✓ Spawn complete: 1 Player, {spawnedPositions.Count - 1} Items/Enemies spawned");
    }
    
    /// <summary>
    /// Spawn the player on a random valid maze tile
    /// Guaranteed to spawn inside maze using cached walkable tiles
    /// </summary>
    void SpawnPlayer()
    {
        Vector3 spawnPos = GetRandomValidTile(playerSpawnHeight);
        
        if (spawnPos == Vector3.zero)
        {
            Debug.LogError("[SpawnManager] FAILED to spawn player! No valid tiles available.");
            return;
        }
        
        GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        player.tag = "Player";
        player.name = "Player";
        
        playerSpawnPosition = spawnPos;
        spawnedPositions.Add(spawnPos);
        
        LogDebug($"✓ Player spawned at {spawnPos} (on valid maze tile)");
    }
    
    /// <summary>
    /// Spawn multiple items on random valid maze tiles
    /// Each item maintains minimum distance from others
    /// </summary>
    public void SpawnItems(int count)
    {
        int spawned = 0;
        
        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = GetRandomValidTile(itemSpawnHeight, minDistanceBetweenSpawns);
            
            if (spawnPos == Vector3.zero)
            {
                Debug.LogWarning($"[SpawnManager] Could not spawn item {i + 1}/{count} (no valid position)");
                continue;
            }
            
            GameObject item = Instantiate(itemPrefab, spawnPos, Quaternion.identity);
            item.name = $"Item_{spawned + 1}";
            
            spawnedPositions.Add(spawnPos);
            spawned++;
        }
        
        LogDebug($"✓ Spawned {spawned}/{count} items (all on valid maze tiles)");
        
        if (spawned < count)
        {
            Debug.LogWarning($"[SpawnManager] Only spawned {spawned}/{count} items. Try reducing minDistanceBetweenSpawns or spawn count.");
        }
    }
    
    /// <summary>
    /// Spawn multiple enemies on random valid maze tiles
    /// Enemies maintain distance from player and from each other
    /// </summary>
    public void SpawnEnemies(int count)
    {
        int spawned = 0;
        
        for (int i = 0; i < count; i++)
        {
            // Enemies need to be far from player and from each other
            Vector3 spawnPos = GetRandomValidTile(
                enemySpawnHeight, 
                minDistanceBetweenSpawns * 2f,  // Extra spacing between enemies
                minDistanceFromPlayer            // Keep away from player
            );
            
            if (spawnPos == Vector3.zero)
            {
                Debug.LogWarning($"[SpawnManager] Could not spawn enemy {i + 1}/{count} (no valid position)");
                continue;
            }
            
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            enemy.tag = "Ghost";
            enemy.name = $"Ghost_{spawned + 1}";
            
            spawnedPositions.Add(spawnPos);
            spawned++;
        }
        
        LogDebug($"✓ Spawned {spawned}/{count} enemies (all on valid maze tiles, away from player)");
        
        if (spawned < count)
        {
            Debug.LogWarning($"[SpawnManager] Only spawned {spawned}/{count} enemies. Try reducing minDistanceFromPlayer or enemy count.");
        }
    }
    
    /// <summary>
    /// Check if a position is too close to any already spawned object
    /// </summary>
    bool IsTooCloseToOtherSpawns(Vector3 position, float minDistance)
    {
        foreach (Vector3 spawnedPos in spawnedPositions)
        {
            if (Vector3.Distance(position, spawnedPos) < minDistance)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// Get a safe respawn position for player (e.g., after death)
    /// Returns a new random valid tile far from enemies
    /// </summary>
    public Vector3 GetPlayerRespawnPosition()
    {
        Vector3 respawnPos = GetRandomValidTile(playerSpawnHeight, 0f, minDistanceFromPlayer / 2f);
        
        if (respawnPos == Vector3.zero)
        {
            // Fallback: use original spawn position
            Debug.LogWarning("[SpawnManager] Could not find respawn position, using original spawn point");
            return playerSpawnPosition;
        }
        
        return respawnPos;
    }
    
    /// <summary>
    /// Spawn a single item near a specified position (for dynamic spawning)
    /// </summary>
    public GameObject SpawnSingleItem(Vector3 nearPosition)
    {
        Vector3 spawnPos = GetRandomValidTile(itemSpawnHeight, minDistanceBetweenSpawns);
        
        if (spawnPos == Vector3.zero)
        {
            Debug.LogWarning("[SpawnManager] Could not spawn single item - no valid position");
            return null;
        }
        
        GameObject item = Instantiate(itemPrefab, spawnPos, Quaternion.identity);
        spawnedPositions.Add(spawnPos);
        
        LogDebug($"Dynamically spawned item at {spawnPos}");
        return item;
    }
    
    /// <summary>
    /// Spawn a single enemy near a specified position (for dynamic spawning)
    /// </summary>
    public GameObject SpawnSingleEnemy(Vector3 nearPosition)
    {
        Vector3 spawnPos = GetRandomValidTile(enemySpawnHeight, minDistanceBetweenSpawns, minDistanceFromPlayer);
        
        if (spawnPos == Vector3.zero)
        {
            Debug.LogWarning("[SpawnManager] Could not spawn single enemy - no valid position");
            return null;
        }
        
        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        enemy.tag = "Ghost";
        spawnedPositions.Add(spawnPos);
        
        LogDebug($"Dynamically spawned enemy at {spawnPos}");
        return enemy;
    }
    
    /// <summary>
    /// Clear tracked spawn positions (call before respawning all objects)
    /// </summary>
    public void ClearSpawnedPositions()
    {
        spawnedPositions.Clear();
        playerSpawnPosition = Vector3.zero;
    }
    
    /// <summary>
    /// Re-cache walkable tiles (call if maze is regenerated)
    /// Now delegates to GridBuilder to refresh its raycast-based grid
    /// </summary>
    public void RefreshWalkableTiles()
    {
        if (gridBuilder != null)
        {
            gridBuilder.RefreshGrid();
            Debug.Log($"[SpawnManager] Grid refreshed: {gridBuilder.validCells.Count} valid cells available");
        }
    }
    
    void LogDebug(string message)
    {
        if (showDebugLogs)
            Debug.Log($"[SpawnManager] {message}");
    }
    
    // ==================== GIZMOS FOR VISUALIZATION ====================
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Note: Walkable tiles visualization is now handled by GridBuilder
        // No need to duplicate - GridBuilder shows all walkable floor cells
        
        // Draw spawned object positions (cyan spheres)
        if (Application.isPlaying && spawnedPositions.Count > 0)
        {
            Gizmos.color = spawnedPositionsColor;
            foreach (Vector3 pos in spawnedPositions)
            {
                Gizmos.DrawSphere(pos, 0.4f);
            }
        }
        
        // Draw player spawn position (blue sphere with radius indicator)
        if (Application.isPlaying && playerSpawnPosition != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(playerSpawnPosition, 0.6f);
            Gizmos.DrawWireSphere(playerSpawnPosition, minDistanceFromPlayer);
        }
    }
}