// DamageEffect.cs
// Efek visual saat player terkena damage atau mati
// - Menggunakan UI Image merah transparan (redOverlay) dan fade in/out coroutine

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DamageEffect : MonoBehaviour
{
    public Image redOverlay; // UI Image warna merah transparan
    public float fadeSpeed = 1.5f;
    public float maxAlpha = 0.6f;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (redOverlay != null)
        {
            Color c = redOverlay.color;
            c.a = 0f;
            redOverlay.color = c;
        }
    }

    public void ShowDamageEffect()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeEffect());
    }

    private IEnumerator FadeEffect()
    {
        // Naikkan alpha ke merah pekat
        float alpha = 0;
        while (alpha < maxAlpha)
        {
            alpha += Time.deltaTime * fadeSpeed;
            SetAlpha(alpha);
            yield return null;
        }

        // Turunkan kembali (fade-out)
        while (alpha > 0)
        {
            alpha -= Time.deltaTime * fadeSpeed;
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(0);
    }

    private void SetAlpha(float a)
    {
        if (redOverlay == null) return;
        Color color = redOverlay.color;
        color.a = Mathf.Clamp01(a);
        redOverlay.color = color;
    }
}
