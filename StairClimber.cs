using UnityEngine;

/// <summary>
/// StairClimber - Membuat player berjalan smooth di tangga
/// 
/// CARA PENGGUNAAN:
/// 1. Attach script ini ke GameObject Player (yang memiliki MovementLogic)
/// 2. Setup tangga:
///    - Pastikan tangga punya Mesh Collider (convex atau non-convex, NOT trigger)
///    - Tag tangga dengan "Stair" atau assign ke layer "Stair"
/// 3. Layer Setup (PENTING!):
///    - Buat layer "Stair" 
///    - Set layer tangga menjadi "Stair"
///    - Assign "Stair" layer ke stairLayer di Inspector
/// 
/// FITUR:
/// - Smooth vertical movement saat naik/turun tangga
/// - Animation speed adjustment otomatis
/// - Physics correction untuk mencegah sliding
/// - Compatible dengan Rigidbody movement
/// - Bekerja langsung dengan Mesh Collider
/// </summary>`
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MovementLogic))]
public class StairClimber : MonoBehaviour
{
    [Header("References")]
    private Rigidbody rb;
    private MovementLogic movement;
    private Animator anim;
    
    [Header("Detection Settings")]
    [Tooltip("Layer tangga - pastikan tangga menggunakan layer ini")]
    public LayerMask stairLayer;
    
    [Tooltip("Gunakan tag 'Stair' sebagai alternatif layer (jika tidak pakai layer)")]
    public bool useStairTag = true;
    
    [Tooltip("Radius untuk mendeteksi tangga di depan player")]
    public float detectionRadius = 1.5f;
    
    [Tooltip("Distance untuk raycast ke bawah mencari permukaan tangga")]
    public float stairCheckDistance = 2.5f;
    
    [Tooltip("Jumlah raycast untuk deteksi tangga (lebih banyak = lebih akurat)")]
    [Range(1, 9)]
    public int raycastCount = 5;
    
    [Header("Movement Settings")]
    [Tooltip("Seberapa cepat player menyesuaikan tinggi saat di tangga")]
    [Range(1f, 20f)]
    public float heightAdjustmentSpeed = 10f;
    
    [Tooltip("Offset tinggi player dari permukaan tangga")]
    public float heightOffset = 0.1f;
    
    [Tooltip("Multiplier kecepatan animasi saat di tangga (1 = normal speed)")]
    [Range(0.5f, 2f)]
    public float stairAnimationSpeedMultiplier = 1.2f;
    
    [Header("Physics Settings")]
    [Tooltip("Mengurangi gravity saat di tangga untuk movement lebih smooth")]
    public bool reduceGravityOnStairs = true;
    
    [Tooltip("Gravity scale saat di tangga (0 = no gravity, 1 = normal)")]
    [Range(0f, 1f)]
    public float stairGravityScale = 0.3f;
    
    [Tooltip("Extra downward force untuk menjaga kontak dengan tangga")]
    public float stairDownForce = 5f;
    
    [Header("Smooth Movement")]
    [Tooltip("Step smoothing - mengurangi 'bumpy' movement")]
    public bool enableStepSmoothing = true;
    
    [Tooltip("Max step height yang bisa di-smooth")]
    public float maxStepHeight = 0.4f;
    
    [Tooltip("Kecepatan smoothing vertical")]
    public float stepSmoothSpeed = 15f;
    
    [Header("Slope Detection")]
    [Tooltip("Angle minimum untuk dianggap tangga (derajat)")]
    [Range(0f, 89f)]
    public float minStairAngle = 20f;
    
    [Tooltip("Angle maksimum untuk dianggap tangga (derajat)")]
    [Range(1f, 89f)]
    public float maxStairAngle = 70f;
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool showGizmos = true;
    
    // State tracking
    private bool isOnStair = false;
    private bool wasOnStair = false;
    private GameObject currentStairObject;
    private Vector3 stairNormal;
    private float stairAngle;
    
