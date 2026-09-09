# Architecture

How the Venus sequence is put together, and why it is built this way.

## The orbit engine

`SynodicManager` owns the simulation. On `Awake` it reads the planets' authored
positions out of the scene and converts them to polar coordinates about the Sun:

```csharp
Vector3 off = venus.transform.localPosition - _sunLocalPos;
_venusRadius = new Vector2(off.x, off.z).magnitude;
_venusAngle  = Mathf.Atan2(off.z, off.x) * Mathf.Rad2Deg;
```

From then on the angle is the state and the transform is a projection of it. Nothing
reads position back out to decide anything, so there is no feedback path where
floating-point error can accumulate into the simulation.

Angular rates come straight from the periods:

```csharp
float speedRatio = earthPeriodYears / venusPeriodYears;   // ~1.625
_synodicSeconds  = (1f / (speedRatio - 1f)) * secondsPerYear;
_omegaVenus      = 360f / (venusPeriodYears * secondsPerYear);
_omegaEarth      = 360f / (earthPeriodYears * secondsPerYear);
```

`1/(n-1)` is the synodic period only because Earth's period is normalised to 1 year
here. The general form is `T_v·T_e/(T_e − T_v)`. Both are implemented in
`sim/src/MayaSynodic/SynodicPeriod.cs`, and a test asserts they agree under the
normalisation and diverge without it — the shortcut is correct, but it is correct
for a reason worth pinning down.

### Why not a physics simulation

Rigidbodies with gravity would have been more "realistic" and much worse here:

- **Reproducibility.** The claim on screen is that five cycles is eight years. With
  an integrator that claim depends on frame rate. Analytically, the angle after `t`
  seconds is `ω·t` and the cycle ends where it began, on a Quest or in the editor.
- **Authored composition.** Orbit radii are set by where the artist put the planets,
  not by physically consistent semi-major axes. Real gravity would fight the scene.
- **Cost.** This runs on a standalone Quest. Two `sin`/`cos` per frame is free.

The trade is that this is a kinematic model of circular coplanar orbits, not an
n-body simulation. Eccentricity (Venus e = 0.0068, Earth e = 0.0167) and inclination
are ignored. For the phenomenon being shown — the commensurability of the periods —
neither matters at the resolution a viewer can perceive.

### Time-step handling

`OrbitForSeconds` clamps the final step so a cycle lands exactly on its boundary
rather than overshooting by whatever `Time.deltaTime` happened to be:

```csharp
float dt = Mathf.Min(Time.deltaTime, duration - elapsed);
```

Without the clamp, five chained cycles accumulate up to five frames of overshoot and
the "return to start" claim quietly stops being true.

## The phase machine

The Venus scene is an eight-state sequence, advanced by the player rather than by a
timer:

```
VenusFadeIn → WaitHover → FadeRest → WaitClick1
            → FirstCycle → WaitClick2 → FullCycle → Manual
```

`WaitHover`, `WaitClick1` and `WaitClick2` are the blocking states;
`VenusInteraction` calls `AdvancePhase()` and the manager decides what that means
from the current phase. Input handling never needs to know where in the sequence it
is, so adding a stage does not touch the interaction scripts.

`FirstCycle` runs one synodic period alone so the player sees what a cycle *is*
before `FullCycle` runs the remaining four and the eight-year closure lands.

`Manual` hands control over: the player drives Venus and Earth follows at
`_omegaEarth / _omegaVenus`, which keeps the ratio — and therefore the resonance —
intact under direct manipulation.

## The calendar rings

Two concentric rings, 13 teeth and 20, geared so a step advances both. Because
gcd(13, 20) = 1 the pair does not repeat for 260 days, which is the Tzolk'in.

`BdayGetter` computes the target date in closed form rather than stepping:

```csharp
int daysBetween = (int)(inputDate - referenceDate).TotalDays;
int number      = ((referenceNumber - 1 + daysBetween) % 13 + 13) % 13 + 1;
int signIndex   = ((referenceSignIndex + daysBetween) % 20 + 20) % 20;
```

The doubled `% n + n) % n` is not redundant. C# `%` follows the sign of the dividend,
so for any date before the 21 Dec 2012 anchor — which is every user's birthday — a
single `%` returns a negative index and throws on the day-sign array. There is a test
in `sim/tests/` that walks 800 days backwards specifically to hold that closed.

`BirthdaySceneManager` then animates the rings to the computed indices, adding whole
extra turns (`bigRingExtraSpins`, `smallRingExtraSpins`) so the motion reads as a
spin-up and settle rather than a snap.

## Scene graph

Seven scenes, chained by `SceneFadeLoader`:

| # | Scene | Role |
|---|---|---|
| 1 | `1Start_Scene` | Jungle opening — captions, ambient wildlife, movement tutorial |
| 2 | `2Starting_Animation` | Illustrated intro over voiceover |
| 3 | `3SynodicCycle` | The Venus/Earth sequence above |
| 4 | `4PressInteraction` | Button/pressure-plate interaction over narration |
| 5 | `5Storytelling` | Narrated sequence with a Venus trail and date UI |
| 6 | `6MeteorFall` | Narrated set piece |
| 7 | `7CalendarScene` | The Tzolk'in rings |

(`SnakeGame`, `TempleOfVenusMain` and several `LucasSolarSystem*` scenes also exist
as a mini-game and as development scratch scenes.)

State does not persist across scenes; each is self-contained and driven by its own
manager. For a linear experience with no inventory or progression this is simpler
than a persistent game-state singleton and removes a whole class of load-order bug.

## What lives in `sim/` and why

`sim/` is not a port of the Unity code — it is the same arithmetic with the engine
removed, so that:

- the claims in the README are **executable** rather than asserted;
- the numerical edge cases (negative modulo, continued-fraction termination on
  rationals) have tests that run in CI-time rather than requiring a headset;
- a reader with no Unity install can still evaluate the interesting part.

`Tzolkin.cs` is `BdayGetter.cs` with the unused `UnityEngine` import dropped and
nothing else changed, so the tested path is the shipped path.

Writing the tests changed two things I had believed. The eighths quantisation is
*less* accurate against the sky than the Maya's integer 584 days, not more; and
consecutive conjunctions are separated by three fifths of a turn, not one fifth.
Both corrections are in the README.
