// ExitTrigger.cs
// Memicu ending (normal / secret) berdasarkan jumlah lembaran yang dikumpulkan
// - Nonaktifkan kontrol player saat cutscene dimainkan

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ExitTrigger : MonoBehaviour
{
    public CutsceneController cutsceneController;
    public GameObject player;

    private bool hasTriggered = false;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag("Player"))
        {
            hasTriggered = true;

            if (player == null)
                player = other.gameObject;

            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller != null)
                controller.enabled = false; // Nonaktifkan kontrol saat cutscene

            // Pilih ending berdasarkan jumlah koleksi
            if (GameManager.Instance != null && GameManager.Instance.collectedCount >= GameManager.Instance.totalCollectibles)
            {
                cutsceneController?.PlayNormalEnding();
            }
            else
            {
                cutsceneController?.PlaySecretEnding();
            }
        }
    }
}
