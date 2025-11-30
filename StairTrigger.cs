using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class StairTrigger : MonoBehaviour
{
    [Header("Stair Settings")]
    [Tooltip("Tinggi total tangga (otomatis terdeteksi dari collider)")]
    public float stairHeight = 2f;
    
    [Tooltip("Panjang tangga (otomatis terdeteksi dari collider)")]
    public float stairLength = 3f;
    
    [Tooltip("Kecepatan naik/turun tangga")]
    public float climbSpeed = 3f;
    
    [Header("Direction")]
    [Tooltip("Arah tangga naik (Forward = Z+, Right = X+, Back = Z-, Left = X-)")]
    public StairDirection direction = StairDirection.Forward;
    
    [Header("Visualization")]
    public bool showGizmos = true;
    public Color gizmoColor = new Color(0, 1, 1, 0.3f);
    
    private BoxCollider stairCollider;
    
    public enum StairDirection
    {
        Forward,  // Naik ke arah Z+
        Right,    // Naik ke arah X+
        Back,     // Naik ke arah Z-
        Left      // Naik ke arah X-
    }
    
    void Start()
    {
        SetupStairCollider();
        AutoDetectDimensions();
    }
    
    /// <summary>
    /// Setup collider sebagai trigger
    /// </summary>
    void SetupStairCollider()
    {
        stairCollider = GetComponent<BoxCollider>();
        
        if (stairCollider == null)
        {
            stairCollider = gameObject.AddComponent<BoxCollider>();
        }
        
        // Set sebagai trigger agar player bisa masuk
        stairCollider.isTrigger = true;
        
        // Tag tangga untuk easy detection
        if (gameObject.tag == "Untagged")
        {
            gameObject.tag = "Stair";
        }
        
        Debug.Log($"[StairTrigger] Setup complete on {gameObject.name}");
    }
    
    /// <summary>
    /// Auto-detect dimensi tangga dari collider
    /// </summary>
    void AutoDetectDimensions()
    {
        if (stairCollider != null)
        {
            Vector3 size = stairCollider.size;
            
            // Height biasanya di Y-axis
            stairHeight = size.y * transform.localScale.y;
            
            // Length tergantung direction
            switch (direction)
            {
                case StairDirection.Forward:
                case StairDirection.Back:
                    stairLength = size.z * transform.localScale.z;
                    break;
                case StairDirection.Right:
                case StairDirection.Left:
                    stairLength = size.x * transform.localScale.x;
                    break;
            }
            
            Debug.Log($"[StairTrigger] Auto-detected: Height={stairHeight:F2}, Length={stairLength:F2}");
        }
    }
    
    /// <summary>
    /// Get climb direction vector
    /// </summary>
    public Vector3 GetClimbDirection()
    {
        switch (direction)
        {
            case StairDirection.Forward:
                return transform.forward;
            case StairDirection.Right:
                return transform.right;
            case StairDirection.Back:
                return -transform.forward;
            case StairDirection.Left:
                return -transform.right;
            default:
                return transform.forward;
        }
    }
    
    /// <summary>
    /// Get point di puncak tangga
    /// </summary>
    public Vector3 GetTopPoint()
    {
        Vector3 climbDir = GetClimbDirection();
        return transform.position + climbDir * stairLength + Vector3.up * stairHeight;
    }
    
    /// <summary>
    /// Get point di bawah tangga
    /// </summary>
    public Vector3 GetBottomPoint()
    {
        return transform.position;
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Draw stair volume
        Gizmos.color = gizmoColor;
        if (stairCollider != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(stairCollider.center, stairCollider.size);
        }
        
        // Draw climb direction arrow
        if (Application.isPlaying)
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.cyan;
            
            Vector3 start = GetBottomPoint();
            Vector3 end = GetTopPoint();
            
            Gizmos.DrawLine(start, end);
            Gizmos.DrawSphere(end, 0.3f);
        }
    }
}