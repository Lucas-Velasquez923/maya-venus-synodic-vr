namespace MayaSynodic;

/// <summary>
/// The Calendar Round — the Tzolk'in (260 days) and the Haab' (365 days) running
/// in lockstep.
///
/// gcd(260, 365) = 5, so the pair repeats after lcm(260, 365) = 18,980 days
/// (52 Haab' years), not after 260 x 365. The shared factor of 5 is the same
/// factor that shows up in the Venus table: 5 synodic periods of 584 days is
/// 2,920 days, exactly 8 Haab' years, and 2,920 = 5 x 584 = 8 x 365 is the
/// commensurability the whole scene is built around.
/// </summary>
public static class CalendarRound
{
    /// <summary>Days in the Tzolk'in ritual round.</summary>
    public const int TzolkinDays = 260;
    /// <summary>Days in the Haab' vague year.</summary>
    public const int HaabDays = 365;

    /// <summary>Days until a (Tzolk'in, Haab') pair recurs: lcm(260, 365) = 18,980.</summary>
    public static int RoundDays => Lcm(TzolkinDays, HaabDays);

    /// <summary>The Calendar Round in Haab' years: 18,980 / 365 = 52.</summary>
    public static int RoundHaabYears => RoundDays / HaabDays;

    /// <summary>
    /// Days spanned by <paramref name="cycles"/> Venus cycles of the codex's
    /// integer 584-day period. Five of them is 2,920 days — eight Haab' years.
    /// </summary>
    public static int CodexVenusRunDays(int cycles) => cycles * OrbitalElements.DresdenCodexVenusDays;

    /// <summary>
    /// Accumulated error, in days, of the codex's integer 584-day Venus period
    /// against the true mean synodic period, after <paramref name="cycles"/> cycles.
    /// Positive means the codex runs ahead of the sky.
    /// </summary>
    public static double CodexDriftDays(int cycles)
        => cycles * (OrbitalElements.DresdenCodexVenusDays - SynodicPeriod.VenusDays);

    /// <summary>
    /// How many days the Haab' has fallen *behind* the seasons after
    /// <paramref name="years"/> Haab' years. The Haab' is 365 days with no leap
    /// day, so it runs short of the tropical year by about a quarter day annually
    /// and the error grows without bound — which is why the Haab' is a "vague"
    /// year and why it was never used alone to fix a date.
    /// </summary>
    public static double HaabDriftDays(int years)
        => years * (OrbitalElements.TropicalYearDays - OrbitalElements.HaabDays);

    /// <summary>Greatest common divisor, by the Euclidean algorithm.</summary>
    public static int Gcd(int a, int b)
    {
        a = Math.Abs(a);
        b = Math.Abs(b);
        while (b != 0) (a, b) = (b, a % b);
        return a;
    }

    /// <summary>Least common multiple. Divides before multiplying to avoid overflow.</summary>
    public static int Lcm(int a, int b) => a / Gcd(a, b) * b;
}
