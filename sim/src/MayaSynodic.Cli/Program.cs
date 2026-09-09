using MayaSynodic;

// Prints the derivation the VR scene animates. Every number below is computed
// from the two JPL sidereal periods in OrbitalElements; nothing is hard-coded.

const string Rule = "──────────────────────────────────────────────────────────────";

double tVenus = OrbitalElements.VenusSiderealDays;
double tEarth = OrbitalElements.EarthSiderealDays;

Console.WriteLine();
Console.WriteLine("  THE 5:8 VENUS-EARTH RESONANCE");
Console.WriteLine(Rule);
Console.WriteLine($"  Venus sidereal period   T_v = {tVenus,10:F3} days");
Console.WriteLine($"  Earth sidereal period   T_e = {tEarth,10:F3} days");
Console.WriteLine();

// ── 1. Synodic period ────────────────────────────────────────────────────────
double synodic = SynodicPeriod.Days(tVenus, tEarth);
double ratio = tEarth / tVenus;

Console.WriteLine("  1. SYNODIC PERIOD      S = T_v*T_e / (T_e - T_v)");
Console.WriteLine(Rule);
Console.WriteLine($"     S = {synodic:F3} days = {synodic / tEarth:F4} Earth years");
Console.WriteLine($"     Dresden Codex Venus table uses {OrbitalElements.DresdenCodexVenusDays} days " +
                  $"(off by {OrbitalElements.DresdenCodexVenusDays - synodic:+0.000;-0.000} d/cycle)");
Console.WriteLine();

// ── 2. Why eighths ───────────────────────────────────────────────────────────
Console.WriteLine("  2. WHY EIGHTHS         continued fraction of T_e / T_v");
Console.WriteLine(Rule);
var cf = RationalApproximation.ContinuedFraction(ratio, maxTerms: 8);
Console.WriteLine($"     T_e / T_v = {ratio:F9}");
Console.WriteLine($"     [{cf[0]}; {string.Join(", ", cf.Skip(1))}]");
Console.WriteLine();
Console.WriteLine($"     {"convergent",-12}{"value",14}{"error",14}{"rel. error",14}");

foreach (var c in RationalApproximation.Convergents(ratio, maxTerms: 8))
{
    string mark = c.Denominator == 8 ? "  <-- the scene rounds here" : "";
    Console.WriteLine($"     {c.ToString(),-12}{c.Value,14:F9}{c.Error,14:+0.000000;-0.000000}" +
                      $"{Math.Abs(c.Error) / ratio,14:E2}{mark}");
}

Console.WriteLine();
Console.WriteLine("     13/8 is a convergent; the next one has a denominator ~30x larger.");
Console.WriteLine("     Eighths are the last cheap grid before accuracy gets expensive.");
Console.WriteLine();

// ── 3. The closure ───────────────────────────────────────────────────────────
Console.WriteLine("  3. THE CLOSURE         5 synodic periods ~ 8 Earth years ~ 13 Venus years");
Console.WriteLine(Rule);
double fiveSynodic = 5 * synodic;
double eightEarth = 8 * tEarth;
double thirteenVenus = 13 * tVenus;

Console.WriteLine($"      5 x S   = {fiveSynodic,10:F2} days");
Console.WriteLine($"      8 x T_e = {eightEarth,10:F2} days   (delta {eightEarth - fiveSynodic,+6:F2} d)");
Console.WriteLine($"     13 x T_v = {thirteenVenus,10:F2} days   (delta {thirteenVenus - fiveSynodic,+6:F2} d)");
Console.WriteLine();
Console.WriteLine($"     Venus-Earth separation after 8 Earth years: " +
                  $"{SynodicPeriod.SeparationDegrees(tVenus, tEarth, eightEarth):F3} deg");
Console.WriteLine("     (After 5 synodic periods it is 0 by definition; 8 Earth years is the");
Console.WriteLine("      independent check, and it misses conjunction by that much.)");
Console.WriteLine();

