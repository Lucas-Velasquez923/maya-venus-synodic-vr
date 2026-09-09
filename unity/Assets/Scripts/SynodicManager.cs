using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Orchestrates the Venus-Earth synodic cycle scene.
//
// FLOW:
//   1. Venus fades in → player hovers → VenusInteraction calls AdvancePhase()
//   2. Earth + Sun fade in → player clicks Venus → first synodic cycle plays
//   3. Player clicks Venus again → 4 more cycles play (5 total = 8 Earth years)
//   4. Manual phase: player can auto-orbit Venus by clicking it
//   5. Player clicks Sun → fade out and load next scene
public class SynodicManager : MonoBehaviour
{
    [Header("Scene References")]
    public GameObject sun;
    public GameObject venus;
    public GameObject earth;
    public LineRenderer beam;

    [Header("Labels")]
    public TextMeshProUGUI venusLabel;
    public TextMeshProUGUI earthLabel;
    public TextMeshProUGUI sunLabel;

    [Header("Scene Transition")]
    public string nextScene;

    [Header("Fade-In Sounds")]
    public AudioClip venusFadeSound;
    public AudioClip earthFadeSound;
    public AudioClip sunFadeSound;
    [Range(0f, 1f)] public float fadeSoundVolume = 1f;

    [Header("Cycle Audio")]
    public AudioClip cycleChime;
    [Range(0f, 1f)] public float chimeVolume = 1f;

    [Header("Timing")]
    public float fadeDuration = 2f;
    public float pauseBetween = 0.5f;
    public float secondsPerYear = 10f;

    [Header("Orbital")]
    public float venusPeriodYears = 0.6152f;
    public float earthPeriodYears = 1f;

    [Header("Fade Out Transition")]
    public Image fadeImage;
    public float fadeOutDuration = 1f;
    public Transform xrRig;
    public float rigMoveDistance = 3f;
    public float rigMoveDuration = 1f;

    public enum Phase { VenusFadeIn, WaitHover, FadeRest, WaitClick1, FirstCycle, WaitClick2, FullCycle, Manual }

    [Header("Runtime (read only)")]
    [SerializeField] Phase _phase = Phase.VenusFadeIn;
    public Phase CurrentPhase => _phase;

    float _venusAngle, _earthAngle;
    float _venusRadius, _earthRadius;
    float _omegaVenus, _omegaEarth;
    float _synodicSeconds;
    Vector3 _sunLocalPos;
    float _totalDegVenus, _totalDegEarth;
    int _currentCycle;
    bool _isTransitioning;

    AudioSource _audio;
    Vector3 _sunScale, _venusScale, _earthScale;

    void Awake()
    {
        _sunScale   = sun   ? sun.transform.localScale   : Vector3.one;
        _venusScale = venus ? venus.transform.localScale : Vector3.one;
        _earthScale = earth ? earth.transform.localScale : Vector3.one;

        _audio = GetComponent<AudioSource>();
        if (_audio == null)
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
        }

        float speedRatio  = earthPeriodYears / venusPeriodYears; // ~1.625
        _synodicSeconds   = (1f / (speedRatio - 1f)) * secondsPerYear;
        _omegaVenus       = 360f / (venusPeriodYears * secondsPerYear);
        _omegaEarth       = 360f / (earthPeriodYears  * secondsPerYear);

        _sunLocalPos = sun ? sun.transform.localPosition : Vector3.zero;

