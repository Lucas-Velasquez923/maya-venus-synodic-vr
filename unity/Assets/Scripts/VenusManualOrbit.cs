using System.Reflection;
using UnityEngine;

/// <summary>
/// Attach to the Venus GameObject. Once the SynodicCycleManager reaches
/// Phase.Complete (the full 8:5 Venus round has finished), the user can
/// grab/drag Venus along its orbital path.
///
/// While the user drags Venus, Earth rotates accordingly:
///     Δθ_Earth = Δθ_Venus × (8 / 13)
/// which preserves the Mesoamerican 13:8 Venus–Earth resonance (the ratio
/// tracked by the Dresden Codex Venus Table).
///
/// Works on:
///   • Desktop / Editor — via Unity's OnMouseDown/OnMouseDrag/OnMouseUp.
///                        The mouse ray is intersected with the orbital
///                        plane so Venus follows the cursor on its circle.
///   • VR — via XR Grab Interactable or XR Simple Interactable (reflection-
///          based, so this script compiles whether or not XRI is installed).
///          The interactor transform's position drives Venus.
///
/// ── SETUP ─────────────────────────────────────────────────────────────────
///   1. Select Venus, Add Component → VenusManualOrbit.
///   2. Drag your SynodicCycleManager into the "Manager" field.
///   3. Venus needs a Collider (Sphere Collider works fine — same one used
///      by VenusClickTrigger).
///   4. (VR) If you want controller grab, add an XRGrabInteractable or
///      XRSimpleInteractable. The script auto-subscribes via reflection.
///   5. Press Play. Complete the full Venus round. Then grab/drag Venus.
/// </summary>
[DisallowMultipleComponent]
public class VenusManualOrbit : MonoBehaviour
{
    [Tooltip("Reference to the SynodicCycleManager that runs the animation. Found automatically if left null.")]
    public SynodicCycleManager manager;

    [Header("Behavior")]
    [Tooltip("Require Phase.Complete before manual control is allowed. Turn off to allow grabbing at any phase (not recommended).")]
    public bool requireCompletePhase = true;

    [Header("Debug")]
    public bool verboseLogs = true;

    // Runtime state
    bool _dragging;
    Transform _xrInteractor;        // the VR interactor transform currently holding Venus (if any)
    Component _xrInteractable;      // cached XR interactable on this GameObject
    Plane _orbitPlane;              // world-space plane of the orbit; recomputed when drag starts
    Camera _cam;

    void Awake()
    {
        if (manager == null)
            manager = FindObjectOfType<SynodicCycleManager>();

        if (manager == null)
            Debug.LogWarning("[VenusManualOrbit] No SynodicCycleManager assigned/found.");

        if (GetComponent<Collider>() == null)
            Debug.LogWarning($"[VenusManualOrbit] '{name}' needs a Collider for drag input.");

        TryWireXRInteractable();
    }

    void OnEnable() { _cam = Camera.main; }

