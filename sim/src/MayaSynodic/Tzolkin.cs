namespace MayaSynodic;

/// <summary>A Tzolk'in date: a number 1-13 paired with one of 20 day signs.</summary>
public readonly record struct TzolkinDate(int Number, int SignIndex, string SignName)
{
    /// <summary>Renders the date in the usual form, e.g. "4 Ajaw".</summary>
    public override string ToString() => $"{Number} {SignName}";
}

/// <summary>
/// The 260-day Tzolk'in round.
///
/// Two independent counters advance together, one modulo 13 and one modulo 20.
/// Because gcd(13, 20) = 1, the pair (number, sign) does not repeat until
/// lcm(13, 20) = 260 days — the Chinese Remainder Theorem in its most concrete
/// form, and the reason the calendar rings in the VR scene are geared 13:20.
///
/// Ported unchanged (minus the unused UnityEngine dependency) from
/// unity/Assets/Scripts/BdayGetter.cs so the same code path can be unit-tested.
/// </summary>
public static class Tzolkin
{
    /// <summary>Length of the number cycle.</summary>
    public const int NumberCount = 13;
    /// <summary>Length of the day-sign cycle.</summary>
    public const int SignCount = 20;

    /// <summary>Total days before the (number, sign) pair repeats: lcm(13, 20).</summary>
    public const int RoundDays = 260;

    /// <summary>The 20 day signs, in cycle order.</summary>
    public static readonly IReadOnlyList<string> DaySigns = new[]
    {
        "Imix",   "Ik'",   "Ak'bal", "K'an",    "Chicchan",
        "Cimi",   "Manik'", "Lamat", "Muluk",   "Ok",
        "Chuwen", "Eb'",   "Ben",    "Ix",      "Men",
        "Kib'",   "Kab'an", "Etz'nab'", "Kawak", "Ajaw"
    };

    // Anchor: 21 Dec 2012 (Gregorian) = 4 Ajaw, the close of the 13th b'ak'tun
    // under the GMT correlation. Ajaw is sign index 19.
    private static readonly DateTime Anchor = new(2012, 12, 21);
    private const int AnchorNumber = 4;
    private const int AnchorSignIndex = 19;

    /// <summary>Tzolk'in date for a Gregorian calendar date.</summary>
    public static TzolkinDate FromGregorian(int year, int month, int day)
        => FromGregorian(new DateTime(year, month, day));

    /// <summary>Tzolk'in date for a Gregorian calendar date.</summary>
    public static TzolkinDate FromGregorian(DateTime date)
    {
        int elapsed = (int)(date.Date - Anchor).TotalDays;

        // Double-mod keeps negative day counts (dates before the anchor) in range;
        // C# % returns a negative remainder for negative operands.
        int number = Mod(AnchorNumber - 1 + elapsed, NumberCount) + 1;
        int signIndex = Mod(AnchorSignIndex + elapsed, SignCount);

        return new TzolkinDate(number, signIndex, DaySigns[signIndex]);
    }

    private static int Mod(int value, int modulus) => (value % modulus + modulus) % modulus;
}
