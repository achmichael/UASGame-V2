using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
  private const string PauseScene = "Pause";
  private bool isPaused = false;
  private bool isPauseSceneLoaded = false;
  private const string MainMenuScene = "Main-menu";
  private const string SettingScene = "Pause-Setting";
  // Start is called before the first frame update
  void Start()
  {

  }

  // Update is called once per frame
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

  public void GoToMainMenu()
  {
    Time.timeScale = 1f;
    SceneManager.LoadScene(MainMenuScene);
  }

  public void OpenSettings()
  {
    SceneManager.UnloadSceneAsync(PauseScene);
    SceneManager.LoadScene(SettingScene, LoadSceneMode.Additive);
  }

  public void BackToPause()
  {
    SceneManager.UnloadSceneAsync(SettingScene);
    SceneManager.LoadScene(PauseScene, LoadSceneMode.Additive);
  }
}
