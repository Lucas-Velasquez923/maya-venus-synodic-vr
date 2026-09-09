namespace MayaSynodic;

/// <summary>A rational p/q together with how well it approximates a target.</summary>
public readonly record struct Convergent(long Numerator, long Denominator, double Value, double Error)
{
    /// <summary>Renders the convergent as "p/q".</summary>
    public override string ToString() => $"{Numerator}/{Denominator}";
}

/// <summary>
/// Continued-fraction machinery for finding the best rational approximations to a
/// real number.
///
/// Why this is here: the Unity scene quantises the Venus/Earth speed ratio to the
/// nearest eighth —
///
///     n = round(8 * T_earth / T_venus) / 8
///
/// — which is what makes the 5:8 resonance land *exactly* on a closed loop in the
/// simulation instead of drifting. That is a design decision, and it deserves to
/// be justified rather than asserted. The continued-fraction expansion of the true
/// ratio shows that 13/8 is a convergent, and that the next convergent has a
/// denominator two orders of magnitude larger. Eighths are not an arbitrary grid:
/// they are the last cheap place to round before the approximation gets expensive.
/// </summary>
public static class RationalApproximation
{
    /// <summary>
    /// Continued-fraction terms [a0; a1, a2, ...] of <paramref name="x"/>.
    /// Stops after <paramref name="maxTerms"/> or once the remainder is below
    /// <paramref name="tolerance"/> (at which point further terms are just noise
    /// in the input measurement).
    /// </summary>
    public static IReadOnlyList<long> ContinuedFraction(double x, int maxTerms = 12, double tolerance = 1e-12)
    {
        var terms = new List<long>();
        double remainder = x;

        for (int i = 0; i < maxTerms; i++)
        {
            // Naively taking floor(remainder) breaks on rationals. Expanding 13/8
            // that way lands on a remainder of 1.9999999999999964 at the last step,
            // floors it to 1, and emits [1;1,1,1,1,1,...] instead of [1;1,1,1,2] —
            // the expansion runs away on accumulated rounding rather than
            // terminating. So: if the remainder is within tolerance of an integer,
            // treat it as that integer and stop. Tolerance is relative, because
            // remainders grow without bound as the expansion proceeds.
            double nearest = Math.Round(remainder);
            if (Math.Abs(remainder - nearest) <= tolerance * Math.Max(1.0, Math.Abs(remainder)))
            {
                terms.Add((long)nearest);
                break;
            }

            long a = (long)Math.Floor(remainder);
            terms.Add(a);
            remainder = 1.0 / (remainder - a);
        }

        return terms;
    }

    /// <summary>
    /// The convergents of <paramref name="x"/>, in increasing denominator order.
    /// Each is the best rational approximation to x among all rationals with a
    /// denominator no larger than its own (Dirichlet / best-approximation theorem).
    /// </summary>
    public static IReadOnlyList<Convergent> Convergents(double x, int maxTerms = 12)
    {
        var terms = ContinuedFraction(x, maxTerms);
        var result = new List<Convergent>(terms.Count);

        // Standard recurrence: h_i = a_i*h_{i-1} + h_{i-2}, k_i likewise.
        long hPrev = 1, hPrevPrev = 0;
        long kPrev = 0, kPrevPrev = 1;

        foreach (long a in terms)
        {
            long h = a * hPrev + hPrevPrev;
            long k = a * kPrev + kPrevPrev;

            hPrevPrev = hPrev; hPrev = h;
            kPrevPrev = kPrev; kPrev = k;

            double value = (double)h / k;
            result.Add(new Convergent(h, k, value, value - x));
        }

        return result;
    }

    /// <summary>
    /// Rounds <paramref name="x"/> to the nearest multiple of 1/<paramref name="denominator"/>.
    /// This is the quantisation the Unity scene applies with denominator = 8.
    /// </summary>
    public static double QuantiseTo(double x, int denominator)
    {
        if (denominator <= 0)
            throw new ArgumentOutOfRangeException(nameof(denominator));

        return Math.Round(x * denominator) / denominator;
    }
}
