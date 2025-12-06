// CheckpointZone.cs
// Skrip untuk area checkpoint tempat player bisa respawn
// - Simpan posisi checkpoint ke GameManager
// - Aktifkan light/efek dan suara saat pertama kali diaktifkan

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CheckpointZone : MonoBehaviour
{
    public int checkpointID = 0;
    public Light checkpointLight;
    public AudioClip activateSound;
    private bool isActivated = false;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // =============================
        // BAGIAN UTAMA (KODE ORIGINAL TIM)
        // =============================
        if (other.CompareTag("Player") && !isActivated)
        {
            isActivated = true;

            // Simpan checkpoint aktif di GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.SetCheckpoint(transform.position);
            else
                Debug.LogWarning("GameManager.Instance is null. Pastikan GameManager ada di scene.");

            // Aktifkan efek cahaya
            if (checkpointLight != null)
                checkpointLight.color = Color.green;

            // Suara aktivasi
            if (activateSound != null)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(activateSound, transform.position);
                else
                    AudioSource.PlayClipAtPoint(activateSound, transform.position);
            }

            Debug.Log($"Checkpoint {checkpointID} diaktifkan!");
        }

        // =============================================================
        // ✨ FITUR TAMBAHAN (UPDATE): Hentikan musuh mengejar player
        // -------------------------------------------------------------
        if (other.CompareTag("Player"))
        {
            // Set status safe pada player
            // GhostAI akan membaca status ini dan berhenti mengejar
            MovementLogic playerMovement = other.GetComponent<MovementLogic>();
            if (playerMovement != null)
            {
                playerMovement.isSafe = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // =============================================================
        // ✨ FITUR TAMBAHAN (UPDATE): Musuh kembali bisa mengejar
        // -------------------------------------------------------------
        if (other.CompareTag("Player"))
        {
            // Set status safe pada player
            MovementLogic playerMovement = other.GetComponent<MovementLogic>();
            if (playerMovement != null)
            {
                playerMovement.isSafe = false;
            }
        }
    }
}
