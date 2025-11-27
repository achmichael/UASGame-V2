using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseController : MonoBehaviour
{
    private PauseManager pauseManager;
    void Start()
    {
        GameObject managerObject = GameObject.Find("GameManager");
        if (managerObject != null)
        {
            pauseManager = managerObject.GetComponent<PauseManager>();
        }
        else
        {
            Debug.LogError("PauseManager tidak ditemukan!");
        }
    }

    public void OnPlayClicked()
    {
        if (pauseManager != null)
        {
            pauseManager.ResumeGame();
        }
    }

    public void OnMainMenuClicked()
    {
        if (pauseManager != null)
        {
            pauseManager.GoToMainMenu();
        }
    }

    public void OnSettingClicked()
  {
    if (pauseManager !=null)
    {
      pauseManager.OpenSettings();
    }
  }

  public void OnBackClicked()
  {
    if (pauseManager != null)
    {
      pauseManager.BackToPause();
    }
  }

    // Update is called once per frame
    void Update()
    {

    }
}
