using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    private const string mainmenu = "Main-menu";
    private const string SettingMenu = "Setting-mainmenu";
    private const string gameplay = "gameplay";
    private const string DifficulityScene = "DifficultyScene";

    public void startnewgame()
    {
        SceneManager.LoadScene(DifficulityScene);
    }

    public void OpenSettings()
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
        SceneManager.LoadScene(mainmenu);
    }
}
