// LabyrinthSpawnManager.cs - FIXED VERSION
// Perbaikan:
// 1. Mencegah double-spawning dengan flag isSpawned
// 2. Distance check yang lebih ketat
// 3. Validasi jumlah spawn yang lebih baik
// 4. Debug logging untuk tracking spawn count

using System.Collections.Generic;
using UnityEngine;

public class LabyrinthSpawnManager : MonoBehaviour
{
    [Header("Grid Reference - REQUIRED")]
    [Tooltip("WAJIB: GridBuilder yang sudah generate maze")]
    public GridBuilder gridBuilder;
    
    [Header("Spawn Settings")]
    public float minDistanceBetweenSpawns = 5f; // Dinaikkan dari 2f
    public float minDistanceFromPlayer = 15f;   // Dinaikkan dari 10f
    
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
    public int maxDistanceAttempts = 100; // Dinaikkan dari 50
    [Tooltip("If true, guarantees spawn even if distance requirements can't be met")]
    public bool allowFallbackSpawn = false; // Ubah ke false untuk lebih strict
    
    [Header("Visualization")]
    public bool showGizmos = true;
    public bool showDebugLogs = true;
    public Color walkableTilesColor = new Color(0, 1, 0, 0.3f);
    public Color spawnedPositionsColor = Color.cyan;
    
    [Header("Auto Spawn Control")]
    [Tooltip("Auto spawn on Start - Set false if GameManager triggers spawn")]
    public bool autoSpawnOnStart = false;
    
    // Cached walkable tiles
    private List<Vector3> spawnedPositions = new List<Vector3>();
    private Vector3 playerSpawnPosition;
    
    // BARU: Flag untuk mencegah double spawning
    private bool isSpawned = false;
    
