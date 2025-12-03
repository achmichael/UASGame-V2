using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class OpeningController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string mainMenuSceneName = "Main-menu";

    void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        // register event selesai video
        videoPlayer.loopPointReached += OnVideoFinished;

        // opsional biar ga blank frame di awal
        videoPlayer.Prepare();
    }

    void OnVideoFinished(VideoPlayer vp)
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
