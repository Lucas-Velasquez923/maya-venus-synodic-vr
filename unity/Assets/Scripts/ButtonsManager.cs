using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class ButtonsManager : MonoBehaviour
{
    public static ButtonsManager Instance;

    public Transform targetSprite;
    public float targetScaleY = 1.959014f;
    public float scaleDuration = 1f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip completionClip;

    private HoverTrigger[] buttons; // ??? HoverTrigger
    private int currentIndex = 0;

    void Awake()
    {
        Instance = this;
    }

    // ????????
    public void StartSequence(HoverTrigger[] buttonList)
    {
        buttons = buttonList;
        currentIndex = 0;

        ShowCurrentButton();
    }

    // ?????????
    void ShowCurrentButton()
    {
        if (currentIndex < buttons.Length)
        {
            HoverTrigger hover = buttons[currentIndex];
            if (hover != null)
            {
                hover.Unlock();

                // ????
                if (hover.animator != null)
                    hover.animator.SetTrigger("Reveal");
            }
        }
    }

    // ??????????????
    public void NotifyButtonActivated()
    {
        currentIndex++;

        if (currentIndex < buttons.Length)
        {
            ShowCurrentButton();
        }
        else
        {
            StartCoroutine(ScaleAndLoad());
        }
    }

    IEnumerator ScaleAndLoad()
    {
        float time = 0f;
        Vector3 startScale = targetSprite.localScale;
        Vector3 endScale = new Vector3(startScale.x, targetScaleY, startScale.z);

        // ??? ? ????
        while (time < scaleDuration)
        {
            time += Time.deltaTime;
            float t = time / scaleDuration;

            targetSprite.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }

        // ???????
        targetSprite.localScale = endScale;

        // Play completion audio and wait for it to finish
        if (audioSource != null && completionClip != null)
        {
            audioSource.clip = completionClip;
            audioSource.Play();
            yield return new WaitForSeconds(completionClip.length);
        }

        // ?????
        SceneFadeLoader loader = FindObjectOfType<SceneFadeLoader>();
        loader.StartFadeAndLoad();
    }
}