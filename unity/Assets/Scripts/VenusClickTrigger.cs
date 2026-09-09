using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Attach this script to VENUS (the same GameObject the player should click).
///
/// When Venus is clicked/selected AFTER the first synodic cycle completes,
/// this calls SynodicCycleManager.StartFullCycle() to run the remaining
/// 4 cycles (full 8:5 Venus round — the Mesoamerican Venus cycle).
///
/// ─── SUPPORTS BOTH ─────────────────────────────────────────────────────────
///   • Desktop / Editor  : Unity OnMouseDown (requires a Collider on Venus)
///   • VR                : XR Simple Interactable (auto-wired via reflection,
///                         so this script compiles with or without XRI installed)
///
/// ─── SETUP IN UNITY ────────────────────────────────────────────────────────
///   1. Select the Venus GameObject
///   2. Add Component → VenusClickTrigger
///   3. Drag your SynodicCycleManager object into the "Manager" field
///   4. Make sure Venus has a Collider (SphereCollider is simplest)
///      - If you also want VR interaction, add an XR Simple Interactable
///        component. This script will auto-subscribe to its selectEntered event.
///   5. Press Play — after the first cycle completes, click Venus.
///
/// The click is IGNORED unless the manager is in the WaitingForInteraction phase,
/// so stray clicks during the fade-in or first cycle do nothing.
/// </summary>
[DisallowMultipleComponent]
public class VenusClickTrigger : MonoBehaviour
{
    [Tooltip("The SynodicCycleManager that will receive the click.")]
    public SynodicCycleManager manager;

    [Tooltip("Optional — fires the moment Venus is successfully clicked and the full Venus round begins.")]
    public UnityEvent onVenusClicked;

    [Header("Debug")]
    [Tooltip("Log every click attempt, including ones rejected because the phase is wrong.")]
    public bool verboseLogs = true;

    // Cached XR Simple Interactable reference (found via reflection at runtime).
    Component _xrInteractable;
    bool _xrWired;

    void Awake()
    {
        if (manager == null)
        {
            // Best effort — find one in the scene
            manager = FindObjectOfType<SynodicCycleManager>();
            if (manager == null)
                Debug.LogWarning("[VenusClickTrigger] No SynodicCycleManager assigned and none found in scene.");
        }

        EnsureCollider();
        TryWireXRInteractable();
    }

