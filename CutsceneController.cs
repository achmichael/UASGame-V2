// CutsceneController.cs
// Mengatur cutscene normal dan secret ending dengan Unity Timeline dan subtitle
// - Memainkan PlayableDirector (Timeline) dan memanggil SubtitleController
// - Menggunakan FadeTransition untuk transisi lebih halus

using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

public class CutsceneController : MonoBehaviour
{
    public PlayableDirector normalEndingDirector;
    public PlayableDirector secretEndingDirector;

    public SubtitleController subtitleController;
    public FadeTransition fadeTransition;

    public AudioClip bgmNormal;
    public AudioClip bgmSecret;

    public void PlayNormalEnding()
    {
        StartCoroutine(PlayCutsceneSequence(normalEndingDirector, bgmNormal, "normal"));
    }

    public void PlaySecretEnding()
    {
        StartCoroutine(PlayCutsceneSequence(secretEndingDirector, bgmSecret, "secret"));
    }

    private IEnumerator PlayCutsceneSequence(PlayableDirector director, AudioClip bgm, string type)
    {
        if (fadeTransition != null)
            StartCoroutine(fadeTransition.Fade(1)); // quick fade out

        yield return new WaitForSeconds(1.0f);

        if (bgm != null && Camera.main != null)
            AudioSource.PlayClipAtPoint(bgm, Camera.main.transform.position);

        director?.Play();

        if (type == "normal")
        {
            subtitleController?.ShowSubtitleSequence(new string[] {
                "Saipul berhasil mengumpulkan semua lembaran Al-Qur’an...",
                "Cahaya terang mulai mengelilinginya...",
                "Dia pun terbangun dari mimpi buruknya dan berdoa dengan penuh penyesalan."
            }, 4f);
        }
        else
        {
            subtitleController?.ShowSubtitleSequence(new string[] {
                "Saipul menemukan pintu sebelum semua lembaran terkumpul...",
                "Di balik cahaya, muncul sosok Pak Ustadz...",
                "\"Nak, jangan lalaikan shalatmu. Karena gelapnya mimpi ini lahir dari kelalaianmu sendiri.\""
            }, 5f);
        }

        // Wait for timeline duration (if director available)
        float waitTime = 0f;
        if (director != null)
            waitTime = (float)director.duration + 1.5f;

        yield return new WaitForSeconds(waitTime);

        if (fadeTransition != null)
            StartCoroutine(fadeTransition.Fade(1));

        yield return new WaitForSeconds(0.8f);

        // After cutscene, go to credits or main menu
        UnityEngine.SceneManagement.SceneManager.LoadScene("CreditScene");
    }
}
