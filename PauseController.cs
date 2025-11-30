using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    // Nama scene
    private const string MainMenu = "Main-menu";
    private const string GameplayScene = "GameplayScene";
    private const string PauseScene = "Pause";
    private const string SettingScene = "Pause-Setting";
    private bool isPaused = false;
    private bool isPauseSceneLoaded = false;

    // ==========================
    // MAIN BUTTON ACTIONS
    // ==========================

    public void OnPlayClicked()
    {
        Debug.Log("Play button clicked");
        ResumeGame();
    }

    public void OnMainMenuClicked()
    {
        Debug.Log("Go to main menu");
        Time.timeScale = 1f;
        SceneManager.LoadScene(MainMenu);
    }

    public void OnSettingClicked()
    {
        Debug.Log("Opening pause settings...");
        OpenSettings();
    }

    // ==========================
    // CORE PAUSE LOGIC
    // ==========================

    public void PauseGame()
    {
        if (isPauseSceneLoaded) return;

        SceneManager.LoadScene(PauseScene, LoadSceneMode.Additive);
        Time.timeScale = 0f;
        isPaused = true;
        isPauseSceneLoaded = true;

        // Pause background music while pause scene is active
        if (AudioManager.Instance != null)
            AudioManager.Instance.PauseMusic();
    }

    public void ResumeGame()
    {
        if (!isPauseSceneLoaded) return;

        SceneManager.UnloadSceneAsync(PauseScene);
        Time.timeScale = 1f;
        isPaused = false;
        isPauseSceneLoaded = false;

        // Resume background music when returning to gameplay
        if (AudioManager.Instance != null)
            AudioManager.Instance.ResumeMusic();
    }

    public void OpenSettings()
    {
        Debug.Log("Opening settings from Pause");
        SceneManager.UnloadSceneAsync(PauseScene);
        SceneManager.LoadScene(SettingScene, LoadSceneMode.Additive);
    }

    public void BackToPause()
    {
        Debug.Log("Back to pause scene");
        SceneManager.UnloadSceneAsync(SettingScene);
        SceneManager.LoadScene(PauseScene, LoadSceneMode.Additive);
    }

    public void TryAgain()
    {
        Debug.Log("Restarting gameplay...");
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameplayScene);
    }

    // ==========================
    // OPTIONAL UI CLICK DEBUG
    // ==========================
    void Update()
    {
        // Toggle ESC pause
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }

        // Debugging raycast UI
        if (Input.GetMouseButtonDown(0))
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            if (results.Count > 0)
                Debug.Log("UI clicked: " + results[0].gameObject.name);
            else
                Debug.Log("No UI hit");
        }
    }
}
