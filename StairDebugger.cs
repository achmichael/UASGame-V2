// StairDebugger.cs
// 🔍 Debug khusus untuk cek kenapa tangga tidak terdeteksi sebagai spawn area
// Attach ke GameObject kosong, lalu Play untuk test raycast di area tangga

using UnityEngine;

public class StairDebugger : MonoBehaviour
{
    [Header("Tangga untuk Debug")]
    [Tooltip("Drag GameObject tangga yang bermasalah kesini")]
    public GameObject stairObject;
    
    [Header("Raycast Settings")]
    [Tooltip("Ketinggian mulai raycast (coba 10, 20, 50)")]
    public float raycastHeight = 50f;
    
    [Tooltip("Jarak maksimal raycast")]
    public float raycastDistance = 100f;
    
    [Header("Test Grid")]
    [Tooltip("Berapa banyak test point di area tangga")]
    public int testPointsX = 5;
    public int testPointsZ = 5;
    
    [Header("Visualization")]
    public bool showGizmos = true;
    public Color hitColor = Color.green;
    public Color missColor = Color.red;
    
    private Vector3[] testPoints;
    private bool[] testHits;
    private RaycastHit[] testHitInfo;
    
    void Start()
    {
        if (stairObject == null)
        {
            Debug.LogError("[StairDebugger] Assign stairObject di Inspector!");
            return;
        }
        
        TestStairDetection();
    }
    
