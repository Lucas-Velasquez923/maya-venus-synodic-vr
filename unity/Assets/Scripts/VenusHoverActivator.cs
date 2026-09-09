using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach to Venus. On the FIRST hover, enables the Spin script and the Glow child.
/// Works with XR gaze, controller pointer events, and standard mouse hover.
/// </summary>
[RequireComponent(typeof(Collider))]
public class VenusHoverActivator : MonoBehaviour, IPointerEnterHandler
{
    [Header("References (auto-found if left empty)")]
    [Tooltip("The Spin script on this GameObject.")]
    public Spin spinScript;

    [Tooltip("The 'Glow' child GameObject to enable.")]
    public GameObject glowObject;

    [Header("Fade In")]
    [Tooltip("Seconds for the Glow to fade in after hover.")]
    public float glowFadeDuration = 1f;

    bool _triggered = false;

    void Awake()
    {
        // Auto-find Spin if not assigned
        if (spinScript == null)
            spinScript = GetComponent<Spin>();

        // Auto-find Glow child if not assigned
        if (glowObject == null)
        {
            Transform t = transform.Find("Glow");
            if (t != null) glowObject = t.gameObject;
        }

        // Start with both disabled
        if (spinScript != null) spinScript.enabled = false;
        if (glowObject != null) glowObject.SetActive(false);
    }

    // Covers XR gaze / controller pointer events via EventSystem
    public void OnPointerEnter(PointerEventData eventData) => Activate();

    // Covers mouse hover (editor testing)
    void OnMouseEnter() => Activate();

    void Activate()
    {
        if (_triggered) return;
        _triggered = true;

        if (spinScript != null)
            spinScript.enabled = true;

        if (glowObject != null)
            StartCoroutine(FadeInGlow());
    }

    IEnumerator FadeInGlow()
    {
        glowObject.SetActive(true);

        // ── Option 1: CanvasGroup (UI glow) ──────────────────────────────────
        CanvasGroup cg = glowObject.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f;
            float elapsed = 0f;
            while (elapsed < glowFadeDuration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Clamp01(elapsed / glowFadeDuration);
                yield return null;
            }
            cg.alpha = 1f;
            yield break;
        }

        // ── Option 2: Renderer material alpha (mesh / sprite glow) ───────────
        Renderer[] renderers = glowObject.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            float elapsed = 0f;
            while (elapsed < glowFadeDuration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Clamp01(elapsed / glowFadeDuration);
                foreach (var r in renderers)
                    foreach (var mat in r.materials)
                    {
                        if (mat.HasProperty("_Color"))
                        {
                            Color c = mat.color; c.a = a; mat.color = c;
                        }
                        else if (mat.HasProperty("_BaseColor"))
                        {
                            Color c = mat.GetColor("_BaseColor"); c.a = a;
                            mat.SetColor("_BaseColor", c);
                        }
                    }
                yield return null;
            }
            yield break;
        }

        // ── Option 3: VFX / Particle System — just activate and let it play ──
        // Nothing extra needed; SetActive(true) above already started it.
    }
}