using MayaSynodic;
using Xunit;

namespace MayaSynodic.Tests;

public class TzolkinTests
{
    [Fact]
    public void AnchorDate_IsFourAjaw()
    {
        var d = Tzolkin.FromGregorian(2012, 12, 21);

        Assert.Equal(4, d.Number);
        Assert.Equal("Ajaw", d.SignName);
        Assert.Equal("4 Ajaw", d.ToString());
    }

    [Fact]
    public void Round_Repeats_AfterExactly260Days()
    {
        var start = new DateTime(2012, 12, 21);
        var origin = Tzolkin.FromGregorian(start);

        for (int offset = 1; offset < 260; offset++)
            Assert.NotEqual(origin, Tzolkin.FromGregorian(start.AddDays(offset)));

        Assert.Equal(origin, Tzolkin.FromGregorian(start.AddDays(260)));
    }

    // The failure this guards against is real: C# `%` returns negative remainders,
    // so a naive implementation indexes out of the day-sign array for any date
    // before the 2012 anchor — which is every user's birthday.
    [Fact]
    public void DatesBeforeTheAnchor_StayInRange()
    {
        var d = new DateTime(2012, 12, 21);

        for (int back = 1; back <= 800; back++)
        {
            var t = Tzolkin.FromGregorian(d.AddDays(-back));
            Assert.InRange(t.Number, 1, 13);
            Assert.InRange(t.SignIndex, 0, 19);
            Assert.Equal(Tzolkin.DaySigns[t.SignIndex], t.SignName);
        }
    }

    [Fact]
    public void EveryPairOccursExactlyOnce_PerRound()
    {
        var seen = new HashSet<(int, int)>();
        var start = new DateTime(2012, 12, 21);

        for (int i = 0; i < 260; i++)
        {
            var t = Tzolkin.FromGregorian(start.AddDays(i));
            Assert.True(seen.Add((t.Number, t.SignIndex)),
                $"Pair {t} repeated after {i} days; the round is not 260 long.");
        }

        Assert.Equal(260, seen.Count);
    }

    [Fact]
    public void ThereAreTwentyDaySigns()
    {
        Assert.Equal(20, Tzolkin.DaySigns.Count);
        Assert.Equal(20, Tzolkin.DaySigns.Distinct().Count());
    }
}

public class CalendarRoundTests
{
    [Fact]
    public void TzolkinRound_IsLcmOfThirteenAndTwenty()
    {
        Assert.Equal(1, CalendarRound.Gcd(13, 20));
        Assert.Equal(260, CalendarRound.Lcm(13, 20));
        Assert.Equal(260, Tzolkin.RoundDays);
    }

    [Fact]
    public void CalendarRound_Is18980Days_Or52HaabYears()
    {
        Assert.Equal(5, CalendarRound.Gcd(260, 365));
        Assert.Equal(18_980, CalendarRound.RoundDays);
        Assert.Equal(52, CalendarRound.RoundHaabYears);
    }

    // The commensurability the Dresden Codex is built on, in integers:
    // 5 x 584 = 2920 = 8 x 365, with no remainder on either side.
    [Fact]
    public void FiveCodexVenusCycles_AreExactlyEightHaabYears()
    {
        Assert.Equal(2920, CalendarRound.CodexVenusRunDays(5));
        Assert.Equal(0, CalendarRound.CodexVenusRunDays(5) % CalendarRound.HaabDays);
        Assert.Equal(8, CalendarRound.CodexVenusRunDays(5) / CalendarRound.HaabDays);
    }

    [Fact]
    public void CodexDrift_IsUnderHalfADay_PerEightYearRound()
    {
        Assert.InRange(CalendarRound.CodexDriftDays(5), 0.0, 0.5);
    }

    // 0.0764 d/cycle puts the one-day mark just past the 13th cycle — 14 cycles,
    // about 22 years. Pinning the crossing on both sides rather than asserting a
    // round number keeps the test honest about where it actually falls.
    [Fact]
    public void CodexDrift_CrossesOneDay_BetweenThirteenAndFourteenCycles()
    {
        Assert.True(CalendarRound.CodexDriftDays(13) < 1.0);
        Assert.True(CalendarRound.CodexDriftDays(14) > 1.0);
        Assert.InRange(CalendarRound.CodexDriftDays(1), 0.07, 0.08);
    }

    // 52 years with no leap day: 52 x 0.2422 ~ 12.6 days behind the seasons.
    // Sign matters here — the Haab' runs short of the tropical year, so it falls
    // behind rather than ahead.
    [Fact]
    public void HaabFallsBehindTheSeasons_ByAboutThirteenDays_PerCalendarRound()
    {
        Assert.InRange(CalendarRound.HaabDriftDays(52), 12.0, 13.0);
        Assert.True(CalendarRound.HaabDriftDays(52) > 0);
    }

    [Theory]
    [InlineData(12, 18, 6)]
    [InlineData(13, 20, 1)]
    [InlineData(260, 365, 5)]
    [InlineData(-12, 18, 6)]
    public void Gcd_IsCorrect(int a, int b, int expected)
        => Assert.Equal(expected, CalendarRound.Gcd(a, b));
}

public class RationalApproximationTests
{
    [Fact]
    public void ContinuedFractionOfARational_Terminates()
    {
        var terms = RationalApproximation.ContinuedFraction(13.0 / 8.0);

        // 13/8 = [1; 1, 1, 1, 2]
        Assert.Equal(new long[] { 1, 1, 1, 1, 2 }, terms);
    }

    [Fact]
    public void ConvergentsOfPi_HitTwentyTwoSevenths_AndThreeFiveFiveOverOneThirteen()
    {
        var convergents = RationalApproximation.Convergents(Math.PI, maxTerms: 6);

        Assert.Contains(convergents, c => c.Numerator == 22 && c.Denominator == 7);
        Assert.Contains(convergents, c => c.Numerator == 355 && c.Denominator == 113);
    }

    [Fact]
    public void ConvergentErrors_ShrinkMonotonically()
    {
        var convergents = RationalApproximation.Convergents(
            OrbitalElements.EarthSiderealDays / OrbitalElements.VenusSiderealDays, maxTerms: 8);

        for (int i = 1; i < convergents.Count; i++)
            Assert.True(Math.Abs(convergents[i].Error) < Math.Abs(convergents[i - 1].Error),
                $"Convergent {convergents[i]} is no better than {convergents[i - 1]}.");
    }

    [Theory]
    [InlineData(1.6255, 8, 1.625)]
    [InlineData(1.6255, 2, 1.5)]
    [InlineData(1.6255, 1, 2.0)]
    public void QuantiseTo_RoundsToTheNearestFraction(double x, int denominator, double expected)
        => Assert.Equal(expected, RationalApproximation.QuantiseTo(x, denominator), precision: 12);

    [Fact]
    public void QuantiseTo_RejectsNonPositiveDenominators()
        => Assert.Throws<ArgumentOutOfRangeException>(() => RationalApproximation.QuantiseTo(1.5, 0));
}