        if (venus)
        {
            Vector3 off = venus.transform.localPosition - _sunLocalPos;
            _venusRadius = new Vector2(off.x, off.z).magnitude;
            _venusAngle  = Mathf.Atan2(off.z, off.x) * Mathf.Rad2Deg;
        }
        if (earth)
        {
            Vector3 off = earth.transform.localPosition - _sunLocalPos;
            _earthRadius = new Vector2(off.x, off.z).magnitude;
            _earthAngle  = Mathf.Atan2(off.z, off.x) * Mathf.Rad2Deg;
        }
    }

    void Start()
    {
        if (sun)   sun.transform.localScale   = Vector3.zero;
        if (earth) earth.transform.localScale = Vector3.zero;
        if (venus) venus.transform.localScale = Vector3.zero;
        if (beam)  beam.enabled = false;

        SetLabel(venusLabel, "Venus");
        SetLabel(earthLabel, "Earth");

        StartCoroutine(FadeInVenus());
    }

    void LateUpdate()
    {
        if (beam != null && beam.enabled && venus && earth)
        {
            beam.SetPosition(0, venus.transform.position);
            beam.SetPosition(1, earth.transform.position);
        }
    }

    // ── Phase Sequence ───────────────────────────────────────────────────────

    IEnumerator FadeInVenus()
    {
        _phase = Phase.VenusFadeIn;
        PlaySound(venusFadeSound, fadeSoundVolume);
        yield return ScaleFade(venus, _venusScale, fadeDuration);
        _phase = Phase.WaitHover;
    }

    // Called by VenusInteraction on hover (WaitHover) or click (WaitClick1/2)
    public void AdvancePhase()
    {
        if      (_phase == Phase.WaitHover)  StartCoroutine(FadeInRest());
        else if (_phase == Phase.WaitClick1) StartCoroutine(RunFirstCycle());
        else if (_phase == Phase.WaitClick2) StartCoroutine(RunFullCycle());
    }

    IEnumerator FadeInRest()
    {
        _phase = Phase.FadeRest;

        PlaySound(earthFadeSound, fadeSoundVolume);
        yield return ScaleFade(earth, _earthScale, fadeDuration);
        yield return new WaitForSeconds(pauseBetween);

        PlaySound(sunFadeSound, fadeSoundVolume);
        yield return ScaleFade(sun, _sunScale, fadeDuration);
        yield return new WaitForSeconds(pauseBetween);

        if (beam) { beam.positionCount = 2; beam.enabled = true; }
        if (sunLabel) sunLabel.gameObject.SetActive(false);

        StartCoroutine(RunFirstCycle());
    }

    IEnumerator RunFirstCycle()
    {
        _phase = Phase.FirstCycle;
        _totalDegVenus = 0f;
        _totalDegEarth = 0f;

        yield return OrbitForSeconds(_synodicSeconds);
        PlaySound(cycleChime, chimeVolume);

        _phase = Phase.WaitClick2;
    }

    IEnumerator RunFullCycle()
    {
        _phase = Phase.FullCycle;
        _currentCycle = 1;
        UpdateLabels();

        for (int i = 2; i <= 5; i++)
        {
            yield return OrbitForSeconds(_synodicSeconds);
            PlaySound(cycleChime, chimeVolume);
            _currentCycle = i;
            UpdateLabels();
        }

        SetLabel(venusLabel, "Cycle 5/5");
        SetLabel(earthLabel, "Complete");

        _totalDegVenus = 0f;
        _totalDegEarth = 0f;
        _phase = Phase.Manual;
        UpdateLabels();
    }

    // ── Orbit Engine ─────────────────────────────────────────────────────────

    IEnumerator OrbitForSeconds(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float dt = Mathf.Min(Time.deltaTime, duration - elapsed);
            _venusAngle    += _omegaVenus * dt;
            _earthAngle    += _omegaEarth * dt;
            _totalDegVenus += _omegaVenus * dt;
            _totalDegEarth += _omegaEarth * dt;
            elapsed += dt;

            PlaceOnOrbit(venus, _venusAngle, _venusRadius);
            PlaceOnOrbit(earth, _earthAngle, _earthRadius);
            UpdateLabels();
            yield return null;
        }
    }

    void PlaceOnOrbit(GameObject planet, float angleDeg, float radius)
    {
        if (!planet) return;
        float rad = angleDeg * Mathf.Deg2Rad;
        planet.transform.localPosition = _sunLocalPos + new Vector3(
            radius * Mathf.Cos(rad), 0f, radius * Mathf.Sin(rad));
    }

    // Called by VenusInteraction each frame when auto-orbiting is on
    public void AdvanceManualAngle(float angleDelta)
    {
        if (_phase != Phase.Manual || angleDelta <= 0f) return;

        float earthDelta = angleDelta * (_omegaEarth / _omegaVenus); // Earth moves slower
        _venusAngle    += angleDelta;
        _earthAngle    += earthDelta;
        _totalDegVenus += angleDelta;
        _totalDegEarth += earthDelta;

        PlaceOnOrbit(venus, _venusAngle, _venusRadius);
        PlaceOnOrbit(earth, _earthAngle, _earthRadius);
        UpdateLabels();
    }

    // ── Labels ───────────────────────────────────────────────────────────────

    void UpdateLabels()
    {
        if (_phase == Phase.FullCycle)
        {
            SetLabel(venusLabel, $"Cycle {_currentCycle}/5");
            SetLabel(earthLabel, $"{(_totalDegEarth / 360f) * earthPeriodYears:F2} years");
        }
        else if (_phase == Phase.Manual)
        {
            SetLabel(venusLabel, $"{(_totalDegVenus / 360f) * venusPeriodYears * 365.25f:F0} days");
            SetLabel(earthLabel, $"{(_totalDegEarth / 360f) * earthPeriodYears:F2} years");
        }
        else
        {
            SetLabel(venusLabel, $"{(_totalDegVenus / 360f) * venusPeriodYears * 365.25f:F0} days");
            SetLabel(earthLabel, "");
        }
    }

    static void SetLabel(TextMeshProUGUI tmp, string text)
    {
        if (tmp) tmp.text = text;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    IEnumerator ScaleFade(GameObject go, Vector3 targetScale, float duration)
    {
        if (!go) yield break;
        go.transform.localScale = Vector3.zero;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            go.transform.localScale = targetScale * Mathf.Clamp01(t / duration);
            yield return null;
        }
        go.transform.localScale = targetScale;
    }

    void PlaySound(AudioClip clip, float volume)
    {
        if (_audio != null && clip != null)
            _audio.PlayOneShot(clip, volume);
    }

    // ── Sun Click → Scene Transition ─────────────────────────────────────────

    public void OnSunClicked()
    {
        if (_phase != Phase.Manual || _isTransitioning) return;
        _isTransitioning = true;
        if (xrRig != null) StartCoroutine(SlideRigRight());
        StartCoroutine(FadeOutAndLoad());
    }

    IEnumerator FadeOutAndLoad()
    {
        if (fadeImage != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                Color c = fadeImage.color;
                c.a = Mathf.Clamp01(elapsed / fadeOutDuration);
                fadeImage.color = c;
                yield return null;
            }
            Color final = fadeImage.color;
            final.a = 1f;
            fadeImage.color = final;
        }
        if (!string.IsNullOrEmpty(nextScene))
            SceneManager.LoadScene(nextScene);
    }

    IEnumerator SlideRigRight()
    {
        Vector3 startPos = xrRig.localPosition;
        Vector3 endPos   = startPos + xrRig.right * rigMoveDistance;
        float elapsed = 0f;
        while (elapsed < rigMoveDuration)
        {
            elapsed += Time.deltaTime;
            xrRig.localPosition = Vector3.Lerp(startPos, endPos,
                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / rigMoveDuration)));
            yield return null;
        }
        xrRig.localPosition = endPos;
    }
}
