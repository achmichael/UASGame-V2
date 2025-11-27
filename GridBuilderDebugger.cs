// GridBuilderDebugger.cs
// 🔍 Script untuk debug masalah GridBuilder
// Attach ke GameObject kosong, lalu Play untuk lihat info detail

using UnityEngine;

public class GridBuilderDebugger : MonoBehaviour
{
    [Header("References")]
    public GridBuilder gridBuilder;
    public LabyrinthSpawnManager spawnManager;
    
    [Header("Auto Find")]
    public bool autoFindReferences = true;
    
    [Header("Debug Options")]
    public bool showFloorObjects = true;
    public bool testSpawnPosition = true;
    public bool showRaycastTest = true;
    
    void Start()
    {
        if (autoFindReferences)
        {
            if (gridBuilder == null)
                gridBuilder = FindObjectOfType<GridBuilder>();
            
            if (spawnManager == null)
                spawnManager = FindObjectOfType<LabyrinthSpawnManager>();
        }
        
        Debug.Log("═══════════════════════════════════════════════════");
        Debug.Log("     🔍 GRIDBUILDER DEBUGGER - STARTING TESTS");
        Debug.Log("═══════════════════════════════════════════════════");
        
        CheckGridBuilder();
        CheckSpawnManager();
        
        if (showFloorObjects)
            FindAllFloorObjects();
        
        if (testSpawnPosition && gridBuilder != null)
            TestSpawnPositions();
        
        if (showRaycastTest)
            TestRaycastFromCenter();
        
        Debug.Log("═══════════════════════════════════════════════════");
        Debug.Log("     🔍 DEBUGGER - TESTS COMPLETE");
        Debug.Log("═══════════════════════════════════════════════════");
    }
    
    void CheckGridBuilder()
    {
        Debug.Log("\n[1] CHECKING GRIDBUILDER...");
        
        if (gridBuilder == null)
        {
            Debug.LogError("❌ GridBuilder NOT FOUND in scene!");
            return;
        }
        
        Debug.Log("✓ GridBuilder found");
        Debug.Log($"  - Grid Size: {gridBuilder.gridWidth} x {gridBuilder.gridHeight}");
        Debug.Log($"  - Node Spacing: {gridBuilder.nodeSpacing}");
        Debug.Log($"  - Grid Origin: {gridBuilder.transform.position}");
        Debug.Log($"  - Raycast Height: {gridBuilder.raycastHeight}");
        Debug.Log($"  - Floor Tag: '{gridBuilder.floorTag}'");
        
        int walkableCount = gridBuilder.GetWalkableCellCount();
        Debug.Log($"  - Walkable Cells: {walkableCount}");
        
        if (walkableCount == 0)
        {
            Debug.LogError("❌ NO WALKABLE CELLS! Grid tidak bisa spawn object!");
            Debug.LogError("   → Cek apakah floor sudah di-tag dengan benar");
        }
        else if (walkableCount < 10)
        {
            Debug.LogWarning($"⚠️ Hanya {walkableCount} walkable cells - mungkin terlalu sedikit");
        }
        else
        {
            Debug.Log($"✓ Grid OK - {walkableCount} valid spawn points");
        }
    }
    
    void CheckSpawnManager()
    {
        Debug.Log("\n[2] CHECKING SPAWNMANAGER...");
        
        if (spawnManager == null)
        {
            Debug.LogError("❌ LabyrinthSpawnManager NOT FOUND in scene!");
            return;
        }
        
        Debug.Log("✓ SpawnManager found");
        
        if (spawnManager.gridBuilder == null)
        {
            Debug.LogError("❌ SpawnManager.gridBuilder NOT ASSIGNED!");
            Debug.LogError("   → Assign GridBuilder di Inspector SpawnManager");
        }
        else
        {
            Debug.Log("✓ SpawnManager.gridBuilder assigned");
        }
        
        if (spawnManager.playerPrefab == null)
            Debug.LogWarning("⚠️ Player Prefab not assigned");
        else
            Debug.Log("✓ Player Prefab assigned");
        
        if (spawnManager.itemPrefab == null)
            Debug.LogWarning("⚠️ Item Prefab not assigned");
        else
            Debug.Log("✓ Item Prefab assigned");
        
        if (spawnManager.enemyPrefab == null)
            Debug.LogWarning("⚠️ Enemy Prefab not assigned");
        else
            Debug.Log("✓ Enemy Prefab assigned");
    }
    