    // ═════════════════════════════════════════════════════════════════════════
    // ALLOWED?
    // ═════════════════════════════════════════════════════════════════════════
    bool ManualAllowed()
    {
        if (manager == null) return false;
        if (!requireCompletePhase) return true;
        return manager.IsComplete;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // DESKTOP / EDITOR MOUSE DRAG
    // ═════════════════════════════════════════════════════════════════════════
    void OnMouseDown()
    {
        if (!ManualAllowed()) { if (verboseLogs) Debug.Log("[VenusManualOrbit] Drag blocked — phase not Complete."); return; }
        BeginDrag();
    }

    void OnMouseDrag()
    {
        if (!_dragging || _xrInteractor != null) return;
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        if (_orbitPlane.Raycast(ray, out float enter))
        {
            Vector3 worldPoint = ray.GetPoint(enter);
            ApplyFromWorldPoint(worldPoint);
        }
    }

    void OnMouseUp() { if (_xrInteractor == null) EndDrag(); }

    // ═════════════════════════════════════════════════════════════════════════
    // VR CONTROLLER DRAG (XR Grab / Simple Interactable via reflection)
    // ═════════════════════════════════════════════════════════════════════════
    void TryWireXRInteractable()
    {
        foreach (var c in GetComponents<MonoBehaviour>())
        {
            if (c == null) continue;
            string n = c.GetType().Name;
            if (n == "XRGrabInteractable" || n == "XRSimpleInteractable" || n == "XRBaseInteractable")
            {
                _xrInteractable = c;

                // Warn about the common foot-gun: XRGrabInteractable will fight this script
                // because it auto-moves/reparents the grabbed object. VenusManualOrbit wants
                // to own the transform itself and just use the XR component for its events.
                if (n == "XRGrabInteractable")
                {
                    Debug.LogWarning(
                        $"[VenusManualOrbit] '{name}' has an XRGrabInteractable. " +
                        "This component will move Venus to the controller and override orbital placement. " +
                        "Replace it with XRSimpleInteractable for proper drag-along-orbit behavior. " +
                        "If you must keep XRGrabInteractable, disable 'Track Position', 'Track Rotation', " +
                        "and 'Throw On Detach' on the component, and set Movement Type = Instantaneous.");
                }
                break;
            }
        }

        if (_xrInteractable == null) return;

        // Subscribe selectEntered + selectExited
        WireEvent("selectEntered", nameof(OnXRGrabbedGeneric));
        WireEvent("selectExited",  nameof(OnXRReleasedGeneric));
    }

    void WireEvent(string eventName, string handlerName)
    {
        object evt = FindFieldValueSafe(_xrInteractable, eventName)
                  ?? GetPropertyValueSafe(_xrInteractable, eventName);

        if (evt == null) return;

        var addListener = evt.GetType().GetMethod("AddListener");
        if (addListener == null) return;

        var actionType = addListener.GetParameters()[0].ParameterType; // UnityAction<T>
        var tArg = actionType.GetGenericArguments()[0];

        var boundMethod = typeof(VenusManualOrbit)
            .GetMethod(handlerName, BindingFlags.NonPublic | BindingFlags.Instance)
            .MakeGenericMethod(tArg);

        try
        {
            var del = System.Delegate.CreateDelegate(actionType, this, boundMethod);
            addListener.Invoke(evt, new object[] { del });
            if (verboseLogs) Debug.Log($"[VenusManualOrbit] Wired XR {eventName} on '{name}'.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[VenusManualOrbit] Could not wire XR {eventName}: {e.Message}");
        }
    }

    // Generic handlers called via reflection — argument type is SelectEnterEventArgs / SelectExitEventArgs.
    void OnXRGrabbedGeneric<T>(T args)
    {
        if (!ManualAllowed()) { if (verboseLogs) Debug.Log("[VenusManualOrbit] XR grab ignored — phase not Complete."); return; }

        // Pull the interactor's transform out of the args via reflection.
        _xrInteractor = ExtractInteractorTransform(args);
        BeginDrag();
    }

    void OnXRReleasedGeneric<T>(T args)
    {
        EndDrag();
        _xrInteractor = null;
    }

    /// <summary>
    /// SelectEnterEventArgs exposes its interactor as a property called either
    /// 'interactorObject' (newer XRI) or 'interactor' (older XRI). Newer XRI
    /// declares 'interactorObject' on both a base class and an interface, which
    /// makes GetProperty() throw AmbiguousMatchException — so we iterate all
    /// properties and return the first non-null match.
    /// </summary>
    static Transform ExtractInteractorTransform(object args)
    {
        if (args == null) return null;

        object interactor = GetPropertyValueSafe(args, "interactorObject")
                         ?? GetPropertyValueSafe(args, "interactor");

        if (interactor == null) return null;

        // Interactors expose .transform either directly (Component) or via a property.
        if (interactor is Component comp) return comp.transform;

        object tr = GetPropertyValueSafe(interactor, "transform");
        return tr as Transform;
    }

    /// <summary>
    /// Returns the value of the first public instance property whose Name matches
    /// `propertyName` and whose getter returns a non-null value. Handles the
    /// AmbiguousMatchException that Type.GetProperty(string, BindingFlags) throws
    /// when the same name appears on multiple declaring types (e.g. a class plus
    /// an interface it implements explicitly).
    /// </summary>
    static object GetPropertyValueSafe(object obj, string propertyName)
    {
        if (obj == null) return null;
        var type = obj.GetType();

        foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (p.Name != propertyName) continue;
            if (!p.CanRead) continue;
            try
            {
                object v = p.GetValue(obj);
                if (v != null) return v;
            }
            catch { /* skip and keep looking */ }
        }
        return null;
    }

    /// <summary>Safe field reader that walks up the type hierarchy and skips null values.</summary>
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
                catch { /* skip and keep looking */ }
            }
            type = type.BaseType;
        }
        return null;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // CORE DRAG LIFECYCLE
    // ═════════════════════════════════════════════════════════════════════════
    void BeginDrag()
    {
        if (manager == null) return;
        _dragging = true;

        // Build the world-space orbital plane (normal = parent's local Y in world space).
        Transform parent = manager.OrbitParent;
        Vector3 planeNormal = (parent != null) ? parent.up : Vector3.up;
        Vector3 planePoint;
        if (parent != null)
            planePoint = parent.TransformPoint(manager.SunLocalPos);
        else
            planePoint = (manager.sunRoot != null) ? manager.sunRoot.transform.position : Vector3.zero;

        _orbitPlane = new Plane(planeNormal, planePoint);

        if (verboseLogs) Debug.Log("[VenusManualOrbit] Drag started.");
    }

    void EndDrag()
    {
        if (_dragging && verboseLogs) Debug.Log("[VenusManualOrbit] Drag ended.");
        _dragging = false;
    }

    void Update()
    {
        // VR path: each frame while held, use the interactor's world position.
        if (_dragging && _xrInteractor != null)
            ApplyFromWorldPoint(_xrInteractor.position);
    }

    /// <summary>
    /// Given a world-space point (from mouse ray or VR controller), compute the
    /// Venus orbital angle in the SolarSystem parent's local XZ plane, then ask
    /// the manager to drive Venus there. Earth follows at 8/13 ratio.
    /// </summary>
    void ApplyFromWorldPoint(Vector3 worldPoint)
    {
        if (manager == null) return;

        Transform parent = manager.OrbitParent;
        Vector3 local = (parent != null) ? parent.InverseTransformPoint(worldPoint) : worldPoint;

        // Offset from the Sun in local space, then angle in the XZ plane.
        Vector3 fromSun = local - manager.SunLocalPos;
        float angleDeg = Mathf.Atan2(fromSun.z, fromSun.x) * Mathf.Rad2Deg;

        manager.DriveVenusToAngle(angleDeg);
    }
}
