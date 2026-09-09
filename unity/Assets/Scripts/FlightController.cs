using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FlightController : MonoBehaviour
{
    [Header("References")]
    public Transform xrOrigin;
    public Transform targetPoint;
    public Image fadeImage;

    [Header("Flight Settings")]
    public float duration = 5f;
    public AnimationCurve motionCurve;

    [Header("Scene Settings")]
    public string nextSceneName;
    public float fadeDuration = 2f;

    private bool isFlying = false;

    public GameObject earth;

    // ?????????
    [Header("Blackout Settings")]
    public float blackoutHeight = 22f;   // ????
    public float blackoutDuration = 0.3f; // ?????????“??”??

    private bool hasBlackedOut = false; // ??????

    public void ShowEarth()
    {
        earth.SetActive(true);
    }

    public void StartFlight()
    {
        if (!isFlying)
        {
            StartCoroutine(FlightSequence());
        }
    }

    private IEnumerator FlightSequence()
    {
        isFlying = true;

        Vector3 startPos = xrOrigin.position;
        Vector3 endPos = targetPoint.position;
        float time = 0f;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        asyncLoad.allowSceneActivation = false;

        SetFadeAlpha(0f);

        while (time < duration)
        {
            float t = time / duration;
            float curvedT = motionCurve != null ? motionCurve.Evaluate(t) : t;

            xrOrigin.position = Vector3.Lerp(startPos, endPos, curvedT);


            // ---------- ????????? ----------
            if (xrOrigin.position.y > blackoutHeight && !hasBlackedOut)
            {
                hasBlackedOut = true;
                StartCoroutine(BlackoutEffect());
            }
            else if (xrOrigin.position.y <= blackoutHeight)
            {
                // ??????????????????
                hasBlackedOut = false;
            }
            // --------------------------------------

            // ? fade ??
            if (t > 0.5f)
            {
                float fadeT = (t - 0.5f) * 2f;
                SetFadeAlpha(Mathf.Lerp(0f, 1f, fadeT));
            }

            time += Time.deltaTime;
            yield return null;
        }

        xrOrigin.position = endPos;
        SetFadeAlpha(1f);

        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        asyncLoad.allowSceneActivation = true;

        isFlying = false;
    }

    private void SetFadeAlpha(float alpha)
    {
        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;
    }

    // ???????
    private IEnumerator BlackoutEffect()
    {
        // ?? fade in ? ??
        yield return StartCoroutine(FadeToAlpha(1f, 0.5f));
        yield return new WaitForSeconds(blackoutDuration);
        // ?? fade out ? ??????
        ShowEarth();
        yield return StartCoroutine(FadeToAlpha(0f, 0.3f));
    }

    private IEnumerator FadeToAlpha(float targetAlpha, float fadeTime)
    {
        float startAlpha = fadeImage.color.a;
        float time = 0f;

        while (time < fadeTime)
        {
            float t = time / fadeTime;
            SetFadeAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            time += Time.deltaTime;
            yield return null;
        }

        SetFadeAlpha(targetAlpha);
    }
}
