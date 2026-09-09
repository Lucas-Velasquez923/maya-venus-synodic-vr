using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the Solar System scene intro and synodic-cycle animation.
///
/// ── Scene layout (verified from Inspector) ────────────────────────────────
///   Sun Sphere  : world pos (0, 0, 0)   — always at origin / Sun's position
///   Venus root  : world pos (-70.6, 0, -23.2)  → r ≈ 74.3 units in XZ plane
///   Earth root  : world pos (-12.6, 0, 106.1)  → r ≈ 106.9 units in XZ plane
///   Both planets have Y = 0, confirming orbits lie in the XZ plane.
///   Ellipse 20/21 are visual sprites tilted -30.339° in X to appear elliptical
///   from the viewer; the actual 3D orbital path is circular in XZ.
///
/// ── Synodic-period math (Desmos worksheet) ────────────────────────────────
///   D_inner = 108.21 Mkm  (Venus semi-major axis)
///   D_outer = 149.60 Mkm  (Earth semi-major axis)
///   I_inner ≈ 0.6152 yr   Venus orbital period  (≈ 224.7 days)
///   O_outer = 1.0000 yr   Earth orbital period  (= 365.25 days)
///   n  = (1/8)·round(8 · O_outer / I_inner)      Desmos line 136
///      = (1/8)·round(8/0.6152)  =  (1/8)·13  =  1.625
///   T_syn = 1 / (n − 1) = 1 / 0.625 = 1.60 yr ≈ 584 days
///   (This 13:8 resonance is the Venus–Earth pentagram relationship.)
///
/// ── Sequence ──────────────────────────────────────────────────────────────
///   Phase 1 – Fade-in  : Sun → Venus → Earth each fade in sequentially.
///   Phase 2 – Orbit    : Both planets orbit counter-clockwise (viewed from +Y)
///                        for exactly one synodic period, then the sequence ends.
///
/// ── Setup ─────────────────────────────────────────────────────────────────
///   1. Create an empty GameObject in the scene (e.g. "SceneManager").
///   2. Attach this script to it.
///   3. Assign the six public references in the Inspector:
///        sunRoot            → "Sun Sphere"
///        venusRoot          → "Venus"  (the parent with Orbit/Spin scripts)
///        earthRoot          → "Earth"  (the parent with Orbit script)
///        venusEllipseTransform → "Ellipse 20"
///        earthEllipseTransform → "Ellipse 21"
///
/// ── Shader note ───────────────────────────────────────────────────────────
///   The fade system writes alpha via MaterialPropertyBlock.
///   For mesh fading to work, each planet material's Surface must be set to
///   Transparent in its shader graph / material settings. The script tries
///   "_BaseColor" (URP Lit), "_Color" (legacy), and "_Alpha" (custom float)
///   in that order. Add any custom property names to k_ColorProps if needed.
/// </summary>
[DisallowMultipleComponent]
public class SolarSystemSceneManager : MonoBehaviour
{
    // ── Inspector: scene references ───────────────────────────────────────────

    [Header("Planet Root Objects")]
    [Tooltip("'Sun Sphere' in the Hierarchy")]
    public GameObject sunRoot;

    [Tooltip("'Venus' in the Hierarchy — parent that holds Orbit, Spin, Glow, VFX…")]
    public GameObject venusRoot;

    [Tooltip("'Earth' in the Hierarchy — parent that holds Orbit, Canvas, Glow, VFX…")]
    public GameObject earthRoot;

    [Header("Ellipse Orbit Visual Sprites")]
    [Tooltip("'Ellipse 20' – Venus orbit ring. Tilt: -30.339° X  (visual only)")]
    public Transform venusEllipseTransform;

    [Tooltip("'Ellipse 21' – Earth orbit ring. Tilt: -30.339° X  (visual only)")]
    public Transform earthEllipseTransform;

    // ── Inspector: timing ─────────────────────────────────────────────────────

    [Header("Fade Timing")]
    [Range(0.5f, 15f)]
    [Tooltip("Real-world seconds for each planet to fully fade in")]
    public float fadeDuration = 2.5f;

    [Range(0f, 5f)]
    [Tooltip("Pause between consecutive planet fade-ins (seconds)")]
    public float pauseBetweenFades = 0.8f;

    // ── Inspector: orbital parameters ────────────────────────────────────────

    [Header("Orbital Parameters  (from Desmos model)")]
    [Range(1f, 120f)]
    [Tooltip("Real seconds that equal one in-game year")]
    public float secondsPerGameYear = 20f;

