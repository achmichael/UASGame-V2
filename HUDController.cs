// HUDController.cs
// Menampilkan status player (jumlah lembaran dan nyawa) di layar
// - Menggunakan TextMeshProUGUI untuk teks modern

using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    public TextMeshProUGUI collectibleText;
    public TextMeshProUGUI livesText;

    void Start()
    {
        // Saat HUD aktif, minta GameManager untuk refresh references dan update UI
        // Ini mengatasi masalah saat GameManager sudah ada (DontDestroyOnLoad)
        // tapi HUD baru saja di-instantiate di scene baru
        if (GameManager.Instance != null)
        {
            // Register diri ke GameManager
            GameManager.Instance.RefreshUIReferences();
        }
    }

    void OnEnable()
    {
        // Juga refresh saat di-enable (untuk kasus HUD di-disable kemudian di-enable lagi)
        if (GameManager.Instance != null)
        {
            // Delay 1 frame untuk memastikan object sudah fully initialized
            StartCoroutine(RequestUpdateNextFrame());
        }
    }

    private System.Collections.IEnumerator RequestUpdateNextFrame()
    {
        yield return null;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RefreshUIReferences();
        }
    }

    public void UpdateHUD(int collected, int lives, int total)
    {
        if (collectibleText != null)
            collectibleText.text = $"{collected} / {total}";

        if (livesText != null)
            livesText.text = $"Health: {lives}";
    }

}
