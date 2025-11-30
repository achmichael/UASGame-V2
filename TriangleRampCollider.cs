// ====================================================================================
// TRIANGLE RAMP COLLIDER GENERATOR
// Generates collider untuk tangga berbentuk segitiga siku-siku
// ====================================================================================
// 3 METODE:
// 1. Single Rotated Box (FASTEST - Recommended)
// 2. Mesh Collider (MOST ACCURATE)
// 3. Multiple Slanted Boxes (HYBRID)
// ====================================================================================

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TriangleRampCollider : MonoBehaviour
{
    [Header("Ramp Dimensions")]
    [Tooltip("Lebar ramp (X-axis)")]
    public float rampWidth = 2f;
    
    [Tooltip("Tinggi ramp (Y-axis) - vertical height")]
    public float rampHeight = 2f;
    
    [Tooltip("Panjang horizontal ramp (Z-axis) - horizontal length")]
    public float rampLength = 3f;
    
    [Header("Collider Method")]
    public RampMethod method = RampMethod.SingleRotatedBox;
    
    public enum RampMethod
    {
        SingleRotatedBox,    // 1 box collider dirotasi (RECOMMENDED)
        MeshCollider,        // Gunakan mesh dari visual
        MultipleBoxes        // Multiple boxes untuk smooth surface
    }
    
    [Header("Multi-Box Settings")]
    [Tooltip("Jumlah boxes untuk method MultipleBoxes")]
    [Range(3, 20)]
    public int boxCount = 10;
    
    [Header("Settings")]
    public bool generateOnStart = true;
    public bool clearOldColliders = true;
    
    [Header("Visualization")]
    public bool showGizmos = true;
    public Color gizmoColor = new Color(0, 1, 0, 0.3f);
    
    private GameObject colliderParent;
    
    void Start()
    {
        if (generateOnStart)
        {
            GenerateRampCollider();
        }
    }
    
    /// <summary>
    /// Main generation function
    /// </summary>
    [ContextMenu("Generate Ramp Collider")]
    public void GenerateRampCollider()
    {
        if (clearOldColliders)
            ClearExistingColliders();
        
        switch (method)
        {
            case RampMethod.SingleRotatedBox:
                GenerateSingleRotatedBox();
                break;
            case RampMethod.MeshCollider:
                GenerateMeshCollider();
                break;
            case RampMethod.MultipleBoxes:
                GenerateMultipleBoxes();
                break;
        }
        
        Debug.Log($"[TriangleRampCollider] Generated using {method} method");
        Debug.Log($"[TriangleRampCollider] Dimensions: {rampWidth}W x {rampHeight}H x {rampLength}L");
    }
    
    // ====================================================================================
    // METHOD 1: Single Rotated Box (RECOMMENDED)
    // Paling simple dan efisien
    // ====================================================================================
    
    void GenerateSingleRotatedBox()
    {
        // Create collider object
        GameObject rampCollider = new GameObject("RampCollider_Rotated");
        rampCollider.transform.parent = transform;
        rampCollider.transform.localPosition = Vector3.zero;
        rampCollider.layer = gameObject.layer;
        rampCollider.tag = gameObject.tag;
        
        // Add box collider
        BoxCollider box = rampCollider.AddComponent<BoxCollider>();
        
        // Calculate diagonal length (hypotenuse)
        float diagonal = Mathf.Sqrt(rampLength * rampLength + rampHeight * rampHeight);
        
        // Set box size
        box.size = new Vector3(rampWidth, 0.1f, diagonal);
        
        // Calculate angle
        float angle = Mathf.Atan2(rampHeight, rampLength) * Mathf.Rad2Deg;
        
        // Rotate box to match ramp slope
        rampCollider.transform.localRotation = Quaternion.Euler(-angle, 0, 0);
        
        // Position box di tengah ramp
        float centerY = rampHeight / 2f;
        float centerZ = rampLength / 2f;
        rampCollider.transform.localPosition = new Vector3(0, centerY, centerZ);
        
        colliderParent = rampCollider;
        
        Debug.Log($"[TriangleRampCollider] Rotated box at {angle:F1}° angle");
    }
    
    // ====================================================================================
    // METHOD 2: Mesh Collider
    // Gunakan mesh dari visual object
    // ====================================================================================
    
    void GenerateMeshCollider()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("[TriangleRampCollider] No MeshFilter found! Add visual mesh first or use SingleRotatedBox method.");
            return;
        }
        
        // Add or get mesh collider
        MeshCollider meshCollider = GetComponent<MeshCollider>();
        if (meshCollider == null)
            meshCollider = gameObject.AddComponent<MeshCollider>();
        
        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshCollider.convex = true;  // Required for Rigidbody collision
        meshCollider.isTrigger = false;
        
        Debug.Log("[TriangleRampCollider] Mesh collider created from visual mesh");
    }
    
    // ====================================================================================
    // METHOD 3: Multiple Boxes (Smooth Surface)
    // Lebih akurat tapi lebih banyak colliders
    // ====================================================================================
    
    void GenerateMultipleBoxes()
    {
        colliderParent = new GameObject("RampCollider_MultiBox");
        colliderParent.transform.parent = transform;
        colliderParent.transform.localPosition = Vector3.zero;
        
        float stepLength = rampLength / boxCount;
        float stepHeight = rampHeight / boxCount;
        
        for (int i = 0; i < boxCount; i++)
        {
            CreateRampSegment(i, stepLength, stepHeight);
        }
        
        Debug.Log($"[TriangleRampCollider] Created {boxCount} box segments");
    }
    
    void CreateRampSegment(int index, float stepLength, float stepHeight)
    {
        GameObject segment = new GameObject($"RampSegment_{index + 1}");
        segment.transform.parent = colliderParent.transform;
        segment.layer = gameObject.layer;
        segment.tag = gameObject.tag;
        
        // Position
        float zPos = index * stepLength;
        float yPos = index * stepHeight;
        segment.transform.localPosition = new Vector3(0, yPos, zPos);
        
        // Add collider
        BoxCollider box = segment.AddComponent<BoxCollider>();
        
        // Calculate angle for this segment
        float angle = Mathf.Atan2(stepHeight, stepLength) * Mathf.Rad2Deg;
        float diagonal = Mathf.Sqrt(stepLength * stepLength + stepHeight * stepHeight);
        
        box.size = new Vector3(rampWidth, 0.1f, diagonal);
        
        // Rotate to match slope
        segment.transform.localRotation = Quaternion.Euler(-angle, 0, 0);
        
        // Center position
        segment.transform.localPosition += new Vector3(0, stepHeight / 2f, stepLength / 2f);
    }
    
    // ====================================================================================
    // UTILITY FUNCTIONS
    // ====================================================================================
    
    [ContextMenu("Clear Ramp Colliders")]
    public void ClearExistingColliders()
    {
        // Clear child colliders
        foreach (Transform child in transform)
        {
            if (child.name.Contains("RampCollider"))
            {
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }
        
        // Clear mesh collider
        MeshCollider meshCol = GetComponent<MeshCollider>();
        if (meshCol != null)
        {
            if (Application.isPlaying)
                Destroy(meshCol);
            else
                DestroyImmediate(meshCol);
        }
        
        Debug.Log("[TriangleRampCollider] Cleared existing colliders");
    }
    
    /// <summary>
    /// Get calculated angle of ramp
    /// </summary>
    public float GetRampAngle()
    {
        return Mathf.Atan2(rampHeight, rampLength) * Mathf.Rad2Deg;
    }
    
    /// <summary>
    /// Get top point of ramp
    /// </summary>
    public Vector3 GetTopPoint()
    {
        return transform.position + transform.forward * rampLength + transform.up * rampHeight;
    }
    
    /// <summary>
    /// Get bottom point of ramp
    /// </summary>
    public Vector3 GetBottomPoint()
    {
        return transform.position;
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Gizmos.matrix = Matrix4x4.identity;
        
        // Draw ramp outline
        Vector3 p1 = transform.position; // Bottom left
        Vector3 p2 = transform.position + transform.right * rampWidth; // Bottom right
        Vector3 p3 = transform.position + transform.forward * rampLength + transform.up * rampHeight; // Top left
        Vector3 p4 = p3 + transform.right * rampWidth; // Top right
        
        // Bottom
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(p1, p2);
        
        // Sides
        Gizmos.color = Color.green;
        Gizmos.DrawLine(p1, p3);
        Gizmos.DrawLine(p2, p4);
        
        // Top
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(p3, p4);
        
        // Hypotenuse (ramp surface)
        Gizmos.color = gizmoColor;
        Gizmos.DrawLine(p1, p3);
        Gizmos.DrawLine(p2, p4);
        
        // Draw triangle face (left side)
        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Vector3 backBottom = p1;
        Vector3 backTop = p3;
        Vector3 backCorner = p1 + transform.up * rampHeight;
        
        // Draw direction arrow
        if (Application.isPlaying)
        {
            Vector3 center = (p1 + p3) / 2f;
            Vector3 direction = (p3 - p1).normalized;
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(center, center + direction * 1f);
            Gizmos.DrawSphere(center + direction * 1f, 0.1f);
        }
        
        // Draw angle indicator
        Gizmos.color = Color.white;
        float angle = GetRampAngle();
        Vector3 labelPos = p1 + Vector3.up * 0.5f + transform.forward * 0.5f;
        
#if UNITY_EDITOR
        UnityEditor.Handles.Label(labelPos, $"Angle: {angle:F1}°");
#endif
    }
}

