using UnityEngine;

/// <summary>
/// Attach to: Venus  →  then Earth
///
/// Orbits this planet around the Sun in SolarSystem's local XZ plane (Y = 0).
///
/// Can optionally stop after exactly ONE synodic cycle with another planet.
/// For Venus/Earth:
///   T_syn = 1 / |(1/P1) - (1/P2)|
///         = 1 / |(1/0.6152) - (1/1.0)|
///         ≈ 1.5985 years
/// </summary>
[DisallowMultipleComponent]
public class PlanetOrbit : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Sun Reference")]
    [Tooltip("Drag the Sun Sphere here")]
    public Transform sunCenter;

    [Header("Orbital Speed  (Desmos)")]
    [Tooltip("Venus = 0.6152   |   Earth = 1.0")]
    [Range(0.05f, 5f)]
    public float periodYears = 1.0f;

    [Tooltip("Real-world seconds that represent one in-game year. Must be the same value on both Venus and Earth.")]
    [Range(1f, 120f)]
    public float secondsPerYear = 20f;

    [Header("Orbit Radius")]
    [Tooltip("Set to 0 to auto-calculate from current distance to the Sun on Start.")]
    public float orbitRadius = 0f;

    [Header("Stop After One Synodic Cycle")]
    [Tooltip("If enabled, this orbit stops after one synodic cycle relative to the partner planet.")]
    public bool stopAfterOneSynodicCycle = true;

    [Tooltip("For Venus/Earth pair: Venus should use 1.0, Earth should use 0.6152")]
    [Range(0.05f, 5f)]
    public float partnerPeriodYears = 1.0f;

    // ── Runtime (read-only in play mode) ─────────────────────────────────────

    [Header("Runtime Info  (read-only)")]
    [SerializeField] float _angularSpeedDeg;   // deg / second
    [SerializeField] float _currentAngleDeg;   // current position on orbit
    [SerializeField] float _elapsedSeconds;    // how long this has been running
    [SerializeField] float _synodicYears;      // computed synodic cycle in years
    [SerializeField] float _synodicSeconds;    // computed synodic cycle in real seconds
    [SerializeField] bool _finishedCycle;      // true once stopped

    // ── Private cache ─────────────────────────────────────────────────────────

    Vector3 _sunLocalPos;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Start()
    {
        // 1. Cache Sun local position
        _sunLocalPos = sunCenter != null
            ? sunCenter.localPosition
            : Vector3.zero;

        // 2. Auto-calculate orbit radius from current local position
        if (orbitRadius <= 0f)
        {
            Vector3 offset = transform.localPosition - _sunLocalPos;
            orbitRadius = new Vector2(offset.x, offset.z).magnitude;
        }

        // 3. Derive starting angle from current XZ position
        Vector3 startOffset = transform.localPosition - _sunLocalPos;
        _currentAngleDeg = Mathf.Atan2(startOffset.z, startOffset.x) * Mathf.Rad2Deg;

        // 4. Angular speed from orbital period
        _angularSpeedDeg = 360f / (periodYears * secondsPerYear);

        // 5. Compute synodic cycle duration
        float freqA = 1f / periodYears;
        float freqB = 1f / partnerPeriodYears;
        float relativeFreq = Mathf.Abs(freqA - freqB);

        if (relativeFreq > 0f)
        {
            _synodicYears = 1f / relativeFreq;
            _synodicSeconds = _synodicYears * secondsPerYear;
        }
        else
        {
            _synodicYears = Mathf.Infinity;
            _synodicSeconds = Mathf.Infinity;
        }

        Debug.Log($"[PlanetOrbit: {name}] " +
                  $"r = {orbitRadius:F2} units | " +
                  $"θ₀ = {_currentAngleDeg:F2}° | " +
                  $"ω = {_angularSpeedDeg:F4} °/s | " +
                  $"period = {periodYears} yr | " +
                  $"synodic = {_synodicYears:F4} yr ({_synodicSeconds:F2} s)");
        
        // Make sure it starts exactly where it already is
        PlaceOnOrbit();
    }

    void Update()
    {
        if (_finishedCycle)
            return;

        float dt = Time.deltaTime;

        // If stopping after one synodic cycle, clamp the very last step
        // so it ends exactly at the end of the cycle.
        if (stopAfterOneSynodicCycle && _elapsedSeconds + dt >= _synodicSeconds)
        {
            dt = _synodicSeconds - _elapsedSeconds;
            if (dt < 0f) dt = 0f;
        }

        // Advance orbit
        _currentAngleDeg += _angularSpeedDeg * dt;
        _elapsedSeconds += dt;

        // Keep angle tidy
        if (_currentAngleDeg >= 360f) _currentAngleDeg %= 360f;
        if (_currentAngleDeg < 0f) _currentAngleDeg += 360f;

        PlaceOnOrbit();

        // Stop once one synodic cycle is complete
        if (stopAfterOneSynodicCycle && _elapsedSeconds >= _synodicSeconds)
        {
            _finishedCycle = true;
            Debug.Log($"[PlanetOrbit: {name}] Finished exactly one synodic cycle.");
        }
    }

    // ── Orbit position ────────────────────────────────────────────────────────

    void PlaceOnOrbit()
    {
        float rad = _currentAngleDeg * Mathf.Deg2Rad;

        transform.localPosition = _sunLocalPos + new Vector3(
            orbitRadius * Mathf.Cos(rad),
            0f,
            orbitRadius * Mathf.Sin(rad)
        );
    }

    // ── Scene-view gizmo ──────────────────────────────────────────────────────

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        float r = orbitRadius;

        if (r <= 0f && sunCenter != null)
        {
            Vector3 off = transform.localPosition - sunCenter.localPosition;
            r = new Vector2(off.x, off.z).magnitude;
        }

        if (r <= 0f) return;

        Matrix4x4 parentMat = transform.parent != null
            ? transform.parent.localToWorldMatrix
            : Matrix4x4.identity;

        Vector3 sunLP = sunCenter != null ? sunCenter.localPosition : Vector3.zero;

        Gizmos.color = name.ToLower().Contains("venus")
            ? new Color(1f, 0.85f, 0.1f, 0.85f)
            : new Color(0.25f, 0.65f, 1f, 0.85f);

        const int segments = 72;
        Vector3 prev = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float a = i * (Mathf.PI * 2f / segments);
            Vector3 lp = sunLP + new Vector3(r * Mathf.Cos(a), 0f, r * Mathf.Sin(a));
            Vector3 wp = parentMat.MultiplyPoint3x4(lp);

            if (i > 0) Gizmos.DrawLine(prev, wp);
            prev = wp;
        }
    }
#endif
}