// WallCollisionPreventer.cs
// Mencegah GameObject (Player/Ghost) menembus dinding labirin
// - Tambahkan script ini ke Player dan Ghost
// - Akan auto-correct posisi jika menembus wall

using UnityEngine;

public class WallCollisionPreventer : MonoBehaviour
{
    [Header("Wall Detection")]
    public LayerMask wallLayer; // Set ke layer "Wall"
    public float detectionRadius = 0.5f;
    public float pushBackForce = 2f;
    public bool autoCorrectPosition = true;
    
    [Header("Debug")]
    public bool showDebugRays = false;
    
    private CharacterController characterController;
    private Rigidbody rb;
    private Vector3 lastValidPosition;
    
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();
        lastValidPosition = transform.position;
    }
    
    void Update()
    {
        // Check apakah sedang menembus wall
        if (IsInsideWall())
        {
            if (autoCorrectPosition)
            {
                CorrectPosition();
            }
        }
        else
        {
            // Update last valid position
            lastValidPosition = transform.position;
        }
    }
    
    /// <summary>
    /// Check apakah object sedang berada di dalam wall
    /// </summary>
    bool IsInsideWall()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, wallLayer);
        return colliders.Length > 0;
    }
    
    /// <summary>
    /// Perbaiki posisi jika menembus wall
    /// </summary>
    void CorrectPosition()
    {
        // Metode 1: Kembali ke last valid position
        if (Vector3.Distance(transform.position, lastValidPosition) < 5f)
        {
            if (characterController != null)
            {
                characterController.enabled = false;
                transform.position = lastValidPosition;
                characterController.enabled = true;
            }
            else
            {
                transform.position = lastValidPosition;
            }
            
            Debug.LogWarning($"[WallPreventer] {gameObject.name} corrected to last valid position: {lastValidPosition}");
            return;
        }
        
        // Metode 2: Push away dari nearest wall
        PushAwayFromWall();
    }
    
    /// <summary>
    /// Push object menjauh dari wall terdekat
    /// </summary>
    void PushAwayFromWall()
    {
        Collider[] walls = Physics.OverlapSphere(transform.position, detectionRadius * 2f, wallLayer);
        
        if (walls.Length > 0)
        {
            // Hitung direction menjauh dari semua walls
            Vector3 pushDirection = Vector3.zero;
            
            foreach (Collider wall in walls)
            {
                Vector3 directionFromWall = transform.position - wall.ClosestPoint(transform.position);
                pushDirection += directionFromWall.normalized;
            }
            
            pushDirection = pushDirection.normalized;
            
            // Apply correction
            Vector3 correctedPosition = transform.position + pushDirection * pushBackForce;
            
            if (characterController != null)
            {
                characterController.enabled = false;
                transform.position = correctedPosition;
                characterController.enabled = true;
            }
            else if (rb != null)
            {
                rb.MovePosition(correctedPosition);
            }
            else
            {
                transform.position = correctedPosition;
            }
            
            Debug.LogWarning($"[WallPreventer] {gameObject.name} pushed away from wall to {correctedPosition}");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugRays) return;
        
        // Draw detection sphere
        Gizmos.color = IsInsideWall() ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Draw last valid position
        if (Application.isPlaying && lastValidPosition != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(lastValidPosition, 0.2f);
            Gizmos.DrawLine(transform.position, lastValidPosition);
        }
    }
}
