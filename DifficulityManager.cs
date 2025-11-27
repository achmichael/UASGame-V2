using UnityEngine;
using UnityEngine.SceneManagement;

public class DifficulityManager : MonoBehaviour
{
    public void SetDifficulty(int level)
    {
        PlayerPrefs.SetInt("Difficulty", level); // 0=Easy, 1=Normal, 2=Hard
        PlayerPrefs.Save();
        SceneManager.LoadScene("GameplayScene");
    }
}