using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    // ==========================
    // BUTTON EVENTS (Dihubungkan via Inspector)
    // ==========================

    public void OnPlayClicked()
    {
        Debug.Log("Resume button clicked");
        // Delegasikan ke GameManager agar state sinkron
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
    }

    public void OnMainMenuClicked()
    {
        Debug.Log("Main Menu button clicked");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GoToMainMenu();
        }
    }

    public void OnSettingClicked()
    {
        Debug.Log("Opening Settings...");
        // Unload pause scene sementara, load setting scene
        // Atau tumpuk di atasnya, tergantung desain. 
        // Di sini kita ikuti pola sebelumnya:
        SceneManager.UnloadSceneAsync("Pause");
        SceneManager.LoadScene("Pause-Setting", LoadSceneMode.Additive);
    }

    public void TryAgain()
    {
        Debug.Log("Retrying level...");
        Time.timeScale = 1f;
        // Reload scene gameplay saat ini (bukan hardcoded string jika memungkinkan)
        // Tapi jika GameManager punya logic khusus, bisa panggil GameManager
        SceneManager.LoadScene("GameplayScene"); 
    }
}
