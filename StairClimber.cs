using UnityEngine;

public class StairClimber : MonoBehaviour
{
    [Header("Climbing Settings")]
    public float climbSpeed = 3f;
    public float climbSmoothness = 5f;
    
    [Header("Detection")]
    public LayerMask stairLayer;
    public float detectionRadius = 1f;
    [Tooltip("Jika true, gunakan OnTriggerEnter/Exit pada StairTrigger. Jika false, gunakan OverlapSphere sebagai fallback.")]
    public bool useTriggerDetection = true;
    
    [Header("Debug")]
    public bool showDebug = true;
    
    // State
    private bool isOnStair = false;
    private StairTrigger currentStair = null;
    private Rigidbody rb;
    private MovementLogic movementLogic;
    private bool isSnappingToStart = false;
    
    // Climbing data
    private Vector3 stairStartPos;
    private Vector3 stairEndPos;
    private float climbProgress = 0f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        movementLogic = GetComponent<MovementLogic>();
        
        if (rb == null)
        {
            Debug.LogError("[StairClimber] No Rigidbody found on player!");
        }
    }
    
    void Update()
    {
        // Check for stair nearby (fallback when not using trigger detection)
        if (!useTriggerDetection)
            DetectStairs();
    }
    
    void FixedUpdate()
    {
        if (isSnappingToStart && currentStair != null)
        {
            // Smooth snap to stair start position then allow climbing
            Vector3 target = stairStartPos;
            rb.MovePosition(Vector3.Lerp(rb.position, target, Time.fixedDeltaTime * climbSmoothness * 2f));
            if (Vector3.Distance(rb.position, target) < 0.05f)
                isSnappingToStart = false;
            return;
        }

        if (isOnStair && currentStair != null)
        {
            HandleStairClimbing();
        }
    }
    
    /// <summary>
    /// Detect stairs nearby using trigger
    /// </summary>
    void DetectStairs()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius, stairLayer);

        bool foundStair = false;

        foreach (Collider col in colliders)
        {
            StairTrigger stair = col.GetComponent<StairTrigger>();

            if (stair != null)
            {
                if (!isOnStair)
                {
                    EnterStair(stair);
                }

                currentStair = stair;
                foundStair = true;
                break;
            }
        }

        // Exit stair if no longer detected
        if (!foundStair && isOnStair)
        {
            ExitStair();
        }
    }
    
    /// <summary>
    /// Enter stair climbing mode
    /// </summary>
    void EnterStair(StairTrigger stair)
    {
        isOnStair = true;
        currentStair = stair;
        
        stairStartPos = stair.GetBottomPoint();
        stairEndPos = stair.GetTopPoint();
        climbProgress = 0f;
        
        // Disable gravity while on stair to avoid sliding
        if (rb != null)
            rb.useGravity = false;
        
        // Optionally snap player to stair start for reliable climbing
        if (Vector3.Distance(rb.position, stairStartPos) > 0.1f)
        {
            isSnappingToStart = true;
        }
        
        if (showDebug)
        {
            Debug.Log($"[StairClimber] Entered stair: {stair.gameObject.name}");
        }
        
        // Notify MovementLogic
        if (movementLogic != null)
        {
            movementLogic.SetOnStair(true);
        }
    }
    
    /// <summary>
    /// Exit stair climbing mode
    /// </summary>
    void ExitStair()
    {
        isOnStair = false;
        currentStair = null;
        climbProgress = 0f;
        
        // Re-enable gravity
        if (rb != null)
            rb.useGravity = true;
        
        if (showDebug)
        {
            Debug.Log("[StairClimber] Exited stair");
        }
        
        // Notify MovementLogic
        if (movementLogic != null)
        {
            movementLogic.SetOnStair(false);
        }
    }
    
    /// <summary>
    /// Handle climbing movement
    /// </summary>
    void HandleStairClimbing()
    {
        if (rb == null || currentStair == null) return;
        // Get input direction (W/S or Up/Down)
        float verticalInput = Input.GetAxisRaw("Vertical");

        // Climb direction provided by stair trigger (world space)
        Vector3 climbDir = currentStair.GetClimbDirection().normalized;
        float heightRatio = (currentStair.stairLength > 0f) ? (currentStair.stairHeight / currentStair.stairLength) : 0f;
        Vector3 climbVector = (climbDir + Vector3.up * heightRatio).normalized;

        if (Mathf.Abs(verticalInput) > 0.1f)
        {
            // Build target velocity along climbVector
            Vector3 target = climbVector * verticalInput * climbSpeed;

            // Smoothly interpolate current velocity towards target (preserve lateral movement if any)
            Vector3 smoothVel = Vector3.Lerp(rb.velocity, target, Time.fixedDeltaTime * climbSmoothness);

            rb.velocity = smoothVel;

            if (showDebug)
            {
                Debug.DrawRay(transform.position, smoothVel * 2f, Color.green);
            }
        }
        else
        {
            // No vertical input: zero-out climb component but preserve horizontal movement
            Vector3 planar = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.velocity = Vector3.Lerp(rb.velocity, planar, Time.fixedDeltaTime * climbSmoothness);
        }
    }
    
    /// <summary>
    /// Public getter
    /// </summary>
    public bool IsOnStair()
    {
        return isOnStair;
    }
    
    void OnDrawGizmos()
    {
        // Draw detection radius
        Gizmos.color = isOnStair ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Draw stair path if on stair
        if (isOnStair && currentStair != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(stairStartPos, stairEndPos);
        }
    }
}
