// HUDController.cs
// Menampilkan status player (jumlah lembaran dan nyawa) di layar
// - Menggunakan TextMeshProUGUI untuk teks modern

using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    public TextMeshProUGUI collectibleText;
    public TextMeshProUGUI livesText;

    public void UpdateHUD(int collected, int lives)
    {
        if (collectibleText != null)
            collectibleText.text = $"{collected} / 5";

        if (livesText != null)
            livesText.text = $"Health: {lives}";
    }
}