#if UNITY_EDITOR
// ====================================================================================
// CUSTOM EDITOR
// ====================================================================================
[CustomEditor(typeof(TriangleRampCollider))]
public class TriangleRampColliderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        TriangleRampCollider ramp = (TriangleRampCollider)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Generate Ramp Collider", GUILayout.Height(40)))
        {
            ramp.GenerateRampCollider();
        }
        
        if (GUILayout.Button("Clear Colliders"))
        {
            ramp.ClearExistingColliders();
        }
        
        EditorGUILayout.Space();
        
        // Show calculated values
        EditorGUILayout.LabelField("Calculated Values", EditorStyles.boldLabel);
        float angle = ramp.GetRampAngle();
        float diagonal = Mathf.Sqrt(ramp.rampLength * ramp.rampLength + ramp.rampHeight * ramp.rampHeight);
        
        EditorGUILayout.LabelField($"Ramp Angle: {angle:F2}°");
        EditorGUILayout.LabelField($"Surface Length: {diagonal:F2}m");
        EditorGUILayout.LabelField($"Total Volume: {ramp.rampWidth * ramp.rampHeight * ramp.rampLength / 2f:F2}m³");
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "TRIANGLE RAMP SETUP:\n\n" +
            "1. Set Width, Height, Length above\n" +
            "2. Choose method (SingleRotatedBox recommended)\n" +
            "3. Click 'Generate Ramp Collider'\n\n" +
            "METHODS:\n" +
            "• SingleRotatedBox = Fast, efficient (best)\n" +
            "• MeshCollider = Perfect shape, requires mesh\n" +
            "• MultipleBoxes = Smoother, more colliders",
            MessageType.Info
        );
    }
}
#endif