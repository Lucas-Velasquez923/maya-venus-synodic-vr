using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BirthdaySceneManager : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────────────────
    [Header("Spirit Character")]
    [Tooltip("The spirit GameObject (Meshy_AI_Crimson_Elder)")]
    public GameObject spirit;

    [Tooltip("Where the spirit starts (off-screen left). If empty, uses spirit's current position.")]
    public Transform spiritStartPoint;

    [Tooltip("Where the spirit stops (next to calendar). If empty, calculated automatically.")]
    public Transform spiritEndPoint;

    [Tooltip("How far left of current position the spirit starts (if no start point set)")]
    public float spiritOffsetLeft = 10f;

    [Tooltip("How long the spirit takes to walk in (seconds)")]
    public float spiritWalkDuration = 3f;

    [Header("Spirit Fog")]
    [Tooltip("Particle System on or near the spirit for the fog effect")]
    public ParticleSystem spiritFog;

    [Tooltip("Emission rate while the spirit is walking in (dense fog)")]
    public float fogDenseRate = 80f;

    [Tooltip("Emission rate after the spirit stops (light fog)")]
    public float fogLightRate = 5f;

    [Tooltip("How long it takes for the fog to thin out after the spirit stops")]
    public float fogFadeDuration = 2f;

    [Header("Spirit Arrival")]
    [Tooltip("How long the spirit takes to rotate after arriving")]
    public float spiritTurnDuration = 1f;

    [Header("Calendar")]
    [Tooltip("The calendarPlaceholder parent object")]
    public GameObject calendarPlaceholder;

    [Tooltip("The outer ring (20 day signs)")]
    public Transform bigRing;

    [Tooltip("The inner ring (13 numbers)")]
    public Transform smallRing;

    [Tooltip("How far below current position the calendar starts")]
    public float calendarDropDistance = 5f;

    [Tooltip("How long the calendar takes to rise (seconds)")]
    public float calendarRiseDuration = 2f;

    [Tooltip("Gentle hover bob amplitude (0 to disable)")]
    public float calendarBobAmount = 0.15f;

    [Tooltip("Hover bob speed")]
    public float calendarBobSpeed = 1f;

    [Header("Ring Rotation")]
    [Tooltip("Which local axis the rings spin around. " +
             "Pick whichever makes them rotate flat like a wheel, not tumble.")]
    public RotationAxis ringSpinAxis = RotationAxis.Z;

    [Tooltip("How many full extra rotations the outer ring does before landing")]
    public int bigRingExtraSpins = 4;

    [Tooltip("How many full extra rotations the inner ring does before landing (more = faster)")]
    public int smallRingExtraSpins = 6;

    [Tooltip("Total rotation animation duration (seconds)")]
    public float rotationDuration = 8f;

    public enum RotationAxis { X, Y, Z }

    [Header("UI")]
    [Tooltip("The World Space canvas with BirthdayScrollInput")]
    public GameObject birthdayInputCanvas;

    [Tooltip("The World Space canvas showing the result")]
    public GameObject resultCanvas;

    [Header("Day Sign Panels (20 PNGs)")]
    [Tooltip("Assign your 20 day sign panel images here in order: " +
             "Imix, Ik', Ak'bal, K'an, Chicchan, Cimi, Manik', Lamat, " +
             "Muluk, Ok, Chuwen, Eb', Ben, Ix, Men, Kib', Kab'an, " +
             "Etz'nab', Kawak, Ajaw")]
    public Sprite[] daySignPanels = new Sprite[20];

    [Tooltip("UI Image on the result canvas that displays the day sign panel PNG")]
    public Image daySignPanelImage;

    [Tooltip("Audio clips for each day sign, same order as panels: " +
             "Imix, Ik', Ak'bal, K'an, Chicchan, Cimi, Manik', Lamat, " +
             "Muluk, Ok, Chuwen, Eb', Ben, Ix, Men, Kib', Kab'an, " +
             "Etz'nab', Kawak, Ajaw")]
    public AudioClip[] daySignSounds = new AudioClip[20];
    [Range(0f, 1f)] public float daySignVolume = 1f;

    [Header("Final Audio")]
    [Tooltip("Plays after the day sign audio finishes, once the result UI is hidden")]
    public AudioClip finalAudioClip;
    [Range(0f, 1f)] public float finalAudioVolume = 1f;

    [Header("End Screen")]
    [Tooltip("The end screen canvas with your Platform of Venus PNG and Restart button")]
    public GameObject endScreenCanvas;

    [Tooltip("The scene to load when Restart is pressed (leave empty to reload current scene)")]
    public string restartSceneName;

    [Header("Exit Animation")]
    [Tooltip("How long the calendar takes to sink back down")]
    public float calendarSinkDuration = 2f;

    [Tooltip("How long the spirit takes to walk off screen")]
    public float spiritExitDuration = 3f;

    [Header("Timing")]
    [Tooltip("Delay before spirit starts walking")]
    public float spiritDelay = 2f;

    [Tooltip("Delay before calendar starts rising")]
    public float calendarDelay = 3f;

    [Tooltip("Delay after calendar rise before showing input UI")]
    public float inputUIDelay = 1f;

    [Header("Audio")]
    [Tooltip("Sound that plays when the calendar rises")]
    public AudioClip calendarRiseSound;
    [Range(0f, 1f)] public float calendarRiseVolume = 1f;

    [Header("Intro Voiceover")]
    [Tooltip("Plays as soon as the scene starts")]
    public AudioClip introAudioClip1;
    [Tooltip("Plays immediately after introAudioClip1 finishes")]
    public AudioClip introAudioClip2;
    [Tooltip("Plays immediately after introAudioClip2 finishes")]
    public AudioClip introAudioClip3;
    [Range(0f, 1f)] public float introAudioVolume = 1f;

    [Header("Volumetric Fog (optional)")]
    public GameObject volumetricFog;

    // ── State ────────────────────────────────────────────────────────────
    Vector3 _calendarRestPos;    // Where the calendar should float
    Vector3 _spiritEndPos;
    Vector3 _spiritStartPos;
    bool _isHovering;
    AudioSource _audioSource;
    Coroutine _introAudioRoutine;

    // ── Lifecycle ────────────────────────────────────────────────────────

    void Awake()
    {
        // Cache calendar rest position (where it is in the editor)
        if (calendarPlaceholder)
            _calendarRestPos = calendarPlaceholder.transform.position;

        // Calculate spirit positions
        if (spirit)
        {
            _spiritEndPos = spiritEndPoint
                ? spiritEndPoint.position
                : spirit.transform.position;

            _spiritStartPos = spiritStartPoint
                ? spiritStartPoint.position
                : _spiritEndPos + Vector3.left * spiritOffsetLeft;
        }

        // Set up audio source
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
        }
    }

    void Start()
    {
        // Hide UIs
        if (birthdayInputCanvas) birthdayInputCanvas.SetActive(false);
        if (resultCanvas) resultCanvas.SetActive(false);
        if (endScreenCanvas) endScreenCanvas.SetActive(false);

        // Move calendar below ground
        if (calendarPlaceholder)
        {
            calendarPlaceholder.transform.position =
                _calendarRestPos + Vector3.down * calendarDropDistance;
        }

        // Move spirit to start position (off-screen left)
        if (spirit)
        {
            spirit.transform.position = _spiritStartPos;
        }

        // Enable fog if assigned
        if (volumetricFog) volumetricFog.SetActive(true);

        // Intro voiceover starts immediately, independent of the visual sequence.
        _introAudioRoutine = StartCoroutine(PlayIntroAudio());

        // Start the sequence
        StartCoroutine(RunScene());
    }

    void Update()
    {
        // Gentle hover bob after calendar has risen
        if (_isHovering && calendarPlaceholder && calendarBobAmount > 0f)
        {
            Vector3 pos = _calendarRestPos;
            pos.y += Mathf.Sin(Time.time * calendarBobSpeed) * calendarBobAmount;
            calendarPlaceholder.transform.position = pos;
        }
    }

    // ── Scene Sequence ───────────────────────────────────────────────────

    IEnumerator RunScene()
    {
        // Wait, then spirit walks in (includes turn + fog fade)
        yield return new WaitForSeconds(spiritDelay);
        yield return StartCoroutine(SpiritWalkIn());

        // Calendar rises after spirit is fully settled and fog is gone
        yield return StartCoroutine(CalendarRise());

        // Short pause, then show birthday input
        yield return new WaitForSeconds(inputUIDelay);

        // Don't reveal the input UI until the intro voiceover has finished,
        // even if the visual sequence above already has.
        if (_introAudioRoutine != null)
            yield return _introAudioRoutine;

        ShowBirthdayInput();
    }

    // ── Intro Voiceover ──────────────────────────────────────────────────

    IEnumerator PlayIntroAudio()
    {
        if (_audioSource == null) yield break;

        if (introAudioClip1 != null)
        {
            _audioSource.PlayOneShot(introAudioClip1, introAudioVolume);
            yield return new WaitForSeconds(introAudioClip1.length);
        }

        if (introAudioClip2 != null)
        {
            _audioSource.PlayOneShot(introAudioClip2, introAudioVolume);
            yield return new WaitForSeconds(introAudioClip2.length);
        }

        if (introAudioClip3 != null)
        {
            _audioSource.PlayOneShot(introAudioClip3, introAudioVolume);
            yield return new WaitForSeconds(introAudioClip3.length);
        }
    }

    // ── Spirit Walk ──────────────────────────────────────────────────────

    IEnumerator SpiritWalkIn()
    {
        if (!spirit) yield break;

        // Start dense fog
        if (spiritFog != null)
        {
            var emission = spiritFog.emission;
            emission.rateOverTime = fogDenseRate;
            spiritFog.Play();
        }

        float elapsed = 0f;
        Vector3 start = _spiritStartPos;
        Vector3 end = _spiritEndPos;

        // Face the direction of movement (look right)
        Vector3 dir = (end - start).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            spirit.transform.rotation = Quaternion.LookRotation(
                new Vector3(dir.x, 0f, dir.z), Vector3.up);
        }

        while (elapsed < spiritWalkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spiritWalkDuration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            spirit.transform.position = Vector3.Lerp(start, end, eased);
            yield return null;
        }

        spirit.transform.position = end;

        // Rotate to face the user
        yield return StartCoroutine(SpiritTurnToFace());

        // Fade fog from dense to light
        yield return StartCoroutine(FadeFog());
    }

    IEnumerator SpiritTurnToFace()
    {
        if (!spirit) yield break;

        Quaternion startRot = spirit.transform.rotation;
        Quaternion endRot = Quaternion.Euler(0f, 180f, 0f);

        float elapsed = 0f;
        while (elapsed < spiritTurnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / spiritTurnDuration));
            spirit.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        spirit.transform.rotation = endRot;
    }

    IEnumerator FadeFog()
    {
        if (spiritFog == null) yield break;

        var emission = spiritFog.emission;
        var main = spiritFog.main;
        float startRate = fogDenseRate;
        float elapsed = 0f;

        // Ramp emission down to zero
        while (elapsed < fogFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fogFadeDuration);
            emission.rateOverTime = Mathf.Lerp(startRate, 0f, t);
            yield return null;
        }

        // Stop emitting and clear any remaining particles
        emission.rateOverTime = fogLightRate;
        spiritFog.Stop();
        spiritFog.Clear();
    }

    // ── Calendar Rise ────────────────────────────────────────────────────

    IEnumerator CalendarRise()
    {
        if (!calendarPlaceholder) yield break;

        if (_audioSource != null && calendarRiseSound != null)
            _audioSource.PlayOneShot(calendarRiseSound, calendarRiseVolume);

        Vector3 start = _calendarRestPos + Vector3.down * calendarDropDistance;
        Vector3 end = _calendarRestPos;
        float elapsed = 0f;

        while (elapsed < calendarRiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / calendarRiseDuration);
            // Ease out — decelerates as it reaches the top
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            calendarPlaceholder.transform.position = Vector3.Lerp(start, end, eased);
            yield return null;
        }

        calendarPlaceholder.transform.position = end;
        _isHovering = true;
    }

    // ── Birthday Input ───────────────────────────────────────────────────

    void ShowBirthdayInput()
    {
        if (birthdayInputCanvas)
        {
            birthdayInputCanvas.SetActive(true);

            // Tell the scroll input to call us back when done
            var scrollInput = birthdayInputCanvas.GetComponentInChildren<BirthdayScrollInput>();
            if (scrollInput != null)
            {
                scrollInput.OnBirthdaySubmitted = OnBirthdaySubmitted;
            }
        }
    }

    /// <summary>
    /// Called by BirthdayScrollInput when the player submits their birthday.
    /// </summary>
    public void OnBirthdaySubmitted(int year, int month, int day)
    {
        // Hide input
        if (birthdayInputCanvas) birthdayInputCanvas.SetActive(false);

        // Calculate Tzolk'in date
        var result = BdayGetter.GetTzolkinDate(year, month, day);
        Debug.Log($"[BirthdayScene] Birthday: {month}/{day}/{year} → {result.fullName}");

        // Rotate calendar and show result
        StartCoroutine(RotateAndReveal(result));
    }

    // ── Calendar Rotation ────────────────────────────────────────────────

    IEnumerator RotateAndReveal(BdayGetter.TzolkinResult result)
    {
        // Stop hover bob during rotation for stability
        _isHovering = false;
        if (calendarPlaceholder)
            calendarPlaceholder.transform.position = _calendarRestPos;

        // Calculate target rotations
        // bigRing: 20 segments, each = 18 degrees.
        float bigRingTarget = -(result.signIndex / 20f) * 360f;
        float bigRingTotal = bigRingTarget - (bigRingExtraSpins * 360f);

        // smallRing: 13 segments, each = ~27.69 degrees
        float smallRingTarget = -((result.number - 1) / 13f) * 360f;
        float smallRingTotal = smallRingTarget - (smallRingExtraSpins * 360f);

        // Cache starting rotations
        Quaternion bigStart = bigRing ? bigRing.localRotation : Quaternion.identity;
        Quaternion smallStart = smallRing ? smallRing.localRotation : Quaternion.identity;

        float elapsed = 0f;
        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotationDuration);

            // Ease in-out with a long, gradual deceleration at the end
            // Quick ramp up in the first 30%, then a slow coast to a stop
            float eased = t < 0.3f
                ? (t / 0.3f) * (t / 0.3f) * 0.3f
                : 0.3f + 0.7f * (1f - Mathf.Pow(1f - (t - 0.3f) / 0.7f, 4f));

            // Rotate both rings around the chosen axis (flat spin, not tumble)
            if (bigRing)
                bigRing.localRotation = bigStart * AxisRotation(bigRingTotal * eased);
            if (smallRing)
                smallRing.localRotation = smallStart * AxisRotation(smallRingTotal * eased);

            yield return null;
        }

        // Snap to exact final rotations
        if (bigRing)
            bigRing.localRotation = bigStart * AxisRotation(bigRingTotal);
        if (smallRing)
            smallRing.localRotation = smallStart * AxisRotation(smallRingTotal);

        // Resume hover bob
        _isHovering = true;

        // Short pause before showing result
        yield return new WaitForSeconds(2f);

        yield return StartCoroutine(ShowResultAndExit(result));
    }

    // ── Result Display ───────────────────────────────────────────────────

    IEnumerator ShowResultAndExit(BdayGetter.TzolkinResult result)
    {
        // ── Show result panel ────────────────────────────────────────────
        if (resultCanvas) resultCanvas.SetActive(true);

        // Show the matching day sign panel PNG
        if (daySignPanelImage != null
            && daySignPanels != null
            && result.signIndex >= 0
            && result.signIndex < daySignPanels.Length
            && daySignPanels[result.signIndex] != null)
        {
            daySignPanelImage.sprite = daySignPanels[result.signIndex];
            daySignPanelImage.enabled = true;
        }

        // Play the matching day sign sound and wait for it to finish
        float waitTime = 0f;
        if (_audioSource != null
            && daySignSounds != null
            && result.signIndex >= 0
            && result.signIndex < daySignSounds.Length
            && daySignSounds[result.signIndex] != null)
        {
            AudioClip clip = daySignSounds[result.signIndex];
            _audioSource.PlayOneShot(clip, daySignVolume);
            waitTime = clip.length;
        }

        Debug.Log($"[BirthdayScene] Result displayed: {result.fullName}");

        // Wait for the audio to finish
        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);
        else
            yield return new WaitForSeconds(3f); // fallback if no audio assigned

        // ── Hide result panel ────────────────────────────────────────────
        if (resultCanvas) resultCanvas.SetActive(false);

        // ── Final audio, plays with the result UI already hidden ──────────
        if (_audioSource != null && finalAudioClip != null)
        {
            _audioSource.PlayOneShot(finalAudioClip, finalAudioVolume);
            yield return new WaitForSeconds(finalAudioClip.length);
        }

        // ── Exit animation: calendar sinks + spirit walks off simultaneously
        _isHovering = false;
        StartCoroutine(CalendarSink());
        StartCoroutine(SpiritWalkOff());

        // Wait for the longer of the two animations
        float longestExit = Mathf.Max(calendarSinkDuration, spiritExitDuration);
        yield return new WaitForSeconds(longestExit + 0.5f);

        // ── Show end screen ──────────────────────────────────────────────
        if (endScreenCanvas) endScreenCanvas.SetActive(true);
    }

    // ── Exit Animations ─────────────────────────────────────────────────

    IEnumerator CalendarSink()
    {
        if (!calendarPlaceholder) yield break;

        Vector3 start = calendarPlaceholder.transform.position;
        Vector3 end = _calendarRestPos + Vector3.down * calendarDropDistance;
        float elapsed = 0f;

        while (elapsed < calendarSinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / calendarSinkDuration);
            // Ease in — accelerates as it drops
            float eased = t * t;
            calendarPlaceholder.transform.position = Vector3.Lerp(start, end, eased);
            yield return null;
        }

        calendarPlaceholder.transform.position = end;
    }

    IEnumerator SpiritWalkOff()
    {
        if (!spirit) yield break;

        // Ramp fog back up as spirit leaves
        if (spiritFog != null)
        {
            var emission = spiritFog.emission;
            emission.rateOverTime = fogDenseRate;
        }

        Vector3 start = spirit.transform.position;
        Vector3 end = _spiritStartPos; // back to off-screen left

        // Face the exit direction
        Vector3 dir = (end - start).normalized;
        if (dir.sqrMagnitude > 0.001f)
        {
            spirit.transform.rotation = Quaternion.LookRotation(
                new Vector3(dir.x, 0f, dir.z), Vector3.up);
        }

        float elapsed = 0f;
        while (elapsed < spiritExitDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spiritExitDuration);
            // Ease in — starts slow, then speeds up as it leaves
            float eased = t * t;
            spirit.transform.position = Vector3.Lerp(start, end, eased);
            yield return null;
        }

        spirit.transform.position = end;

        // Stop fog and hide spirit
        if (spiritFog != null) spiritFog.Stop();
        spirit.SetActive(false);
    }

    // ── Restart ─────────────────────────────────────────────────────────

    /// <summary>
    /// Called by the Restart button on the end screen canvas.
    /// </summary>
    public void RestartGame()
    {
        if (!string.IsNullOrEmpty(restartSceneName))
            SceneManager.LoadScene(restartSceneName);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ── Rotation Helper ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a Quaternion rotation around the axis chosen in the Inspector.
    /// Change ringSpinAxis in the Inspector until the rings spin flat like a wheel.
    /// </summary>
    Quaternion AxisRotation(float degrees)
    {
        switch (ringSpinAxis)
        {
            case RotationAxis.X: return Quaternion.Euler(degrees, 0f, 0f);
            case RotationAxis.Y: return Quaternion.Euler(0f, degrees, 0f);
            case RotationAxis.Z: return Quaternion.Euler(0f, 0f, degrees);
            default:             return Quaternion.Euler(0f, 0f, degrees);
        }
    }
}