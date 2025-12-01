// CameraController.cs
// Mengatur rotasi kamera First-Person berdasarkan input mouse
// - Batasi rotasi vertikal agar tidak flip 360 derajat
// - Rotasi horizontal memutar badan player (playerBody)

using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float mouseSensitivity = 100f;
    public Transform playerBody; // assign player root transform (not camera)
    public bool lockCursorOnStart = true;

    float xRotation = 0f;

    void Start()
    {
        if (lockCursorOnStart)
            Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Ambil input mouse (per frame)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f); // batas vertikal

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);
    }
}