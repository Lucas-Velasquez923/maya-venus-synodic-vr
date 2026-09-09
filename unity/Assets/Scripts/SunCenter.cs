using UnityEngine;

/// <summary>
/// Attach to: Sun Sphere
///
/// Marks the Sun as the fixed orbital centre of the solar system.
/// PlanetOrbit scripts on Venus and Earth reference this transform.
///
/// The Sun does not move — this script has no Update logic.
/// </summary>
public class SunCenter : MonoBehaviour
{
    // Intentionally empty.
    // Drag this GameObject into the PlanetOrbit "sunCenter" field on Venus and Earth.
}