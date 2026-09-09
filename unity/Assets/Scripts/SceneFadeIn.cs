using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneFadeIn : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 5f;
    public float delayBeforeFade = 0.5f;

    void Start()
    {
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        Color color = fadeImage.color;

        color.a = 1f;
        fadeImage.color = color;

        yield return new WaitForSeconds(delayBeforeFade);

        yield return null;

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;
            color.a = Mathf.Lerp(1f, 0f, t);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 0f;
        fadeImage.color = color;
    }
}