    void FindAllFloorObjects()
    {
        Debug.Log("\n[3] FINDING FLOOR OBJECTS...");
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int floorTagCount = 0;
        int floorColliderCount = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.CompareTag("Floor"))
            {
                floorTagCount++;
                
                if (obj.GetComponent<Collider>() != null)
                {
                    floorColliderCount++;
                    
                    if (floorTagCount <= 3)
                    {
                        Debug.Log($"  ✓ Found floor: {obj.name} (has Collider)");
                    }
                }
                else
                {
                    Debug.LogWarning($"  ⚠️ Floor '{obj.name}' has NO COLLIDER!");
                }
            }
        }
        
        Debug.Log($"Total objects with 'Floor' tag: {floorTagCount}");
        Debug.Log($"Floor objects with Collider: {floorColliderCount}");
        
        if (floorTagCount == 0)
        {
            Debug.LogError("❌ NO objects tagged 'Floor' found in scene!");
            Debug.LogError("   → Gunakan LabyrinthFloorAutoTagger untuk auto-tag");
            Debug.LogError("   → Atau manual tag floor objects di Inspector");
        }
        else if (floorColliderCount < floorTagCount)
        {
            Debug.LogWarning($"⚠️ {floorTagCount - floorColliderCount} floor objects tidak punya Collider!");
        }
        else
        {
            Debug.Log($"✓ All {floorTagCount} floor objects have Colliders");
        }
    }
    
    void TestSpawnPositions()
    {
        Debug.Log("\n[4] TESTING SPAWN POSITIONS...");
        
        if (gridBuilder.GetWalkableCellCount() == 0)
        {
            Debug.LogError("❌ Cannot test spawn - no walkable cells!");
            return;
        }
        
        for (int i = 0; i < 3; i++)
        {
            Vector3 spawnPos = gridBuilder.GetRandomWalkablePosition(1f);
            
            if (spawnPos != Vector3.zero)
            {
                Debug.Log($"  Test {i+1}: Spawn position = {spawnPos}");
                
                // Draw gizmo sphere at spawn position
                Debug.DrawRay(spawnPos, Vector3.up * 5f, Color.green, 5f);
            }
            else
            {
                Debug.LogError($"  Test {i+1}: FAILED - returned Vector3.zero");
            }
        }
        
        Debug.Log("✓ Spawn position test complete");
    }
    
    void TestRaycastFromCenter()
    {
        Debug.Log("\n[5] TESTING RAYCAST...");
        
        if (gridBuilder == null) return;
        
        // Test raycast di tengah grid
        Vector3 centerPos = gridBuilder.transform.position + new Vector3(
            gridBuilder.gridWidth * gridBuilder.nodeSpacing * 0.5f,
            gridBuilder.raycastHeight,
            gridBuilder.gridHeight * gridBuilder.nodeSpacing * 0.5f
        );
        
        Debug.Log($"Testing raycast from: {centerPos}");
        
        RaycastHit hit;
        if (Physics.Raycast(centerPos, Vector3.down, out hit, gridBuilder.raycastMaxDistance))
        {
            Debug.Log($"✓ Raycast HIT:");
            Debug.Log($"  - Object: {hit.collider.name}");
            Debug.Log($"  - Tag: {hit.collider.tag}");
            Debug.Log($"  - Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
            Debug.Log($"  - Position: {hit.point}");
            Debug.Log($"  - Distance: {hit.distance}");
            
            if (hit.collider.CompareTag("Floor"))
            {
                Debug.Log("  ✓ Tagged as 'Floor' - VALID!");
            }
            else
            {
                Debug.LogWarning($"  ⚠️ NOT tagged as 'Floor' - Tag is '{hit.collider.tag}'");
            }
            
            // Draw debug line
            Debug.DrawLine(centerPos, hit.point, Color.yellow, 5f);
        }
        else
        {
            Debug.LogError("❌ Raycast MISSED - tidak mengenai apapun!");
            Debug.LogError("   → Cek raycastHeight (mungkin terlalu rendah)");
            Debug.LogError("   → Cek gridOrigin (mungkin tidak menutupi labirin)");
        }
    }
    
    // Gizmo untuk visualisasi
    void OnDrawGizmos()
    {
        if (gridBuilder == null) return;
        
        // Draw grid bounds
        Gizmos.color = Color.yellow;
        Vector3 center = gridBuilder.transform.position + new Vector3(
            gridBuilder.gridWidth * gridBuilder.nodeSpacing * 0.5f,
            0,
            gridBuilder.gridHeight * gridBuilder.nodeSpacing * 0.5f
        );
        Vector3 size = new Vector3(
            gridBuilder.gridWidth * gridBuilder.nodeSpacing,
            1f,
            gridBuilder.gridHeight * gridBuilder.nodeSpacing
        );
        Gizmos.DrawWireCube(center, size);
        
        // Draw raycast height
        Gizmos.color = Color.cyan;
        Vector3 raycastPlane = center + Vector3.up * gridBuilder.raycastHeight;
        Gizmos.DrawWireCube(raycastPlane, size * 0.5f);
    }
}
