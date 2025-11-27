// FadeTransition.cs
// Efek fade-out dan fade-in untuk transisi kematian dan respawn
// - Menggunakan full-screen Image (hitam) yang diassign di inspector

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeTransition : MonoBehaviour
{
    public Image fadePanel;
    public float fadeDuration = 1.2f;

    private void Start()
    {
        if (fadePanel != null)
            SetAlpha(0);
    }

    public void FadeOutAndIn()
    {
        StartCoroutine(FadeSequence());
    }

    public IEnumerator FadeSequence()
    {
        yield return Fade(1); // fade out (ke hitam)
        yield return new WaitForSeconds(0.25f);
        yield return Fade(0); // fade in
    }

    public IEnumerator Fade(float targetAlpha)
    {
        if (fadePanel == null) yield break;

        float startAlpha = fadePanel.color.a;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime / fadeDuration;
            float a = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetAlpha(a);
            yield return null;
        }
    }

    void SetAlpha(float a)
    {
        if (fadePanel == null) return;
        Color c = fadePanel.color;
        c.a = Mathf.Clamp01(a);
        fadePanel.color = c;
    }
}