// ── 3b. The pentagram ────────────────────────────────────────────────────────
double advance = (360.0 * synodic / tEarth) % 360.0;
Console.WriteLine($"     Earth advances {advance:F2} deg between conjunctions = {advance / 360.0:F4} turn (~3/5).");
Console.WriteLine("     Stepping 3/5 of a circle five times is the construction of a");
Console.WriteLine("     five-pointed star — hence the Venus pentagram, not a pentagon.");
Console.WriteLine();

var longitudes = Enumerable.Range(0, 5).Select(i => (i * advance) % 360.0).OrderBy(v => v).ToList();
var gaps = Enumerable.Range(0, 5)
    .Select(i => i < 4 ? longitudes[i + 1] - longitudes[i] : longitudes[0] + 360.0 - longitudes[4])
    .ToList();
Console.WriteLine($"     Gaps between the five points: {string.Join(", ", gaps.Select(g => $"{g:F2}"))} deg");
Console.WriteLine($"     Not a regular pentagon — spread of {gaps.Max() - gaps.Min():F2} deg. The star");
Console.WriteLine("     precesses instead of closing; that is the 2.43-day residual again.");
Console.WriteLine();

// ── 3c. What the quantisation costs and buys ─────────────────────────────────
double nQ = RationalApproximation.QuantiseTo(ratio, 8);
double synodicQ = SynodicPeriod.FromSpeedRatio(nQ) * tEarth;
Console.WriteLine("     TRADE-OFF of rounding the speed ratio to 13/8:");
Console.WriteLine($"       period error vs sky   {Math.Abs(synodicQ - synodic),8:F3} d/cycle " +
                  $"(codex's 584 d: {Math.Abs(OrbitalElements.DresdenCodexVenusDays - synodic):F3})");
Console.WriteLine($"       drift over 5 cycles   {Math.Abs(8.0 - 5 * SynodicPeriod.FromSpeedRatio(nQ)),8:F3} yr " +
                  $"(true ratio: {Math.Abs(8 * tEarth - fiveSynodic) / tEarth:F3})");
Console.WriteLine("     The codex is the better fit to the sky. The quantisation is the better");
Console.WriteLine("     fit to a loop that has to return to its start on screen.");
Console.WriteLine();

// ── 4. Calendar arithmetic ───────────────────────────────────────────────────
Console.WriteLine("  4. CALENDAR ARITHMETIC");
Console.WriteLine(Rule);
Console.WriteLine($"     Tzolk'in round   lcm(13, 20)  = {Tzolkin.RoundDays,6} days");
Console.WriteLine($"     Calendar Round   lcm(260, 365) = {CalendarRound.RoundDays,6} days " +
                  $"= {CalendarRound.RoundHaabYears} Haab' years");
Console.WriteLine($"     Codex Venus run  5 x 584       = {CalendarRound.CodexVenusRunDays(5),6} days " +
                  $"= {CalendarRound.CodexVenusRunDays(5) / CalendarRound.HaabDays} Haab' years exactly");
Console.WriteLine();
Console.WriteLine($"     Codex drift after 5 cycles (8 yr):  {CalendarRound.CodexDriftDays(5),8:F3} days");
Console.WriteLine($"     Codex drift after 65 cycles (104 yr):{CalendarRound.CodexDriftDays(65),8:F3} days");
Console.WriteLine($"     Haab' drift after 52 yr:            {CalendarRound.HaabDriftDays(52),8:F3} days");
Console.WriteLine();

// ── 5. Tzolk'in lookup ───────────────────────────────────────────────────────
Console.WriteLine("  5. TZOLK'IN LOOKUP     (the calendar-ring scene)");
Console.WriteLine(Rule);
DateTime[] samples = args.Length > 0
    ? args.Select(DateTime.Parse).ToArray()
    : new[] { new DateTime(2012, 12, 21), DateTime.Today };

foreach (var d in samples)
    Console.WriteLine($"     {d:yyyy-MM-dd}  ->  {Tzolkin.FromGregorian(d)}");

Console.WriteLine();
Console.WriteLine("     Pass dates as arguments to look up your own, e.g.");
Console.WriteLine("       dotnet run --project sim/src/MayaSynodic.Cli 1997-04-15");
Console.WriteLine();
