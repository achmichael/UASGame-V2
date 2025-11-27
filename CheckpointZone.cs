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
                AudioSource.PlayClipAtPoint(activateSound, transform.position);

            Debug.Log($"Checkpoint {checkpointID} diaktifkan!");
        }

        // =============================================================
        // ✨ FITUR TAMBAHAN (UPDATE): Hentikan musuh mengejar player
        // -------------------------------------------------------------
        // Penjelasan:
        // - Ketika player memasuki area checkpoint (baik checkpoint baru
        //   maupun checkpoint yang sudah aktif sebelumnya),
        //   semua musuh diberi sinyal untuk "kehilangan jejak".
        // - Cara kerjanya: memanggil method SetCheckpointState(true)
        //   pada setiap EnemyLogicSementara.
        // - Dengan ini, AI musuh berhenti mengejar sampai player keluar
        //   dari checkpoint.
        // =============================================================
        if (other.CompareTag("Player"))
        {
            // Cari semua enemy yang menggunakan EnemyLogicSementara
            EnemyLogicSementara[] enemies = FindObjectsOfType<EnemyLogicSementara>();

            // Aktifkan mode "player berada di checkpoint"
            foreach (var e in enemies)
            {
                e.SetCheckpointState(true); // Enemy kehilangan jejak
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // =============================================================
        // ✨ FITUR TAMBAHAN (UPDATE): Musuh kembali bisa mengejar
        // -------------------------------------------------------------
        // Penjelasan:
        // - Saat player keluar dari area checkpoint,
        //   AI musuh diaktifkan kembali (bisa mengejar).
        // - Ini menjaga perilaku enemy tetap dinamis.
        // =============================================================
        if (other.CompareTag("Player"))
        {
            EnemyLogicSementara[] enemies = FindObjectsOfType<EnemyLogicSementara>();

            foreach (var e in enemies)
            {
                e.SetCheckpointState(false); // Enemy mulai mengejar lagi
            }
        }
    }
}
