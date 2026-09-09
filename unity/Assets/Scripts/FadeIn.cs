using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fades in a list of GameObjects one at a time.
/// Assign objects to the 'objectsToFade' list in the Inspector,
/// or leave it empty to automatically use all direct children.
/// </summary>
public class SequentialFadeIn : MonoBehaviour
{
    [Header("Objects to Fade In")]
    [Tooltip("Leave empty to use all direct children of this GameObject.")]
    public List<GameObject> objectsToFade;

    [Header("Timing")]
    [Tooltip("Seconds to wait before starting the sequence.")]
    public float initialDelay = 0.5f;

    [Tooltip("How long each object takes to fade in (seconds).")]
    public float fadeDuration = 1.5f;

    [Tooltip("Pause between finishing one fade and starting the next (seconds).")]
    public float delayBetweenObjects = 0.3f;

    [Header("Options")]
    [Tooltip("Start fading automatically on Play.")]
    public bool playOnStart = true;

    void Start()
    {
        // If no objects were assigned, grab all direct children
        if (objectsToFade == null || objectsToFade.Count == 0)
        {
            objectsToFade = new List<GameObject>();
            foreach (Transform child in transform)
                objectsToFade.Add(child.gameObject);
        }

        // Hide everything before we begin
        foreach (var obj in objectsToFade)
            SetAlpha(obj, 0f);

        if (playOnStart)
            StartCoroutine(FadeInSequence());
    }

    /// <summary>Call this from code or a UI button to (re)start the sequence.</summary>
    public void Play() => StartCoroutine(FadeInSequence());

    IEnumerator FadeInSequence()
    {
        yield return new WaitForSeconds(initialDelay);

        foreach (var obj in objectsToFade)
        {
            if (obj == null) continue;

            obj.SetActive(true);
            yield return StartCoroutine(FadeObject(obj, 0f, 1f, fadeDuration));
            yield return new WaitForSeconds(delayBetweenObjects);
        }
    }

    IEnumerator FadeObject(GameObject obj, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetAlpha(obj, Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetAlpha(obj, to);
    }

    // ---------------------------------------------------------------
    // Helpers – works with standard materials and URP/HDRP materials
    // ---------------------------------------------------------------

    void SetAlpha(GameObject obj, float alpha)
    {
        foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true))
        {
            foreach (var mat in renderer.materials)
            {
                // Make sure the material supports transparency
                EnsureTransparentMode(mat);

                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
                else if (mat.HasProperty("_BaseColor")) // URP
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = alpha;
                    mat.SetColor("_BaseColor", c);
                }
            }
        }
    }

    void EnsureTransparentMode(Material mat)
    {
        // Only adjust once (when alpha is first being set to 0)
        if (mat.renderQueue >= 3000) return; // Already transparent

        // Standard shader
        if (mat.HasProperty("_Mode"))
        {
            mat.SetFloat("_Mode", 2); // Fade mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
        // URP Lit shader
        else if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
        }
    }
}