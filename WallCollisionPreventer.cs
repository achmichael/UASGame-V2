using UnityEngine;

public class WallCollisionPreventer : MonoBehaviour
{
    [Header("Wall Detection")]
    public LayerMask wallLayer;
    public float detectionRadius = 0.5f;
    public float pushBackForce = 2f;
    public bool autoCorrectPosition = true;
    
    // TAMBAHAN: Cooldown untuk mencegah spam correction
    private float correctionCooldown = 0f;
    public float cooldownDuration = 0.5f; // Tunggu 0.5 detik sebelum correction berikutnya
    
    private Rigidbody rb;
    private Vector3 lastValidPosition;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        lastValidPosition = transform.position;
        
        if (rb != null)
        {
            rb.freezeRotation = true;
        }
    }
    
    void Update()
    {
        // Kurangi cooldown timer
        if (correctionCooldown > 0f)
        {
            correctionCooldown -= Time.deltaTime;
            return; // Skip correction saat cooldown aktif
        }
        
        if (IsInsideWall())
        {
            if (autoCorrectPosition)
            {
                CorrectPosition();
                correctionCooldown = cooldownDuration; // Set cooldown
            }
        }
        else
        {
            lastValidPosition = transform.position;
        }
    }
    
    bool IsInsideWall()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, wallLayer);
        return colliders.Length > 0;
    }
    
    void CorrectPosition()
    {
        // Gunakan metode yang lebih smooth
        if (rb != null)
        {
            // Stop velocity dulu
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Kembalikan ke last valid position
            rb.MovePosition(lastValidPosition);
            
            Debug.LogWarning($"Player corrected to {lastValidPosition}");
        }
    }
}