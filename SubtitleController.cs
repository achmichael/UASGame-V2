// SubtitleController.cs
// Menampilkan teks subtitle selama cutscene berlangsung
// - Menggunakan TextMeshProUGUI
// - Menampilkan beberapa baris secara bergantian dengan fade in/out

using UnityEngine;
using TMPro;
using System.Collections;

public class SubtitleController : MonoBehaviour
{
    public TextMeshProUGUI subtitleText;
    public float fadeSpeed = 2f;

    private Coroutine subtitleRoutine;

    public void ShowSubtitleSequence(string[] lines, float durationPerLine)
    {
        if (subtitleRoutine != null)
            StopCoroutine(subtitleRoutine);

        subtitleRoutine = StartCoroutine(SubtitleRoutine(lines, durationPerLine));
    }

    IEnumerator SubtitleRoutine(string[] lines, float duration)
    {
        foreach (string line in lines)
        {
            yield return StartCoroutine(FadeInText(line));
            yield return new WaitForSeconds(duration);
            yield return StartCoroutine(FadeOutText());
        }

        if (subtitleText != null)
            subtitleText.text = "";
    }

    IEnumerator FadeInText(string line)
    {
        if (subtitleText == null) yield break;

        subtitleText.text = line;
        Color c = subtitleText.color;
        c.a = 0;
        subtitleText.color = c;

        while (c.a < 1f)
        {
            c.a += Time.deltaTime * fadeSpeed;
            subtitleText.color = c;
            yield return null;
        }
    }

    IEnumerator FadeOutText()
    {
        if (subtitleText == null) yield break;

        Color c = subtitleText.color;
        while (c.a > 0f)
        {
            c.a -= Time.deltaTime * fadeSpeed;
            subtitleText.color = c;
            yield return null;
        }

        subtitleText.text = "";
    }
}
