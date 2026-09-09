using MayaSynodic;
using Xunit;

namespace MayaSynodic.Tests;

public class SynodicPeriodTests
{
    // JPL publishes Venus's mean synodic period as 583.92 days. Deriving it from
    // the two sidereal periods should reproduce that to within a hundredth of a day.
    [Fact]
    public void VenusSynodicPeriod_MatchesPublishedValue()
    {
        Assert.Equal(583.92, SynodicPeriod.VenusDays, precision: 1);
        Assert.InRange(SynodicPeriod.VenusDays, 583.91, 583.93);
    }

    // The Unity scene computes the synodic period in normalised units as
    // 1/(n-1). That shortcut is only valid when the outer period is exactly 1,
    // which is the case there. Confirm the two forms agree under that assumption
    // and document where the shortcut breaks.
    [Fact]
    public void NormalisedForm_AgreesWithGeneralForm_WhenOuterPeriodIsOne()
    {
        double innerYears = OrbitalElements.VenusSiderealDays / OrbitalElements.EarthSiderealDays;
        double general = SynodicPeriod.Days(innerYears, 1.0);
        double normalised = SynodicPeriod.FromSpeedRatio(1.0 / innerYears);

        Assert.Equal(general, normalised, precision: 10);
    }

    [Fact]
    public void NormalisedForm_DivergesFromGeneralForm_WhenOuterPeriodIsNotOne()
    {
        // Same physical system expressed in days rather than years: the speed
        // ratio is unchanged, but 1/(n-1) now returns years, not days.
        double ratio = OrbitalElements.EarthSiderealDays / OrbitalElements.VenusSiderealDays;
        double normalised = SynodicPeriod.FromSpeedRatio(ratio);
        double general = SynodicPeriod.Days(OrbitalElements.VenusSiderealDays,
                                            OrbitalElements.EarthSiderealDays);

        Assert.NotEqual(general, normalised, precision: 3);
        Assert.Equal(general, normalised * OrbitalElements.EarthSiderealDays, precision: 6);
    }

    [Fact]
    public void SynodicPeriod_IsSymmetricUnderUnitChange()
    {
        double inDays = SynodicPeriod.Days(OrbitalElements.VenusSiderealDays,
                                           OrbitalElements.EarthSiderealDays);
        double inYears = SynodicPeriod.Days(
            OrbitalElements.VenusSiderealDays / OrbitalElements.EarthSiderealDays, 1.0);

        Assert.Equal(inDays / OrbitalElements.EarthSiderealDays, inYears, precision: 9);
    }

    [Theory]
    [InlineData(0, 1)]        // inner period not positive
    [InlineData(1, 1)]        // inner not strictly faster
    [InlineData(2, 1)]        // inner slower than outer
    public void InvalidPeriods_Throw(double inner, double outer)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SynodicPeriod.Days(inner, outer));
    }

    [Fact]
    public void SpeedRatioAtOrBelowOne_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SynodicPeriod.FromSpeedRatio(1.0));
        Assert.Throws<ArgumentOutOfRangeException>(() => SynodicPeriod.FromSpeedRatio(0.5));
    }

    // Separation returns to zero at every whole multiple of the synodic period —
    // this is the definition of conjunction, and the invariant the VR animation
    // has to preserve for the beam between the planets to line up.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(13)]
    public void SeparationReturnsToZero_AtEveryWholeSynodicPeriod(int n)
    {
        double days = n * SynodicPeriod.VenusDays;
        double sep = SynodicPeriod.SeparationDegrees(
            OrbitalElements.VenusSiderealDays, OrbitalElements.EarthSiderealDays, days);

        // Wrap: a separation just under 360 is the same as one just over 0.
        double fromZero = Math.Min(sep, 360.0 - sep);
        Assert.True(fromZero < 1e-9, $"Expected conjunction after {n} synodic periods, got {sep} deg.");
    }
}
