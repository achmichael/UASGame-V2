using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class LogicScene : MonoBehaviour
{
    private const string mainmenu = "Main-menu";
    private const string SettingMenu = "Setting-mainmenu";
    private const string GameplayScene = "GameplayScene";
    private const string DifficultyScene = "DifficultyScene";
    private const string PauseScene = "Pause";
    private bool isPaused = false;
    private bool isPauseSceneLoaded = false;
    private const string SettingScene = "Pause-Setting";

    void Start()
    {

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }

         if (Input.GetMouseButtonDown(0))
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            Debug.Log("Results value: " + results);
            if (results.Count > 0)
            {
                Debug.Log("UI paling atas yang diklik: " + results[0].gameObject.name);
            }
            else
            {
                Debug.Log("Tidak mengenai UI");
            }
        }
    }

    public void startnewgame()
    {
        SceneManager.LoadScene(DifficultyScene);
    }

    public void OpenSettingMainMenu()
    {
        SceneManager.LoadScene(SettingMenu);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainmenu);
    }

    public void PauseGame()
    {
        if (isPauseSceneLoaded) return;
        SceneManager.LoadScene(PauseScene, LoadSceneMode.Additive);
        Time.timeScale = 0f;
        isPaused = true;
        isPauseSceneLoaded = true;
    }

    public void ResumeGame()
    {
        if (!isPauseSceneLoaded) return;
        SceneManager.UnloadSceneAsync(PauseScene);
        Time.timeScale = 1f;
        isPaused = false;
        isPauseSceneLoaded = false;
    }

    public void OpenSettingPause()
    {
        Debug.Log("LogicScene.OpenSettingPause invoked");
        SceneManager.UnloadSceneAsync(PauseScene);
        SceneManager.LoadScene(SettingScene, LoadSceneMode.Additive);
    }

    public void BackToPause()
    {
        SceneManager.UnloadSceneAsync(SettingScene);
        SceneManager.LoadScene(PauseScene, LoadSceneMode.Additive);
    }

    public void TryAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GameplayScene);
    }
}