    [Range(0.1f, 1f)]
    [Tooltip("Venus orbital period in years  —  I_inner from Desmos (≈ 0.6152)")]
    public float venusPeriodYears = 0.6152f;

    [Range(0.5f, 2f)]
    [Tooltip("Earth orbital period in years  —  O_outer from Desmos (= 1.0)")]
    public float earthPeriodYears = 1.0f;

    // ── Private runtime ───────────────────────────────────────────────────────

    float _synodicYears;          // T_syn calculated from Desmos formula
    float _venusRadius;           // orbital radius in world units  (≈ 74.3)
    float _venusStartDeg;         // starting angle in XZ plane (degrees)
    float _earthRadius;           // orbital radius in world units  (≈ 106.9)
    float _earthStartDeg;         // starting angle in XZ plane (degrees)

    // Orbit scripts are disabled while this manager drives positions to avoid
    // conflicts. Spin scripts are left alone so planets still rotate on axis.
    MonoBehaviour _venusOrbitScript;
    MonoBehaviour _earthOrbitScript;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    void Awake()
    {
        ComputeSynodicPeriod();
        SampleOrbitParameters();
        FindOrbitScripts();
    }

    void Start()
    {
        // Suppress pre-existing Orbit behaviour; we take full control here.
        SetScriptEnabled(_venusOrbitScript, false);
        SetScriptEnabled(_earthOrbitScript, false);

        // Start everything invisible.
        SetAlphaAll(sunRoot,   0f);
        SetAlphaAll(venusRoot, 0f);
        SetAlphaAll(earthRoot, 0f);

        StartCoroutine(RunSequence());
    }

    // ── Initialisation ────────────────────────────────────────────────────────

    /// <summary>
    /// Replicates the Desmos n and T_syn formulae exactly.
    /// </summary>
    void ComputeSynodicPeriod()
    {
        // n = (1/8) · round( 8 · O_outer / I_inner ,  0 )   — Desmos line 136
        float n = Mathf.Round(8f * (earthPeriodYears / venusPeriodYears)) / 8f;

        // T_syn = 1 / (n − 1)   [in years]
        _synodicYears = 1f / (n - 1f);

        Debug.Log($"[SolarSystemSceneManager]  " +
                  $"n = {n:F4}  |  " +
                  $"T_syn = {_synodicYears:F4} yr  ({_synodicYears * 365.25f:F1} days)");
    }

    /// <summary>
    /// Reads each planet's current world position to derive orbital radius
    /// and starting angle in the XZ plane.  Called once on Awake so the
    /// values are frozen before the animation begins.
    /// </summary>
    void SampleOrbitParameters()
    {
        if (venusRoot)
        {
            Vector3 p        = venusRoot.transform.position;
            _venusRadius     = Mathf.Sqrt(p.x * p.x + p.z * p.z);  // ≈ 74.3
            _venusStartDeg   = Mathf.Atan2(p.z, p.x) * Mathf.Rad2Deg;
            Debug.Log($"[SolarSystemSceneManager]  Venus  " +
                      $"r = {_venusRadius:F2}  θ₀ = {_venusStartDeg:F1}°");
        }

        if (earthRoot)
        {
            Vector3 p        = earthRoot.transform.position;
            _earthRadius     = Mathf.Sqrt(p.x * p.x + p.z * p.z);  // ≈ 106.9
            _earthStartDeg   = Mathf.Atan2(p.z, p.x) * Mathf.Rad2Deg;
            Debug.Log($"[SolarSystemSceneManager]  Earth  " +
                      $"r = {_earthRadius:F2}  θ₀ = {_earthStartDeg:F1}°");
        }
    }

    void FindOrbitScripts()
    {
        _venusOrbitScript = FindScriptByNameContaining(venusRoot, "orbit");
        _earthOrbitScript = FindScriptByNameContaining(earthRoot, "orbit");
    }

    static MonoBehaviour FindScriptByNameContaining(GameObject root, string substring)
    {
        if (!root) return null;
        foreach (MonoBehaviour mb in root.GetComponents<MonoBehaviour>())
            if (mb != null && mb.GetType().Name.ToLower().Contains(substring))
                return mb;
        return null;
    }

    // ── Main sequence coroutine ───────────────────────────────────────────────

