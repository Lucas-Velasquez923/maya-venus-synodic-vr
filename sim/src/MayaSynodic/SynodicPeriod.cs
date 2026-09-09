namespace MayaSynodic;

/// <summary>
/// Synodic-period arithmetic for a two-body (inner planet / outer planet) system.
///
/// The synodic period S is the time between successive conjunctions as seen from
/// the outer body. Angular rates subtract:
///
///     2*pi/S = 2*pi/T_inner - 2*pi/T_outer
///     1/S    = 1/T_inner - 1/T_outer
///     S      = (T_inner * T_outer) / (T_outer - T_inner)
///
/// The VR simulation works in normalised units where T_outer = 1 Earth year, in
/// which case the expression collapses to S = 1 / (n - 1) with n = T_outer/T_inner
/// the speed ratio. That normalised form is what appears in the Unity code
/// (SynodicManager.Awake); <see cref="FromSpeedRatio"/> reproduces it exactly, and
/// <see cref="Days"/> is the general form used to verify it.
/// </summary>
public static class SynodicPeriod
{
    /// <summary>
    /// Synodic period of two circular coplanar orbits, in whatever time unit the
    /// inputs use. Requires an inner period strictly shorter than the outer one.
    /// </summary>
    public static double Days(double innerPeriodDays, double outerPeriodDays)
    {
        if (innerPeriodDays <= 0 || outerPeriodDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(innerPeriodDays),
                "Orbital periods must be positive.");
        if (innerPeriodDays >= outerPeriodDays)
            throw new ArgumentOutOfRangeException(nameof(innerPeriodDays),
                "Inner period must be shorter than the outer period; otherwise the " +
                "bodies never lap each other and the synodic period is undefined.");

        return innerPeriodDays * outerPeriodDays / (outerPeriodDays - innerPeriodDays);
    }

    /// <summary>
    /// The normalised form used by the VR scene: given the speed ratio
    /// n = T_outer / T_inner, the synodic period in units of T_outer is 1/(n-1).
    /// </summary>
    public static double FromSpeedRatio(double speedRatio)
    {
        if (speedRatio <= 1.0)
            throw new ArgumentOutOfRangeException(nameof(speedRatio),
                "Speed ratio must exceed 1 (the inner body must be faster).");

        return 1.0 / (speedRatio - 1.0);
    }

    /// <summary>Venus's mean synodic period in days, from JPL sidereal periods.</summary>
    public static double VenusDays =>
        Days(OrbitalElements.VenusSiderealDays, OrbitalElements.EarthSiderealDays);

    /// <summary>
    /// Angular separation (degrees, in [0, 360)) between the two bodies after
    /// <paramref name="days"/>, starting from conjunction. Zero at every multiple
    /// of the synodic period; this is the residual the resonance test measures.
    /// </summary>
    public static double SeparationDegrees(double innerPeriodDays, double outerPeriodDays, double days)
    {
        double innerDeg = 360.0 * days / innerPeriodDays;
        double outerDeg = 360.0 * days / outerPeriodDays;
        double sep = (innerDeg - outerDeg) % 360.0;
        return sep < 0 ? sep + 360.0 : sep;
    }
}
