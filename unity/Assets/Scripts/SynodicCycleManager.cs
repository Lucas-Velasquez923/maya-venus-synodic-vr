using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Complete Venus–Earth synodic cycle orchestrator for LucasSolarSystem2.
///
/// This script consolidates the overlapping logic that was spread across
/// SolarSystemSceneManager, SynodicCycleAnimator (SynodicCycle.cs),
/// and PlanetOrbit (PlaneOrbit.cs). Those scripts remain in the project —
/// this script auto-disables any existing orbit scripts on the planets
/// so they don't fight for control.
///
/// ═══════════════════════════════════════════════════════════════════════════
/// SEQUENCE
/// ═══════════════════════════════════════════════════════════════════════════
///   Phase 1 – FADE IN     : Sun → Venus → Earth fade in sequentially
///   Phase 2 – FIRST CYCLE : Both planets orbit for exactly 1 synodic cycle,
///                           then STOP
///   Phase 3 – WAITING     : Idle until user interacts (VR click / UI button)
///   Phase 4 – FULL CYCLE  : Run 4 more synodic cycles (5 total = 8 Earth years)
///                           completing the Mesoamerican Venus round —
///                           13 Venus orbits, 8 Earth orbits, 5 conjunctions.
///   Phase 5 – COMPLETE    : Events fire. Venus is then draggable by the user
///                           (see VenusManualOrbit.cs).
///
/// ═══════════════════════════════════════════════════════════════════════════
/// SYNODIC MATH  (Desmos quantized — 13:8 resonance)
/// ═══════════════════════════════════════════════════════════════════════════
///   Venus sidereal period   I = 0.6152 yr  (224.7 days)
///   Earth sidereal period   O = 1.0000 yr  (365.25 days)
///
///   Speed ratio   n = (1/8) · round(8 · O / I)
///                   = (1/8) · round(12.997)
///                   = (1/8) · 13
///                   = 1.625
///
///   Synodic period  T_syn = 1 / (n − 1)
///                         = 1 / 0.625
///                         = 1.6 yr  ≈ 584 days
///
///   Full 8:5 Venus round = 5 × T_syn = 8.0 Earth years
///     → Venus completes 13 orbits
///     → Earth completes 8 orbits
///     → 5 conjunctions, each 216° apart on the orbital circle
///
/// ═══════════════════════════════════════════════════════════════════════════
/// CONJUNCTION PROGRESSION
/// ═══════════════════════════════════════════════════════════════════════════
///   After each synodic cycle, Earth (and Venus) advance 216° net:
///     Earth moves 1.6 yr × (360°/1.0 yr) = 576° = 216° mod 360°
///
///   5 conjunction points at: 0°, 216°, 72°, 288°, 144°  (chronological)
///   The Dresden Codex Venus Table explicitly records this 584-day / 8-year
///   return cycle. These points are tracked for data/editor gizmos only —
///   no pentagram shape is drawn in play mode.
///
/// ═══════════════════════════════════════════════════════════════════════════
/// SETUP IN UNITY
/// ═══════════════════════════════════════════════════════════════════════════
///   1. Create empty GameObject **as a child of the SolarSystem parent**
///      (so it inherits the 61.5° X-axis tilt). Name it "SynodicCycleManager".
///   2. Attach this script
///   3. Assign: sunRoot, venusRoot, earthRoot in Inspector
///   4. Existing orbit scripts (PlanetOrbit, Orbit, RotateAround) are
///      auto-disabled at runtime — no need to remove them
///   5. For VR click interaction:
///      a) Add a Collider to whatever object the user should click
///      b) Add XR Simple Interactable to that object
///      c) In "Select Entered" event → drag SynodicCycleManager →
///         choose StartFullCycle()
///   6. (Optional) Assign ellipse transforms if you want them to fade in
///      with the planets
///
/// IMPORTANT: All orbit math uses localPosition (not world position) so the
/// planets orbit correctly within the SolarSystem parent's tilted local space.
/// </summary>
[DisallowMultipleComponent]
public class SynodicCycleManager : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════════
    // INSPECTOR
    // ═══════════════════════════════════════════════════════════════════════

    [Header("Scene References")]
    [Tooltip("The Sun GameObject (centre of the system)")]
    public GameObject sunRoot;

    [Tooltip("The Venus root GameObject")]
    public GameObject venusRoot;

    [Tooltip("The Earth root GameObject")]
    public GameObject earthRoot;

    [Header("Ellipse Visuals (Optional)")]
    [Tooltip("Venus orbit ellipse sprite — fades in with Venus")]
    public GameObject venusEllipse;

    [Tooltip("Earth orbit ellipse sprite — fades in with Earth")]
    public GameObject earthEllipse;

    // ── Labels (name → day counter) ───────────────────────────────────────

    [Header("Planet Labels (Name → Days Counter)")]
    [Tooltip("Drag the TextMeshPro or UI.Text component that shows 'Venus'. After fade-in it will switch to a live day counter.")]
    public Component venusLabel;

    [Tooltip("Drag the TextMeshPro or UI.Text component that shows 'Earth'. After fade-in it will switch to a live day counter.")]
    public Component earthLabel;

    [Tooltip("Name shown on Venus before the orbit starts.")]
    public string venusName = "Venus";

    [Tooltip("Name shown on Earth before the orbit starts.")]
    public string earthName = "Earth";

    public enum DayCounterMode
    {
        /// <summary>Counts 0 → period days, resets each orbit. Shows that planet's local orbital time.</summary>
        DaysInCurrentOrbit,
        /// <summary>Total Earth days elapsed since orbital motion began. Keeps increasing.</summary>
        TotalElapsedEarthDays
    }

    [Tooltip("How the day counter should tick.")]
    public DayCounterMode dayCounterMode = DayCounterMode.DaysInCurrentOrbit;

    [Tooltip("Format string for the day number. {0:F0} = whole days, {0:F1} = one decimal.")]
    public string dayLabelFormat = "{0:F0} days";

    // ── Fade ──────────────────────────────────────────────────────────────

    public enum FadeMode
    {
        /// <summary>Scale from 0 to original size. ALWAYS works (recommended).</summary>
        Scale,
        /// <summary>Alpha fade via MaterialPropertyBlock. Requires Transparent materials with _BaseColor / _Color / _Alpha.</summary>
        Alpha,
        /// <summary>Both scale and alpha at the same time.</summary>
        Both
    }

    [Header("Fade Settings")]
    [Tooltip("Scale = always works (grow from 0). Alpha = requires Transparent materials. Both = combined.")]
    public FadeMode fadeMode = FadeMode.Scale;

    [Range(0.5f, 15f)]
    [Tooltip("Seconds for each object to fully fade in")]
    public float fadeDuration = 2.5f;

    [Range(0f, 5f)]
    [Tooltip("Pause between consecutive fade-ins")]
    public float pauseBetweenFades = 0.8f;

    // ── Ellipse timing (independent from planets) ─────────────────────────

    [Header("Ellipse Fade Timing (Independent)")]
    [Tooltip("If true, ellipses are sequenced separately from their planets (they get their own fade step, duration, and pause). If false, ellipses are hidden entirely — fade them in yourself however you want.")]
    public bool fadeEllipsesSeparately = true;

    [Range(0.5f, 15f)]
    [Tooltip("Seconds for each ellipse to fully fade in (independent from planet fadeDuration)")]
    public float ellipseFadeDuration = 2.5f;

    [Range(0f, 5f)]
    [Tooltip("Pause before each ellipse begins its fade (after the preceding fade step finishes)")]
    public float pauseBeforeEllipse = 0.4f;

    [Range(0f, 5f)]
    [Tooltip("Pause after each ellipse finishes fading, before the next step in the sequence")]
    public float pauseAfterEllipse = 0.4f;

    public enum EllipseOrder
    {
        /// <summary>Planet fades in first, then its ellipse fades in after.</summary>
        AfterPlanet,
        /// <summary>Ellipse fades in first, then its planet fades in after.</summary>
        BeforePlanet
    }

    [Tooltip("Should each ellipse fade in BEFORE or AFTER its planet?")]
    public EllipseOrder ellipseOrder = EllipseOrder.AfterPlanet;

    // ── Orbital parameters ────────────────────────────────────────────────

    [Header("Orbital Parameters (Desmos Model)")]
    [Tooltip("Venus sidereal period in Earth years (I_inner ≈ 0.6152)")]
    [Range(0.1f, 1f)]
    public float venusPeriodYears = 0.6152f;

    [Tooltip("Earth sidereal period in Earth years (O_outer = 1.0)")]
    [Range(0.5f, 2f)]
    public float earthPeriodYears = 1.0f;

    [Tooltip("Real-world seconds that equal one in-game year")]
    [Range(1f, 120f)]
    public float secondsPerYear = 20f;

    // ── Venus ↔ Earth live beam ───────────────────────────────────────────

    [Header("Venus ↔ Earth Beam (Live Connection)")]
    [Tooltip("LineRenderer that draws a beam between Venus and Earth every frame. Create a GameObject with a LineRenderer and drag it here.")]
    public LineRenderer venusEarthBeam;

    [Tooltip("If true, the beam is only visible while the planets are actually orbiting (hidden during fade-in, visible through Phase 2 and the full cycle, visible after Complete).")]
    public bool beamOnlyDuringMotion = false;

    [Range(0f, 3f)]
    [Tooltip("Seconds for the beam to fade in when motion starts.")]
    public float beamFadeInSeconds = 0.6f;

    [Header("Gizmos (Editor Only)")]
    [Tooltip("Draw orbit rings + conjunction dots in the Scene/Game view. Turn OFF to hide the red dots in the Game view.")]
    public bool drawGizmos = false;

    [Tooltip("Radius of conjunction point dots (world units). Keep small — 0.1 to 0.3 usually works.")]
    [Range(0.01f, 2f)]
    public float gizmoDotRadius = 0.1f;

    // ── Events ────────────────────────────────────────────────────────────

    [Header("Events")]
    [Tooltip("Fires when the first synodic cycle finishes and the system is waiting for interaction")]
    public UnityEvent onFirstCycleComplete;

    [Tooltip("Fires when all 5 synodic cycles are done (full 8:5 Venus round complete)")]
    public UnityEvent onFullCycleComplete;

    // ═══════════════════════════════════════════════════════════════════════
    // STATE
    // ═══════════════════════════════════════════════════════════════════════

    public enum Phase
    {
        Idle,
        FadingIn,
        FirstCycle,
        WaitingForInteraction,
        FullCycle,
        Complete
    }

    [Header("Runtime (Read Only)")]
    [SerializeField] Phase _phase = Phase.Idle;
    [SerializeField] float _synodicYears;
    [SerializeField] float _synodicSeconds;
    [SerializeField] float _speedRatio_n;
    [SerializeField] int   _conjunctionCount;
    [SerializeField] float _venusAngleDeg;
    [SerializeField] float _earthAngleDeg;

    // ═══════════════════════════════════════════════════════════════════════
    // PRIVATE
    // ═══════════════════════════════════════════════════════════════════════

    float _venusRadius;
    float _earthRadius;
    float _venusStartDeg;
    float _earthStartDeg;
    float _omegaVenus;   // degrees per second
    float _omegaEarth;   // degrees per second

    // Day counter state
    float _venusDegreesTravelled;  // total degrees Venus has swept since motion started
    float _earthDegreesTravelled;  // total degrees Earth has swept since motion started
    bool  _countersActive;         // true once labels should reflect days instead of names

    // Beam state
    float _beamBaseStartAlpha = 1f;
    float _beamBaseEndAlpha   = 1f;

    // Sun's local position within the SolarSystem parent — used as orbit centre offset
    Vector3 _sunLocalPos;

    List<Vector3> _conjunctionPoints = new List<Vector3>();
    List<MonoBehaviour> _disabledScripts = new List<MonoBehaviour>();

    // Original local scales cached at Awake so Scale fade can restore them
    Dictionary<GameObject, Vector3> _originalScales = new Dictionary<GameObject, Vector3>();

    // ═══════════════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ═══════════════════════════════════════════════════════════════════════

    void Awake()
    {
        ComputeSynodicPeriod();
        SampleStartingPositions();
        ComputeAngularSpeeds();
        DisableConflictingOrbitScripts();

        // Cache original scales BEFORE we zero them out for the fade
        CacheOriginalScale(sunRoot);
        CacheOriginalScale(venusRoot);
        CacheOriginalScale(earthRoot);
        CacheOriginalScale(venusEllipse);
        CacheOriginalScale(earthEllipse);
    }

    void Start()
    {
        // Start everything hidden (at zero visibility) using the chosen fade mode
        SetVisibilityAll(sunRoot, 0f);
        SetVisibilityAll(venusRoot, 0f);
        SetVisibilityAll(earthRoot, 0f);
        if (venusEllipse != null) SetVisibilityAll(venusEllipse, 0f);
        if (earthEllipse != null) SetVisibilityAll(earthEllipse, 0f);

        // Initialize labels to planet names
        SetLabelText(venusLabel, venusName);
        SetLabelText(earthLabel, earthName);

        // Hide the Venus↔Earth beam until motion starts
        if (venusEarthBeam != null)
        {
            venusEarthBeam.positionCount = 2;
            venusEarthBeam.enabled = false;
            _beamBaseStartAlpha = venusEarthBeam.startColor.a;
            _beamBaseEndAlpha   = venusEarthBeam.endColor.a;
            ApplyBeamAlpha(0f);
        }

        StartCoroutine(RunSequence());
    }

    /// <summary>
    /// Runs every frame after all Update / coroutine movement has happened,
    /// so the beam always reflects the planets' final per-frame positions.
    /// </summary>
    void LateUpdate()
    {
        if (venusEarthBeam == null || venusRoot == null || earthRoot == null) return;
        if (!venusEarthBeam.enabled) return;

        venusEarthBeam.useWorldSpace = true;
        venusEarthBeam.SetPosition(0, venusRoot.transform.position);
        venusEarthBeam.SetPosition(1, earthRoot.transform.position);
    }

    void CacheOriginalScale(GameObject go)
    {
        if (go == null) return;
        if (!_originalScales.ContainsKey(go))
            _originalScales[go] = go.transform.localScale;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // INITIALIZATION
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Desmos quantized synodic formula preserving the 13:8 resonance.
    /// </summary>
    void ComputeSynodicPeriod()
    {
        // n = (1/8) · round(8 · O_outer / I_inner)
        _speedRatio_n = Mathf.Round(8f * (earthPeriodYears / venusPeriodYears)) / 8f;

        // T_syn = 1 / (n − 1)
        float denominator = _speedRatio_n - 1f;
        if (denominator <= 0f)
        {
            Debug.LogError("[SynodicCycleManager] Invalid periods — n ≤ 1. Check venusPeriodYears and earthPeriodYears.");
            _synodicYears = Mathf.Infinity;
            _synodicSeconds = Mathf.Infinity;
            return;
        }

        _synodicYears   = 1f / denominator;
        _synodicSeconds = _synodicYears * secondsPerYear;

        Debug.Log($"[SynodicCycleManager] MATH:" +
                  $"\n  n = {_speedRatio_n:F4} (speed ratio, 13:8 resonance)" +
                  $"\n  T_syn = {_synodicYears:F4} yr = {_synodicYears * 365.25f:F1} days = {_synodicSeconds:F2} real seconds" +
                  $"\n  Full Venus round = {5f * _synodicYears:F4} yr = {5f * _synodicSeconds:F2} s");
    }

    /// <summary>
    /// Reads each planet's LOCAL position (relative to the SolarSystem parent)
    /// to derive orbital radius and starting angle in the parent's local XZ plane.
    ///
    /// This matches PlanetOrbit's approach: the SolarSystem parent is tilted
    /// ~61.5° around X, so we must work in local space to orbit on the correct axis.
    /// </summary>
    void SampleStartingPositions()
    {
        // Cache the Sun's local position as the orbit centre offset
        _sunLocalPos = (sunRoot != null)
            ? sunRoot.transform.localPosition
            : Vector3.zero;

        if (venusRoot != null)
        {
            Vector3 offset = venusRoot.transform.localPosition - _sunLocalPos;
            _venusRadius   = new Vector2(offset.x, offset.z).magnitude;
            _venusStartDeg = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
            _venusAngleDeg = _venusStartDeg;
            Debug.Log($"[SynodicCycleManager] Venus  r={_venusRadius:F2}  θ₀={_venusStartDeg:F1}° (local)");
        }

        if (earthRoot != null)
        {
            Vector3 offset = earthRoot.transform.localPosition - _sunLocalPos;
            _earthRadius   = new Vector2(offset.x, offset.z).magnitude;
            _earthStartDeg = Mathf.Atan2(offset.z, offset.x) * Mathf.Rad2Deg;
            _earthAngleDeg = _earthStartDeg;
            Debug.Log($"[SynodicCycleManager] Earth  r={_earthRadius:F2}  θ₀={_earthStartDeg:F1}° (local)");
        }
    }

    void ComputeAngularSpeeds()
    {
        // ω = 360° / (period × secondsPerYear)   →   degrees per real second
        _omegaVenus = 360f / (venusPeriodYears * secondsPerYear);
        _omegaEarth = 360f / (earthPeriodYears  * secondsPerYear);

        Debug.Log($"[SynodicCycleManager] ω_Venus={_omegaVenus:F4}°/s  ω_Earth={_omegaEarth:F4}°/s");
    }

    /// <summary>
    /// Finds and disables any MonoBehaviour on Venus/Earth whose type name
    /// contains "orbit" or "rotatearound" so they don't fight this script.
    /// </summary>
    void DisableConflictingOrbitScripts()
    {
        DisableOrbitScriptsOn(venusRoot);
        DisableOrbitScriptsOn(earthRoot);
    }

    void DisableOrbitScriptsOn(GameObject root)
    {
        if (root == null) return;
        foreach (MonoBehaviour mb in root.GetComponents<MonoBehaviour>())
        {
            if (mb == null || mb == this) continue;
            string typeName = mb.GetType().Name.ToLower();
            if (typeName.Contains("orbit") || typeName.Contains("rotatearound") || typeName.Contains("planetorbit"))
            {
                mb.enabled = false;
                _disabledScripts.Add(mb);
                Debug.Log($"[SynodicCycleManager] Auto-disabled: {mb.GetType().Name} on {root.name}");
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // MAIN SEQUENCE
    // ═══════════════════════════════════════════════════════════════════════

    IEnumerator RunSequence()
    {
        // ── Phase 1: Fade In (Venus → Earth → Sun) ───────────────────────
        _phase = Phase.FadingIn;
        Debug.Log("[SynodicCycleManager] Phase 1: Fading in Venus → Earth → Sun (ellipses sequenced independently)...");

        // Venus group: planet + ellipse in chosen order
        yield return StartCoroutine(FadePlanetGroup(venusRoot, venusEllipse));
        yield return new WaitForSeconds(pauseBetweenFades);

        // Earth group: planet + ellipse in chosen order
        yield return StartCoroutine(FadePlanetGroup(earthRoot, earthEllipse));
        yield return new WaitForSeconds(pauseBetweenFades);

        // Sun last (no ellipse)
        yield return StartCoroutine(FadeIn(sunRoot));
        yield return new WaitForSeconds(pauseBetweenFades);

        // ── Phase 2: First Synodic Cycle ─────────────────────────────────
        _phase = Phase.FirstCycle;
        Debug.Log("[SynodicCycleManager] Phase 2: First synodic cycle...");

        // Planets have finished loading in — switch labels from names to day counters
        _countersActive = true;
        RefreshDayLabels(); // show "0 days" immediately

        // Reveal the Venus↔Earth beam
        if (venusEarthBeam != null)
        {
            venusEarthBeam.enabled = true;
            StartCoroutine(FadeInBeam());
        }

        yield return StartCoroutine(OrbitForDuration(_synodicSeconds));

        // Record this conjunction
        RecordConjunction();

        // ── Phase 3: Waiting for Interaction ─────────────────────────────
        _phase = Phase.WaitingForInteraction;
        Debug.Log("[SynodicCycleManager] Phase 3: First cycle complete — waiting for interaction.");
        onFirstCycleComplete?.Invoke();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PUBLIC — TRIGGER THE FULL 8:5 VENUS ROUND
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Call this from an XR Simple Interactable "Select Entered" event,
    /// a UI button OnClick, or any other trigger.
    ///
    /// Runs 4 more synodic cycles (5 total with the initial one) to
    /// complete the full 8:5 Venus round — 8 Earth years, 13 Venus orbits,
    /// 5 conjunctions. This is the Mesoamerican Venus cycle tracked by
    /// the Dresden Codex.
    /// </summary>
    public void StartFullCycle()
    {
        if (_phase != Phase.WaitingForInteraction)
        {
            Debug.LogWarning($"[SynodicCycleManager] StartFullCycle() ignored — " +
                             $"current phase is {_phase}, need WaitingForInteraction.");
            return;
        }

        StartCoroutine(RunFullCycle());
    }

    IEnumerator RunFullCycle()
    {
        _phase = Phase.FullCycle;
        Debug.Log("[SynodicCycleManager] Phase 4: Running 4 more synodic cycles (5 total)...");

        // We already completed cycle 1. Run 4 more.
        for (int i = 2; i <= 5; i++)
        {
            yield return StartCoroutine(OrbitForDuration(_synodicSeconds));
            RecordConjunction();

            Debug.Log($"[SynodicCycleManager] Conjunction {_conjunctionCount}/5 — " +
                      $"Venus θ={NormalizeAngle(_venusAngleDeg):F1}°  " +
                      $"Earth θ={NormalizeAngle(_earthAngleDeg):F1}°");
        }

        _phase = Phase.Complete;
        Debug.Log("[SynodicCycleManager] COMPLETE — 5 synodic cycles = 8 Earth years. Venus round finished.");

        // Optionally hide the live beam once the run is done
        if (beamOnlyDuringMotion && venusEarthBeam != null)
        {
            ApplyBeamAlpha(0f);
            venusEarthBeam.enabled = false;
        }

        onFullCycleComplete?.Invoke();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // ORBIT ENGINE
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Advances both planets at their real angular velocities for the
    /// specified duration in real seconds. Clamps the final frame so
    /// planets land at exact positions with no overshoot.
    /// </summary>
    IEnumerator OrbitForDuration(float durationSeconds)
    {
        float elapsed = 0f;

        // Remember where we started this segment for exact final snap
        float segStartVenus = _venusAngleDeg;
        float segStartEarth = _earthAngleDeg;
        float segVenusTravelled = _venusDegreesTravelled;
        float segEarthTravelled = _earthDegreesTravelled;

        while (elapsed < durationSeconds)
        {
            float dt = Time.deltaTime;

            // Clamp the last frame
            if (elapsed + dt > durationSeconds)
                dt = durationSeconds - elapsed;

            float dVenus = _omegaVenus * dt;
            float dEarth = _omegaEarth * dt;

            _venusAngleDeg          += dVenus;
            _earthAngleDeg          += dEarth;
            _venusDegreesTravelled  += dVenus;
            _earthDegreesTravelled  += dEarth;
            elapsed += dt;

            PlaceOnOrbit(venusRoot, _venusAngleDeg, _venusRadius);
            PlaceOnOrbit(earthRoot, _earthAngleDeg, _earthRadius);

            if (_countersActive) RefreshDayLabels();

            yield return null;
        }

        // Snap to mathematically exact final values (eliminates float drift)
        float totalVenus = _omegaVenus * durationSeconds;
        float totalEarth = _omegaEarth * durationSeconds;
        _venusAngleDeg         = segStartVenus     + totalVenus;
        _earthAngleDeg         = segStartEarth     + totalEarth;
        _venusDegreesTravelled = segVenusTravelled + totalVenus;
        _earthDegreesTravelled = segEarthTravelled + totalEarth;

        PlaceOnOrbit(venusRoot, _venusAngleDeg, _venusRadius);
        PlaceOnOrbit(earthRoot, _earthAngleDeg, _earthRadius);

        if (_countersActive) RefreshDayLabels();
    }

    /// <summary>
    /// Places a planet on its circular orbit in the PARENT'S LOCAL XZ plane,
    /// offset by the Sun's local position. Y (local up) is preserved at 0.
    ///
    /// Using localPosition ensures the orbit respects the SolarSystem parent's
    /// rotation (tilted ~61.5° around X), matching PlanetOrbit's behaviour.
    /// </summary>
    void PlaceOnOrbit(GameObject planet, float angleDeg, float radius)
    {
        if (planet == null) return;
        float rad = angleDeg * Mathf.Deg2Rad;

        planet.transform.localPosition = _sunLocalPos + new Vector3(
            radius * Mathf.Cos(rad),
            0f,
            radius * Mathf.Sin(rad)
        );
    }

    // ═══════════════════════════════════════════════════════════════════════
    // CONJUNCTION TRACKING (Mesoamerican Venus round — 5 conjunctions = 8 yr)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Records Venus's position at each conjunction for debug logging and
    /// editor gizmo display. No visual shape is drawn — the Mesoamerican
    /// astronomical tradition tracked these 5 conjunctions as a sacred 8-year
    /// Venus round without connecting them into a star shape.
    /// </summary>
    void RecordConjunction()
    {
        _conjunctionCount++;

        if (venusRoot != null)
        {
            Vector3 pt = venusRoot.transform.position;
            _conjunctionPoints.Add(pt);

            Vector3 localOffset = venusRoot.transform.localPosition - _sunLocalPos;
            float localAngle = Mathf.Atan2(localOffset.z, localOffset.x) * Mathf.Rad2Deg;
            Debug.Log($"[SynodicCycleManager] Conjunction #{_conjunctionCount} — " +
                      $"Venus local θ={NormalizeAngle(localAngle):F1}°  " +
                      $"world pos ({pt.x:F1}, {pt.y:F1}, {pt.z:F1})");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // FADE SYSTEM
    // ═══════════════════════════════════════════════════════════════════════

    IEnumerator FadeIn(GameObject root)
    {
        if (root == null) yield break;

        for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
        {
            SetVisibilityAll(root, Mathf.Clamp01(t / fadeDuration));
            yield return null;
        }
        SetVisibilityAll(root, 1f);
    }

    /// <summary>
    /// Fades in with a custom duration. Used for ellipses so they can have
    /// a different timing from planets.
    /// </summary>
    IEnumerator FadeInCustom(GameObject root, float duration)
    {
        if (root == null) yield break;
        if (duration <= 0f) { SetVisibilityAll(root, 1f); yield break; }

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            SetVisibilityAll(root, Mathf.Clamp01(t / duration));
            yield return null;
        }
        SetVisibilityAll(root, 1f);
    }

    /// <summary>
    /// Sequences a planet + its ellipse using the chosen ellipse order.
    /// Ellipse timing uses its own duration/pauses so it's independent from the planet.
    /// If fadeEllipsesSeparately is false, only the planet fades in.
    /// </summary>
    IEnumerator FadePlanetGroup(GameObject planet, GameObject ellipse)
    {
        bool hasEllipse = (ellipse != null) && fadeEllipsesSeparately;

        if (hasEllipse && ellipseOrder == EllipseOrder.BeforePlanet)
        {
            if (pauseBeforeEllipse > 0f) yield return new WaitForSeconds(pauseBeforeEllipse);
            yield return StartCoroutine(FadeInCustom(ellipse, ellipseFadeDuration));
            if (pauseAfterEllipse > 0f) yield return new WaitForSeconds(pauseAfterEllipse);
        }

        yield return StartCoroutine(FadeIn(planet));

        if (hasEllipse && ellipseOrder == EllipseOrder.AfterPlanet)
        {
            if (pauseBeforeEllipse > 0f) yield return new WaitForSeconds(pauseBeforeEllipse);
            yield return StartCoroutine(FadeInCustom(ellipse, ellipseFadeDuration));
            if (pauseAfterEllipse > 0f) yield return new WaitForSeconds(pauseAfterEllipse);
        }
    }

    /// <summary>
    /// Unified visibility setter — applies scale, alpha, or both based on FadeMode.
    /// Scale fade is guaranteed to work regardless of material/shader setup.
    /// </summary>
    void SetVisibilityAll(GameObject root, float v)
    {
        if (root == null) return;
        v = Mathf.Clamp01(v);

        if (fadeMode == FadeMode.Scale || fadeMode == FadeMode.Both)
        {
            Vector3 originalScale;
            if (_originalScales.TryGetValue(root, out originalScale))
                root.transform.localScale = originalScale * v;
        }

        if (fadeMode == FadeMode.Alpha || fadeMode == FadeMode.Both)
        {
            SetAlphaAll(root, v);
        }
    }

    // ── Material alpha helpers ────────────────────────────────────────────
    // Ported from SolarSystemSceneManager — uses MaterialPropertyBlock
    // to avoid instantiating shared materials.

    static readonly int k_BaseColor = Shader.PropertyToID("_BaseColor"); // URP Lit
    static readonly int k_Color     = Shader.PropertyToID("_Color");     // Legacy
    static readonly int k_AlphaF    = Shader.PropertyToID("_Alpha");     // Custom

    static MaterialPropertyBlock s_Mpb;
    static MaterialPropertyBlock Mpb => s_Mpb ?? (s_Mpb = new MaterialPropertyBlock());

    void SetAlphaAll(GameObject root, float a)
    {
        if (root == null) return;
        a = Mathf.Clamp01(a);

        // Mesh / Sprite renderers
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer) continue;
            ApplyRendererAlpha(r, a);
        }

        // Particle systems
        foreach (ParticleSystem ps in root.GetComponentsInChildren<ParticleSystem>(true))
            ApplyParticleAlpha(ps, a);

        // Canvas groups (for any UI elements on planets)
        foreach (CanvasGroup cg in root.GetComponentsInChildren<CanvasGroup>(true))
            cg.alpha = a;
    }

    static void ApplyRendererAlpha(Renderer rend, float a)
    {
        if (rend == null || rend.sharedMaterial == null) return;

        rend.GetPropertyBlock(Mpb);
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

        rend.SetPropertyBlock(Mpb);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // BEAM FADE
    // ═══════════════════════════════════════════════════════════════════════

    IEnumerator FadeInBeam()
    {
        if (venusEarthBeam == null) yield break;

        float duration = Mathf.Max(0.01f, beamFadeInSeconds);
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            ApplyBeamAlpha(Mathf.Clamp01(t / duration));
            yield return null;
        }
        ApplyBeamAlpha(1f);
    }

    /// <summary>
    /// Multiplies the LineRenderer's original vertex colors by `v` on both ends,
    /// so the beam fades in while preserving whatever color/gradient you configured.
    /// </summary>
    void ApplyBeamAlpha(float v)
    {
        if (venusEarthBeam == null) return;
        v = Mathf.Clamp01(v);

        Color sc = venusEarthBeam.startColor;
        Color ec = venusEarthBeam.endColor;
        sc.a = _beamBaseStartAlpha * v;
        ec.a = _beamBaseEndAlpha   * v;
        venusEarthBeam.startColor = sc;
        venusEarthBeam.endColor   = ec;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // LABEL SYSTEM (reflection-based so it works with both TextMeshPro and UI.Text)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sets the .text property on a TMP_Text, TextMeshProUGUI, TextMeshPro,
    /// or UnityEngine.UI.Text via reflection. Logs a warning if the component
    /// doesn't expose a settable 'text' property.
    /// </summary>
    static void SetLabelText(Component label, string text)
    {
        if (label == null) return;

        var prop = label.GetType().GetProperty("text",
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Instance);

        if (prop == null || !prop.CanWrite)
        {
            Debug.LogWarning($"[SynodicCycleManager] Label '{label.name}' ({label.GetType().Name}) has no settable 'text' property. " +
                             "Make sure you dragged a TextMeshProUGUI, TextMeshPro, or UI.Text component (not the GameObject).");
            return;
        }

        try { prop.SetValue(label, text); }
        catch (System.Exception e) { Debug.LogWarning($"[SynodicCycleManager] Failed to set label text: {e.Message}"); }
    }

    /// <summary>
    /// Converts accumulated orbital degrees into a day number, formatted via dayLabelFormat,
    /// and pushes it to both labels. Uses the selected DayCounterMode.
    /// </summary>
    void RefreshDayLabels()
    {
        // Venus
        if (venusLabel != null)
        {
            float days = DegreesToDays(_venusDegreesTravelled, venusPeriodYears);
            if (dayCounterMode == DayCounterMode.DaysInCurrentOrbit)
            {
                float periodDays = venusPeriodYears * 365.25f;
                days = Mathf.Repeat(days, periodDays);
            }
            SetLabelText(venusLabel, string.Format(dayLabelFormat, days));
        }

        // Earth
        if (earthLabel != null)
        {
            float days = DegreesToDays(_earthDegreesTravelled, earthPeriodYears);
            if (dayCounterMode == DayCounterMode.DaysInCurrentOrbit)
            {
                float periodDays = earthPeriodYears * 365.25f;
                days = Mathf.Repeat(days, periodDays);
            }
            SetLabelText(earthLabel, string.Format(dayLabelFormat, days));
        }
    }

    /// <summary>
    /// Converts degrees travelled into Earth-days for a planet whose full orbit
    /// takes `periodYears` Earth years.
    ///   full orbit (360°) = periodYears × 365.25 days
    /// </summary>
    static float DegreesToDays(float degrees, float periodYears)
    {
        return (degrees / 360f) * (periodYears * 365.25f);
    }

    static void ApplyParticleAlpha(ParticleSystem ps, float a)
    {
        var emission = ps.emission;
        emission.enabled = a > 0.01f;

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
            // Gradient / TwoGradients: emission toggle is sufficient
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PUBLIC GETTERS
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Current phase of the sequence.</summary>
    public Phase CurrentPhase => _phase;

    /// <summary>Computed synodic period in years.</summary>
    public float SynodicPeriodYears => _synodicYears;

    /// <summary>Computed synodic period in real seconds.</summary>
    public float SynodicPeriodSeconds => _synodicSeconds;

    /// <summary>How many conjunctions have been recorded so far (max 5).</summary>
    public int ConjunctionCount => _conjunctionCount;

    /// <summary>True once the full 8:5 Venus round is complete.</summary>
    public bool IsComplete => _phase == Phase.Complete;

    /// <summary>True when the system is waiting for the user to click.</summary>
    public bool IsWaitingForInteraction => _phase == Phase.WaitingForInteraction;

    // ── Read-only state access for external scripts (VenusManualOrbit) ────
    /// <summary>Venus's current orbital angle in degrees (parent local XZ plane).</summary>
    public float VenusAngleDeg => _venusAngleDeg;
    /// <summary>Earth's current orbital angle in degrees (parent local XZ plane).</summary>
    public float EarthAngleDeg => _earthAngleDeg;
    /// <summary>Venus's orbital radius in parent local units.</summary>
    public float VenusRadius => _venusRadius;
    /// <summary>Earth's orbital radius in parent local units.</summary>
    public float EarthRadius => _earthRadius;
    /// <summary>Sun's local position (orbit centre) within the SolarSystem parent.</summary>
    public Vector3 SunLocalPos => _sunLocalPos;
    /// <summary>Speed ratio n (Venus angular speed / Earth angular speed). 13:8 resonance ≈ 1.625.</summary>
    public float SpeedRatioN => _speedRatio_n;
    /// <summary>The SolarSystem parent transform that tilts the orbital plane. Null if the Sun has no parent.</summary>
    public Transform OrbitParent => (sunRoot != null) ? sunRoot.transform.parent : null;

    // ═══════════════════════════════════════════════════════════════════════
    // MANUAL CONTROL — DRIVE VENUS, EARTH FOLLOWS AT 8:13 RATIO
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sets Venus to an absolute orbital angle. Earth rotates by the SAME SHORTEST
    /// delta that Venus rotated, scaled by Earth's angular-speed ratio (8/13),
    /// so the Mesoamerican 13:8 resonance stays locked while the user drags Venus.
    ///
    /// Intended for VenusManualOrbit after Phase.Complete. Call once per frame
    /// with the user's target angle (from controller ray or mouse).
    ///
    /// Signed shortest-angle delta is used so dragging clockwise/counter-clockwise
    /// both work naturally and wrap-around through 0°/360° doesn't cause jumps.
    /// </summary>
    /// <param name="targetVenusAngleDeg">New Venus orbital angle (degrees, same frame as _venusAngleDeg).</param>
    public void DriveVenusToAngle(float targetVenusAngleDeg)
    {
        // Shortest signed delta for Venus (handles 360° wrap-around)
        float dVenus = Mathf.DeltaAngle(_venusAngleDeg, targetVenusAngleDeg);

        // Earth follows at 8/13 of Venus's angular step (inverse of n = 13/8 = 1.625)
        float earthRatio = (_speedRatio_n > 0f) ? (1f / _speedRatio_n) : (8f / 13f);
        float dEarth = dVenus * earthRatio;

        _venusAngleDeg          += dVenus;
        _earthAngleDeg          += dEarth;
        _venusDegreesTravelled  += dVenus;
        _earthDegreesTravelled  += dEarth;

        PlaceOnOrbit(venusRoot, _venusAngleDeg, _venusRadius);
        PlaceOnOrbit(earthRoot, _earthAngleDeg, _earthRadius);

        if (_countersActive) RefreshDayLabels();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // UTILITY
    // ═══════════════════════════════════════════════════════════════════════

    static float NormalizeAngle(float deg)
    {
        deg %= 360f;
        if (deg < 0f) deg += 360f;
        return deg;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // EDITOR GIZMOS
    // ═══════════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;

        // We need the parent's localToWorldMatrix to draw gizmos correctly
        // in the SolarSystem parent's tilted local space.
        Vector3 sunLP = Vector3.zero;
        Matrix4x4 parentMat = Matrix4x4.identity;

        if (sunRoot != null)
        {
            sunLP = sunRoot.transform.localPosition;
            if (sunRoot.transform.parent != null)
                parentMat = sunRoot.transform.parent.localToWorldMatrix;
        }

        // ── Orbit circles (in parent's local space → world) ──────────────
        if (venusRoot != null)
        {
            Vector3 offset = venusRoot.transform.localPosition - sunLP;
            float r = Application.isPlaying ? _venusRadius
                : new Vector2(offset.x, offset.z).magnitude;
            Gizmos.color = new Color(1f, 0.85f, 0f, 0.6f); // Venus gold
            DrawCircleLocalXZ(parentMat, sunLP, r, 72);
        }

        if (earthRoot != null)
        {
            Vector3 offset = earthRoot.transform.localPosition - sunLP;
            float r = Application.isPlaying ? _earthRadius
                : new Vector2(offset.x, offset.z).magnitude;
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.6f); // Earth blue
            DrawCircleLocalXZ(parentMat, sunLP, r, 72);
        }

        // ── Conjunction points (editor-only reference dots) ──────────────
        if (_conjunctionPoints.Count > 0)
        {
            Gizmos.color = new Color(1f, 0.85f, 0f, 0.8f);
            foreach (var pt in _conjunctionPoints)
                Gizmos.DrawSphere(pt, gizmoDotRadius);
        }
    }

    /// <summary>
    /// Draws a circle in the parent's LOCAL XZ plane, converted to world space
    /// via the parent's localToWorldMatrix. This ensures the gizmo orbit ring
    /// matches the tilted SolarSystem parent.
    /// </summary>
    static void DrawCircleLocalXZ(Matrix4x4 parentMat, Vector3 centreLocal, float radius, int segments)
    {
        float step = 360f / segments;
        Vector3 prevLocal = centreLocal + new Vector3(radius, 0f, 0f);
        Vector3 prev = parentMat.MultiplyPoint3x4(prevLocal);

        for (int i = 1; i <= segments; i++)
        {
            float rad = i * step * Mathf.Deg2Rad;
            Vector3 lp = centreLocal + new Vector3(
                radius * Mathf.Cos(rad), 0f, radius * Mathf.Sin(rad));
            Vector3 wp = parentMat.MultiplyPoint3x4(lp);
            Gizmos.DrawLine(prev, wp);
            prev = wp;
        }
    }
#endif
}
