// LabyrinthFloorAutoTagger.cs
// 🔧 UTILITY SCRIPT - Auto-tag semua floor collider di labirin
// Jalankan sekali untuk setup, lalu bisa dihapus
//
// CARA PAKAI:
// 1. Attach script ini ke GameObject parent labirin
// 2. Atur floorKeywords (misal: "floor", "ground", "lantai")
// 3. Play mode atau klik kanan → "Tag All Floors"
// 4. Cek Console untuk hasil
// 5. Hapus script setelah selesai

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LabyrinthFloorAutoTagger : MonoBehaviour
{
    [Header("Auto-Tagging Settings")]
    [Tooltip("Tag yang akan diberikan ke floor objects")]
    public string targetTag = "Floor";
    
    [Tooltip("Kata kunci nama object yang dianggap floor (case-insensitive)")]
    public string[] floorKeywords = new string[] { "floor", "ground", "lantai", "base" };
    
    [Tooltip("Layer yang akan diberikan ke floor objects (optional)")]
    public string targetLayer = "Default";
    
    [Header("Filter Options")]
    [Tooltip("Hanya tag object yang punya Collider")]
    public bool requireCollider = true;
    
    [Tooltip("Tag semua child collider tanpa filter keyword")]
    public bool tagAllColliders = false;
    
    [Header("⚠️ EXCLUDE FROM AUTO-TAGGING")]
    [Tooltip("Floor objects yang TIDAK ingin di-tag")]
    public GameObject[] excludedFloors;
    
    [Header("Info")]
    [SerializeField] private int floorCount = 0;
    
    void Start()
    {
        // Auto-tag saat game dimulai (hanya di Editor)
        #if UNITY_EDITOR
        if (!Application.isPlaying)
            return;
            
        TagAllFloors();
        #endif
    }
    
    [ContextMenu("Tag All Floors")]
    public void TagAllFloors()
    {
        floorCount = 0;
        
        // Validasi tag exists
        if (!TagExists(targetTag))
        {
            Debug.LogError($"[FloorTagger] Tag '{targetTag}' tidak ada! Buat tag dulu di Edit → Project Settings → Tags and Layers");
            return;
        }
        
        // Get semua collider di children
        Collider[] allColliders = GetComponentsInChildren<Collider>(true);
        
        if (allColliders.Length == 0)
        {
            Debug.LogWarning("[FloorTagger] Tidak ada Collider ditemukan di children!");
            return;
        }
        
        Debug.Log($"[FloorTagger] Memindai {allColliders.Length} colliders...");
        
        foreach (Collider col in allColliders)
        {
            if (col == null) continue;
            
            // SKIP jika ada di exclude list
            if (IsExcluded(col.gameObject))
            {
                continue;
            }
            
            bool shouldTag = false;
            
            if (tagAllColliders)
            {
                // Tag semua collider
                shouldTag = true;
            }
            else
            {
                // Tag hanya yang sesuai keyword
                string objName = col.gameObject.name.ToLower();
                
                foreach (string keyword in floorKeywords)
                {
                    if (objName.Contains(keyword.ToLower()))
                    {
                        shouldTag = true;
                        break;
                    }
                }
            }
            
            if (shouldTag)
            {
                // Set tag
                col.gameObject.tag = targetTag;
                
                // Set layer (optional)
                if (!string.IsNullOrEmpty(targetLayer) && targetLayer != "Default")
                {
                    int layerIndex = LayerMask.NameToLayer(targetLayer);
                    if (layerIndex >= 0)
                    {
                        col.gameObject.layer = layerIndex;
                    }
                }
                
                floorCount++;
                
                if (floorCount <= 5)
                {
                    Debug.Log($"[FloorTagger] ✓ Tagged: {col.gameObject.name} → Tag='{targetTag}'");
                }
            }
        }
        
        Debug.Log($"[FloorTagger] ✓ SELESAI! {floorCount} objects diberi tag '{targetTag}'");
        
        if (floorCount == 0)
        {
            Debug.LogWarning("[FloorTagger] ⚠️ Tidak ada object yang cocok dengan keywords!");
            Debug.LogWarning("[FloorTagger] Coba enable 'tagAllColliders' atau ubah 'floorKeywords'");
        }
        
        #if UNITY_EDITOR
        // Mark scene dirty untuk save changes
        EditorUtility.SetDirty(gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
        #endif
    }
    
    // Check apakah GameObject ada di exclude list
    bool IsExcluded(GameObject obj)
    {
        if (excludedFloors == null || excludedFloors.Length == 0)
            return false;
        
        // Check exact match
        foreach (GameObject excluded in excludedFloors)
        {
            if (excluded == null) continue;
            
            if (obj == excluded)
                return true;
            
            // Check parent hierarchy (jika exclude parent, semua child ikut excluded)
            Transform parent = obj.transform;
            while (parent != null)
            {
                if (parent.gameObject == excluded)
                    return true;
                parent = parent.parent;
            }
        }
        
        return false;
    }
    
    bool TagExists(string tag)
    {
        try
        {
            GameObject.FindGameObjectWithTag(tag);
            return true;
        }
        catch
        {
            // Alternatif check
            #if UNITY_EDITOR
            return System.Array.Exists(UnityEditorInternal.InternalEditorUtility.tags, t => t == tag);
            #else
            return false;
            #endif
        }
    }
    
    [ContextMenu("Show Floor Statistics")]
    public void ShowStatistics()
    {
        Collider[] allColliders = GetComponentsInChildren<Collider>(true);
        int tagged = 0;
        int untagged = 0;
        
        foreach (Collider col in allColliders)
        {
            if (col.CompareTag(targetTag))
                tagged++;
            else
                untagged++;
        }
        
        Debug.Log($"[FloorTagger] === STATISTICS ===");
        Debug.Log($"Total Colliders: {allColliders.Length}");
        Debug.Log($"Tagged '{targetTag}': {tagged}");
        Debug.Log($"Untagged: {untagged}");
    }
}
