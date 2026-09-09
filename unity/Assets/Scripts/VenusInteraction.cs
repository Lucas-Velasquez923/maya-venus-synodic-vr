using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Attach to Venus. Handles all VR interaction:
///   HOVER  → spin + glow, tells manager to fade in rest
///   CLICK  → advances synodic cycle phases
///   CLICK  → in Manual phase, TOGGLES Venus auto-orbiting forward.
///            First click = starts moving. Second click = stops.
///            Earth follows at 8/13 ratio.
///
/// Requires: SphereCollider + XRSimpleInteractable on Venus.
/// </summary>
[RequireComponent(typeof(Collider))]
public class VenusInteraction : MonoBehaviour
{
    [Header("References")]
    public SynodicManager manager;

    [Header("Auto-found (leave empty)")]
    public Spin spinScript;
    public GameObject glowObject;

    [Header("Glow")]
    public float glowFadeDuration = 1f;

    [Header("Manual Orbit Speed")]
    [Tooltip("Degrees per second Venus moves while auto-orbiting is active.")]
    [SerializeField] float orbitSpeed = 90f;

    bool _hoverDone;
    bool _autoOrbiting;

    void Awake()
    {
        if (!manager) manager = FindFirstObjectByType<SynodicManager>();
        if (!spinScript) spinScript = GetComponent<Spin>();
        if (!glowObject)
        {
            Transform t = transform.Find("Glow");
            if (t) glowObject = t.gameObject;
        }

        if (spinScript) spinScript.enabled = false;
        if (glowObject) glowObject.SetActive(false);

        var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener(OnHover);
            interactable.selectEntered.AddListener(OnSelect);
        }
    }

    // ── HOVER ────────────────────────────────────────────────────────────

    void OnHover(HoverEnterEventArgs args)
    {
        if (_hoverDone) return;
        if (manager && manager.CurrentPhase != SynodicManager.Phase.WaitHover) return;

        _hoverDone = true;
        if (spinScript) spinScript.enabled = true;
        if (glowObject) StartCoroutine(FadeInGlow());
        if (manager) manager.AdvancePhase();
    }

    IEnumerator FadeInGlow()
    {
        glowObject.SetActive(true);
        Renderer[] renderers = glowObject.GetComponentsInChildren<Renderer>(true);
        for (float t = 0f; t < glowFadeDuration; t += Time.deltaTime)
        {
            float a = Mathf.Clamp01(t / glowFadeDuration);
            foreach (var r in renderers)
                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty("_BaseColor"))
                    {
                        Color c = mat.GetColor("_BaseColor"); c.a = a;
                        mat.SetColor("_BaseColor", c);
                    }
                    else if (mat.HasProperty("_Color"))
                    {
                        Color c = mat.color; c.a = a; mat.color = c;
                    }
                }
            yield return null;
        }
    }

    // ── SELECT ───────────────────────────────────────────────────────────

    void OnSelect(SelectEnterEventArgs args)
    {
        if (!manager) return;
        var phase = manager.CurrentPhase;

        // Click advances to full cycle
        if (phase == SynodicManager.Phase.WaitClick2)
        {
            manager.AdvancePhase();
            return;
        }

        // In Manual phase, toggle auto-orbit on/off
        if (phase == SynodicManager.Phase.Manual)
        {
            _autoOrbiting = !_autoOrbiting;
            Debug.Log($"[VenusInteraction] Auto-orbit: {(_autoOrbiting ? "ON" : "OFF")}");
        }
    }

    // ── UPDATE — auto-orbit when toggled on ──────────────────────────────

    void Update()
    {
        if (!_autoOrbiting) return;
        if (!manager || manager.CurrentPhase != SynodicManager.Phase.Manual) return;

        float angleDelta = orbitSpeed * Time.deltaTime;
        manager.AdvanceManualAngle(angleDelta);
    }
}