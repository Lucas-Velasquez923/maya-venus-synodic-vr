# maya-venus-synodic-vr

A VR experience that puts you inside the Maya's model of Venus — and the orbital
arithmetic that model rests on, extracted into a library you can run in ten seconds
without Unity.

```
5 Venus synodic periods  =  2919.62 days
8 Earth years            =  2922.05 days   (2.43 days apart)
13 Venus years           =  2921.11 days   (1.49 days apart)
```

Three different clocks agree to within one part in twelve hundred. That coincidence
is why the Dresden Codex tracks Venus in runs of five, why the calendar rounds close
where they do, and what this project is built to let you see.

---

## Run the math first

No Unity, no headset, no art assets. Requires only a .NET SDK (8 or newer).

```bash
dotnet test sim                              # 44 tests
dotnet run --project sim/src/MayaSynodic.Cli # the full derivation, printed
```

The CLI derives every number below from two constants — Venus's and Earth's sidereal
periods, as published by JPL. Nothing is hard-coded downstream of those two values.

You can look up your own Tzolk'in date too:

```bash
dotnet run --project sim/src/MayaSynodic.Cli 1997-04-15
```

---

## The math

### The synodic period

Angular rates subtract, so the interval between successive Venus–Earth conjunctions is

$$\frac{1}{S} = \frac{1}{T_v} - \frac{1}{T_e} \qquad\Longrightarrow\qquad S = \frac{T_v T_e}{T_e - T_v} = 583.92\ \text{days}$$

The Dresden Codex Venus table uses **584 days** — an integer, off by 0.076 days per
cycle. That integer keeps the table commensurate with the 365-day Haab', and the
codex carries correction tables to absorb the residue.

### Why the ratio is 13/8

The simulation quantises the Venus/Earth speed ratio to the nearest eighth:

```csharp
n = round(8 * T_earth / T_venus) / 8    // = 13/8 exactly
```

Rounding to eighths looks arbitrary until you expand the true ratio as a continued
fraction:

$$\frac{T_e}{T_v} = 1.625520\ldots = [1; 1, 1, 1, 2, 29, \ldots]$$

| convergent | value | relative error |
|---|---|---|
| 1/1 | 1.000000000 | 3.9 × 10⁻¹ |
| 2/1 | 2.000000000 | 2.3 × 10⁻¹ |
| 3/2 | 1.500000000 | 7.7 × 10⁻² |
| 5/3 | 1.666666667 | 2.5 × 10⁻² |
| **13/8** | **1.625000000** | **3.2 × 10⁻⁴** |
| 382/235 | 1.625531915 | 7.3 × 10⁻⁶ |

`13/8` is a convergent, and the term after it is **29** — an unusually large
partial quotient, which means the next convergent needs a denominator ~30× bigger
to improve on it. Eighths are the last cheap grid before accuracy gets expensive.
That is the whole justification for the constant, and it is checked by a test.

### What the quantisation costs, and what it buys

I expected the eighths approximation to beat the Maya's integer 584 days. It does
not, and it is not close:

| | error vs. true synodic period | drift over 5 cycles |
|---|---|---|
| Codex, 584 days | **0.076 d/cycle** | 0.38 d |
| Simulation, ratio → 13/8 | 0.486 d/cycle | **exactly 0** |

The two are optimising different objectives. The codex is fitting the *sky*, so it
minimises per-cycle error. The simulation is fitting an *animation loop* that has to
return to its starting configuration on screen, so it minimises drift over five
cycles and accepts a six-times-worse period to get exact closure. The Maya's number
is the better astronomy; the quantised one is the better animation.

### The pentagram

Between one conjunction and the next, Earth advances 215.52° — **three fifths of a
turn**, not one fifth. Stepping 3/5 of the way around a circle five times is the
straight-edge construction of a five-pointed star, which is why Venus traces a
pentagram and not a pentagon.

It does not quite close. Sorted, the five conjunction longitudes sit at 0°, 71.04°,
142.08°, 215.52°, 286.56°, giving **three gaps of 71.04° and two of 73.44°** — a
spread of 2.39°. That asymmetry is the 2.43-day residual from the table at the top,
seen from a different direction: the star precesses slowly rather than repeating.

### The calendars

The same small integers govern the calendar round, and for the same reason —
coprimality:

