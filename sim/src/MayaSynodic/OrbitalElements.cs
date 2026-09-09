namespace MayaSynodic;

/// <summary>
/// Sidereal orbital periods, in days.
///
/// Source: NASA/JPL planetary fact sheets. These are mean values; the real
/// orbits are eccentric (Venus e = 0.0068, Earth e = 0.0167), so individual
/// synodic intervals vary by a few days about the mean computed here.
/// </summary>
public static class OrbitalElements
{
    /// <summary>Venus sidereal orbital period (days).</summary>
    public const double VenusSiderealDays = 224.701;

    /// <summary>Earth sidereal orbital period (days).</summary>
    public const double EarthSiderealDays = 365.256;

    /// <summary>Mean tropical year (days) — the year the Haab' approximates.</summary>
    public const double TropicalYearDays = 365.2422;

    /// <summary>Venus semi-major axis in astronomical units.</summary>
    public const double VenusSemiMajorAxisAu = 0.7233;

    /// <summary>Earth semi-major axis in astronomical units (by definition, ~1).</summary>
    public const double EarthSemiMajorAxisAu = 1.0;

    /// <summary>
    /// The Venus synodic period recorded in the Dresden Codex Venus table (days).
    /// An integer, unlike the true value — which is the source of the drift the
    /// codex's correction tables exist to absorb.
    /// </summary>
    public const int DresdenCodexVenusDays = 584;

    /// <summary>Days in a Haab' (Maya vague year).</summary>
    public const int HaabDays = 365;

    /// <summary>Days in a Tzolk'in (ritual round): 13 numbers x 20 day signs.</summary>
    public const int TzolkinDays = 260;
}