    IEnumerator RunSequence()
    {
        // ── Phase 1: Sequential fade-in ───────────────────────────────────────
        yield return StartCoroutine(FadeIn(sunRoot));
        yield return new WaitForSeconds(pauseBetweenFades);

        yield return StartCoroutine(FadeIn(venusRoot));
        yield return new WaitForSeconds(pauseBetweenFades);

        yield return StartCoroutine(FadeIn(earthRoot));
        yield return new WaitForSeconds(pauseBetweenFades);

        // ── Phase 2: Orbit for one synodic period ─────────────────────────────
        yield return StartCoroutine(OrbitOneSynodicCycle());

        Debug.Log("[SolarSystemSceneManager]  One synodic cycle complete. ✓");
    }

    // ── Phase 1 – Fade ────────────────────────────────────────────────────────

    IEnumerator FadeIn(GameObject root)
    {
        if (!root) yield break;
        for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
        {
            SetAlphaAll(root, Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }
        SetAlphaAll(root, 1f);
    }

    // ── Phase 2 – Orbital motion ──────────────────────────────────────────────

    /// <summary>
    /// Moves Venus and Earth around their circular orbits (XZ plane, CCW)
    /// for exactly T_syn years, then stops.
    ///
    /// Angular velocities derived from the Desmos periods:
    ///   ω_venus = 360° / (T_venus × secondsPerGameYear)   [deg / s]
    ///   ω_earth = 360° / (T_earth × secondsPerGameYear)   [deg / s]
    ///
    /// After one synodic period  (ω_venus − ω_earth) · T_syn = 360°,
    /// confirming Venus and Earth have returned to the same relative alignment.
    /// </summary>
    IEnumerator OrbitOneSynodicCycle()
    {
        float totalSec   = _synodicYears * secondsPerGameYear;

        // Counter-clockwise angular velocities (positive = CCW from +Y view)
        float omegaVenus = 360f / (venusPeriodYears * secondsPerGameYear); // deg/s
        float omegaEarth = 360f / (earthPeriodYears  * secondsPerGameYear); // deg/s

        Debug.Log($"[SolarSystemSceneManager]  Orbit start  |  " +
                  $"ω_Venus = {omegaVenus:F4} °/s   " +
                  $"ω_Earth = {omegaEarth:F4} °/s   " +
                  $"duration = {totalSec:F1} s");

        for (float t = 0f; t < totalSec; t += Time.deltaTime)
        {
            PlaceOnOrbit(venusRoot, _venusStartDeg + omegaVenus * t, _venusRadius);
            PlaceOnOrbit(earthRoot, _earthStartDeg + omegaEarth * t, _earthRadius);
            yield return null;
        }

        // Snap to exact final position (avoids frame-timing overshoot)
        PlaceOnOrbit(venusRoot, _venusStartDeg + omegaVenus * totalSec, _venusRadius);
        PlaceOnOrbit(earthRoot, _earthStartDeg + omegaEarth * totalSec, _earthRadius);
    }

    /// <summary>
    /// Positions <paramref name="planet"/> on the circle of <paramref name="radius"/>
    /// at <paramref name="angleDeg"/> degrees, measured counter-clockwise from
    /// the +X axis in the XZ plane.  The planet's Y coordinate is preserved.
    ///
    /// This matches the scene layout: both Venus and Earth have Y = 0 and the
    /// Sun is at the world origin, so the XZ-plane circle IS the correct orbit.
    /// </summary>
    static void PlaceOnOrbit(GameObject planet, float angleDeg, float radius)
    {
        if (!planet) return;
        float rad = angleDeg * Mathf.Deg2Rad;
        Vector3 pos = planet.transform.position;
        pos.x = radius * Mathf.Cos(rad);
        pos.z = radius * Mathf.Sin(rad);
        planet.transform.position = pos;
    }

    // ── Alpha / visibility helpers ────────────────────────────────────────────

    /// <summary>
    /// Sets alpha on every visual element under <paramref name="root"/>.
    ///   • MeshRenderers / SpriteRenderers  — via MaterialPropertyBlock
    ///   • ParticleSystems                  — start-colour alpha + emission toggle
    ///   • CanvasGroups                     — alpha property
    /// </summary>
    void SetAlphaAll(GameObject root, float a)
    {
        if (!root) return;
        a = Mathf.Clamp01(a);

        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer) continue;  // handled below
            ApplyRendererAlpha(r, a);
        }

        foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
            ApplyParticleAlpha(ps, a);