    // BARU: Tracking actual spawned objects
    private GameObject spawnedPlayer;
    private List<GameObject> spawnedItems = new List<GameObject>();
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    
    void Start()
    {
        ValidateSetup();
        
        if (gridBuilder == null)
            gridBuilder = FindObjectOfType<GridBuilder>();
            
        // Validate GridBuilder
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
    
    void ValidateSetup()
    {
        if (gridBuilder == null)
        {
            Debug.LogError("[SpawnManager] ⚠️ GridBuilder NOT ASSIGNED! Cannot spawn objects.");
        }
        
        if (playerPrefab == null)
        {
            Debug.LogError("[SpawnManager] ⚠️ Player Prefab NOT ASSIGNED!");
        }
        else
        {
            Camera cam = playerPrefab.GetComponentInChildren<Camera>();
            if (cam == null)
            {
                Debug.LogError("[SpawnManager] ⚠️⚠️⚠️ PLAYER PREFAB HAS NO CAMERA!");
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
    /// Get random valid tile dengan distance checking yang lebih ketat
    /// </summary>
    public Vector3 GetRandomValidTile(float heightOffset, float minDistanceFromOthers = 0f, float minDistanceFromPlayer = 0f)
    {
        if (gridBuilder == null)
        {
            Debug.LogError("[SpawnManager] GridBuilder is null!");
            return Vector3.zero;
        }
        
        if (gridBuilder.validCells.Count == 0)
        {
            Debug.LogError("[SpawnManager] No valid cells in GridBuilder!");
            return Vector3.zero;
        }
        
        // Try multiple times dengan distance checking
        for (int attempt = 0; attempt < maxDistanceAttempts; attempt++)
        {
            Vector3 spawnPos = gridBuilder.GetRandomValidCell(heightOffset);
            
            if (spawnPos == Vector3.zero)
                continue;
            
            // Check distance dari semua spawned objects
            if (minDistanceFromOthers > 0f && IsTooCloseToOtherSpawns(spawnPos, minDistanceFromOthers))
                continue;
            
            // Check distance dari player
            if (minDistanceFromPlayer > 0f && playerSpawnPosition != Vector3.zero)
            {
                if (Vector3.Distance(spawnPos, playerSpawnPosition) < minDistanceFromPlayer)
                    continue;
            }
            
            // Position valid!
            return spawnPos;
        }
        
        // Fallback hanya jika allowFallbackSpawn = true
        if (allowFallbackSpawn)
        {
            Vector3 fallbackPos = gridBuilder.GetRandomValidCell(heightOffset);
            Debug.LogWarning($"[SpawnManager] Using fallback spawn (distance requirements couldn't be met)");
            return fallbackPos;
        }
        
        // Jika tidak ada fallback, return zero
        Debug.LogWarning($"[SpawnManager] No valid position found after {maxDistanceAttempts} attempts");
        return Vector3.zero;
    }
    
    /// <summary>
    /// MAIN SPAWN FUNCTION - Dipanggil dari GameManager
    /// Mencegah double spawning dengan flag
    /// </summary>
    public void SpawnAllObjects()
    {
        // CRITICAL: Cek apakah sudah pernah spawn
        if (isSpawned)
        {
            Debug.LogWarning("[SpawnManager] ⚠️ SpawnAllObjects() called but objects already spawned! Ignoring duplicate call.");
            return;
        }
        
        if (gridBuilder == null || gridBuilder.validCells.Count == 0)
        {
            Debug.LogError("[SpawnManager] Cannot spawn - GridBuilder has no valid cells!");
            return;
        }
        
        Debug.Log("=== [SpawnManager] STARTING SPAWN SEQUENCE ===");
        
        // Clear previous data
        ClearSpawnedPositions();
        
        // 1. Spawn Player
        if (playerPrefab != null)
        {
            SpawnPlayer();
        }
        else
        {
            Debug.LogError("[SpawnManager] Cannot spawn player - prefab not assigned!");
        }
        
        // 2. Spawn Items
        if (itemPrefab != null)
        {
            SpawnItems(itemCount);
        }
        
        // 3. Spawn Enemies
        if (enemyPrefab != null)
        {
            SpawnEnemies(enemyCount);
        }
        
        // Set flag untuk mencegah double spawn
        isSpawned = true;
        
        // Final report
        Debug.Log($"=== [SpawnManager] SPAWN COMPLETE ===");
        Debug.Log($"Player: {(spawnedPlayer != null ? "1" : "0")}");
        Debug.Log($"Items: {spawnedItems.Count}/{itemCount}");
        Debug.Log($"Enemies: {spawnedEnemies.Count}/{enemyCount}");
        Debug.Log($"Total Objects: {spawnedPositions.Count}");
    }
    
    /// <summary>
    /// Spawn player - hanya spawn 1 player
    /// </summary>
    void SpawnPlayer()
    {
        // Cek apakah player sudah ada
        if (spawnedPlayer != null)
        {
            Debug.LogWarning("[SpawnManager] Player already spawned! Skipping.");
            return;
        }
        
        Vector3 spawnPos = GetRandomValidTile(playerSpawnHeight);
        
        if (spawnPos == Vector3.zero)
        {
            Debug.LogError("[SpawnManager] FAILED to spawn player! No valid tiles available.");
            return;
        }
        
        spawnedPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        spawnedPlayer.tag = "Player";
        spawnedPlayer.name = "Player";
        
        playerSpawnPosition = spawnPos;
        spawnedPositions.Add(spawnPos);
        
        Debug.Log($"[SpawnManager] ✓ Player spawned at {spawnPos}");
    }
    
    /// <summary>
    /// Spawn items dengan jumlah yang exact
    /// </summary>
    public void SpawnItems(int count)
    {
        Debug.Log($"[SpawnManager] Spawning {count} items...");
        
        int successfulSpawns = 0;
        int failedAttempts = 0;
        int maxFailures = count * 2; // Batas maksimal kegagalan
        
        for (int i = 0; i < count; i++)
        {
            if (failedAttempts >= maxFailures)
            {
                Debug.LogWarning($"[SpawnManager] Stopped spawning items after {maxFailures} failed attempts");
                break;
            }
            
            Vector3 spawnPos = GetRandomValidTile(itemSpawnHeight, minDistanceBetweenSpawns);
            
            if (spawnPos == Vector3.zero)
            {
                Debug.LogWarning($"[SpawnManager] Failed to spawn item {i + 1}/{count} - no valid position");
                failedAttempts++;
                i--; // Coba lagi untuk index ini
                continue;
            }
            
            GameObject item = Instantiate(itemPrefab, spawnPos, Quaternion.identity);
            item.name = $"Item_{successfulSpawns + 1}";
            
            spawnedPositions.Add(spawnPos);
            spawnedItems.Add(item);
            successfulSpawns++;
            
            LogDebug($"Item {successfulSpawns}/{count} spawned at {spawnPos}");
        }
        
        Debug.Log($"[SpawnManager] ✓ Items spawned: {successfulSpawns}/{count}");
        
        if (successfulSpawns < count)
        {
            Debug.LogWarning($"[SpawnManager] Only spawned {successfulSpawns}/{count} items.");
            Debug.LogWarning($"Try: reducing minDistanceBetweenSpawns ({minDistanceBetweenSpawns}f) or itemCount");
        }
    }
    
    /// <summary>
    /// Spawn enemies dengan jumlah yang exact
    /// </summary>
    public void SpawnEnemies(int count)
    {
        Debug.Log($"[SpawnManager] Spawning {count} enemies...");
        
        int successfulSpawns = 0;
        int failedAttempts = 0;
        int maxFailures = count * 3; // Lebih banyak toleransi untuk enemy
        
        for (int i = 0; i < count; i++)
        {
            if (failedAttempts >= maxFailures)
            {
                Debug.LogWarning($"[SpawnManager] Stopped spawning enemies after {maxFailures} failed attempts");
                break;
            }
            
            // Enemy butuh jarak lebih jauh dari player dan sesama enemy
            Vector3 spawnPos = GetRandomValidTile(
                enemySpawnHeight, 
                minDistanceBetweenSpawns * 2f,  // Extra spacing antar enemy
                minDistanceFromPlayer            // Jauh dari player
            );
            
            if (spawnPos == Vector3.zero)
            {
                Debug.LogWarning($"[SpawnManager] Failed to spawn enemy {i + 1}/{count} - no valid position");
                failedAttempts++;
                i--; // Coba lagi
                continue;
            }
            
            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            enemy.tag = "Ghost";
            enemy.name = $"Ghost_{successfulSpawns + 1}";
            
            spawnedPositions.Add(spawnPos);
            spawnedEnemies.Add(enemy);
            successfulSpawns++;
            
            LogDebug($"Enemy {successfulSpawns}/{count} spawned at {spawnPos}");
        }
        
        Debug.Log($"[SpawnManager] ✓ Enemies spawned: {successfulSpawns}/{count}");
        
        if (successfulSpawns < count)
        {
            Debug.LogWarning($"[SpawnManager] Only spawned {successfulSpawns}/{count} enemies.");
            Debug.LogWarning($"Try: reducing minDistanceFromPlayer ({minDistanceFromPlayer}f) or enemyCount");
        }
    }
    
    /// <summary>
    /// Check apakah posisi terlalu dekat dengan spawned objects
    /// </summary>
    bool IsTooCloseToOtherSpawns(Vector3 position, float minDistance)
    {
        foreach (Vector3 spawnedPos in spawnedPositions)
        {
            float distance = Vector3.Distance(position, spawnedPos);
            if (distance < minDistance)
            {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Get respawn position untuk player (after death)
    /// </summary>
    public Vector3 GetPlayerRespawnPosition()
    {
        Vector3 respawnPos = GetRandomValidTile(playerSpawnHeight, 0f, minDistanceFromPlayer / 2f);
        
        if (respawnPos == Vector3.zero)
        {
            Debug.LogWarning("[SpawnManager] Using original spawn point for respawn");
            return playerSpawnPosition;
        }
        
        return respawnPos;
    }
    
    /// <summary>
    /// Spawn single item (dynamic spawning)
    /// </summary>
    public GameObject SpawnSingleItem(Vector3 nearPosition)
    {
        Vector3 spawnPos = GetRandomValidTile(itemSpawnHeight, minDistanceBetweenSpawns);
        
        if (spawnPos == Vector3.zero)
        {
            Debug.LogWarning("[SpawnManager] Could not spawn single item");
            return null;
        }
        
        GameObject item = Instantiate(itemPrefab, spawnPos, Quaternion.identity);
        spawnedPositions.Add(spawnPos);
        spawnedItems.Add(item);
        
        LogDebug($"Dynamically spawned item at {spawnPos}");
        return item;
    }
    
    /// <summary>
    /// Spawn single enemy (dynamic spawning)
    /// </summary>
    public GameObject SpawnSingleEnemy(Vector3 nearPosition)
    {
        Vector3 spawnPos = GetRandomValidTile(enemySpawnHeight, minDistanceBetweenSpawns, minDistanceFromPlayer);
        
        if (spawnPos == Vector3.zero)
        {
            Debug.LogWarning("[SpawnManager] Could not spawn single enemy");
            return null;
        }
        
        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        enemy.tag = "Ghost";
        spawnedPositions.Add(spawnPos);
        spawnedEnemies.Add(enemy);
        
        LogDebug($"Dynamically spawned enemy at {spawnPos}");
        return enemy;
    }
    
    /// <summary>
    /// Clear all spawn data - gunakan sebelum respawn all
    /// </summary>
    public void ClearSpawnedPositions()
    {
        spawnedPositions.Clear();
        spawnedItems.Clear();
        spawnedEnemies.Clear();
        spawnedPlayer = null;
        playerSpawnPosition = Vector3.zero;
        isSpawned = false;
    }
    
    /// <summary>
    /// Refresh grid - panggil jika maze di-regenerate
    /// </summary>
    public void RefreshWalkableTiles()
    {
        if (gridBuilder != null)
        {
            gridBuilder.RefreshGrid();
            Debug.Log($"[SpawnManager] Grid refreshed: {gridBuilder.validCells.Count} valid cells available");
        }
    }
    
    /// <summary>
    /// Get spawn statistics - untuk debugging
    /// </summary>
    public void PrintSpawnStats()
    {
        Debug.Log("=== SPAWN STATISTICS ===");
        Debug.Log($"Player: {(spawnedPlayer != null ? "1" : "0")}");
        Debug.Log($"Items: {spawnedItems.Count} (requested: {itemCount})");
        Debug.Log($"Enemies: {spawnedEnemies.Count} (requested: {enemyCount})");
        Debug.Log($"Total spawned positions: {spawnedPositions.Count}");
        Debug.Log($"Valid cells available: {(gridBuilder != null ? gridBuilder.validCells.Count : 0)}");
    }
    
    void LogDebug(string message)
    {
        if (showDebugLogs)
            Debug.Log($"[SpawnManager] {message}");
    }
    
    // ==================== GIZMOS ====================
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Draw spawned positions
        if (Application.isPlaying && spawnedPositions.Count > 0)
        {
            Gizmos.color = spawnedPositionsColor;
            foreach (Vector3 pos in spawnedPositions)
            {
                Gizmos.DrawSphere(pos, 0.4f);
            }
        }
        
        // Draw player spawn dengan radius
        if (Application.isPlaying && playerSpawnPosition != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(playerSpawnPosition, 0.6f);
            
            // Draw min distance dari player (untuk enemy)
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawWireSphere(playerSpawnPosition, minDistanceFromPlayer);
        }
        
        // Draw min distance antar spawns
        if (Application.isPlaying && spawnedPositions.Count > 0)
        {
            Gizmos.color = new Color(0, 1, 0, 0.1f);
            foreach (Vector3 pos in spawnedPositions)
            {
                Gizmos.DrawWireSphere(pos, minDistanceBetweenSpawns);
            }
        }
    }
}