    void EnsureCollider()
    {
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"[VenusClickTrigger] '{name}' has no Collider — OnMouseDown will not fire. " +
                             "Add a Sphere/Box Collider to Venus so clicks register.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DESKTOP / EDITOR CLICK
    // ─────────────────────────────────────────────────────────────────────────
    void OnMouseDown()
    {
        if (verboseLogs) Debug.Log("[VenusClickTrigger] OnMouseDown on Venus.");
        TryTrigger("mouse click");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // VR / XR CLICK  — wired via reflection so we don't need to reference XRI
    // ─────────────────────────────────────────────────────────────────────────
    void TryWireXRInteractable()
    {
        if (_xrWired) return;

        // Look for any component whose type name is XRSimpleInteractable or XRBaseInteractable subclass.
        foreach (var c in GetComponents<MonoBehaviour>())
        {
            if (c == null) continue;
            var t = c.GetType();
            // Match the common XR Interaction Toolkit interactable classes
            if (t.Name == "XRSimpleInteractable" ||
                t.Name == "XRGrabInteractable" ||
                t.Name == "XRBaseInteractable")
            {
                _xrInteractable = c;
                break;
            }
        }

        if (_xrInteractable == null)
        {
            if (verboseLogs)
                Debug.Log("[VenusClickTrigger] No XR Simple Interactable found on Venus. " +
                          "That's fine for desktop — add one if you want VR controller input.");
            return;
        }

        // Grab the selectEntered event (UnityEvent<SelectEnterEventArgs>) and subscribe.
        // We use safe field/property lookups because newer XRI declares the same
        // name on both a class and an interface, which throws AmbiguousMatchException
        // for naive GetField/GetProperty calls.
        object evt = FindFieldValueSafe(_xrInteractable, "selectEntered")
                  ?? GetPropertyValueSafe(_xrInteractable, "selectEntered");

        if (evt == null)
        {
            Debug.LogWarning("[VenusClickTrigger] Found an XR interactable but couldn't locate 'selectEntered'. " +
                             "Fall back to wiring the event manually in the Inspector: drag this GameObject → choose VenusClickTrigger → OnVenusSelected().");
            return;
        }

        SubscribeToUnityEvent(evt);
    }

    static object GetPropertyValueSafe(object obj, string propertyName)
    {
        if (obj == null) return null;
        foreach (var p in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.Name != propertyName || !p.CanRead) continue;
            try
            {
                object v = p.GetValue(obj);
                if (v != null) return v;
            }
            catch { }
        }
        return null;
    }

    static object FindFieldValueSafe(object obj, string fieldName)
    {
        if (obj == null) return null;
        var type = obj.GetType();
        while (type != null)
        {
            foreach (var f in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (f.Name != fieldName) continue;
                try
                {
                    object v = f.GetValue(obj);
                    if (v != null) return v;
                }
                catch { }
            }
            type = type.BaseType;
        }
        return null;
    }

    void SubscribeToUnityEvent(object evt)
    {
        if (evt == null) return;

        // UnityEvent<T> has an AddListener(UnityAction<T>). We'll locate it via reflection.
        var addListener = evt.GetType().GetMethod("AddListener");
        if (addListener == null) return;

        var parameters = addListener.GetParameters();
        if (parameters.Length != 1) return;
        var actionType = parameters[0].ParameterType; // UnityAction<SelectEnterEventArgs>

        // Build a delegate that ignores its argument and calls our OnVenusSelected.
        var methodInfo = typeof(VenusClickTrigger).GetMethod(
            nameof(OnXREventIgnoreArg),
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (methodInfo == null) return;

        try
        {
            // actionType is generic UnityAction<T>. Create a delegate of that type from our 1-arg method.
            // Our method signature is (object ignored) — we need it to match T. Use DynamicInvoke fallback.
            // Simpler: create a UnityAction<object>-style using System.Delegate.CreateDelegate over a
            // synthetic helper that takes a matching arg. Since T is unknown, we use reflection Invoke.
            var genericArg = actionType.GetGenericArguments()[0];
            var boundMethod = typeof(VenusClickTrigger).GetMethod(
                nameof(OnXREventGeneric),
                BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(genericArg);

            var del = System.Delegate.CreateDelegate(actionType, this, boundMethod);
            addListener.Invoke(evt, new object[] { del });
            _xrWired = true;

            if (verboseLogs)
                Debug.Log($"[VenusClickTrigger] Subscribed to XR selectEntered on '{name}'.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[VenusClickTrigger] Could not auto-wire XR event: " + e.Message +
                             "\nWire it manually in the Inspector: selectEntered → VenusClickTrigger.OnVenusSelected().");
        }
    }

    // Used by the reflection-built delegate above. Must be instance + generic so the signature matches UnityAction<T>.
    void OnXREventGeneric<T>(T _)
    {
        if (verboseLogs) Debug.Log("[VenusClickTrigger] XR select entered on Venus.");
        TryTrigger("XR select");
    }

    // Alternate non-generic helper (kept for the reflection lookup; no-op fallback)
    void OnXREventIgnoreArg(object _) { TryTrigger("XR select (object)"); }

    // ─────────────────────────────────────────────────────────────────────────
    // PUBLIC — also callable directly from Inspector events or UI buttons
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Inspector-friendly method. Drag this GameObject into any UnityEvent
    /// (button OnClick, XR Simple Interactable Select Entered, etc.) and
    /// select this method.
    /// </summary>
    public void OnVenusSelected() => TryTrigger("manual / inspector");

    void TryTrigger(string source)
    {
        if (manager == null)
        {
            Debug.LogWarning("[VenusClickTrigger] No manager reference — click ignored.");
            return;
        }

        if (!manager.IsWaitingForInteraction)
        {
            if (verboseLogs)
                Debug.Log($"[VenusClickTrigger] Click from '{source}' ignored — phase is {manager.CurrentPhase}.");
            return;
        }

        Debug.Log($"[VenusClickTrigger] Venus clicked ({source}) — starting full Venus round.");
        manager.StartFullCycle();
        onVenusClicked?.Invoke();
    }
}