    void TestStairDetection()
    {
        Debug.Log("═══════════════════════════════════════════════════");
        Debug.Log($"     🔍 STAIR DEBUGGER - Testing {stairObject.name}");
        Debug.Log("═══════════════════════════════════════════════════");
        
        // Get stair bounds
        Collider stairCollider = stairObject.GetComponent<Collider>();
        if (stairCollider == null)
        {
            stairCollider = stairObject.GetComponentInChildren<Collider>();
        }
        
        if (stairCollider == null)
        {
            Debug.LogError($"❌ {stairObject.name} TIDAK PUNYA COLLIDER!");
            Debug.LogError("   → Tangga harus punya Collider untuk terdeteksi raycast!");
            return;
        }
        
        Bounds bounds = stairCollider.bounds;
        
        Debug.Log($"\n[1] STAIR INFO:");
        Debug.Log($"  - Name: {stairObject.name}");
        Debug.Log($"  - Tag: {stairObject.tag}");
        Debug.Log($"  - Layer: {LayerMask.LayerToName(stairObject.layer)}");
        Debug.Log($"  - Collider: {stairCollider.GetType().Name}");
        Debug.Log($"  - Bounds Center: {bounds.center}");
        Debug.Log($"  - Bounds Size: {bounds.size}");
        Debug.Log($"  - Bounds Min: {bounds.min}");
        Debug.Log($"  - Bounds Max: {bounds.max}");
        
        // Check tag
        if (!stairObject.CompareTag("Floor"))
        {
            Debug.LogWarning($"⚠️ Tag bukan 'Floor', tag saat ini: '{stairObject.tag}'");
            Debug.LogWarning("   → Set tag menjadi 'Floor' agar terdeteksi!");
        }
        else
        {
            Debug.Log("✓ Tag 'Floor' sudah benar");
        }
        
        // Test raycasts di beberapa titik
        Debug.Log($"\n[2] TESTING RAYCASTS:");
        Debug.Log($"  - Test area: {testPointsX}x{testPointsZ} points");
        Debug.Log($"  - Raycast from Y={raycastHeight}");
        
        testPoints = new Vector3[testPointsX * testPointsZ];
        testHits = new bool[testPointsX * testPointsZ];
        testHitInfo = new RaycastHit[testPointsX * testPointsZ];
        
        int hitCount = 0;
        int missCount = 0;
        int hitStairCount = 0;
        
        int index = 0;
        for (int x = 0; x < testPointsX; x++)
        {
            for (int z = 0; z < testPointsZ; z++)
            {
                // Test point di area tangga
                float xPercent = x / (float)(testPointsX - 1);
                float zPercent = z / (float)(testPointsZ - 1);
                
                Vector3 testPos = new Vector3(
                    Mathf.Lerp(bounds.min.x, bounds.max.x, xPercent),
                    raycastHeight,
                    Mathf.Lerp(bounds.min.z, bounds.max.z, zPercent)
                );
                
                testPoints[index] = testPos;
                
                // Raycast downward
                RaycastHit hit;
                bool didHit = Physics.Raycast(testPos, Vector3.down, out hit, raycastDistance);
                
                testHits[index] = didHit;
                testHitInfo[index] = hit;
                
                if (didHit)
                {
                    hitCount++;
                    
                    if (hit.collider.gameObject == stairObject || 
                        hit.collider.transform.IsChildOf(stairObject.transform))
                    {
                        hitStairCount++;
                        
                        if (hitStairCount <= 3)
                        {
                            Debug.Log($"  ✓ [{x},{z}] HIT STAIR at {hit.point}");
                            Debug.Log($"       Tag={hit.collider.tag}, Name={hit.collider.name}");
                        }
                    }
                    else
                    {
                        if (missCount == 0)
                        {
                            Debug.Log($"  ⚠️ [{x},{z}] Hit OTHER object: {hit.collider.name}");
                        }
                    }
                }
                else
                {
                    missCount++;
                }
                
                index++;
            }
        }
        
        Debug.Log($"\n[3] RESULTS:");
        Debug.Log($"  - Total test points: {testPoints.Length}");
        Debug.Log($"  - Hits (any): {hitCount}");
        Debug.Log($"  - Hits (stair): {hitStairCount}");
        Debug.Log($"  - Misses: {missCount}");
        
        float successRate = (hitStairCount / (float)testPoints.Length) * 100f;
        Debug.Log($"  - Stair detection rate: {successRate:F1}%");
        
        // Diagnosis
        Debug.Log($"\n[4] DIAGNOSIS:");
        
        if (hitStairCount == 0)
        {
            Debug.LogError("❌ TANGGA TIDAK TERDETEKSI SAMA SEKALI!");
            Debug.LogError("\nPossible causes:");
            Debug.LogError("1. Raycast height terlalu rendah (di bawah tangga)");
            Debug.LogError($"   → Coba naikkan raycastHeight > {bounds.max.y + 5}");
            Debug.LogError("2. Tangga tidak punya collider atau collider disabled");
            Debug.LogError("   → Pastikan collider enabled di Inspector");
            Debug.LogError("3. Layer tangga diignore oleh Physics");
            Debug.LogError("   → Check Physics Layer Collision Matrix");
        }
        else if (successRate < 30f)
        {
            Debug.LogWarning($"⚠️ Deteksi rendah ({successRate:F1}%)!");
            Debug.LogWarning("\nSuggestions:");
            Debug.LogWarning("1. GridBuilder.nodeSpacing terlalu besar");
            Debug.LogWarning("   → Perkecil ke 0.5 untuk tangga sempit");
            Debug.LogWarning("2. Grid origin tidak menutupi area tangga");
            Debug.LogWarning($"   → Set gridOrigin < {bounds.min}");
            Debug.LogWarning("3. Grid size terlalu kecil");
            Debug.LogWarning($"   → Pastikan grid menutupi sampai {bounds.max}");
        }
        else
        {
            Debug.Log("✓ Tangga terdeteksi dengan baik!");
            Debug.Log("\nJika masih tidak ada titik hijau:");
            Debug.Log("1. Check GridBuilder.gridOrigin menutupi area tangga");
            Debug.Log("2. Check GridBuilder.nodeSpacing (coba 0.5 atau lebih kecil)");
            Debug.Log("3. Check GridBuilder.detectionMethod = UseBoth atau UseTag");
        }
        
        Debug.Log("═══════════════════════════════════════════════════");
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos || testPoints == null) return;
        
        // Draw test results
        for (int i = 0; i < testPoints.Length; i++)
        {
            if (testHits[i])
            {
                // Hit - draw green sphere
                Gizmos.color = hitColor;
                Gizmos.DrawSphere(testHitInfo[i].point, 0.2f);
                
                // Draw line from start to hit
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawLine(testPoints[i], testHitInfo[i].point);
            }
            else
            {
                // Miss - draw red cross
                Gizmos.color = missColor;
                Vector3 pos = testPoints[i];
                Gizmos.DrawLine(pos + Vector3.left * 0.2f, pos + Vector3.right * 0.2f);
                Gizmos.DrawLine(pos + Vector3.forward * 0.2f, pos + Vector3.back * 0.2f);
            }
        }
        
        // Draw stair bounds
        if (stairObject != null)
        {
            Collider col = stairObject.GetComponent<Collider>();
            if (col == null) col = stairObject.GetComponentInChildren<Collider>();
            
            if (col != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }
    
    [ContextMenu("Test Stair Detection")]
    void TestFromMenu()
    {
        TestStairDetection();
    }
}
