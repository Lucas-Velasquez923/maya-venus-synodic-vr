using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Attach to the Sun. When clicked after the full cycle completes,
/// tells SynodicManager to load the next scene.
///
/// SETUP:
///   1. Add a SphereCollider to Sun (set radius to match the mesh)
///   2. Add XRSimpleInteractable to Sun
///   3. Add this script, drag SynodicManager into the "manager" field
/// </summary>
[RequireComponent(typeof(Collider))]
public class SunClickHandler : MonoBehaviour
{
    public SynodicManager manager;

    void Awake()
    {
        if (!manager) manager = FindObjectOfType<SynodicManager>();

        var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnSelect);
    }

    void OnSelect(SelectEnterEventArgs args)
    {
        if (manager) manager.OnSunClicked();
    }
}
