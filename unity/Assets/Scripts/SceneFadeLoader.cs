using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFadeLoader : MonoBehaviour
{
    [Header("Fade Settings")]
    public Image fadeImage;          
    public float fadeDuration = 1f;  

    [Header("Scene Settings")]
    public string nextSceneName;

    public AudioSource audioSource;
    public AudioClip clip;

    private bool isLoading = false;

    public void PlaySound()
    {
        audioSource.PlayOneShot(clip);
    }

    public void StartFadeAndLoad()
    {
        if (!isLoading)
        {
            StartCoroutine(FadeAndLoad());
        }
    }

    private IEnumerator FadeAndLoad()
    {
        isLoading = true;

        float time = 0f;
        SetFadeAlpha(0f);


        while (time < fadeDuration)
        {
            float t = time / fadeDuration;
            SetFadeAlpha(Mathf.Lerp(0f, 1f, t));
            time += Time.deltaTime;
            yield return null;
        }

        SetFadeAlpha(1f);


        SceneManager.LoadScene(nextSceneName);
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null) return;

        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }
}