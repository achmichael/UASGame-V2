using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementLogic : MonoBehaviour
{
    [Header("Components")]
    private Rigidbody rb;
    public Animator anim;
    public Transform cameraTransform; // Reference ke camera
    
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float moveForce = 10f;        // Force multiplier
    public float maxVelocity = 10f;      // Max speed cap
    
    [Header("Jump Settings")]
    public float jumpPower = 10f;
    public float fallSpeed = 2f;
    public float airMultiplier = 0.3f;
    public float jumpForwardMultiplier = 0.5f; // Multiplier untuk arah lompatan
    private Vector3 jumpDirection; // Arah saat melompat
    
    [Header("Ground Detection")]
    public bool useRaycastGroundCheck = false; // Default FALSE untuk compatibility
    public LayerMask groundLayer;
    public float groundCheckDistance = 0.5f;
    private bool grounded = true;
    private bool aerialBoost = true;
    
    [Header("Stair Settings")]
    public bool isOnStair = false;
    
    [Header("Drag Settings")]
    public float groundDrag = 6f;
    public float airDrag = 2f;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    [Header("Game State")]
    public bool TPSMode = true;
    public bool AimMode = false;
    public float HitPoints = 100f;
    
    // Private variables
    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (rb == null)
        {
            Debug.LogError("[MovementLogic] No Rigidbody found!");
            return;
        }
        
        // Setup Rigidbody
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // Auto-find camera
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cameraTransform = cam.transform;
                Debug.Log("[MovementLogic] Camera found: " + cam.name);
            }
            else
            {
                Debug.LogWarning("[MovementLogic] No camera found! Using player transform for movement.");
                cameraTransform = transform; // Fallback
            }
        }
        
        // Validate animator
        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }
        
        Debug.Log("[MovementLogic] Initialized - Walk Speed: " + walkSpeed + ", Run Speed: " + runSpeed);
    }

    void Update()
    {
        // Get input
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        
        // Debug input
        if (showDebugInfo && (horizontalInput != 0 || verticalInput != 0))
        {
            Debug.Log($"[MovementLogic] Input: H={horizontalInput}, V={verticalInput}");
        }
        
        // Handle jumping
        HandleJump();
        
        // Handle aim mode
        HandleAimMode();
        
        // Handle shoot logic
        HandleShootLogic();
        
        // Update animations
        UpdateAnimations();
        
        // Debug damage
        if (Input.GetKeyDown(KeyCode.F))
        {
            PlayerGetHit(100f);
        }
    }
    
    void FixedUpdate()
    {
        // Check grounded
        if (useRaycastGroundCheck)
        {
            CheckGroundedRaycast();
        }
        // Else: grounded stays true (set by collision trigger)
        
        // Apply movement
        MovePlayer();
        
        // Apply drag
        ApplyDrag();
        
        // Limit speed
        LimitSpeed();
    }
    
    /// <summary>
    /// Main movement function
    /// </summary>
    void MovePlayer()
    {
        // Calculate movement direction
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        
        // Flatten vectors (no flying)
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        
        // Calculate desired direction
        moveDirection = forward * verticalInput + right * horizontalInput;
        
        // Normalize untuk consistent speed
        if (moveDirection.magnitude > 1f)
            moveDirection.Normalize();
        
        // Determine speed
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        
        // Apply force
        if (grounded)
        {
            // Ground movement
            rb.AddForce(moveDirection * currentSpeed * moveForce, ForceMode.Force);
            
            if (showDebugInfo && moveDirection.magnitude > 0.1f)
            {
                Debug.Log($"[MovementLogic] Moving - Speed: {currentSpeed}, Grounded: {grounded}, Velocity: {rb.velocity.magnitude:F2}");
            }
        }
        else
        {
            // Air movement - sangat terbatas untuk mencegah perubahan arah tiba-tiba
            // Hanya izinkan sedikit penyesuaian arah, bukan perubahan total
            Vector3 currentHorizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            Vector3 desiredAirControl = moveDirection * currentSpeed * moveForce * airMultiplier;
            
            // Batasi air control agar tidak berlawanan dengan momentum saat jump
            if (currentHorizontalVelocity.magnitude > 0.5f)
            {
                Vector3 velocityDirection = currentHorizontalVelocity.normalized;
                float alignment = Vector3.Dot(moveDirection.normalized, velocityDirection);
                
                // Jika input berlawanan dengan momentum (alignment negatif), kurangi kontrolnya
                if (alignment < 0f)
                {
                    desiredAirControl *= 0.2f; // Sangat kurangi kontrol saat mencoba berbalik arah
                }
            }
            
            rb.AddForce(desiredAirControl, ForceMode.Force);
            
            // Apply gravity
            rb.AddForce(Vector3.down * fallSpeed, ForceMode.Force);
        }
    }
    
    /// <summary>
    /// Apply drag based on grounded state
    /// </summary>
    void ApplyDrag()
    {
        rb.drag = grounded ? groundDrag : airDrag;
    }
    
    /// <summary>
    /// Limit horizontal velocity
    /// </summary>
    void LimitSpeed()
    {
        Vector3 flatVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        
        if (flatVelocity.magnitude > maxVelocity)
        {
            Vector3 limitedVelocity = flatVelocity.normalized * maxVelocity;
            rb.velocity = new Vector3(limitedVelocity.x, rb.velocity.y, limitedVelocity.z);
        }
    }
    
    /// <summary>
    /// Ground check using raycast (OPTIONAL)
    /// </summary>
    void CheckGroundedRaycast()
    {
        // Always grounded on stairs
        if (isOnStair)
        {
            grounded = true;
            return;
        }
        
        RaycastHit hit;
        bool wasGrounded = grounded;
        
        grounded = Physics.Raycast(
            transform.position, 
            Vector3.down, 
            out hit,
            groundCheckDistance, 
            groundLayer
        );
        
        if (showDebugInfo && grounded != wasGrounded)
        {
            Debug.Log($"[MovementLogic] Grounded state changed: {grounded}");
        }
        
        if (grounded && !wasGrounded)
        {
            OnLanded();
        }
    }
    
    /// <summary>
    /// Handle jump
    /// </summary>
    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            // Simpan arah movement saat melompat
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            jumpDirection = forward * verticalInput + right * horizontalInput;
            
            // Normalize jika ada input
            if (jumpDirection.magnitude > 0.1f)
            {
                jumpDirection.Normalize();
            }
            
            // Reset Y velocity
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            
            // Jump dengan arah
            Vector3 jumpForce = Vector3.up * jumpPower;
            
            // Tambahkan directional force
            if (jumpDirection.magnitude > 0.1f)
            {
                jumpForce += jumpDirection * jumpPower * jumpForwardMultiplier;
            }
            
            rb.AddForce(jumpForce, ForceMode.Impulse);
            
            grounded = false;
            aerialBoost = true;
            
            if (anim != null)
                anim.SetBool("Jump", true);
                
            Debug.Log("[MovementLogic] Jump with direction: " + jumpDirection);
        }
    }
    
    /// <summary>
    /// Called when landing
    /// </summary>
    void OnLanded()
    {
        aerialBoost = true;
        
        if (anim != null)
            anim.SetBool("Jump", false);
    }
    
    /// <summary>
    /// Handle aim mode toggle
    /// </summary>
    void HandleAimMode()
    {
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (AimMode)
            {
                TPSMode = true;
                AimMode = false;
                if (anim != null)
                    anim.SetBool("AimMode", false);
            }
            else if (TPSMode)
            {
                TPSMode = false;
                AimMode = true;
                if (anim != null)
                    anim.SetBool("AimMode", true);
            }
        }
    }
    
    /// <summary>
    /// Handle shooting animations
    /// </summary>
    void HandleShootLogic()
    {
        if (anim == null) return;
        
        if (Input.GetKey(KeyCode.Mouse0))
        {
            bool isMoving = moveDirection.magnitude > 0.1f;
            
            anim.SetBool("WalkShoot", isMoving);
            anim.SetBool("IdleShoot", !isMoving);
        }
        else
        {
            anim.SetBool("WalkShoot", false);
            anim.SetBool("IdleShoot", false);
        }
    }
    
    /// <summary>
    /// Update movement animations
    /// </summary>
    void UpdateAnimations()
    {
        if (anim == null) return;
        
        bool isMoving = moveDirection.magnitude > 0.1f && grounded;
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && isMoving;
        
        anim.SetBool("Walk", isMoving && !isRunning);
        anim.SetBool("Run", isRunning);
    }
    
    /// <summary>
    /// Handle player damage
    /// </summary>
    private void PlayerGetHit(float damage)
    {
        Debug.Log("Player Got Hit and took " + damage + " damage");
        HitPoints -= damage;
        
        if (HitPoints <= 0f)
        {
            HitPoints = 0f;
            if (anim != null)
                anim.SetBool("Death", true);
            
            this.enabled = false;
        }
    }
    
    /// <summary>
    /// PUBLIC: Set grounded dari collision trigger (RECOMMENDED METHOD)
    /// Panggil ini dari script lain atau collision trigger
    /// </summary>
    public void SetGrounded(bool value)
    {
        bool wasGrounded = grounded;
        grounded = value;
        
        if (grounded && !wasGrounded)
        {
            OnLanded();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[MovementLogic] SetGrounded called: {value}");
        }
    }
    
    /// <summary>
    /// PUBLIC: Set stair state - dipanggil dari StairClimber
    /// </summary>
    public void SetOnStair(bool value)
    {
        isOnStair = value;
        
        if (isOnStair)
        {
            // Disable gravity saat di tangga
            rb.useGravity = false;
            grounded = true; // Treat stairs as ground
            
            if (showDebugInfo)
            {
                Debug.Log("[MovementLogic] On stair - gravity disabled");
            }
        }
        else
        {
            // Re-enable gravity
            rb.useGravity = true;
            
            if (showDebugInfo)
            {
                Debug.Log("[MovementLogic] Off stair - gravity enabled");
            }
        }
    }
    
    /// <summary>
    /// Alternative: Gunakan OnCollisionStay untuk ground detection
    /// </summary>
    void OnCollisionStay(Collision collision)
    {
        // Don't override ground state if on stairs
        if (isOnStair)
        {
            grounded = true;
            return;
        }
        
        // Check if colliding with ground
        if (!useRaycastGroundCheck)
        {
            // Simple ground detection: check if contact point is below player
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.normal.y > 0.5f) // Surface is mostly horizontal
                {
                    if (!grounded)
                    {
                        grounded = true;
                        OnLanded();
                    }
                    return;
                }
            }
        }
    }
    
    void OnCollisionExit(Collision collision)
    {
        if (!useRaycastGroundCheck && !isOnStair)
        {
            // Check if we left the ground
            grounded = false;
        }
    }
    
    /// <summary>
    /// Debug visualization
    /// </summary>
    void OnDrawGizmos()
    {
        // Draw ground check ray (if using raycast)
        if (useRaycastGroundCheck)
        {
            Gizmos.color = grounded ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
        }
        
        // Draw movement direction
        if (Application.isPlaying && moveDirection.magnitude > 0.1f)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + moveDirection * 3f);
        }
        
        // Draw velocity
        if (Application.isPlaying && rb != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + rb.velocity);
        }
    }
}