        foreach (CanvasGroup cg in root.GetComponentsInChildren<CanvasGroup>(true))
            cg.alpha = a;
    }

    // Shader property IDs — tried in order for mesh / sprite materials
    static readonly int k_BaseColor = Shader.PropertyToID("_BaseColor"); // URP Lit / Simple Lit
    static readonly int k_Color     = Shader.PropertyToID("_Color");     // Legacy Standard
    static readonly int k_AlphaF    = Shader.PropertyToID("_Alpha");     // Custom Shader Graph float

    // Shared MPB reused each frame to avoid GC allocs.
    // NOTE: Cannot be created in a static field initializer — Unity native
    // objects (CreateImpl) must be constructed inside Awake / Start or later.
    static MaterialPropertyBlock s_Mpb;
    static MaterialPropertyBlock Mpb => s_Mpb ?? (s_Mpb = new MaterialPropertyBlock());

    /// <summary>
    /// Writes alpha into the renderer's MaterialPropertyBlock without
    /// modifying (or instantiating) the shared material asset.
    /// </summary>
    static void ApplyRendererAlpha(Renderer rend, float a)
    {
        if (!rend || rend.sharedMaterial == null) return;

        rend.GetPropertyBlock(Mpb);   // populate with any existing overrides

        Material mat = rend.sharedMaterial;

        if (mat.HasProperty(k_BaseColor))
        {
            Color c = mat.GetColor(k_BaseColor); c.a = a;
            Mpb.SetColor(k_BaseColor, c);
        }
        else if (mat.HasProperty(k_Color))
        {
            Color c = mat.GetColor(k_Color); c.a = a;
            Mpb.SetColor(k_Color, c);
        }
        else if (mat.HasProperty(k_AlphaF))
        {
            Mpb.SetFloat(k_AlphaF, a);
        }
        // If none matched: open the material in the Shader Graph editor and
        // ensure Surface is set to Transparent and a colour is exposed.

        rend.SetPropertyBlock(Mpb);
    }

    /// <summary>
    /// Fades a particle system by:
    ///   1. Setting the start-colour alpha (smooth visual blend).
    ///   2. Disabling emission when invisible so no new particles spawn.
    /// </summary>
    static void ApplyParticleAlpha(ParticleSystem ps, float a)
    {
        // ── Emission toggle ───────────────────────────────────────────────────
        var emission   = ps.emission;
        emission.enabled = a > 0.01f;

        // ── Start-colour alpha ────────────────────────────────────────────────
        var main = ps.main;
        var mc   = main.startColor;

        switch (mc.mode)
        {
            case ParticleSystemGradientMode.Color:
            {
                Color c = mc.color; c.a = a;
                main.startColor = c;
                break;
            }
            case ParticleSystemGradientMode.TwoColors:
            {
                Color lo = mc.colorMin; lo.a = a;
                Color hi = mc.colorMax; hi.a = a;
                main.startColor = new ParticleSystem.MinMaxGradient(lo, hi);
                break;
            }
            // Gradient / TwoGradients modes: emission toggle is sufficient.
        }
    }

    // ── Tiny utility ──────────────────────────────────────────────────────────

    static void SetScriptEnabled(MonoBehaviour mb, bool enabled)
    {
        if (mb) mb.enabled = enabled;
    }

    // ── Editor gizmos ─────────────────────────────────────────────────────────
    // Draw the orbital circles in the Scene view so you can visually verify
    // the radii match the ellipse sprites.
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (venusRoot)
        {
            Vector3 p = venusRoot.transform.position;
            float r   = Mathf.Sqrt(p.x * p.x + p.z * p.z);
            Gizmos.color = new Color(1f, 0.85f, 0f, 0.6f);   // Venus yellow
            DrawCircleGizmoXZ(Vector3.zero, r, 64);
        }

        if (earthRoot)
        {
            Vector3 p = earthRoot.transform.position;
            float r   = Mathf.Sqrt(p.x * p.x + p.z * p.z);
            Gizmos.color = new Color(0f, 0.7f, 1f, 0.6f);    // Earth blue
            DrawCircleGizmoXZ(Vector3.zero, r, 64);
        }
    }

    static void DrawCircleGizmoXZ(Vector3 centre, float radius, int segments)
    {
        float step = 360f / segments;
        Vector3 prev = centre + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float rad = i * step * Mathf.Deg2Rad;
            Vector3 next = centre + new Vector3(
                radius * Mathf.Cos(rad), 0f, radius * Mathf.Sin(rad));
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
#endif
}