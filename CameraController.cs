using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Settings")]
    public float mouseSensitivity = 200f;
    [Tooltip("Time to smooth the camera movement. Lower is faster.")]
    public float smoothTime = 0.05f;
    public bool lockCursorOnStart = true;

    [Header("References")]
    public Transform playerBody; // assign player root transform (not camera)

    // Internal variables for smoothing
    private float xRotation = 0f;
    private float yRotation = 0f;
    private float currentXRotation;
    private float currentYRotation;
    private float xRotationV;
    private float yRotationV;

    void Start()
    {
        if (lockCursorOnStart)
            Cursor.lockState = CursorLockMode.Locked;

        // Initialize xRotation from current camera angle
        xRotation = transform.localEulerAngles.x;
        // Normalize angle to -180 to 180 range for clamping
        if (xRotation > 180) xRotation -= 360;
        currentXRotation = xRotation;

        // Initialize yRotation from player body
        if (playerBody != null)
        {
            yRotation = playerBody.eulerAngles.y;
            currentYRotation = yRotation;
        }

        // Ensure no Rigidbody on the camera interferes
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    void Update()
    {
        // Gather input in Update
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Accumulate target rotation
        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f); // Vertical limits
    }

    void LateUpdate()
    {
        // Smoothly interpolate current rotation towards target rotation
        currentXRotation = Mathf.SmoothDamp(currentXRotation, xRotation, ref xRotationV, smoothTime);
        currentYRotation = Mathf.SmoothDamp(currentYRotation, yRotation, ref yRotationV, smoothTime);

        // Apply rotation to Camera (Vertical)
        transform.localRotation = Quaternion.Euler(currentXRotation, 0f, 0f);

        // Apply rotation to Player Body (Horizontal)
        if (playerBody != null)
        {
            playerBody.rotation = Quaternion.Euler(0f, currentYRotation, 0f);
        }
    }
}