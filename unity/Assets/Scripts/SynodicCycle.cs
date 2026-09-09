using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Drives Venus and Earth through exactly one full synodic cycle after being triggered.
///
/// Math from the simulation:
///   I_inner  = 0.62  (Venus orbital period in Earth years)
///   O_outer  = 1.0   (Earth orbital period in Earth years)
///   n        = 1.625 (inner speed ratio: 1/8 * round(8 * O/I, 0))
///   Synodic  = 1 / (n - 1) ≈ 1.6 Earth years ≈ 584 days
///   d        = 0.7233 (Venus orbit radius relative to Earth = 1)
///
/// Trigger this from VenusHoverActivator or any other script via:
///   GetComponent<SynodicCycleAnimator>().StartCycle();
/// </summary>
public class SynodicCycleAnimator : MonoBehaviour
{
    // ── Planet references ─────────────────────────────────────────────────────
    [Header("Planet Transforms")]
    [Tooltip("The Venus GameObject (orbits the sun).")]
    public Transform venus;

    [Tooltip("The Earth GameObject (orbits the sun).")]
    public Transform earth;

    [Tooltip("The Sun / centre of the solar system.")]
    public Transform sun;

    // ── Orbital parameters (from the simulation math) ─────────────────────────
    [Header("Orbital Parameters")]
    [Tooltip("Venus orbital period in Earth years (I_inner). Default: 0.62")]
    public float innerPeriodYears = 0.62f;

    [Tooltip("Earth orbital period in Earth years (O_outer). Default: 1.0")]
    public float outerPeriodYears = 1.0f;

    // ── Animation settings ────────────────────────────────────────────────────
    [Header("Animation")]
    [Tooltip("How many real seconds the full synodic cycle animation takes.")]
    public float animationDuration = 8f;

    [Tooltip("Delay (seconds) after trigger before the cycle begins.")]
    public float startDelay = 1.5f;

    [Tooltip("Start the cycle automatically on Awake (useful for testing).")]
    public bool autoStart = false;

    // ── Callback ──────────────────────────────────────────────────────────────
    [Header("On Complete")]
    public UnityEvent onCycleComplete;

    // ── Private ───────────────────────────────────────────────────────────────

    // Synodic period: 1 / (n - 1) where n = O_outer / I_inner (speed ratio)
    float SynodicPeriodYears => 1f / ((outerPeriodYears / innerPeriodYears) - 1f);

    // Orbital radii — derived from transforms at start time
    float _venusRadius;
    float _earthRadius;

    // Starting angles so we resume from current scene positions
    float _venusStartAngle;
    float _earthStartAngle;

    bool _running = false;

    // ─────────────────────────────────────────────────────────────────────────

    void Start()
    {
        // Auto-find sun if not set
        if (sun == null)
        {
            var sunObj = GameObject.Find("Sun Sphere");
            if (sunObj != null) sun = sunObj.transform;
        }

        // Auto-find planets if not set
        if (venus == null) venus = FindPlanet("Venus");
        if (earth == null) earth = FindPlanet("Earth");

        if (autoStart) StartCycle();
    }

    Transform FindPlanet(string name)
    {
        var go = GameObject.Find(name);
        return go != null ? go.transform : null;
    }

    // ── Public entry point ────────────────────────────────────────────────────

    public void StartCycle()
    {
        if (_running) return;
        StartCoroutine(RunCycle());
    }

    // ─────────────────────────────────────────────────────────────────────────

    IEnumerator RunCycle()
    {
        _running = true;

        yield return new WaitForSeconds(startDelay);

        if (venus == null || earth == null || sun == null)
        {
            Debug.LogWarning("SynodicCycleAnimator: missing planet or sun reference.");
            _running = false;
            yield break;
        }

        // Snapshot current orbital radii and angles
        Vector3 venusFlat = new Vector3(venus.position.x - sun.position.x, 0,
                                        venus.position.z - sun.position.z);
        Vector3 earthFlat = new Vector3(earth.position.x - sun.position.x, 0,
                                        earth.position.z - sun.position.z);

        _venusRadius = venusFlat.magnitude;
        _earthRadius = earthFlat.magnitude;

        _venusStartAngle = Mathf.Atan2(venusFlat.z, venusFlat.x) * Mathf.Rad2Deg;
        _earthStartAngle = Mathf.Atan2(earthFlat.z, earthFlat.x) * Mathf.Rad2Deg;

        // Angular speeds (degrees per normalised time unit)
        float synodicYears   = SynodicPeriodYears;              // ~1.6 years
        float earthDegs      = (360f / outerPeriodYears) * synodicYears; // 360 * 1.6 = 576°
        float venusDegs      = (360f / innerPeriodYears) * synodicYears; // ~935°  (2.6 orbits)

        // Disable existing Orbit scripts so we take full control
        var venusOrbit = venus.GetComponent<Orbit>();
        var earthOrbit = earth.GetComponent<Orbit>();
        if (venusOrbit != null) venusOrbit.enabled = false;
        if (earthOrbit != null) earthOrbit.enabled = false;

        // ── Animate ──────────────────────────────────────────────────────────
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);

            // Use ease-in-out so it feels like an intentional sweep
            float tSmooth = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

            float venusAngle = _venusStartAngle + venusDegs * tSmooth;
            float earthAngle = _earthStartAngle + earthDegs * tSmooth;

            SetOrbitPosition(venus, venusAngle, _venusRadius);
            SetOrbitPosition(earth, earthAngle, _earthRadius);

            yield return null;
        }

        // ── Re-enable orbits ──────────────────────────────────────────────────
        if (venusOrbit != null) venusOrbit.enabled = true;
        if (earthOrbit != null) earthOrbit.enabled = true;

        _running = false;
        onCycleComplete?.Invoke();
    }

    void SetOrbitPosition(Transform planet, float angleDeg, float radius)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        float y   = planet.position.y; // preserve original height
        planet.position = new Vector3(
            sun.position.x + Mathf.Cos(rad) * radius,
            y,
            sun.position.z + Mathf.Sin(rad) * radius
        );
    }
}