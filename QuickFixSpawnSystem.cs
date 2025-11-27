// QuickFixSpawnSystem.cs
// 🚨 EMERGENCY FIX - Jika spawn masih gagal, gunakan script ini
// Attach ke GameObject kosong, lalu Play
// Script ini akan otomatis fix semua masalah umum

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class QuickFixSpawnSystem : MonoBehaviour
{
    [Header("🚨 EMERGENCY AUTO-FIX")]
    [Tooltip("Jalankan auto-fix saat Start")]
    public bool autoFixOnStart = true;
    
    [Tooltip("Auto-tag semua collider sebagai Floor")]
    public bool autoTagFloors = true;
    
    [Tooltip("Auto-configure GridBuilder")]
    public bool autoConfigureGrid = true;
    
    [Tooltip("Auto-configure SpawnManager")]
    public bool autoConfigureSpawnManager = true;
    
    [Header("References (Auto-Find)")]
    public Transform labyrinthParent;
    public GridBuilder gridBuilder;
    public LabyrinthSpawnManager spawnManager;
    
    void Start()
    {
        if (autoFixOnStart)
        {
            Debug.Log("═══════════════════════════════════════════════════");
            Debug.Log("   🚨 QUICK FIX SPAWN SYSTEM - STARTING AUTO-FIX");
            Debug.Log("═══════════════════════════════════════════════════");
            
            AutoFindReferences();
            
            if (autoTagFloors)
                FixFloorTags();
            
            if (autoConfigureGrid)
                FixGridBuilder();
            
            if (autoConfigureSpawnManager)
                FixSpawnManager();
            
            Debug.Log("═══════════════════════════════════════════════════");
            Debug.Log("   ✅ AUTO-FIX COMPLETE - RESTART GAME TO TEST");
            Debug.Log("═══════════════════════════════════════════════════");
            
            #if UNITY_EDITOR
            EditorUtility.DisplayDialog(
                "Quick Fix Complete", 
                "Auto-fix selesai!\n\n" +
                "Cek Console untuk detail.\n" +
                "Restart game untuk test spawn.\n\n" +
                "Jika masih gagal, baca PANDUAN_BAHASA_INDONESIA.txt",
                "OK"
            );
            #endif
        }
    }
    
    void AutoFindReferences()
    {
        Debug.Log("\n[1] AUTO-FINDING REFERENCES...");
        
        if (gridBuilder == null)
        {
            gridBuilder = FindObjectOfType<GridBuilder>();
            if (gridBuilder != null)
                Debug.Log("✓ Found GridBuilder");
            else
                Debug.LogError("❌ GridBuilder NOT FOUND - Create one!");
        }
        
        if (spawnManager == null)
        {
            spawnManager = FindObjectOfType<LabyrinthSpawnManager>();
            if (spawnManager != null)
                Debug.Log("✓ Found LabyrinthSpawnManager");
            else
                Debug.LogError("❌ SpawnManager NOT FOUND - Create one!");
        }
        
        if (labyrinthParent == null)
        {
            // Cari GameObject dengan nama yang mengandung "maze", "labyrinth", "labirin"
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                string name = obj.name.ToLower();
                if (name.Contains("maze") || name.Contains("labyrinth") || name.Contains("labirin"))
                {
                    if (obj.GetComponentsInChildren<Collider>().Length > 0)
                    {
                        labyrinthParent = obj.transform;
                        Debug.Log($"✓ Found labyrinth: {obj.name}");
                        break;
                    }
                }
            }
            
            if (labyrinthParent == null)
            {
                Debug.LogWarning("⚠️ Labyrinth parent not found - assign manually!");
            }
        }
    }
    
    void FixFloorTags()
    {
        Debug.Log("\n[2] FIXING FLOOR TAGS...");
        
        if (labyrinthParent == null)
        {
            Debug.LogError("❌ Cannot fix tags - labyrinthParent is null!");
            return;
        }
        
        // Ensure "Floor" tag exists
        #if UNITY_EDITOR
        if (!TagExists("Floor"))
        {
            Debug.LogWarning("⚠️ Tag 'Floor' tidak ada - akan dibuat...");
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            
            // Add Floor tag
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            SerializedProperty newTag = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTag.stringValue = "Floor";
            tagManager.ApplyModifiedProperties();
            
            Debug.Log("✓ Tag 'Floor' berhasil dibuat!");
        }
        #endif
        
        // Tag all colliders in labyrinth
        Collider[] allColliders = labyrinthParent.GetComponentsInChildren<Collider>(true);
        int tagged = 0;
        
        foreach (Collider col in allColliders)
        {
            if (col == null) continue;
            
            // Tag semua collider yang bukan trigger
            if (!col.isTrigger)
            {
                col.gameObject.tag = "Floor";
                tagged++;
            }
        }
        
        Debug.Log($"✓ Tagged {tagged} objects as 'Floor'");
        
        if (tagged == 0)
        {
            Debug.LogError("❌ No colliders found to tag!");
        }
    }
    
    void FixGridBuilder()
    {
        Debug.Log("\n[3] FIXING GRIDBUILDER CONFIG...");
        
        if (gridBuilder == null)
        {
            Debug.LogError("❌ Cannot fix - GridBuilder is null!");
            return;
        }
        
        // Auto-detect bounds
        gridBuilder.autoDetectBounds = true;
        gridBuilder.labyrinthParent = labyrinthParent;
        gridBuilder.autoDetectPadding = 2f;
        
        // Floor detection
        gridBuilder.floorTag = "Floor";
        
        // Raycast settings
        if (gridBuilder.raycastHeight < 10f)
        {
            gridBuilder.raycastHeight = 50f;
            Debug.Log("✓ Set raycastHeight = 50");
        }
        
        if (gridBuilder.raycastMaxDistance < 50f)
        {
            gridBuilder.raycastMaxDistance = 100f;
            Debug.Log("✓ Set raycastMaxDistance = 100");
        }
        
        // Grid settings
        if (gridBuilder.gridWidth < 20)
        {
            gridBuilder.gridWidth = 50;
            Debug.Log("✓ Set gridWidth = 50");
        }
        
        if (gridBuilder.gridHeight < 20)
        {
            gridBuilder.gridHeight = 50;
            Debug.Log("✓ Set gridHeight = 50");
        }
        
        if (gridBuilder.nodeSpacing > 2f)
        {
            gridBuilder.nodeSpacing = 1f;
            Debug.Log("✓ Set nodeSpacing = 1.0");
        }
        
        // Visualization
        gridBuilder.showGizmos = true;
        gridBuilder.showOnlyWalkable = true;
        gridBuilder.showDebugLogs = true;
        
        // Force refresh grid
        gridBuilder.RefreshGrid();
        
        Debug.Log($"✓ GridBuilder configured - {gridBuilder.GetWalkableCellCount()} walkable cells");
    }
    
    void FixSpawnManager()
    {
        Debug.Log("\n[4] FIXING SPAWNMANAGER CONFIG...");
        
        if (spawnManager == null)
        {
            Debug.LogError("❌ Cannot fix - SpawnManager is null!");
            return;
        }
        
        // Assign GridBuilder reference
        if (spawnManager.gridBuilder == null && gridBuilder != null)
        {
            spawnManager.gridBuilder = gridBuilder;
            Debug.Log("✓ Assigned GridBuilder to SpawnManager");
        }
        
        // Enable debug
        spawnManager.showDebugLogs = true;
        spawnManager.showGizmos = true;
        
        // Reasonable spawn counts
        if (spawnManager.itemCount > 100)
        {
            spawnManager.itemCount = 30;
            Debug.Log("✓ Set itemCount = 30");
        }
        
        if (spawnManager.enemyCount > 20)
        {
            spawnManager.enemyCount = 3;
            Debug.Log("✓ Set enemyCount = 3");
        }
        
        // Distance settings
        if (spawnManager.minDistanceBetweenSpawns < 1f)
        {
            spawnManager.minDistanceBetweenSpawns = 2f;
            Debug.Log("✓ Set minDistanceBetweenSpawns = 2.0");
        }
        
        if (spawnManager.minDistanceFromPlayer < 5f)
        {
            spawnManager.minDistanceFromPlayer = 10f;
            Debug.Log("✓ Set minDistanceFromPlayer = 10.0");
        }
        
        Debug.Log("✓ SpawnManager configured");
    }
    
    bool TagExists(string tag)
    {
        #if UNITY_EDITOR
        return System.Array.Exists(UnityEditorInternal.InternalEditorUtility.tags, t => t == tag);
        #else
        try
        {
            GameObject.FindGameObjectWithTag(tag);
            return true;
        }
        catch
        {
            return false;
        }
        #endif
    }
    
    [ContextMenu("Run Quick Fix")]
    public void RunQuickFix()
    {
        Start();
    }
}
