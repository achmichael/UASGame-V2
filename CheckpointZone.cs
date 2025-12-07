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
    private Color originalLightColor;

    private void Start()
    {
        if (checkpointLight != null)
            originalLightColor = checkpointLight.color;
    }

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Visual: Set warna hijau saat player di dalam
            if (checkpointLight != null)
                checkpointLight.color = Color.green;

            // Logic Aktivasi Checkpoint (Hanya sekali)
            if (!isActivated)
            {
                isActivated = true;

                // Simpan checkpoint aktif di GameManager
                if (GameManager.Instance != null)
                    GameManager.Instance.SetCheckpoint(transform.position);
                else
                    Debug.LogWarning("GameManager.Instance is null. Pastikan GameManager ada di scene.");

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

            // Logic Safe Zone
            MovementLogic playerMovement = other.GetComponent<MovementLogic>();
            if (playerMovement != null)
            {
                playerMovement.isSafe = true;
            }
            
            // Notifikasi semua enemy untuk mulai wander
            NotifyAllEnemiesPlayerSafe(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // =============================================================
        // ✨ FITUR TAMBAHAN (UPDATE): Musuh kembali bisa mengejar
        // -------------------------------------------------------------
        if (other.CompareTag("Player"))
        {
            // Revert warna lampu ke warna asli
            if (checkpointLight != null)
                checkpointLight.color = originalLightColor;

            // Set status safe pada player
            MovementLogic playerMovement = other.GetComponent<MovementLogic>();
            if (playerMovement != null)
            {
                playerMovement.isSafe = false;
            }
            
            // BARU: Notifikasi semua enemy bahwa player sudah tidak safe
            NotifyAllEnemiesPlayerSafe(false);
        }
    }
    
    /// <summary>
    /// Notifikasi semua enemy tentang status safe player
    /// </summary>
    void NotifyAllEnemiesPlayerSafe(bool playerIsSafe)
    {
        GhostAI[] enemies = FindObjectsOfType<GhostAI>();
        
        foreach (GhostAI enemy in enemies)
        {
            if (enemy != null)
            {
                if (playerIsSafe)
                {
                    enemy.OnPlayerEnteredSafeZone();
                }
                else
                {
                    enemy.OnPlayerExitedSafeZone();
                }
            }
        }
        
        Debug.Log($"[CheckpointZone] Notified {enemies.Length} enemies. Player safe: {playerIsSafe}");
    }
}
