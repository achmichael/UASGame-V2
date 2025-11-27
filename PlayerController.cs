// PlayerController.cs
// Mengatur pergerakan karakter utama (Saipul) dalam mode First-Person
// - Menggunakan CharacterController
// - Input WASD, Jump (Space)
// - Gravity handling dan mencegah menembus dinding (CharacterController menanganinya)
// - Terhubung dengan CameraController melalui inspector (cameraTransform)

using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float gravity = -9.81f;
    public float jumpForce = 2f;

    [Header("References")]
    public Transform cameraTransform; // assign main camera (child of player) in inspector

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    // Ground check helpers (optional: better ground detection)
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Mengecek apakah player menyentuh tanah
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        }
        else
        {
            // fallback ke CharacterController.isGrounded
            isGrounded = controller.isGrounded;
        }

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f; // kecilkan untuk menjaga kontak tanah

        // Input arah (lokal terhadap orientasi player)
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        // Normalisasi diagonal movement jika diperlukan (CharacterController.Move menangani deltaTime)
        if (move.magnitude > 1f) move = move.normalized;

        controller.Move(move * speed * Time.deltaTime);

        // Lompat
        if (Input.GetButtonDown("Jump") && isGrounded)
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

        // Gravitasi
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // Optional: public API to disable movement (for cutscenes)
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
    }
}