    // Height smoothing
    private float targetHeight;
    private float smoothHeight;
    private Vector3 lastGroundPosition;
    private Vector3 lastHitPoint;
    
    // Animation
    private float originalAnimationSpeed = 1f;
    
    // Physics
    private Vector3 originalGravity;
    private bool useCustomGravity = false;
    
    void Start()
    {
        // Get components
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<MovementLogic>();
        anim = GetComponent<Animator>();
        
        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>();
        }
        
        // Validate
        if (rb == null)
        {
            Debug.LogError("[StairClimber] Rigidbody not found!");
            enabled = false;
            return;
        }
        
        if (movement == null)
        {
            Debug.LogError("[StairClimber] MovementLogic not found!");
            enabled = false;
            return;
        }
        
        // Setup
        originalGravity = Physics.gravity;
        smoothHeight = transform.position.y;
        lastGroundPosition = transform.position;
        
        if (showDebugInfo)
        {
            Debug.Log("[StairClimber] Initialized successfully");
        }
    }
    
    void Update()
    {
        // Detect stair
        DetectStair();
        
        // Update state
        UpdateStairState();
    }
    
    void FixedUpdate()
    {
        if (isOnStair)
        {
            // Apply stair physics
            ApplyStairPhysics();
            
            // Smooth height adjustment
            if (enableStepSmoothing)
            {
                SmoothStepMovement();
            }
        }
    }
    
    void LateUpdate()
    {
        // Cleanup state tracking
        wasOnStair = isOnStair;
    }
    
    /// <summary>
    /// Detect if player is on stairs using multiple raycasts
    /// </summary>
    void DetectStair()
    {
        bool foundStair = false;
        RaycastHit bestHit = new RaycastHit();
        float closestDistance = float.MaxValue;
        
        // Multiple raycasts dalam pattern grid untuk deteksi lebih akurat
        Vector3 basePosition = transform.position + Vector3.up * 0.5f;
        
        for (int i = 0; i < raycastCount; i++)
        {
            Vector3 rayStart = basePosition;
            
            // Pattern: center + cardinal directions + diagonals
            switch (i)
            {
                case 0: // Center
                    break;
                case 1: // Forward
                    rayStart += transform.forward * 0.3f;
                    break;
                case 2: // Back
                    rayStart -= transform.forward * 0.3f;
                    break;
                case 3: // Right
                    rayStart += transform.right * 0.3f;
                    break;
                case 4: // Left
                    rayStart -= transform.right * 0.3f;
                    break;
                case 5: // Forward-Right
                    rayStart += (transform.forward + transform.right).normalized * 0.3f;
                    break;
                case 6: // Forward-Left
                    rayStart += (transform.forward - transform.right).normalized * 0.3f;
                    break;
                case 7: // Back-Right
                    rayStart += (-transform.forward + transform.right).normalized * 0.3f;
                    break;
                case 8: // Back-Left
                    rayStart += (-transform.forward - transform.right).normalized * 0.3f;
                    break;
            }
            
            RaycastHit hit;
            bool hitDetected = false;
            
            // Raycast dengan layer mask atau tag check
            if (stairLayer != 0)
            {
                // Gunakan layer
                if (Physics.Raycast(rayStart, Vector3.down, out hit, stairCheckDistance, stairLayer))
                {
                    hitDetected = true;
                    if (IsValidStairSurface(hit))
                    {
                        if (hit.distance < closestDistance)
                        {
                            closestDistance = hit.distance;
                            bestHit = hit;
                            foundStair = true;
                        }
                    }
                }
            }
            else if (useStairTag)
            {
                // Gunakan tag sebagai fallback
                if (Physics.Raycast(rayStart, Vector3.down, out hit, stairCheckDistance))
                {
                    hitDetected = true;
                    if (hit.collider.CompareTag("Stair") && IsValidStairSurface(hit))
                    {
                        if (hit.distance < closestDistance)
                        {
                            closestDistance = hit.distance;
                            bestHit = hit;
                            foundStair = true;
                        }
                    }
                }
            }
            
            // Debug visualization
            if (showDebugInfo)
            {
                Color rayColor = hitDetected && foundStair ? Color.green : Color.red;
                Debug.DrawLine(rayStart, rayStart + Vector3.down * stairCheckDistance, rayColor, 0.1f);
            }
        }
        
        // Update state
        if (foundStair)
        {
            isOnStair = true;
            currentStairObject = bestHit.collider.gameObject;
            stairNormal = bestHit.normal;
            stairAngle = Vector3.Angle(Vector3.up, bestHit.normal);
            targetHeight = bestHit.point.y + heightOffset;
            lastHitPoint = bestHit.point;
            
            if (showDebugInfo)
            {
                Debug.DrawLine(bestHit.point, bestHit.point + bestHit.normal, Color.cyan, 0.1f);
                Debug.Log($"[StairClimber] On stair: {currentStairObject.name}, Angle: {stairAngle:F1}°");
            }
        }
        else
        {
            isOnStair = false;
            currentStairObject = null;
        }
    }
    
    /// <summary>
    /// Check if surface is valid stair based on angle
    /// </summary>
    bool IsValidStairSurface(RaycastHit hit)
    {
        // Calculate angle from horizontal
        float angle = Vector3.Angle(Vector3.up, hit.normal);
        
        // Check if angle is within stair range
        bool isValidAngle = angle >= minStairAngle && angle <= maxStairAngle;
        
        if (showDebugInfo && !isValidAngle)
        {
            Debug.Log($"[StairClimber] Invalid angle: {angle:F1}° (Range: {minStairAngle}-{maxStairAngle})");
        }
        
        return isValidAngle;
    }
    
    /// <summary>
    /// Update stair state and notify MovementLogic
    /// </summary>
    void UpdateStairState()
    {
        // State changed
        if (isOnStair != wasOnStair)
        {
            if (isOnStair)
            {
                OnEnterStair();
            }
            else
            {
                OnExitStair();
            }
        }
        
        // Update animation speed
        if (isOnStair && anim != null)
        {
            // Calculate slope factor untuk animation speed berdasarkan angle tangga
            float slopeFactor = 1f + (stairAngle / 90f) * 0.5f; // Max 1.5x speed di 90 derajat
            
            anim.speed = originalAnimationSpeed * stairAnimationSpeedMultiplier * slopeFactor;
            
            if (showDebugInfo)
            {
                Debug.Log($"[StairClimber] Animation speed: {anim.speed:F2} (Angle: {stairAngle:F1}°)");
            }
        }
        else if (anim != null)
        {
            // Reset animation speed
            anim.speed = originalAnimationSpeed;
        }
    }
    
    /// <summary>
    /// Called when entering stair
    /// </summary>
    void OnEnterStair()
    {
        if (showDebugInfo)
        {
            Debug.Log("[StairClimber] Entered stair");
        }
        
        // Notify MovementLogic
        if (movement != null)
        {
            movement.SetOnStair(true);
        }
        
        // Initialize smooth height
        smoothHeight = transform.position.y;
        
        // Setup custom gravity
        if (reduceGravityOnStairs)
        {
            useCustomGravity = true;
        }
    }
    
    /// <summary>
    /// Called when exiting stair
    /// </summary>
    void OnExitStair()
    {
        if (showDebugInfo)
        {
            Debug.Log("[StairClimber] Exited stair");
        }
        
        // Notify MovementLogic
        if (movement != null)
        {
            movement.SetOnStair(false);
        }
        
        // Reset gravity
        useCustomGravity = false;
        
        // Reset animation
        if (anim != null)
        {
            anim.speed = originalAnimationSpeed;
        }
    }
    
    /// <summary>
    /// Apply physics adjustments for stairs
    /// </summary>
    void ApplyStairPhysics()
    {
        if (rb == null) return;
        
        // Reduce gravity on stairs
        if (useCustomGravity)
        {
            // Cancel out normal gravity
            rb.AddForce(-Physics.gravity, ForceMode.Acceleration);
            
            // Apply reduced gravity
            rb.AddForce(Physics.gravity * stairGravityScale, ForceMode.Acceleration);
        }
        
        // Apply downward force to maintain contact
        // Only apply if player is moving
        if (movement != null && rb.velocity.magnitude > 0.5f)
        {
            rb.AddForce(Vector3.down * stairDownForce, ForceMode.Force);
        }
        
        // Prevent sliding backward on stairs
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        
        if (horizontalVelocity.magnitude < 0.1f)
        {
            // If nearly stopped, lock position to prevent sliding
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
        }
    }
    
    /// <summary>
    /// Smooth step movement untuk menghilangkan 'bumpy' effect
    /// </summary>
    void SmoothStepMovement()
    {
        if (rb == null) return;
        
        // Get current position
        Vector3 currentPos = transform.position;
        
        // Calculate height difference
        float heightDiff = targetHeight - currentPos.y;
        
        // Only smooth if difference is within step height
        if (Mathf.Abs(heightDiff) < maxStepHeight)
        {
            // Smooth interpolation
            smoothHeight = Mathf.Lerp(
                smoothHeight,
                targetHeight,
                Time.fixedDeltaTime * stepSmoothSpeed
            );
            
            // Apply smoothed height
            Vector3 newPos = currentPos;
            newPos.y = smoothHeight;
            
            // Move player smoothly
            rb.MovePosition(newPos);
            
            if (showDebugInfo && Mathf.Abs(heightDiff) > 0.01f)
            {
                Debug.Log($"[StairClimber] Smoothing height: {currentPos.y:F3} -> {smoothHeight:F3}");
            }
        }
        else
        {
            // Large height difference - reset smooth height
            smoothHeight = currentPos.y;
        }
    }
    
    /// <summary>
    /// Alternative collision-based detection untuk mesh collider
    /// </summary>
    void OnCollisionStay(Collision collision)
    {
        // Check if colliding dengan tangga
        bool isStairCollision = false;
        
        if (stairLayer != 0)
        {
            // Check layer
            if (((1 << collision.gameObject.layer) & stairLayer) != 0)
            {
                isStairCollision = true;
            }
        }
        
        if (useStairTag && collision.gameObject.CompareTag("Stair"))
        {
            isStairCollision = true;
        }
        
        if (isStairCollision)
        {
            // Verify surface angle dari contact points
            foreach (ContactPoint contact in collision.contacts)
            {
                float angle = Vector3.Angle(Vector3.up, contact.normal);
                
                if (angle >= minStairAngle && angle <= maxStairAngle)
                {
                    // Valid stair surface
                    currentStairObject = collision.gameObject;
                    stairNormal = contact.normal;
                    stairAngle = angle;
                    
                    if (showDebugInfo)
                    {
                        Debug.DrawLine(contact.point, contact.point + contact.normal, Color.magenta, 0.1f);
                    }
                    
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// Public API - Force set stair state
    /// </summary>
    public void SetOnStair(bool value, GameObject stairObject = null)
    {
        isOnStair = value;
        currentStairObject = stairObject;
        
        if (value)
        {
            OnEnterStair();
        }
        else
        {
            OnExitStair();
        }
    }
    
    /// <summary>
    /// Get current stair state
    /// </summary>
    public bool IsOnStair()
    {
        return isOnStair;
    }
    
    /// <summary>
    /// Debug visualization
    /// </summary>
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Draw detection sphere
        Gizmos.color = isOnStair ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Draw raycast check
        if (Application.isPlaying)
        {
            Vector3 rayStart = transform.position + Vector3.up * 0.5f;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(rayStart, rayStart + Vector3.down * stairCheckDistance);
            
            // Draw target height
            if (isOnStair)
            {
                Gizmos.color = Color.cyan;
                Vector3 targetPos = transform.position;
                targetPos.y = targetHeight;
                Gizmos.DrawWireCube(targetPos, Vector3.one * 0.3f);
            }
        }
    }
}