| quantity | arithmetic | days |
|---|---|---|
| Tzolk'in | lcm(13, 20), gcd = 1 | 260 |
| Calendar Round | lcm(260, 365), gcd = 5 | 18,980 (52 Haab' years) |
| Codex Venus run | 5 × 584 = 8 × 365 | 2,920 (8 Haab' years, exactly) |

The Tzolk'in is two counters advancing in lockstep, one mod 13 and one mod 20; since
gcd(13,20) = 1 the pair does not repeat for 260 days. That is the Chinese Remainder
Theorem made out of wood, and it is literally geared that way in the scene — two
concentric rings, 13 teeth and 20.

The factor of 5 shared by 260 and 365 is the same 5 that appears in the Venus run.
The Maya's calendrical and astronomical cycles interlock because they were chosen to.

---

## What the VR experience does

You start in a Maya temple at night. Venus fades in; looking at it brings up Earth
and the Sun, with a beam drawn between the two planets that stays lit as they move.
Clicking Venus runs one synodic cycle — the planets sweep their orbits, the beam
tracks them, and a counter reports elapsed days and years. Clicking again runs the
remaining four, and the labels tick over `Cycle 1/5` … `Cycle 5/5` as eight Earth
years pass. After the fifth, you can drive Venus by hand and watch Earth follow at
the ratio the math dictates.

Later scenes put you in front of the calendar itself: two rings, 13 numbers and 20
day signs, that spin up and land on the Tzolk'in date for a birthday you enter.

## What I built

Every `.cs` file in `unity/Assets/` is mine. The pieces worth naming:

| System | Files | What it does |
|---|---|---|
| **Synodic simulation** | `SynodicManager.cs`, `SynodicCycle.cs` | The orbit engine and the eight-phase state machine driving the whole Venus sequence. Analytic positioning from angle and radius rather than physics stepping, so the cycle is exactly reproducible and cannot drift on a slow frame. |
| **Tzolk'in conversion** | `BdayGetter.cs` | Gregorian → Tzolk'in in closed form: day offset from a fixed anchor, then mod 13 and mod 20. No table lookup, no iteration. |
| **Calendar ring rig** | `BirthdaySceneManager.cs`, `BirthdayScrollInput.cs`, `RingBuilder.cs`, `InnerRingOrbit.cs` | Drives the two geared rings to land on a computed date, with the spin-up and settle choreographed against the audio. |
| **Interaction layer** | `VenusInteraction.cs`, `VenusClickTrigger.cs`, `VenusHoverActivator.cs`, `SunClickHandler.cs`, `PressurePlate.cs` | XR Interaction Toolkit hover/select wiring, gaze activation, and the physical trigger volumes. |
| **Scene orchestration** | `SolarSystemSceneManager.cs`, `TempleIntroSequencer.cs`, `SceneFadeLoader.cs`, `FadeIn.cs` | Sequencing, fades, and transitions between the seven scenes. |
| **Mini-game** | `SnakeMoveUP.cs`, `SnakeTimer.cs`, `SnakeTriggerArea.cs`, `SnakeMiniGameManager.cs` | A timed interaction sequence in the jungle scene. |

Two design decisions worth naming:

1. **Analytic orbits over physics.** Positions are computed from
   `(angle, radius)` every frame instead of integrated. The cycle takes the same
   wall-clock time and ends in the same configuration regardless of frame rate,
   which is what makes the "five cycles = eight years" claim demonstrable rather
   than approximate.
2. **Quantising the ratio to 13/8.** See *What the quantisation costs, and what it
   buys* above.

## What is here and what is not

**Here:** the 52 C# files I wrote (51 runtime scripts plus one editor tool), the
package manifest, and `sim/` — a standalone, tested, dependency-free
reimplementation of the orbital and calendrical math.

**Not here:** the art. The Unity project depends on roughly 13 GB of licensed Asset
Store content — Maya architecture, skyboxes, planet models, particle systems, audio.
None of that is mine to redistribute, so **cloning this repo will not give you an
openable Unity project.**

There is no proprietary SDK dependency here — the manifest is stock Unity XR plus
`whisper.unity`, which is pulled in by a purchased captioning asset to generate
caption timings from voiceover audio at edit time. None of my code calls it; it is
in the manifest because the tool that uses it was in the project. The limitation
is asset licensing, not code licensing.

## Stack

Unity 6000.2.15f1 · C# · XR Interaction Toolkit 3.2.1 · OpenXR (Meta Quest) ·
URP 17.2.0 · `sim/` targets .NET 8 with xUnit

## Layout

```
sim/                     standalone math core — run this
  src/MayaSynodic/         SynodicPeriod, RationalApproximation, Tzolkin, CalendarRound
  src/MayaSynodic.Cli/     prints the full derivation
  tests/                   44 xUnit tests
unity/Assets/Scripts/    51 runtime C# files I authored
unity/Assets/Editor/     1 editor tool I authored
unity/Packages/          manifest, to show what the project depends on
docs/architecture.md     how the scene systems fit together
```

## Running this

`sim/` runs anywhere with a .NET SDK — see the top of this README.

The Unity half does not run standalone; it is source from a deployed project whose
art dependencies are licensed and not redistributable.
