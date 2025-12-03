// CollectibleItem.cs
// Skrip untuk item lembaran Al-Qur’an yang bisa dikumpulkan oleh player
// - Berputar untuk VFX sederhana
// - Memanggil GameManager.Instance.AddCollectedItem() ketika disentuh player
// - Bisa memainkan suara saat dikoleksi

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CollectibleItem : MonoBehaviour
{
    public string itemName = "Lembaran Al-Qur'an";
    public float rotationSpeed = 50f;
    public AudioClip collectSound;
    public Light glowLight; // optional light VFX

    private void Reset()
    {
        // Ensure collider set to trigger for easy collection
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Update()
    {
        // Efek rotasi agar terlihat menarik
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);

        // optional pulsing light
        if (glowLight != null)
        {
            glowLight.intensity = 1f + Mathf.Sin(Time.time * 3f) * 0.25f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Tambahkan ke counter di GameManager
            if (GameManager.Instance != null)
                GameManager.Instance.AddCollectedItem();
            else
                Debug.LogWarning("GameManager.Instance is null. Pastikan GameManager ada di scene.");

            // Mainkan efek suara jika ada
            if (collectSound != null)
            {
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(collectSound, transform.position);
                else
                    AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }

            // Hancurkan objek setelah dikumpulkan
            Destroy(gameObject);
        }
    }
}
