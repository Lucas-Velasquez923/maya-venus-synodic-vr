using MayaSynodic;
using Xunit;

namespace MayaSynodic.Tests;

/// <summary>
/// The claim the whole project rests on: five Venus synodic periods, eight Earth
/// years and thirteen Venus years are the same span of time to within a couple of
/// days. These tests put a number on "to within".
/// </summary>
public class ResonanceTests
{
    private const double TVenus = OrbitalElements.VenusSiderealDays;
    private const double TEarth = OrbitalElements.EarthSiderealDays;

    [Fact]
    public void FiveSynodicPeriods_CloseToEightEarthYears()
    {
        double fiveSynodic = 5 * SynodicPeriod.VenusDays;
        double eightEarth = 8 * TEarth;

        // ~2.5 days out of ~2920 — better than one part in a thousand.
        Assert.InRange(Math.Abs(eightEarth - fiveSynodic), 0.0, 3.0);
        Assert.InRange(Math.Abs(eightEarth - fiveSynodic) / eightEarth, 0.0, 1e-3);
    }

    [Fact]
    public void FiveSynodicPeriods_CloseToThirteenVenusYears()
    {
        double fiveSynodic = 5 * SynodicPeriod.VenusDays;
        double thirteenVenus = 13 * TVenus;

        Assert.InRange(Math.Abs(thirteenVenus - fiveSynodic), 0.0, 3.0);
    }

    [Fact]
    public void ThirteenOverEight_IsAConvergentOfTheSpeedRatio()
    {
        double ratio = TEarth / TVenus;
        var convergents = RationalApproximation.Convergents(ratio, maxTerms: 8);

        Assert.Contains(convergents, c => c.Numerator == 13 && c.Denominator == 8);
    }

    // The interesting structural fact: after 13/8 the continued fraction jumps.
    // The next convergent buys ~3 more digits at ~30x the denominator, which is
    // exactly why rounding to eighths is defensible and rounding to, say,
    // sixteenths would be arbitrary.
    [Fact]
    public void NextConvergentAfterThirteenEighths_CostsAMuchLargerDenominator()
    {
        double ratio = TEarth / TVenus;
        var convergents = RationalApproximation.Convergents(ratio, maxTerms: 8);

        int idx = convergents.ToList().FindIndex(c => c.Denominator == 8);
        Assert.True(idx >= 0 && idx + 1 < convergents.Count,
            "Expected a convergent after 13/8 within the computed terms.");

        var next = convergents[idx + 1];
        Assert.True(next.Denominator > 8 * 20,
            $"Expected the next denominator to dwarf 8; got {next.Denominator}.");
    }

    [Fact]
    public void QuantisingSpeedRatioToEighths_YieldsExactlyThirteenEighths()
    {
        // This is the line from the Unity source: n = round(8 * T_e/T_v) / 8.
        double quantised = RationalApproximation.QuantiseTo(TEarth / TVenus, 8);

        Assert.Equal(13.0 / 8.0, quantised, precision: 12);
    }

    [Fact]
    public void QuantisingToEighths_ClosesTheLoopExactly()
    {
        // With the quantised ratio the simulation's synodic period is exactly
        // 8/5 years, so five of them is exactly eight years and the animation
        // returns to its start with no accumulated drift. That is the whole point
        // of the quantisation.
        double n = RationalApproximation.QuantiseTo(TEarth / TVenus, 8);
        double synodicYears = SynodicPeriod.FromSpeedRatio(n);

        Assert.Equal(8.0 / 5.0, synodicYears, precision: 12);
        Assert.Equal(8.0, 5 * synodicYears, precision: 12);
    }

    // I expected the eighths quantisation to beat the Maya's integer 584 days on
    // accuracy. It does not, and it is not close: 0.486 d/cycle against 0.076.
    // The two are optimising different things. The codex is fitting the sky, so
    // it minimises error against the true synodic period. The simulation is
    // fitting the *animation loop*, so it minimises drift over five cycles and
    // accepts a worse per-cycle period to get exact closure. Worth stating
    // plainly rather than quietly asserting the flattering version.
    [Fact]
    public void CodexIntegerPeriod_BeatsTheEighthsQuantisation_OnRawAccuracy()
    {
        double trueSynodicDays = SynodicPeriod.VenusDays;

        double n = RationalApproximation.QuantiseTo(TEarth / TVenus, 8);
        double quantisedSynodicDays = SynodicPeriod.FromSpeedRatio(n) * TEarth;

        double quantisationError = Math.Abs(quantisedSynodicDays - trueSynodicDays);
        double codexError = Math.Abs(OrbitalElements.DresdenCodexVenusDays - trueSynodicDays);

        Assert.InRange(codexError, 0.07, 0.08);
        Assert.InRange(quantisationError, 0.48, 0.49);
        Assert.True(codexError < quantisationError,
            "The codex's 584 days is the better fit to the sky; the quantisation " +
            "buys exact loop closure instead.");
    }

    // What the quantisation actually buys, stated as its own claim: the true
    // ratio leaves 2.4 days of drift across five cycles, the quantised one leaves
    // none at all.
    [Fact]
    public void QuantisationBuysExactClosure_WhichTheTrueRatioDoesNotHave()
    {
        double trueDriftDays = Math.Abs(8 * TEarth - 5 * SynodicPeriod.VenusDays);
        Assert.InRange(trueDriftDays, 2.0, 3.0);

        double n = RationalApproximation.QuantiseTo(TEarth / TVenus, 8);
        double quantisedDriftYears = Math.Abs(8.0 - 5 * SynodicPeriod.FromSpeedRatio(n));
        Assert.Equal(0.0, quantisedDriftYears, precision: 12);
    }

    // The pentagram, precisely. Between one conjunction and the next, Earth
    // advances 215.5 deg — about three fifths of a turn, not one fifth. Stepping
    // 3/5 of the way round a circle five times is exactly the straight-edge
    // construction of a five-pointed star, which is why the figure Venus traces
    // is a pentagram and not a pentagon.
    [Fact]
    public void ConsecutiveConjunctions_AdvanceThreeFifthsOfATurn()
    {
        double advancePerCycle = (360.0 * SynodicPeriod.VenusDays / TEarth) % 360.0;

        Assert.InRange(advancePerCycle, 215.0, 216.0);
        Assert.InRange(advancePerCycle / 360.0, 0.59, 0.61);   // ~ 3/5
    }

    // Taken as an unordered set, the five conjunction points do sit close to
    // 72 deg apart — but only close. Three gaps are 71.04 and two are 73.44, and that
    // asymmetry is the same 2.4-day residual seen from a different angle: the
    // pentagram precesses instead of closing on itself.
    [Fact]
    public void FiveConjunctionPoints_AreNearlyButNotExactlyEvenlySpaced()
    {
        double advance = (360.0 * SynodicPeriod.VenusDays / TEarth) % 360.0;

        var longitudes = Enumerable.Range(0, 5)
            .Select(i => (i * advance) % 360.0)
            .OrderBy(x => x)
            .ToList();

        var gaps = Enumerable.Range(0, 5)
            .Select(i => i < 4 ? longitudes[i + 1] - longitudes[i]
                               : longitudes[0] + 360.0 - longitudes[4])
            .ToList();

        Assert.All(gaps, g => Assert.InRange(g, 70.0, 74.0));
        Assert.Equal(360.0, gaps.Sum(), precision: 9);

        // Not a regular pentagon: the spread between largest and smallest gap is
        // over two degrees.
        Assert.InRange(gaps.Max() - gaps.Min(), 2.0, 3.0);
    }
}
